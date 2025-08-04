using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ArimartEcommerceAPI.Services.Services;

namespace ArimartEcommerceAPI.Services.Hangfire
{
    public static class HangfireJobScheduler
    {
        public static void ConfigureRecurringJobs(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<IAutomaticNotificationJob>>();
            var recurringJobManager = serviceProvider.GetRequiredService<IRecurringJobManager>();

            try
            {
                // 🛒 Cart Abandonment - Every 30 minutes
                recurringJobManager.AddOrUpdate<IAutomaticNotificationJob>(
                    "cart-abandonment-notifications",
                    job => job.ProcessCartAbandonmentNotifications(),
                    "*/30 * * * *", // Every 30 minutes
                    TimeZoneInfo.Local);

                // 📦 Order Status - Every 15 minutes
                recurringJobManager.AddOrUpdate<IAutomaticNotificationJob>(
                    "order-status-notifications",
                    job => job.ProcessOrderStatusNotifications(),
                    "*/15 * * * *", // Every 15 minutes
                    TimeZoneInfo.Local);

                // 👥 Group Buy - Every 20 minutes
                recurringJobManager.AddOrUpdate<IAutomaticNotificationJob>(
                    "group-buy-notifications",
                    job => job.ProcessGroupBuyNotifications(),
                    "*/20 * * * *", // Every 20 minutes
                    TimeZoneInfo.Local);

                // 🎯 Recommendations - Twice daily
                recurringJobManager.AddOrUpdate<IAutomaticNotificationJob>(
                    "recommendation-notifications",
                    job => job.ProcessRecommendationNotifications(),
                    "0 10,18 * * *", // 10 AM and 6 PM daily
                    TimeZoneInfo.Local);

                // 💰 Price Drops - Twice daily
                recurringJobManager.AddOrUpdate<IAutomaticNotificationJob>(
                    "price-drop-notifications",
                    job => job.ProcessPriceDropNotifications(),
                    "0 9,15 * * *", // 9 AM and 3 PM daily
                    TimeZoneInfo.Local);

                // 📦 Restock - Every 2 hours
                recurringJobManager.AddOrUpdate<IAutomaticNotificationJob>(
                    "restock-notifications",
                    job => job.ProcessRestockNotifications(),
                    "0 */2 * * *", // Every 2 hours
                    TimeZoneInfo.Local);

                // 🎂 Birthday - Daily at 9 AM
                recurringJobManager.AddOrUpdate<IAutomaticNotificationJob>(
                    "birthday-notifications",
                    job => job.ProcessBirthdayNotifications(),
                    "0 9 * * *", // Daily at 9 AM
                    TimeZoneInfo.Local);

                // 😴 Inactive Users - Weekly on Monday at 11 AM
                recurringJobManager.AddOrUpdate<IAutomaticNotificationJob>(
                    "inactive-user-notifications",
                    job => job.ProcessInactiveUserNotifications(),
                    "0 11 * * 1", // Every Monday at 11 AM
                    TimeZoneInfo.Local);

                // 📊 Weekly Reports - Sunday at 8 PM
                recurringJobManager.AddOrUpdate<IAutomaticNotificationJob>(
                    "weekly-report-notifications",
                    job => job.ProcessWeeklyReportNotifications(),
                    "0 20 * * 0", // Every Sunday at 8 PM
                    TimeZoneInfo.Local);

                // ⚡ Flash Sales - Every hour during business hours
                recurringJobManager.AddOrUpdate<IAutomaticNotificationJob>(
                    "flash-sale-notifications",
                    job => job.ProcessFlashSaleNotifications(),
                    "0 9-21 * * *", // Every hour from 9 AM to 9 PM
                    TimeZoneInfo.Local);

                // 📉 Low Stock - Every 4 hours
                recurringJobManager.AddOrUpdate<IAutomaticNotificationJob>(
                    "low-stock-notifications",
                    job => job.ProcessLowStockNotifications(),
                    "0 */4 * * *", // Every 4 hours
                    TimeZoneInfo.Local);

                // 🔄 Return Reminders - Daily at 3 PM
                recurringJobManager.AddOrUpdate<IAutomaticNotificationJob>(
                    "return-reminder-notifications",
                    job => job.ProcessReturnReminderNotifications(),
                    "0 15 * * *", // Daily at 3 PM
                    TimeZoneInfo.Local);

                logger.LogInformation("All Hangfire recurring jobs configured successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error configuring Hangfire recurring jobs");
                throw;
            }
        }

        // Method to trigger jobs manually
        public static void TriggerJobManually<T>(string jobId, Expression<Func<T, Task>> methodCall)
        {
            BackgroundJob.Enqueue(methodCall);
        }

        // Method to schedule a job for later
        public static void ScheduleJobLater<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay)
        {
            BackgroundJob.Schedule(methodCall, delay);
        }

        // Method to remove a recurring job
        public static void RemoveRecurringJob(string jobId)
        {
            RecurringJob.RemoveIfExists(jobId);
        }
    }
}
