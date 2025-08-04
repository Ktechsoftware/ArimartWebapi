using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire;
using ArimartEcommerceAPI.Infrastructure.Data.DTO;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using ArimartEcommerceAPI.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace ArimartEcommerceAPI.Services.Services
{
    public class AutomaticNotificationJob : IAutomaticNotificationJob
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IFcmPushService _fcmPushService;
        private readonly ILogger<AutomaticNotificationJob> _logger;

        public AutomaticNotificationJob(
            ApplicationDbContext context,
            INotificationService notificationService,
            IFcmPushService fcmPushService,
            ILogger<AutomaticNotificationJob> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _fcmPushService = fcmPushService;
            _logger = logger;
        }

        // 🛒 Cart Abandonment Notifications
        [AutomaticRetry(Attempts = 3)]
        public async Task ProcessCartAbandonmentNotifications()
        {
            try
            {
                _logger.LogInformation("Starting cart abandonment notification processing");

                var cutoffTime = DateTime.UtcNow.AddHours(-3); // 3 hours ago
                var reminderCutoff = DateTime.UtcNow.AddHours(-24); // 24 hours ago

                var abandonedCarts = await _context.TblAddcarts
                    .Where(c => !c.IsDeleted &&
                               c.AddedDate <= cutoffTime &&
                               c.AddedDate >= reminderCutoff)
                    .GroupBy(c => c.Userid)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        ItemCount = g.Count(),
                        LastAddedDate = g.Max(c => c.AddedDate),
                        TotalValue = g.Sum(c => c.Price * c.Qty)
                    })
                    .ToListAsync();

                foreach (var cart in abandonedCarts)
                {
                    // Check if we already sent cart abandonment notification today
                    var alreadySent = await _context.TblNotifications
                        .AnyAsync(n => n.UserId == cart.UserId &&
                                      n.AddedDate.Date == DateTime.UtcNow.Date &&
                                      n.Title.Contains("items waiting"));

                    if (!alreadySent)
                    {
                        await SendCartAbandonmentNotification((int)cart.UserId, cart.ItemCount, (decimal)cart.TotalValue);
                    }
                }

                _logger.LogInformation($"Processed {abandonedCarts.Count} cart abandonment notifications");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing cart abandonment notifications");
                throw; // Re-throw to trigger Hangfire retry
            }
        }

        // 📦 Order Status Notifications
        [AutomaticRetry(Attempts = 3)]
        public async Task ProcessOrderStatusNotifications()
        {
            try
            {
                _logger.LogInformation("Starting order status notification processing");

                var recentStatusUpdates = await _context.TblOrdernows
                    .Where(o => !o.IsDeleted &&
                               (o.DassignidTime.HasValue && o.DassignidTime.Value >= DateTime.UtcNow.AddHours(-1) ||
                                o.DvendorpickupTime.HasValue && o.DvendorpickupTime.Value >= DateTime.UtcNow.AddHours(-1) ||
                                o.ShipOrderidTime.HasValue && o.ShipOrderidTime.Value >= DateTime.UtcNow.AddHours(-1) ||
                                o.DdeliverredidTime.HasValue && o.DdeliverredidTime.Value >= DateTime.UtcNow.AddHours(-1)))
                    .Select(o => new
                    {
                        o.Id,
                        o.TrackId,
                        o.Userid,
                        o.DassignidTime,
                        o.DvendorpickupTime,
                        o.ShipOrderidTime,
                        o.DdeliverredidTime,
                        Status = o.DdeliverredidTime != null ? "Delivered"
                                : o.ShipOrderidTime != null ? "Shipped"
                                : o.DvendorpickupTime != null ? "Picked Up"
                                : o.DassignidTime != null ? "Assigned"
                                : "Placed"
                    })
                    .ToListAsync();

                foreach (var order in recentStatusUpdates)
                {
                    var alreadySent = await _context.TblNotifications
                        .AnyAsync(n => n.UserId == order.Userid &&
                                      n.Message.Contains(order.TrackId) &&
                                      n.Message.Contains(order.Status) &&
                                      n.AddedDate >= DateTime.UtcNow.AddHours(-2));

                    if (!alreadySent)
                    {
                        await SendOrderStatusNotification((int)order.Userid, order.TrackId, order.Status);
                    }
                }

                _logger.LogInformation($"Processed {recentStatusUpdates.Count} order status notifications");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order status notifications");
                throw;
            }
        }

        // 👥 Group Buy Notifications
        [AutomaticRetry(Attempts = 3)]
        public async Task ProcessGroupBuyNotifications()
        {
            try
            {
                _logger.LogInformation("Starting group buy notification processing");

                // Process new group joins
                var recentJoins = await _context.TblGroupjoins
                    .Where(j => !j.IsDeleted &&
                               j.AddedDate >= DateTime.UtcNow.AddHours(-1))
                    .Include(j => j.Userid)
                    .ToListAsync();

                foreach (var join in recentJoins)
                {
                    if (join.Groupid != null)
                    {
                        var alreadySent = await _context.TblNotifications
                            .AnyAsync(n => n.UserId == join.Userid &&
                                          n.Message.Contains($"joined your group") &&
                                          n.AddedDate >= DateTime.UtcNow.AddMinutes(-30));

                        if (!alreadySent)
                        {
                            await SendGroupJoinNotification((int)join.Groupid, (int)join.Userid, (long)join.Groupid);
                        }
                    }
                }

                // Process groups nearing completion
                var almostCompleteGroups = await _context.VwGroups
                    .Where(g => g.IsDeleted1 == false &&
                               g.EventSend1 > DateTime.UtcNow)
                    .ToListAsync();

                foreach (var group in almostCompleteGroups)
                {
                    int required = int.TryParse(group.Gqty, out var gqtyParsed) ? gqtyParsed : 0;
                    int joined = await _context.TblGroupjoins
                        .CountAsync(j => j.Groupid == group.Gid &&
                                        !j.IsDeleted && j.IsActive == true);

                    if (required - joined <= 2 && required - joined > 0)
                    {
                        var groupMembers = await _context.TblGroupjoins
                            .Where(j => j.Groupid == group.Gid && !j.IsDeleted)
                            .Select(j => j.Userid)
                            .ToListAsync();

                        foreach (var memberId in groupMembers)
                        {
                            var alreadySent = await _context.TblNotifications
                                .AnyAsync(n => n.UserId == memberId &&
                                              n.Message.Contains("almost complete") &&
                                              n.AddedDate >= DateTime.UtcNow.AddHours(-12));

                            if (!alreadySent)
                            {
                                await SendGroupAlmostCompleteNotification((int)memberId, group.Gid, required - joined);
                            }
                        }
                    }
                }

                _logger.LogInformation("Group buy notifications processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing group buy notifications");
                throw;
            }
        }

        // 🎯 Product Recommendation Notifications
        [AutomaticRetry(Attempts = 3)]
        public async Task ProcessRecommendationNotifications()
        {
            try
            {
                _logger.LogInformation("Starting recommendation notification processing");

                var usersForRecommendations = await _context.TblUsers
                    .Where(u => u.IsActive == true && !u.IsDeleted)
                    .Where(u => !_context.TblNotifications
                        .Any(n => n.UserId == u.Id &&
                                 n.Title.Contains("Recommended") &&
                                 n.AddedDate >= DateTime.UtcNow.AddDays(-3)))
                    .Take(50)
                    .ToListAsync();

                foreach (var user in usersForRecommendations)
                {
                    var recommendations = await GetUserRecommendations((int)user.Id);
                    if (recommendations.Any())
                    {
                        await SendRecommendationNotification((int)user.Id, recommendations);
                    }
                }

                _logger.LogInformation($"Processed recommendations for {usersForRecommendations.Count} users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing recommendation notifications");
                throw;
            }
        }

        // 💰 Price Drop Notifications
        [AutomaticRetry(Attempts = 3)]
        public async Task ProcessPriceDropNotifications()
        {
            try
            {
                _logger.LogInformation("Starting price drop notification processing");

                var discountedProducts = await _context.VwProducts
                    .Where(p => p.IsActive == true && !p.IsDeleted &&
                               !string.IsNullOrEmpty(p.Discountprice) &&
                               decimal.Parse(p.Discountprice) > 0)
                    .Take(20)
                    .ToListAsync();

                foreach (var product in discountedProducts)
                {
                    var interestedUsers = await _context.TblAddcarts
                        .Where(c => c.Pdid == product.Id && c.IsDeleted)
                        .Select(c => c.Userid)
                        .Distinct()
                        .Take(10)
                        .ToListAsync();

                    foreach (var userId in interestedUsers)
                    {
                        var alreadySent = await _context.TblNotifications
                            .AnyAsync(n => n.UserId == userId &&
                                          n.Message.Contains(product.ProductName) &&
                                          n.Title.Contains("Price dropped") &&
                                          n.AddedDate >= DateTime.UtcNow.AddDays(-1));

                        if (!alreadySent)
                        {
                            await SendPriceDropNotification((int)userId, product.Id, product.ProductName, product.Discountprice);
                        }
                    }
                }

                _logger.LogInformation($"Processed price drop notifications for {discountedProducts.Count} products");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing price drop notifications");
                throw;
            }
        }

        // 📦 Restock Notifications
        [AutomaticRetry(Attempts = 3)]
        public async Task ProcessRestockNotifications()
        {
            try
            {
                _logger.LogInformation("Starting restock notification processing");

                // This would require a proper inventory tracking system
                // For now, we'll implement a basic version
                var restockedProducts = await _context.VwProducts
                    .Where(p => p.IsActive == true && !p.IsDeleted)
                    .Take(5)
                    .ToListAsync();

                _logger.LogInformation("Restock notifications processed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing restock notifications");
                throw;
            }
        }

        // 🎂 Birthday Notifications
        [AutomaticRetry(Attempts = 3)]
        public async Task ProcessBirthdayNotifications()
        {
            try
            {
                _logger.LogInformation("Starting birthday notification processing");

                var today = DateTime.UtcNow.Date;

                // This would require a DOB field in your user table
                var birthdayUsers = await _context.TblUsers
                    .Where(u => u.IsActive == true && !u.IsDeleted)
                    .Take(0) // Placeholder - implement DOB logic
                    .ToListAsync();

                foreach (var user in birthdayUsers)
                {
                    await SendBirthdayNotification((int)user.Id, user.VendorName ?? user.ContactPerson);
                }

                _logger.LogInformation($"Processed birthday notifications for {birthdayUsers.Count} users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing birthday notifications");
                throw;
            }
        }

        // 😴 Inactive User Notifications
        [AutomaticRetry(Attempts = 3)]
        public async Task ProcessInactiveUserNotifications()
        {
            try
            {
                _logger.LogInformation("Starting inactive user notification processing");

                var inactiveThreshold = DateTime.UtcNow.AddDays(-7);

                var inactiveUsers = await _context.TblUsers
                    .Where(u => u.IsActive == true && !u.IsDeleted &&
                               u.ModifiedDate <= inactiveThreshold)
                    .Where(u => !_context.TblNotifications
                        .Any(n => n.UserId == u.Id &&
                                 n.Title.Contains("miss you") &&
                                 n.AddedDate >= DateTime.UtcNow.AddDays(-7)))
                    .Take(20)
                    .ToListAsync();

                foreach (var user in inactiveUsers)
                {
                    await SendInactiveUserNotification((int)user.Id, user.VendorName ?? user.ContactPerson);
                }

                _logger.LogInformation($"Processed inactive user notifications for {inactiveUsers.Count} users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing inactive user notifications");
                throw;
            }
        }

        // 📊 Weekly Report Notifications
        [AutomaticRetry(Attempts = 3)]
        public async Task ProcessWeeklyReportNotifications()
        {
            try
            {
                _logger.LogInformation("Starting weekly report notification processing");

                var activeUsers = await _context.TblUsers
                    .Where(u => u.IsActive == true && !u.IsDeleted)
                    .Take(100)
                    .ToListAsync();

                foreach (var user in activeUsers)
                {
                    var weeklyStats = await GetUserWeeklyStats((int)user.Id);
                    if (weeklyStats != null)
                    {
                        await SendWeeklyReportNotification((int)user.Id, weeklyStats);
                    }
                }

                _logger.LogInformation($"Processed weekly reports for {activeUsers.Count} users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing weekly report notifications");
                throw;
            }
        }

        // ⚡ Flash Sale Notifications
        [AutomaticRetry(Attempts = 3)]
        public async Task ProcessFlashSaleNotifications()
        {
            try
            {
                _logger.LogInformation("Starting flash sale notification processing");

                // This would be based on your flash sale system
                var flashSaleProducts = await _context.VwProducts
                    .Where(p => p.IsActive == true && !p.IsDeleted &&
                               !string.IsNullOrEmpty(p.Discountprice))
                    .Take(5)
                    .ToListAsync();

                if (flashSaleProducts.Any())
                {
                    var activeUsers = await _context.TblUsers
                        .Where(u => u.IsActive == true && !u.IsDeleted)
                        .Take(50)
                        .ToListAsync();

                    foreach (var user in activeUsers)
                    {
                        var alreadySent = await _context.TblNotifications
                            .AnyAsync(n => n.UserId == user.Id &&
                                          n.Title.Contains("Flash Sale") &&
                                          n.AddedDate >= DateTime.UtcNow.AddHours(-1));

                        if (!alreadySent)
                        {
                            await SendFlashSaleNotification((int)user.Id, flashSaleProducts.First());
                        }
                    }
                }

                _logger.LogInformation("Flash sale notifications processed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing flash sale notifications");
                throw;
            }
        }

        // 📉 Low Stock Notifications (for vendors)
        [AutomaticRetry(Attempts = 3)]
        public async Task ProcessLowStockNotifications()
        {
            try
            {
                _logger.LogInformation("Starting low stock notification processing");

                // This would require proper inventory tracking
                var lowStockProducts = await _context.VwProducts
                    .Where(p => p.IsActive == true && !p.IsDeleted)
                    .Take(0) // Placeholder - implement stock level logic
                    .ToListAsync();

                _logger.LogInformation("Low stock notifications processed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing low stock notifications");
                throw;
            }
        }

        // 🔄 Return Reminder Notifications
        [AutomaticRetry(Attempts = 3)]
        public async Task ProcessReturnReminderNotifications()
        {
            try
            {
                _logger.LogInformation("Starting return reminder notification processing");

                var returnEligibleOrders = await _context.TblOrdernows
                    .Where(o => !o.IsDeleted &&
                               o.DdeliverredidTime.HasValue &&
                               o.DdeliverredidTime.Value >= DateTime.UtcNow.AddDays(-7) &&
                               o.DdeliverredidTime.Value <= DateTime.UtcNow.AddDays(-3))
                    .ToListAsync();

                foreach (var order in returnEligibleOrders)
                {
                    var alreadySent = await _context.TblNotifications
                        .AnyAsync(n => n.UserId == order.Userid &&
                                      n.Message.Contains(order.TrackId) &&
                                      n.Title.Contains("Return") &&
                                      n.AddedDate >= DateTime.UtcNow.AddDays(-1));

                    if (!alreadySent)
                    {
                        await SendReturnReminderNotification((int)order.Userid, order.TrackId);
                    }
                }

                _logger.LogInformation($"Processed return reminders for {returnEligibleOrders.Count} orders");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing return reminder notifications");
                throw;
            }
        }

        // Helper Methods
        private async Task<List<VwProduct>> GetUserRecommendations(int userId)
        {
            var recentOrders = await _context.TblOrdernows
                .Where(o => o.Userid == userId && !o.IsDeleted)
                .OrderByDescending(o => o.AddedDate)
                .Take(5)
                .Select(o => o.Pdid)
                .ToListAsync();

            if (!recentOrders.Any())
                return new List<VwProduct>();

            var recentCategories = await _context.VwProducts
                .Where(p => recentOrders.Contains(p.Id))
                .Select(p => p.Categoryid)
                .Distinct()
                .ToListAsync();

            var recommendations = await _context.VwProducts
                .Where(p => p.IsActive == true && !p.IsDeleted &&
                           recentCategories.Contains(p.Categoryid) &&
                           !recentOrders.Contains(p.Id))
                .OrderByDescending(p => p.AddedDate)
                .Take(3)
                .ToListAsync();

            return recommendations;
        }

        private async Task<dynamic> GetUserWeeklyStats(int userId)
        {
            var weekStart = DateTime.UtcNow.AddDays(-7);

            var orders = await _context.TblOrdernows
                .Where(o => o.Userid == userId && !o.IsDeleted &&
                           o.AddedDate >= weekStart)
                .CountAsync();

            var totalSpent = await _context.TblOrdernows
                .Where(o => o.Userid == userId && !o.IsDeleted &&
                           o.AddedDate >= weekStart)
                .SumAsync(o => o.Deliveryprice ?? 0);

            return new { OrderCount = orders, TotalSpent = totalSpent };
        }


        public async Task SendManualTestNotification(int userId)
        {
            var token = await _context.FcmDeviceTokens
                .Where(t => t.UserId == userId)
                .Select(t => t.Token)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(token))
            {
                await _fcmPushService.SendNotificationAsync(
                    token,
                    "🚀 Manual FCM Test",
                    $"Hello User #{userId}, this is a test notification from Hangfire!"
                );
            }
        }


        // Notification sending methods
        private async Task SendCartAbandonmentNotification(int userId, int itemCount, decimal totalValue)
        {
            var title = "🛒 Items waiting in your cart!";
            var message = $"You have {itemCount} items (₹{totalValue:F2}) waiting in your cart. Complete your purchase now!";
            await CreateAndSendNotification(userId, title, message, "cart_abandonment");
        }

        private async Task SendOrderStatusNotification(int userId, string trackId, string status)
        {
            var title = GetOrderStatusTitle(status);
            var message = $"Your order {trackId} has been {status.ToLower()}! 📦";
            await CreateAndSendNotification(userId, title, message, "order_status");
        }

        private async Task SendGroupJoinNotification(int groupCreatorId, int joinedUserId, long groupId)
        {
            var joinedUser = await _context.TblUsers.FindAsync(joinedUserId);
            var userName = joinedUser?.VendorName ?? joinedUser?.ContactPerson ?? "Someone";
            var title = "👥 New member joined!";
            var message = $"{userName} joined your group deal. Spread the word to complete faster!";
            await CreateAndSendNotification(groupCreatorId, title, message, "group_join");
        }

        private async Task SendGroupAlmostCompleteNotification(int userId, long groupId, int remaining)
        {
            var title = "🔥 Group deal almost complete!";
            var message = $"Only {remaining} more member(s) needed! Share with friends to unlock the deal.";
            await CreateAndSendNotification(userId, title, message, "group_almost_complete");
        }

        private async Task SendRecommendationNotification(int userId, List<VwProduct> recommendations)
        {
            var title = "✨ Recommended for you";
            var message = $"Check out {recommendations.First().ProductName} and {recommendations.Count - 1} more items picked just for you!";
            await CreateAndSendNotification(userId, title, message, "recommendations");
        }

        private async Task SendPriceDropNotification(int userId, long productId, string productName, string discountPrice)
        {
            var title = "💰 Price dropped!";
            var message = $"{productName} is now available at ₹{discountPrice}. Limited time offer!";
            await CreateAndSendNotification(userId, title, message, "price_drop");
        }
        private async Task SendBirthdayNotification(int userId, string userName)
        {
            var title = "🎂 Happy Birthday!";
            var message = $"Happy Birthday {userName}! Enjoy special birthday discounts just for you!";
            await CreateAndSendNotification(userId, title, message, "birthday");
        }

        private async Task SendInactiveUserNotification(int userId, string userName)
        {
            var title = "👋 We miss you!";
            var message = $"Hey {userName}, it's been a while! Come back and check out what's new on Arimart.";
            await CreateAndSendNotification(userId, title, message, "inactive_user");
        }

        private async Task SendWeeklyReportNotification(int userId, dynamic stats)
        {
            var title = "📊 Your Weekly Report";
            var message = $"You've placed {stats.OrderCount} orders and spent ₹{stats.TotalSpent:F2} this week. Keep it up!";
            await CreateAndSendNotification(userId, title, message, "weekly_report");
        }

        private async Task SendFlashSaleNotification(int userId, VwProduct product)
        {
            var title = "⚡ Flash Sale is Live!";
            var message = $"Don't miss {product.ProductName} at a steal price! Limited time only.";
            await CreateAndSendNotification(userId, title, message, "flash_sale");
        }

        private async Task SendReturnReminderNotification(int userId, string trackId)
        {
            var title = "🔄 Return Reminder";
            var message = $"Reminder: You can still return order {trackId}. Don't miss the deadline!";
            await CreateAndSendNotification(userId, title, message, "return_reminder");
        }

        private async Task CreateAndSendNotification(int userId, string title, string message, string url)
        {
            // Push Notification
            var token = await _context.FcmDeviceTokens
                .Where(t => t.UserId == userId)
                .Select(t => t.Token)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(token))
            {
                await _fcmPushService.SendNotificationAsync(token, title, message);
            }
        }
    
    private string GetOrderStatusTitle(string status)
        {
            return status switch
            {
                "Assigned" => "📋 Order Assigned",
                "Picked Up" => "📦 Order Picked Up",
                "Shipped" => "🚚 Order Shipped",
                "Delivered" => "✅ Order Delivered",
                _ => "📦 Order Update"
            };
        }
    }
}
