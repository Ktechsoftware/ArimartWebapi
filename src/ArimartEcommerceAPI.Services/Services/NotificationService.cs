using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArimartEcommerceAPI.Infrastructure.Data.DTO;
using ArimartEcommerceAPI.Infrastructure.Data.Hubs;
using ArimartEcommerceAPI.Infrastructure.Data.Repositories;
using ArimartEcommerceAPI.Services;
using Microsoft.AspNetCore.SignalR;


namespace ArimartEcommerceAPI.Services.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(
            INotificationRepository notificationRepository,
            IHubContext<NotificationHub> hubContext)
        {
            _notificationRepository = notificationRepository;
            _hubContext = hubContext;
        }

        public async Task<ApiResponse<NotificationListResponse>> GetNotificationsAsync(long userId, int page = 1, int pageSize = 10)
        {
            try
            {
                var result = await _notificationRepository.GetNotificationsAsync(userId, page, pageSize);
                return new ApiResponse<NotificationListResponse>
                {
                    Success = true,
                    Data = result,
                    Message = "Notifications retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<NotificationListResponse>
                {
                    Success = false,
                    Message = $"Error retrieving notifications: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<NotificationDto>> CreateNotificationAsync(CreateNotificationDto notificationDto)
        {
            try
            {
                var notification = await _notificationRepository.CreateNotificationAsync(notificationDto);

                var notificationResponse = new NotificationDto
                {
                    Id = notification.Id,
                    UserId = notification.UserId,
                    Title = notification.Title,
                    Urlt = notification.Urlt,
                    Message = notification.Message,
                    Acctt = notification.Acctt,
                    AddedDate = notification.AddedDate,
                    IsActive = notification.IsActive,
                    Sipid = notification.Sipid
                };

                // Send real-time notification via SignalR
                await _hubContext.Clients.Group($"user_{notification.UserId}")
                    .SendAsync("ReceiveNotification", notificationResponse);

                return new ApiResponse<NotificationDto>
                {
                    Success = true,
                    Data = notificationResponse,
                    Message = "Notification created successfully"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<NotificationDto>
                {
                    Success = false,
                    Message = $"Error creating notification: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<bool>> MarkAsReadAsync(long id, long userId)
        {
            try
            {
                var result = await _notificationRepository.MarkAsReadAsync(id, userId);
                if (result)
                {
                    // Update unread count via SignalR
                    var unreadCount = await _notificationRepository.GetUnreadCountAsync(userId);
                    await _hubContext.Clients.Group($"user_{userId}")
                        .SendAsync("UpdateUnreadCount", unreadCount);
                }

                return new ApiResponse<bool>
                {
                    Success = result,
                    Data = result,
                    Message = result ? "Notification marked as read" : "Notification not found"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Error marking notification as read: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<int>> GetUnreadCountAsync(long userId)
        {
            try
            {
                var count = await _notificationRepository.GetUnreadCountAsync(userId);
                return new ApiResponse<int>
                {
                    Success = true,
                    Data = count,
                    Message = "Unread count retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<int>
                {
                    Success = false,
                    Message = $"Error retrieving unread count: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<bool>> MarkAllAsReadAsync(long userId)
        {
            try
            {
                var result = await _notificationRepository.MarkAllAsReadAsync(userId);
                if (result)
                {
                    await _hubContext.Clients.Group($"user_{userId}")
                        .SendAsync("UpdateUnreadCount", 0);
                }

                return new ApiResponse<bool>
                {
                    Success = result,
                    Data = result,
                    Message = "All notifications marked as read"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Error marking all notifications as read: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<bool>> DeleteNotificationAsync(long id, long userId)
        {
            try
            {
                var result = await _notificationRepository.DeleteNotificationAsync(id, userId);
                return new ApiResponse<bool>
                {
                    Success = result,
                    Data = result,
                    Message = result ? "Notification deleted successfully" : "Notification not found"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Error deleting notification: {ex.Message}"
                };
            }
        }
    }
}
