using System;
using System.Linq;
using System.Threading.Tasks;
using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
                name = user.VendorName ?? user.ContactPerson,
                phone = user.Phone,
                email = user.Email,
                type = user.UserType
            },
            message = "Login successful."
        });
    }

    // 3. Register New User
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!IsValidMobileNumber(request?.Phone) || string.IsNullOrWhiteSpace(request?.Name))
            return BadRequest(new { message = "Name and valid mobile number are required." });

        var existingUser = await GetUserByPhoneAsync(request.Phone!);
        if (existingUser != null)
            return Conflict(new { message = "User already exists. Please login." });

        var user = new TblUser
        {
            Phone = request.Phone,
            ContactPerson = request.Name,
            Email = request.Email,
            UserType = "User",
        };

        _context.TblUsers.Add(user);
        await _context.SaveChangesAsync();

        var token = _tokenService.CreateToken(user);

        return Ok(new
        {
            token,
            user = new
            {
                id = user.Id,
                name = user.VendorName ?? user.ContactPerson,
                phone = user.Phone,
                email = user.Email,
                type = user.UserType
            },
            message = "Registration successful."
        });
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
            name = user.VendorName ?? user.ContactPerson,
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
                    name = user.VendorName ?? user.ContactPerson,
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
}
