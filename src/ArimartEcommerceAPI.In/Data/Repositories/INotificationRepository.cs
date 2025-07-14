using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArimartEcommerceAPI.Infrastructure.Data.DTO;
using ArimartEcommerceAPI.Infrastructure.Data.Models;

namespace ArimartEcommerceAPI.Infrastructure.Data.Repositories
{
    public interface INotificationRepository
    {
        Task<NotificationListResponse> GetNotificationsAsync(long userId, int page, int pageSize);
        Task<TblNotification> CreateNotificationAsync(CreateNotificationDto notificationDto);
        Task<bool> MarkAsReadAsync(long id, long userId);
        Task<int> GetUnreadCountAsync(long userId);
        Task<bool> MarkAllAsReadAsync(long userId);
        Task<bool> DeleteNotificationAsync(long id, long userId);
    }
}
