using System;
using System.Linq;
using System.Threading.Tasks;
using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ArimartEcommerceAPI.Services.Services; // for INotificationService
using ArimartEcommerceAPI.Infrastructure.Data.DTO; // for CreateNotificationDto


[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IOTPService _otpService;
    private readonly INotificationService _notificationService;
    private readonly IFcmPushService _fcmPushService;

    public AuthController(
        ApplicationDbContext context,
        ITokenService tokenService,
        IOTPService otpService,
        INotificationService notificationService,
        IFcmPushService fcmPushService)
    {
        _context = context;
        _tokenService = tokenService;
        _otpService = otpService;
        _notificationService = notificationService;
        _fcmPushService = fcmPushService;
    }


    // 1. Send OTP
    [HttpPost("send-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
    {
        if (!IsValidMobileNumber(request?.MobileNumber))
            return BadRequest(new { message = "Invalid mobile number" });

        var result = await _otpService.SendOTPAsync(request.MobileNumber!);

        if (result.Success)
        {
            return Ok(new { message = "OTP sent successfully." });
        }
        else
        {
            return StatusCode(500, new
            {
                message = "Failed to send OTP.",
                error = result.ErrorMessage ?? "Unknown error"
            });
        }
    }


    // 2. Login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] VerifyOtpRequest request)
    {
        if (!IsValidMobileNumber(request?.MobileNumber) || string.IsNullOrWhiteSpace(request?.OTP))
            return BadRequest(new { message = "Mobile number and OTP are required." });

        var isValid = await _otpService.VerifyOTPAsync(request.MobileNumber!, request.OTP!);
        if (!isValid)
            return Unauthorized(new { message = "Invalid or expired OTP." });

        var user = await GetUserByPhoneAsync(request.MobileNumber!);

        if (user == null)
        {
            return NotFound(new
            {
                message = "User not found. Please register.",
                requiresRegistration = true
            });
        }

        var token = _tokenService.CreateToken(user);

        return Ok(new
        {
            token,
            user = new
            {
                id = user.Id,
                name = user.ContactPerson,
                phone = user.Phone,
                email = user.Email,
                type = user.UserType
            },
            message = "Login successful."
        });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!IsValidMobileNumber(request?.Phone) || string.IsNullOrWhiteSpace(request?.Name))
            return BadRequest(new { message = "Name and valid mobile number are required." });

        // Check if user already exists in TblUsers
        var existingTblUser = await GetUserByPhoneAsync(request.Phone!);
        if (existingTblUser != null)
            return Conflict(new { message = "User already exists. Please login." });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Create user in TblUsers (your main user table)
            var user = new TblUser
            {
                Phone = request.Phone,
                ContactPerson = request.Name,
                Email = request.Email,
                UserType = "User",
                RefferalCode = request.RefferalCode
            };

            _context.TblUsers.Add(user);
            await _context.SaveChangesAsync();

            // Clean up temporary OTP user from Users table if it exists
            var tempOtpUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserContact == request.Phone && u.UserType == "OTP_TEMP");
            if (tempOtpUser != null)
            {
                _context.Users.Remove(tempOtpUser);
            }

            // Rest of your referral code logic remains the same...
            if (!string.IsNullOrWhiteSpace(request.RefferalCode))
            {
                var inviter = await _context.VwUserrefercodes
                    .FirstOrDefaultAsync(v => v.Refercode == request.RefferalCode);

                if (inviter == null)
                {
                    return BadRequest(new { message = "Invalid referral code." });
                }

                if (inviter != null)
                    {
                        if (inviter.Id == user.Id)
                        {
                            return BadRequest(new { message = "You cannot use your own referral code." });
                        }

                        var referral = new TblUserReferral
                        {
                            InviterUserId = inviter.Id,
                            NewUserId = user.Id,
                            UsedReferralCode = request.RefferalCode!,
                            RewardAmount = 100,
                            IsRewarded = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.TblUserReferrals.Add(referral);

                        _context.TblWallets.AddRange(new[]
                        {
                        new TblWallet
                        {
                            Userid = inviter.Id,
                            ReferAmount = 50,
                            AddedDate = DateTime.UtcNow,
                            IsActive = true,
                            IsDeleted = false,
                            Acctt = true
                        },
                        new TblWallet
                        {
                            Userid = user.Id,
                            ReferAmount = 50,
                            AddedDate = DateTime.UtcNow,
                            IsActive = true,
                            IsDeleted = false,
                            Acctt = true
                        }
                    });

                    await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                        {
                            UserId = inviter.Id,
                            Urlt = "/home/wallet",
                            Title = "Referral Bonus 🎉",
                            Message = $"You received ₹50 for referring {user.ContactPerson}.",
                        });

                        await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                        {
                            UserId = user.Id,
                            Urlt = "/home/wallet",
                            Title = "Welcome Reward 🎁",
                            Message = $"You received ₹50 for using a referral code!",
                        });
                    // ✅ Send FCM to new user
                    var newUserToken = await _context.FcmDeviceTokens
                        .Where(t => t.UserId == user.Id)
                        .Select(t => t.Token)
                        .FirstOrDefaultAsync();

                    if (!string.IsNullOrEmpty(newUserToken))
                    {
                        await _fcmPushService.SendNotificationAsync(
                            newUserToken,
                            "Welcome to Arimart 🎉",
                            "Thanks for using a referral code! ₹50 has been added to your wallet."
                        );
                    }

                    // ✅ Send FCM to inviter
                    var inviterToken = await _context.FcmDeviceTokens
                        .Where(t => t.UserId == inviter.Id)
                        .Select(t => t.Token)
                        .FirstOrDefaultAsync();

                    if (!string.IsNullOrEmpty(inviterToken))
                    {
                        await _fcmPushService.SendNotificationAsync(
                            inviterToken,
                            "Referral Bonus 💰",
                            $"You've received ₹50 for inviting {user.ContactPerson}!"
                        );
                    }



                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var token = _tokenService.CreateToken(user);

            return Ok(new
            {
                token,
                user = new
                {
                    id = user.Id,
                    name = user.ContactPerson,
                    phone = user.Phone,
                    email = user.Email,
                    type = user.UserType,
                    address = user.Address,
                    refferalCode = user.RefferalCode
                },
                message = "Registration successful."
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { message = "Registration failed.", error = ex.Message });
        }
    }


    // 4. Get User Info
    [HttpGet("user-info/{userId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserInfoById(long userId)
    {
        var user = await _context.TblUsers.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return NotFound(new { message = "User not found." });

        return Ok(new
        {
            id = user.Id,
            name = user.ContactPerson,
            phone = user.Phone,
            email = user.Email,
            type = user.UserType,
            adddress = user.Address

        });
    }

    // 5. Logout (JWT stateless)
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return Ok(new { message = "Logged out successfully." });
    }

    // ───── HELPER METHODS ─────────────────────────

    private static bool IsValidMobileNumber(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return false;
        var p = phone.Trim().Replace(" ", "").Replace("-", "").Replace("+", "");
        return p.Length == 10 && p.All(char.IsDigit) && "6789".Contains(p[0]);
    }

    private async Task<TblUser?> GetUserByPhoneAsync(string phone)
    {
        return await _context.TblUsers
            .FirstOrDefaultAsync(u => u.Phone == phone);
    }

    private async Task<TblDeliveryuser?> GetDeliveryUserByPhoneAsync(string phone)
    {
        return await _context.TblDeliveryusers
            .FirstOrDefaultAsync(u => u.Phone == phone);
    }

    // 6. Update User Info
    [HttpPut("update-user/{userId}")]
    public async Task<IActionResult> UpdateUser(long userId, [FromForm] UpdateUserRequest request)
    {
        var user = await _context.TblUsers.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return NotFound(new { message = "User not found." });

        // Update basic fields
        if (!string.IsNullOrWhiteSpace(request.Name))
            user.ContactPerson = request.Name;
        if (!string.IsNullOrWhiteSpace(request.Email))
            user.Email = request.Email;
        if (!string.IsNullOrWhiteSpace(request.Gender))
            user.Gender = request.Gender;

        // Handle image logic
        if (request.Image != null && request.Image.Length > 0)
        {
            // Custom image upload
            var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload");
            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            var fileName = Guid.NewGuid() + Path.GetExtension(request.Image.FileName);
            var filePath = Path.Combine(uploadFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.Image.CopyToAsync(stream);
            }

            user.Image = fileName;
            user.UseDefaultAvatar = false;
        }
        else if (request.UseGenderAvatar && !string.IsNullOrWhiteSpace(request.Gender))
        {
            // Use gender-based default avatar
            user.Image = null; // Clear custom image
            user.UseDefaultAvatar = true;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            user = new
            {
                id = user.Id,
                name = user.ContactPerson,
                phone = user.Phone,
                email = user.Email,
                gender = user.Gender,
                imageUrl = GetUserImageUrl(user),
                useDefaultAvatar = user.UseDefaultAvatar
            },
            message = "User updated successfully."
        });
    }

    private string GetUserImageUrl(TblUser user)
    {
        if (user.UseDefaultAvatar == true)
        {
            // Return gender-based default avatar URLs
            return user.Gender?.ToLower() switch
            {
                "male" => "https://thumbs.dreamstime.com/b/cool-stylish-male-avatar-sunglasses-vector-illustration-profiles-branding-image-features-wearing-characterized-346328055.jpg",
                "female" => "https://img.freepik.com/premium-vector/cute-woman-avatar-profile-vector-illustration_1058532-14592.jpg",
                _ => null
            };
        }

        return string.IsNullOrEmpty(user.Image) ? null : $"/Upload/{user.Image}";
    }



    /// ============================================= Auth for delivery users =============================================
    // Delivery User Registration
    [HttpPost("delivery-user/register")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterDeliveryUser([FromBody] DeliveryUserRegisterRequest request)
    {
        if (!IsValidMobileNumber(request?.Phone) || string.IsNullOrWhiteSpace(request?.Name))
            return BadRequest(new { message = "Name and valid mobile number are required." });

        // Check if user already exists in TblDeliveryusers
        var existingDeliveryUser = await _context.TblDeliveryusers
            .FirstOrDefaultAsync(u => u.Phone == request.Phone && u.IsDeleted == false);

        if (existingDeliveryUser != null)
            return Conflict(new { message = "User already exists. Please login." });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            DateTime? dateOfBirth = null;
            if (!string.IsNullOrWhiteSpace(request.DateOfBirth))
            {
                if (DateTime.TryParse(request.DateOfBirth, out var parsedDate))
                {
                    dateOfBirth = parsedDate;
                }
            }

            var deliveryuser = new TblDeliveryuser
            {
                // Basic Information
                Phone = request.Phone,
                ContactPerson = request.Name,
                Email = request.Email,
                UserType = "deliveryuser",

                // Personal Information from form
                VendorName = request.VendorName,
                Address = request.Address,
                City = request.City,
                State = request.State,
                PostalCode = request.PostalCode,
                BankName = request.BankName,
                AccountNo = request.AccountNo,
                Ifsccode = request.Ifsccode,
                Refid = request.Refid,
                FatherName = request.FatherName,
                Dob = dateOfBirth,
                WhatsappNo = request.WhatsappNumber,
                AlterMobile = request.SecondaryMobile,
                BloodGroup = request.BloodGroup,
                LanguageKnown = request.Language,

                // Registration Status Tracking
                CurrentStep = 2, // Move to documents step after personal info
                PersonalInfoComplete = true,
                DocumentsUploaded = false,
                ProfileComplete = false,
                RegistrationStatus = "PENDING",
                IsActive = true,
                Rating = 0,
                TotalDeliveries = 0,
                Country = "India" // Default country
            };

            _context.TblDeliveryusers.Add(deliveryuser);
            await _context.SaveChangesAsync();

            // Clean up temporary OTP user from Users table if it exists
            var tempOtpUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserContact == request.Phone && u.UserType == "OTP_TEMP");

            if (tempOtpUser != null)
            {
                _context.Users.Remove(tempOtpUser);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            var token = _tokenService.CreateToken(deliveryuser);

            return Ok(new
            {
                token,
                deliveryuser = new
                {
                    id = deliveryuser.Id,
                    name = deliveryuser.ContactPerson,
                    phone = deliveryuser.Phone,
                    email = deliveryuser.Email,
                    type = "deliveryuser",
                    address = deliveryuser.Address,
                    city = deliveryuser.City,
                    state = deliveryuser.State,
                    fatherName = deliveryuser.FatherName,
                    dateOfBirth = deliveryuser.Dob?.ToString("yyyy-MM-dd"),
                    whatsappNumber = deliveryuser.WhatsappNo,
                    secondaryMobile = deliveryuser.AlterMobile,
                    bloodGroup = deliveryuser.BloodGroup,
                    language = deliveryuser.LanguageKnown,
                    refferalCode = deliveryuser.Refid,
                    // Registration status
                    currentStep = deliveryuser.CurrentStep,
                    personalInfoComplete = deliveryuser.PersonalInfoComplete,
                    documentsUploaded = deliveryuser.DocumentsUploaded,
                    profileComplete = deliveryuser.ProfileComplete,
                    vehicledetail = deliveryuser.Vehicledetail,
                    bankcomplete = deliveryuser.Bankdetail,
                    emergencycomplete = deliveryuser.Emergencydetail,
                    registrationStatus = deliveryuser.RegistrationStatus
                },
                message = "Personal information saved successfully. Please proceed to upload documents."
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { message = "Registration failed.", error = ex.Message });
        }
    }
    // Delivery User Login  
    [HttpPost("delivery-user/login")]
    [AllowAnonymous]
    public async Task<IActionResult> DeliveryUserLogin([FromBody] VerifyOtpRequest request)
    {
        if (!IsValidMobileNumber(request?.MobileNumber) || string.IsNullOrWhiteSpace(request?.OTP))
            return BadRequest(new { message = "Mobile number and OTP are required." });

        var isValid = await _otpService.VerifyOTPAsync(request.MobileNumber!, request.OTP!);
        if (!isValid)
            return Unauthorized(new { message = "Invalid or expired OTP." });

        var deliveryuser = await GetDeliveryUserByPhoneAsync(request.MobileNumber!);

        if (deliveryuser == null)
        {
            return NotFound(new
            {
                message = "User not found. Please register.",
                requiresRegistration = true
            });
        }

        var token = _tokenService.CreateToken(deliveryuser);

        return Ok(new
        {
            token,
            user = new
            {
                id = deliveryuser.Id,
                name = deliveryuser.ContactPerson,
                phone = deliveryuser.Phone,
                email = deliveryuser.Email,
                type = "deliveryuser",
                address = deliveryuser.Address,
                city = deliveryuser.City,
                state = deliveryuser.State,
                fatherName = deliveryuser.FatherName,
                dateOfBirth = deliveryuser.Dob?.ToString("yyyy-MM-dd"),
                whatsappNumber = deliveryuser.WhatsappNo,
                secondaryMobile = deliveryuser.AlterMobile,
                bloodGroup = deliveryuser.BloodGroup,
                language = deliveryuser.LanguageKnown,
                refferalCode = deliveryuser.Refid,
                // Registration status
                currentStep = deliveryuser.CurrentStep,
                personalInfoComplete = deliveryuser.PersonalInfoComplete,
                documentsUploaded = deliveryuser.DocumentsUploaded,
                profileComplete = deliveryuser.ProfileComplete,
                vehicledetail = deliveryuser.Vehicledetail,
                bankcomplete = deliveryuser.Bankdetail,
                emergencycomplete = deliveryuser.Emergencydetail,
                registrationStatus = deliveryuser.RegistrationStatus
            },
            message = "Login successful."
        });
    }

    // Get Delivery User Info
    [HttpGet("delivery-user/user-info/{userId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDeliveryUserInfo(long userId)
    {
        var deliveryuser = await _context.TblDeliveryusers
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsDeleted == false);

        if (deliveryuser == null)
            return NotFound(new { message = "User not found." });

        return Ok(new
        {
            id = deliveryuser.Id,
            name = deliveryuser.ContactPerson,
            phone = deliveryuser.Phone,
            email = deliveryuser.Email,
            type = deliveryuser.UserType,
            address = deliveryuser.Address, // Fixed typo: was "adddress"
            city = deliveryuser.City,
            state = deliveryuser.State,
            fatherName = deliveryuser.FatherName,
            dateOfBirth = deliveryuser.Dob?.ToString("yyyy-MM-dd"),
            whatsappNumber = deliveryuser.WhatsappNo,
            secondaryMobile = deliveryuser.AlterMobile,
            bloodGroup = deliveryuser.BloodGroup,
            language = deliveryuser.LanguageKnown,
            refferalCode = deliveryuser.Refid,

            // Registration status fields
            currentStep = deliveryuser.CurrentStep,
            personalInfoComplete = deliveryuser.PersonalInfoComplete,
            documentsUploaded = deliveryuser.DocumentsUploaded,
            profileComplete = deliveryuser.ProfileComplete,
            registrationStatus = deliveryuser.RegistrationStatus
        });
    }

    // Update Delivery User - Enhanced to handle personal info fields
    [HttpPut("delivery-user/update/{userId}")]
    [AllowAnonymous]
    public async Task<IActionResult> UpdateDeliveryUser(long userId, [FromBody] DeliveryUserRegisterRequest request)
    {
        var deliveryuser = await _context.TblDeliveryusers
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsDeleted == false);

        if (deliveryuser == null)
            return NotFound(new { message = "User not found." });

        // Update fields if provided
        if (!string.IsNullOrWhiteSpace(request.Name))
            deliveryuser.ContactPerson = request.Name;

        if (!string.IsNullOrWhiteSpace(request.Email))
            deliveryuser.Email = request.Email;

        if (!string.IsNullOrWhiteSpace(request.Address))
            deliveryuser.Address = request.Address;

        if (!string.IsNullOrWhiteSpace(request.City))
            deliveryuser.City = request.City;

        if (!string.IsNullOrWhiteSpace(request.AccountHolderName))
            deliveryuser.AccountHolderName = request.AccountHolderName;

        if (!string.IsNullOrWhiteSpace(request.BranchName))
            deliveryuser.BranchName = request.BranchName;

        if (!string.IsNullOrWhiteSpace(request.AccountNo))
            deliveryuser.AccountNo = request.AccountNo;

        if (!string.IsNullOrWhiteSpace(request.Ifsccode))
            deliveryuser.Ifsccode = request.Ifsccode;

        if (!string.IsNullOrWhiteSpace(request.AccountType))
            deliveryuser.AccountType = request.AccountType;

        if (!string.IsNullOrWhiteSpace(request.UpiId))
            deliveryuser.UpiId = request.UpiId;

        if (!string.IsNullOrWhiteSpace(request.State))
            deliveryuser.State = request.State;

        if (!string.IsNullOrWhiteSpace(request.VendorName))
            deliveryuser.VendorName = request.VendorName;

        // Update personal information fields
        if (!string.IsNullOrWhiteSpace(request.FatherName))
            deliveryuser.FatherName = request.FatherName;

        if (!string.IsNullOrWhiteSpace(request.WhatsappNumber))
            deliveryuser.WhatsappNo = request.WhatsappNumber;

        if (!string.IsNullOrWhiteSpace(request.SecondaryMobile))
            deliveryuser.AlterMobile = request.SecondaryMobile;

        if (!string.IsNullOrWhiteSpace(request.BloodGroup))
            deliveryuser.BloodGroup = request.BloodGroup;

        if (!string.IsNullOrWhiteSpace(request.Language))
            deliveryuser.LanguageKnown = request.Language;

        if (!string.IsNullOrWhiteSpace(request.DateOfBirth) && DateTime.TryParse(request.DateOfBirth, out var dob))
            deliveryuser.Dob = dob;
        if (!string.IsNullOrWhiteSpace(request.BankName))
            deliveryuser.BankName = request.BankName;

        if (!string.IsNullOrWhiteSpace(request.AccountNo))
            deliveryuser.AccountNo = request.AccountNo;

        if (!string.IsNullOrWhiteSpace(request.Ifsccode))
            deliveryuser.Ifsccode = request.Ifsccode;

        deliveryuser.ModifiedDate = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();

            return Ok(new
            {
                user = new
                {
                    id = deliveryuser.Id,
                    name = deliveryuser.ContactPerson,
                    phone = deliveryuser.Phone,
                    email = deliveryuser.Email,
                    type = "deliveryuser",
                    address = deliveryuser.Address,
                    city = deliveryuser.City,
                    state = deliveryuser.State,
                    fatherName = deliveryuser.FatherName,
                    dateOfBirth = deliveryuser.Dob?.ToString("yyyy-MM-dd"),
                    whatsappNumber = deliveryuser.WhatsappNo,
                    secondaryMobile = deliveryuser.AlterMobile,
                    bloodGroup = deliveryuser.BloodGroup,
                    language = deliveryuser.LanguageKnown,
                    currentStep = deliveryuser.CurrentStep,
                    personalInfoComplete = deliveryuser.PersonalInfoComplete,
                    documentsUploaded = deliveryuser.DocumentsUploaded,
                    profileComplete = deliveryuser.ProfileComplete,
                    registrationStatus = deliveryuser.RegistrationStatus
                },
                message = "User information updated successfully."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to update user information.", error = ex.Message });
        }
    }

    // Get Registration Status
    [HttpGet("delivery-user/registration-status/{userId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRegistrationStatus(long userId)
    {
        var deliveryUser = await _context.TblDeliveryusers
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (deliveryUser == null)
            return NotFound(new { message = "Delivery user not found." });

        return Ok(new
        {
            userId = deliveryUser.Id,
            registrationStatus = deliveryUser.RegistrationStatus,
            isApproved = deliveryUser.RegistrationStatus == "APPROVED",
            isRejected = deliveryUser.RegistrationStatus == "REJECTED",
            rejectRemark = deliveryUser.RejectRemark,
            currentStep = deliveryUser.CurrentStep,
            vehicledetail = deliveryUser.Vehicledetail,
            bankcomplete = deliveryUser.Bankdetail,
            emergencycomplete = deliveryUser.Emergencydetail,
            personalInfoComplete = deliveryUser.PersonalInfoComplete,
            documentsUploaded = deliveryUser.DocumentsUploaded,
            profileComplete = deliveryUser.ProfileComplete
        });
    }



}

public class DeliveryUserRegisterRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? VendorName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public String? DateOfBirth { get; set; }
    public string? CompanyName { get; set; }
    public string? FatherName { get; set; }
    public string? WhatsappNumber { get; set; }
    public string? Language { get; set; }
    public string? BloodGroup { get; set; }
    public string? SecondaryMobile { get; set; }
    public int? BusinessCategory { get; set; }
    public string? BusinessLocation { get; set; }
    public string? AccountHolderName { get; set; }
    public string? BankName { get; set; }
    public string? UpiId { get; set; }
    public string? AccountType { get; set; }
    public string? BranchName { get; set; }
    public string? AccountNo { get; set; }
    public string? Ifsccode { get; set; }
    public long? Refid { get; set; }
}

public class UpdateDeliveryUserRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? VendorName { get; set; }
    public string? City { get; set; }
    // Other delivery user fields
}

// Request model for updating user
public class UpdateUserRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? VendorName { get; set; }
    public IFormFile? Image { get; set; }
    public string? Gender { get; set; }
    public bool UseGenderAvatar { get; set; } = false;
}

public class SendOtpRequest
{
    public string? MobileNumber { get; set; }
}

public class VerifyOtpRequest
{
    public string? MobileNumber { get; set; }
    public string? OTP { get; set; }
}

public class RegisterRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? RefferalCode { get; set; } = null;
}
