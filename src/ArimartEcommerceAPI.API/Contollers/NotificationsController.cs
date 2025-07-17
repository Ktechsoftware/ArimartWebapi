using ArimartEcommerceAPI.Infrastructure.Data.DTO;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using ArimartEcommerceAPI.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ArimartEcommerceAPI.API.Contollers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<NotificationListResponse>>> GetNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var result = await _notificationService.GetNotificationsAsync(userId, page, pageSize);
            return Ok(result);
        }

        [HttpPost]
       
        public async Task<ActionResult<ApiResponse<NotificationDto>>> CreateNotification(
            [FromBody] CreateNotificationDto notificationDto)
        {
            var result = await _notificationService.CreateNotificationAsync(notificationDto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id}/read")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead(long id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var result = await _notificationService.MarkAsReadAsync(id, userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("unread-count")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var result = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(result);
        }

        [HttpPut("mark-all-read")]
        public async Task<ActionResult<ApiResponse<bool>>> MarkAllAsRead()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var result = await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteNotification(long id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var result = await _notificationService.DeleteNotificationAsync(id, userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
