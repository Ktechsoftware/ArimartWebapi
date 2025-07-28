using Microsoft.AspNetCore.Mvc;
using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using ArimartEcommerceAPI.Services.Services;

namespace ArimartEcommerceAPI.API.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IFcmPushService _fcmPushService;

        public UserController(ApplicationDbContext context, IFcmPushService fcmPushService)
        {
            _context = context;
            _fcmPushService = fcmPushService;
        }


        [HttpPost("save-token")]
        public IActionResult SaveFcmToken([FromBody] FcmTokenDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FToken))
                return BadRequest("FCM token is required.");

            var userExists = _context.TblUsers.Any(u => u.Id == dto.UserId);
            if (!userExists)
                return NotFound("User not found.");

            var existing = _context.FcmDeviceTokens
                .FirstOrDefault(t => t.UserId == dto.UserId && t.DeviceType == dto.DeviceType);

            if (existing == null)
            {
                _context.FcmDeviceTokens.Add(new FcmDeviceToken
                {
                    UserId = dto.UserId,
                    Token = dto.FToken!,
                    DeviceType = dto.DeviceType ?? "unknown",
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.Token = dto.FToken!;
                existing.CreatedAt = DateTime.UtcNow;
            }

            _context.SaveChanges();
            return Ok(new { message = "FCM token saved successfully." });
        }
    }

    public class FcmTokenDto
    {
        public int UserId { get; set; }
        public string? FToken { get; set; }
        public string? DeviceType { get; set; } // optional: "android", "ios", "web"
    }
}
