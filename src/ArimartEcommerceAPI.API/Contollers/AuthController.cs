using System.Linq;
using System.Threading.Tasks;
using ArimartEcommerceAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
        if (request == null || string.IsNullOrWhiteSpace(request.MobileNumber))
            return BadRequest(new { message = "Mobile number is required." });

        // Validate mobile number format
        if (!IsValidMobileNumber(request.MobileNumber))
            return BadRequest(new { message = "Invalid mobile number format." });

        try
        {
            var otp = await _otpService.SendOTPAsync(request.MobileNumber);
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
            // Log the exception
            return StatusCode(500, new { message = "An error occurred while sending OTP.", error = ex.Message, details = ex.StackTrace });
        }
    }

    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.MobileNumber) || string.IsNullOrWhiteSpace(request.OTP))
            return BadRequest(new { message = "Mobile number and OTP are required." });

        try
        {
            var isValid = await _otpService.VerifyOTPAsync(request.MobileNumber, request.OTP);
            if (!isValid)
                return Unauthorized(new { message = "Invalid or expired OTP." });

            // Use async query for better performance
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserContact == request.MobileNumber);

            if (user != null)
            {
                var token = _tokenService?.CreateToken(user);

                // Create user data for cookie
                var userData = new UserCookieData
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    UserContact = user.UserContact,
                    Email = user.UserEmail
                };

                // Set secure cookie
                SetUserDataCookie(userData);

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

            return Ok(new
            {
                message = "User not found. Please complete registration.",
                requiresRegistration = true
            });
        }
        catch (Exception ex)
        {
            // Log the exception
            return StatusCode(500, new { message = "An error occurred during verification." });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        // Clear the user data cookie
        ClearUserDataCookie();
        return Ok(new { message = "Logged out successfully." });
    }

    [HttpGet("user-info")]
    [Authorize]
    public async Task<IActionResult> GetUserInfo()
    {
        try
        {
            // Get user data from cookie
            var userData = GetUserDataFromCookie();
            if (userData == null)
            {
                return Unauthorized(new { message = "User session not found." });
            }

            // Optionally verify user still exists in database
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userData.UserId);

            if (user == null)
            {
                ClearUserDataCookie();
                return Unauthorized(new { message = "User not found." });
            }

            return Ok(new
            {
                userId = userData.UserId,
                fullName = userData.FullName,
                userContact = userData.UserContact,
                email = userData.Email
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving user information." });
        }
    }

    // Helper methods for cookie management
    private void SetUserDataCookie(UserCookieData userData)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true, // Prevents XSS attacks
            Secure = true,   // Only send over HTTPS
            SameSite = SameSiteMode.Strict, // CSRF protection
            Expires = DateTimeOffset.UtcNow.AddDays(30) // 30 days expiration
        };

        var userDataJson = JsonSerializer.Serialize(userData);
        Response.Cookies.Append("arimartuserdata", userDataJson, cookieOptions);
    }

    private UserCookieData? GetUserDataFromCookie()
    {
        if (Request.Cookies.TryGetValue("arimartuserdata", out var cookieValue))
        {
            try
            {
                return JsonSerializer.Deserialize<UserCookieData>(cookieValue);
            }
            catch (JsonException)
            {
                // Invalid JSON in cookie, clear it
                ClearUserDataCookie();
                return null;
            }
        }
        return null;
    }

    private void ClearUserDataCookie()
    {
        Response.Cookies.Delete("arimartuserdata");
    }

    private bool IsValidMobileNumber(string mobileNumber)
    {
        // Add your mobile number validation logic here
        // Example for Indian mobile numbers (10 digits starting with 6-9)
        if (string.IsNullOrWhiteSpace(mobileNumber))
            return false;

        // Remove any spaces or special characters
        var cleanNumber = mobileNumber.Replace(" ", "").Replace("-", "").Replace("+", "");

        // Check if it's a valid Indian mobile number (example)
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
    public int UserId { get; set; }
    public string? FullName { get; set; }
    public string? UserContact { get; set; }
    public string? Email { get; set; }
}