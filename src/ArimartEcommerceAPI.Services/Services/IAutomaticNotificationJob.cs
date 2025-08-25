using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArimartEcommerceAPI.Services.Services
{
    public interface IAutomaticNotificationJob
    {
        Task ProcessCartAbandonmentNotifications();
        Task ProcessOrderStatusNotifications();
        Task ProcessGroupBuyNotifications();
        Task ProcessRecommendationNotifications();
        Task ProcessPriceDropNotifications();
        Task ProcessRestockNotifications();
        Task ProcessBirthdayNotifications();
        Task ProcessInactiveUserNotifications();
        Task ProcessWeeklyReportNotifications();
        Task ProcessFlashSaleNotifications();
        Task ProcessLowStockNotifications();
        Task ProcessReturnReminderNotifications();
        Task ProcessGroupStatusNotifications();
        Task ProcessEnhancedOrderNotifications();
    }
}
