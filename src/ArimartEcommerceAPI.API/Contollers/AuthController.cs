using System.Linq;
using System.Threading.Tasks;
using ArimartEcommerceAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ArimartEcommerceAPI.Infrastructure.Data.Models;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IOTPService _otpService;

    public AuthController(ApplicationDbContext context, ITokenService tokenService, IOTPService otpService)
    {
        _context = context;
        _tokenService = tokenService;
        _otpService = otpService;
    }

   [HttpPost("send-otp")]
[AllowAnonymous]
public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
{
    Console.WriteLine($"[DEBUG] SendOtp called with: {request?.MobileNumber}");
    Console.WriteLine($"[DEBUG] OTP Service Instance: {_otpService.GetHashCode()}");
    
    if (request == null || string.IsNullOrWhiteSpace(request.MobileNumber))
        return BadRequest(new { message = "Mobile number is required." });

    if (!IsValidMobileNumber(request.MobileNumber))
        return BadRequest(new { message = "Invalid mobile number format." });

    try
    {
        var otp = await _otpService.SendOTPAsync(request.MobileNumber);
        
        // Add debug check
        if (_otpService is MockOTPService mockService)
        {
            var currentOtps = mockService.GetCurrentOtps();
            Console.WriteLine($"[DEBUG] OTPs in store after send: {currentOtps.Count}");
        }
        
        if (otp != null)
        {
            return Ok(new { message = "OTP sent successfully." });
        }
        else
        {
            return StatusCode(500, new { message = "Failed to send OTP." });
        }
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "An error occurred while sending OTP.", error = ex.Message, details = ex.StackTrace });
    }
}

    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        Console.WriteLine("[DEBUG] === VERIFY OTP REQUEST RECEIVED ===");
        Console.WriteLine($"[DEBUG] Request: {System.Text.Json.JsonSerializer.Serialize(request)}");
        Console.WriteLine($"[DEBUG] Headers: {string.Join(", ", Request.Headers.Select(h => $"{h.Key}={h.Value}"))}");

        if (request == null || string.IsNullOrWhiteSpace(request.MobileNumber) || string.IsNullOrWhiteSpace(request.OTP))
        {
            Console.WriteLine("[DEBUG] Invalid request data");
            return BadRequest(new { message = "Mobile number and OTP are required." });
        }

        try
        {
            Console.WriteLine($"[DEBUG] Mobile: {request.MobileNumber}, OTP: {request.OTP}");
            Console.WriteLine($"[DEBUG] OTP Service Type: {_otpService.GetType().Name}");

            var isValid = await _otpService.VerifyOTPAsync(request.MobileNumber, request.OTP);

            Console.WriteLine($"[DEBUG] OTP Verification Result: {isValid}");

            if (!isValid)
            {
                Console.WriteLine("[DEBUG] OTP verification failed - returning 401");
                return Unauthorized(new
                {
                    message = "Invalid or expired OTP.",
                    debug = new
                    {
                        serviceType = _otpService.GetType().Name,
                        mobileNumber = request.MobileNumber,
                        receivedOtp = request.OTP,
                        timestamp = DateTime.Now
                    }
                });
            }

            Console.WriteLine("[DEBUG] OTP verified successfully - checking user");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserContact == request.MobileNumber);

            if (user != null)
            {
                Console.WriteLine($"[DEBUG] User found: {user.FullName}");
                var token = _tokenService?.CreateToken(user);

                var userData = new UserCookieData
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    UserContact = user.UserContact,
                    Email = user.UserEmail
                };

                return Ok(new
                {
                    token,
                    user = new
                    {
                        id = user.UserId,
                        fullName = user.FullName,
                        userContact = user.UserContact,
                        email = user.UserEmail
                    },
                    message = "Login successful."
                });
            }

            Console.WriteLine("[DEBUG] User not found - requires registration");
            return Ok(new
            {
                message = "User not found. Please complete registration.",
                requiresRegistration = true,
                mobileNumber = request.MobileNumber
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Exception in VerifyOtp: {ex}");
            return StatusCode(500, new { message = "An error occurred during verification.", error = ex.Message });
        }
    }

    [HttpPost("register-user")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterUser([FromBody] RegisterUserRequest request)
    {
        Console.WriteLine("[DEBUG] === REGISTER USER REQUEST RECEIVED ===");
        Console.WriteLine($"[DEBUG] Request: {System.Text.Json.JsonSerializer.Serialize(request)}");

        if (request == null || string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.UserEmail) || string.IsNullOrWhiteSpace(request.MobileNumber))
        {
            Console.WriteLine("[DEBUG] Invalid registration request data");
            return BadRequest(new { message = "Full name, email, and mobile number are required." });
        }

        try
        {
            // Check if user already exists with this mobile number
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserContact == request.MobileNumber);

            if (existingUser != null)
            {
                Console.WriteLine("[DEBUG] User already exists with this mobile number");
                return BadRequest(new { message = "User already exists with this mobile number." });
            }

            // Check if email is already in use
            var existingEmailUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserEmail == request.UserEmail);

            if (existingEmailUser != null)
            {
                Console.WriteLine("[DEBUG] Email already in use");
                return BadRequest(new { message = "Email is already in use." });
            }

            // Create new user (assuming you have a User entity)
            var newUser = new User
            {
                FullName = request.FullName.Trim(),
                UserContact = request.MobileNumber,
                UserEmail = request.UserEmail.Trim().ToLower()
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            Console.WriteLine($"[DEBUG] User registered successfully with ID: {newUser.UserId}");

            // Generate token for the new user
            var token = _tokenService?.CreateToken(newUser);

            return Ok(new
            {
                token,
                user = new
                {
                    id = newUser.UserId,
                    fullName = newUser.FullName,
                    userContact = newUser.UserContact,
                    email = newUser.UserEmail
                },
                message = "User registered successfully."
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Exception in RegisterUser: {ex}");
            return StatusCode(500, new { message = "An error occurred during registration.", error = ex.Message });
        }
    }

    [HttpGet("debug/otp-store")]
    [AllowAnonymous]
    public IActionResult GetOtpStore()
    {
        if (_otpService is MockOTPService mockService)
        {
            var otps = mockService.GetCurrentOtps();
            return Ok(new
            {
                message = "Current OTP store",
                otps = otps.ToDictionary(kvp => kvp.Key, kvp => new {
                    otp = kvp.Value.Otp,
                    createdAt = kvp.Value.CreatedAt,
                    expiresAt = kvp.Value.ExpiresAt,
                    isExpired = DateTime.Now > kvp.Value.ExpiresAt
                })
            });
        }
        return Ok(new { message = "Not using MockOTPService" });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return Ok(new { message = "Logged out successfully." });
    }

    [HttpGet("user-info/{userId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserInfoById(int userId)
        {
        try
        {
            // Get user from database
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return Unauthorized(new { message = "User not found." });
            }

            return Ok(new
            {
                userId = user.UserId,
                fullName = user.FullName,
                userContact = user.UserContact,
                email = user.UserEmail
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving user information." });
        }
    }

    private bool IsValidMobileNumber(string mobileNumber)
    {
        if (string.IsNullOrWhiteSpace(mobileNumber))
            return false;
        var cleanNumber = mobileNumber.Replace(" ", "").Replace("-", "").Replace("+", "");
        return cleanNumber.Length == 10 &&
               cleanNumber.All(char.IsDigit) &&
               "6789".Contains(cleanNumber[0]);
    }
}

// Request DTOs
public class SendOtpRequest
{
    public string? MobileNumber { get; set; }
}

public class VerifyOtpRequest
{
    public string? MobileNumber { get; set; }
    public string? OTP { get; set; }
}

// Cookie data model
public class UserCookieData
{
    public long UserId { get; set; }
    public string? FullName { get; set; }
    public string? UserContact { get; set; }
    public string? Email { get; set; }
}

public class RegisterUserRequest
{
    public string? FullName { get; set; }
    public string? UserEmail { get; set; }
    public string? MobileNumber { get; set; }
}