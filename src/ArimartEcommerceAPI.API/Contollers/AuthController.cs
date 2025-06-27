using System.Linq;
using System.Threading.Tasks;
using ArimartEcommerceAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            return BadRequest("Mobile number is required.");

        var otp = await _otpService.SendOTPAsync(request.MobileNumber);

        if (otp != null)
        {
            return Ok("OTP sent successfully.");
        }
        else
        {
            return StatusCode(500, "Failed to send OTP.");
        }
    }

    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.MobileNumber) || string.IsNullOrWhiteSpace(request.OTP))
            return BadRequest("Mobile number and OTP are required.");

        var isValid = await _otpService.VerifyOTPAsync(request.MobileNumber, request.OTP);

        if (!isValid)
            return Unauthorized("Invalid or expired OTP.");

        var user = _context.Users.FirstOrDefault(u => u.UserContact == request.MobileNumber);
        if (user != null)
        {
            var token = _tokenService?.CreateToken(user);
            return Ok(new { token });
        }

        return NotFound("User not found. Continue to registration.");
    }
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
