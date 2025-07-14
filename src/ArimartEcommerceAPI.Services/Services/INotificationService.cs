using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArimartEcommerceAPI.Infrastructure.Data.DTO;

namespace ArimartEcommerceAPI.Services.Services
{
    public interface INotificationService
    {
        Task<ApiResponse<NotificationListResponse>> GetNotificationsAsync(long userId, int page = 1, int pageSize = 10);
        Task<ApiResponse<NotificationDto>> CreateNotificationAsync(CreateNotificationDto notificationDto);
        Task<ApiResponse<bool>> MarkAsReadAsync(long id, long userId);
        Task<ApiResponse<int>> GetUnreadCountAsync(long userId);
        Task<ApiResponse<bool>> MarkAllAsReadAsync(long userId);
        Task<ApiResponse<bool>> DeleteNotificationAsync(long id, long userId);
    }
}
