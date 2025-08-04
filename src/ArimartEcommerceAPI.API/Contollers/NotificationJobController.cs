using ArimartEcommerceAPI.Services.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArimartEcommerceAPI.API.Contollers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationJobController : ControllerBase
    {
        private readonly IAutomaticNotificationJob _notificationJob;

        public NotificationJobController(IAutomaticNotificationJob notificationJob)
        {
            _notificationJob = notificationJob;
        }

        [HttpPost("trigger/cart-abandonment")]
        public async Task<IActionResult> TriggerCartAbandonment()
        {
            await _notificationJob.ProcessCartAbandonmentNotifications();
            return Ok(new { message = "Cart abandonment notifications processed" });
        }

        [HttpPost("trigger/order-status")]
        public async Task<IActionResult> TriggerOrderStatus()
        {
            await _notificationJob.ProcessOrderStatusNotifications();
            return Ok(new { message = "Order status notifications processed" });
        }

        [HttpPost("trigger/group-buy")]
        public async Task<IActionResult> TriggerGroupBuy()
        {
            await _notificationJob.ProcessGroupBuyNotifications();
            return Ok(new { message = "Group buy notifications processed" });
        }

        [HttpPost("trigger/recommendations")]
        public async Task<IActionResult> TriggerRecommendations()
        {
            await _notificationJob.ProcessRecommendationNotifications();
            return Ok(new { message = "Recommendation notifications processed" });
        }

        [HttpPost("trigger/all")]
        public async Task<IActionResult> TriggerAll()
        {
            await _notificationJob.ProcessCartAbandonmentNotifications();
            await _notificationJob.ProcessOrderStatusNotifications();
            await _notificationJob.ProcessGroupBuyNotifications();
            await _notificationJob.ProcessRecommendationNotifications();

            return Ok(new { message = "All notification jobs processed" });
        }
    }
}
