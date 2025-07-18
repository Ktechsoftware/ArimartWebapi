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

    public AuthController(
        ApplicationDbContext context,
        ITokenService tokenService,
        IOTPService otpService,
        INotificationService notificationService)
    {
        _context = context;
        _tokenService = tokenService;
        _otpService = otpService;
        _notificationService = notificationService;
    }


    // 1. Send OTP
    [HttpPost("send-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
    {
        if (!IsValidMobileNumber(request?.MobileNumber))
            return BadRequest(new { message = "Invalid mobile number" });

        var otpSent = await _otpService.SendOTPAsync(request.MobileNumber!);
        return !string.IsNullOrEmpty(otpSent)
            ? Ok(new { message = "OTP sent." })
            : StatusCode(500, new { message = "Failed to send OTP." });
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

        var existingUser = await GetUserByPhoneAsync(request.Phone!);
        if (existingUser != null)
            return Conflict(new { message = "User already exists. Please login." });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
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

            // Check referral code before creating user
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
                            Urlt = "/wallet",
                            Title = "Referral Bonus 🎉",
                            Message = $"You received ₹50 for referring {user.ContactPerson}.",
                        });

                        await _notificationService.CreateNotificationAsync(new CreateNotificationDto
                        {
                            UserId = user.Id,
                            Urlt = "/wallet",
                            Title = "Welcome Reward 🎁",
                            Message = $"You received ₹50 for using a referral code!",
                        });
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
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Phone == phone);
    }

    // 6. Update User Info
    [AllowAnonymous]
    [HttpPut("update-user/{userId}")]
    public async Task<IActionResult> UpdateUser(long userId, [FromBody] UpdateUserRequest request)
    {
        var user = await _context.TblUsers.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return NotFound(new { message = "User not found." });

        // Update fields if provided
        if (!string.IsNullOrWhiteSpace(request.Name))
            user.ContactPerson = request.Name;

        if (!string.IsNullOrWhiteSpace(request.Email))
            user.Email = request.Email;

        if (!string.IsNullOrWhiteSpace(request.Address))
            user.Address = request.Address;

        if (!string.IsNullOrWhiteSpace(request.VendorName))
            user.VendorName = request.VendorName;

        try
        {
            await _context.SaveChangesAsync();

            return Ok(new
            {
                user = new
                {
                    id = user.Id,
                    name = user.ContactPerson,
                    phone = user.Phone,
                    email = user.Email,
                    type = user.UserType,
                    address = user.Address
                },
                message = "User information updated successfully."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to update user information." });
        }
    }

}


// Request model for updating user
public class UpdateUserRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? VendorName { get; set; }
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
