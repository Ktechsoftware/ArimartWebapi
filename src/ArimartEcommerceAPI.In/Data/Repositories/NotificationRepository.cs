using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArimartEcommerceAPI.Infrastructure.Data.DTO;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ArimartEcommerceAPI.Infrastructure.Data.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly ApplicationDbContext _context;

        public NotificationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<NotificationListResponse> GetNotificationsAsync(long userId, int page, int pageSize)
        {
            var query = _context.TblNotifications
                .Where(n => n.UserId == userId && !n.IsDeleted && (n.IsActive == true || n.IsActive == null))
                .OrderByDescending(n => n.AddedDate);

            var totalCount = await query.CountAsync();
            var notifications = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    Title = n.Title,
                    Urlt = n.Urlt,
                    Message = n.Message,
                    Acctt = n.Acctt,
                    AddedDate = n.AddedDate,
                    IsActive = n.IsActive,
                    Sipid = n.Sipid
                })
                .ToListAsync();

            return new NotificationListResponse
            {
                Notifications = notifications,
                TotalCount = totalCount,
                CurrentPage = page,
                PageSize = pageSize,
                HasMore = totalCount > page * pageSize
            };
        }

        public async Task<TblNotification> CreateNotificationAsync(CreateNotificationDto notificationDto)
        {
            var notification = new TblNotification
            {
                UserId = notificationDto.UserId,
                Title = notificationDto.Title,
                Urlt = notificationDto.Urlt,
                Message = notificationDto.Message,
                Acctt = false,
                AddedDate = DateTime.UtcNow,
                IsDeleted = false,
                IsActive = true,
                Sipid = notificationDto.Sipid
            };

            _context.TblNotifications.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        public async Task<bool> MarkAsReadAsync(long id, long userId)
        {
            var notification = await _context.TblNotifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId && !n.IsDeleted);

            if (notification == null) return false;

            notification.Acctt = true;
            notification.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetUnreadCountAsync(long userId)
        {
            return await _context.TblNotifications
                .CountAsync(n => n.UserId == userId && !n.IsDeleted &&
                           (n.IsActive == true || n.IsActive == null) &&
                           (n.Acctt == false || n.Acctt == null));
        }

        public async Task<bool> MarkAllAsReadAsync(long userId)
        {
            var notifications = await _context.TblNotifications
                .Where(n => n.UserId == userId && !n.IsDeleted &&
                           (n.Acctt == false || n.Acctt == null))
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.Acctt = true;
                notification.ModifiedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteNotificationAsync(long id, long userId)
        {
            var notification = await _context.TblNotifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null) return false;

            notification.IsDeleted = true;
            notification.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
