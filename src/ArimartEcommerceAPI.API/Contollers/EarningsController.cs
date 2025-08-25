using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.DTO;
using ArimartEcommerceAPI.Infrastructure.Data.Models;

namespace ArimartEcommerceAPI.API.Controllers
{
    [ApiController]
    [Route("api/delivery/[controller]")]
    public class EarningsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EarningsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/delivery/earnings/record
        [HttpPost("record")]
        public async Task<ActionResult<OrderEarning>> RecordDeliveryEarning([FromBody] RecordEarningDto request)
        {
            try
            {
                // Verify order exists and belongs to the partner
                var order = await _context.TblOrdernows
                    .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.DeliveryPartnerId == request.PartnerId);

                if (order == null)
                    return NotFound("Order not found or not assigned to this delivery partner.");

                // Check if earning already recorded
                var existingEarning = await _context.OrderEarnings
                    .FirstOrDefaultAsync(e => e.OrderId == request.OrderId && e.PartnerId == request.PartnerId);

                if (existingEarning != null)
                    return BadRequest("Earning already recorded for this order.");

                // Verify order is delivered
                if (order.DdeliverredidTime == null)
                    return BadRequest("Order must be marked as delivered before recording earnings.");

                // Calculate earning amount based on delivery fee or fixed rate
                var earningAmount = request.EarnAmount ?? CalculateDeliveryFee(order);

                var earning = new OrderEarning
                {
                    PartnerId = request.PartnerId,
                    OrderId = request.OrderId,
                    EarnAmount = earningAmount,
                    DeliveredAtUtc = order.DdeliverredidTime.Value,
                    CreatedAt = DateTime.UtcNow
                };

                _context.OrderEarnings.Add(earning);

                // Update delivery partner wallet
                await UpdatePartnerWallet(request.PartnerId, earningAmount, $"Delivery earning for order #{order.TrackId}");

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Delivery earning of ₹{earningAmount} recorded successfully! 💰",
                    earning = new
                    {
                        earning.Id,
                        earning.PartnerId,
                        earning.OrderId,
                        earning.EarnAmount,
                        earning.DeliveredAtUtc,
                        OrderTrackId = order.TrackId
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to record earning",
                    error = ex.Message
                });
            }
        }

        // GET: api/delivery/earnings/{partnerId}
        [HttpGet("{partnerId}")]
        public async Task<ActionResult<List<EarningDetailDto>>> GetPartnerEarnings(
            long partnerId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow.AddDays(1);

                var earnings = await (
                    from earning in _context.OrderEarnings
                    join order in _context.TblOrdernows on earning.OrderId equals order.Id
                    join product in _context.VwProducts on order.Pdid equals (int?)product.Pdid into prodJoin
                    from product in prodJoin.DefaultIfEmpty()
                    where earning.PartnerId == partnerId &&
                          earning.DeliveredAtUtc >= start &&
                          earning.DeliveredAtUtc < end
                    orderby earning.DeliveredAtUtc descending
                    select new EarningDetailDto
                    {
                        Id = earning.Id,
                        OrderId = earning.OrderId,
                        OrderTrackId = order.TrackId,
                        EarnAmount = earning.EarnAmount,
                        DeliveredAt = earning.DeliveredAtUtc,
                        ProductName = product.ProductName ?? "Product not found",
                        OrderValue = order.Deliveryprice * order.Qty,
                        EarningPercentage = order.Deliveryprice > 0 ?
                            (earning.EarnAmount / (order.Deliveryprice * order.Qty)) * 100 : 0
                    })
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(earnings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to get partner earnings",
                    error = ex.Message
                });
            }
        }

        // GET: api/delivery/earnings/summary/{partnerId}
        [HttpGet("summary/{partnerId}")]
        public async Task<ActionResult<EarningSummaryDto>> GetEarningSummary(
            long partnerId,
            [FromQuery] DateTime? date = null)
        {
            try
            {
                var targetDate = date?.Date ?? DateTime.UtcNow.Date;
                var weekStart = GetStartOfWeek(targetDate);
                var monthStart = new DateTime(targetDate.Year, targetDate.Month, 1);

                // Today's earnings
                var todayEarnings = await _context.OrderEarnings
                    .Where(e => e.PartnerId == partnerId && e.DeliveredAtUtc.Date == targetDate)
                    .SumAsync(e => e.EarnAmount);

                var todayDeliveries = await _context.OrderEarnings
                    .CountAsync(e => e.PartnerId == partnerId && e.DeliveredAtUtc.Date == targetDate);

                // This week's earnings
                var weeklyEarnings = await _context.OrderEarnings
                    .Where(e => e.PartnerId == partnerId && e.DeliveredAtUtc >= weekStart && e.DeliveredAtUtc < weekStart.AddDays(7))
                    .SumAsync(e => e.EarnAmount);

                var weeklyDeliveries = await _context.OrderEarnings
                    .CountAsync(e => e.PartnerId == partnerId && e.DeliveredAtUtc >= weekStart && e.DeliveredAtUtc < weekStart.AddDays(7));

                // This month's earnings
                var monthlyEarnings = await _context.OrderEarnings
                    .Where(e => e.PartnerId == partnerId && e.DeliveredAtUtc >= monthStart && e.DeliveredAtUtc < monthStart.AddMonths(1))
                    .SumAsync(e => e.EarnAmount);

                var monthlyDeliveries = await _context.OrderEarnings
                    .CountAsync(e => e.PartnerId == partnerId && e.DeliveredAtUtc >= monthStart && e.DeliveredAtUtc < monthStart.AddMonths(1));

                // All-time totals
                var totalEarnings = await _context.OrderEarnings
                    .Where(e => e.PartnerId == partnerId)
                    .SumAsync(e => e.EarnAmount);

                var totalDeliveries = await _context.OrderEarnings
                    .CountAsync(e => e.PartnerId == partnerId);

                // Calculate averages
                var avgEarningsPerDelivery = totalDeliveries > 0 ? totalEarnings / totalDeliveries : 0;

                // Get peak earning day
                var peakEarningDay = await _context.OrderEarnings
                    .Where(e => e.PartnerId == partnerId)
                    .GroupBy(e => e.DeliveredAtUtc.Date)
                    .Select(g => new { Date = g.Key, Amount = g.Sum(e => e.EarnAmount) })
                    .OrderByDescending(x => x.Amount)
                    .FirstOrDefaultAsync();

                var summary = new EarningSummaryDto
                {
                    PartnerId = partnerId,
                    ReportDate = targetDate,

                    TodayEarnings = todayEarnings,
                    TodayDeliveries = todayDeliveries,

                    WeeklyEarnings = weeklyEarnings,
                    WeeklyDeliveries = weeklyDeliveries,

                    MonthlyEarnings = monthlyEarnings,
                    MonthlyDeliveries = monthlyDeliveries,

                    TotalLifetimeEarnings = totalEarnings,
                    TotalLifetimeDeliveries = totalDeliveries,

                    AverageEarningsPerDelivery = avgEarningsPerDelivery,

                    PeakEarningDay = peakEarningDay?.Date,
                    PeakEarningAmount = peakEarningDay?.Amount ?? 0,

                    DailyGoal = 500m, // Set a daily goal
                    DailyProgress = todayEarnings / 500m * 100,

                    WeeklyGoal = 3500m, // Set a weekly goal
                    WeeklyProgress = weeklyEarnings / 3500m * 100,

                    MonthlyGoal = 15000m, // Set a monthly goal
                    MonthlyProgress = monthlyEarnings / 15000m * 100
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to get earning summary",
                    error = ex.Message
                });
            }
        }

        // GET: api/delivery/earnings/chart/{partnerId}
        [HttpGet("chart/{partnerId}")]
        public async Task<ActionResult> GetEarningsChart(
            long partnerId,
            [FromQuery] string period = "week", // week, month, year
            [FromQuery] DateTime? startDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = DateTime.UtcNow;

                List<object> chartData;

                switch (period.ToLower())
                {
                    case "week":
                        chartData = await GetWeeklyChartData(partnerId, start);
                        break;
                    case "month":
                        chartData = await GetMonthlyChartData(partnerId, start);
                        break;
                    case "year":
                        chartData = await GetYearlyChartData(partnerId, start);
                        break;
                    default:
                        chartData = await GetDailyChartData(partnerId, start, end);
                        break;
                }

                return Ok(new
                {
                    period,
                    startDate = start,
                    endDate = end,
                    data = chartData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to get earnings chart data",
                    error = ex.Message
                });
            }
        }

        // GET: api/delivery/earnings/leaderboard
        [HttpGet("leaderboard")]
        public async Task<ActionResult> GetEarningsLeaderboard(
            [FromQuery] string period = "month", // today, week, month
            [FromQuery] int limit = 10)
        {
            try
            {
                DateTime startDate;
                string periodLabel;

                switch (period.ToLower())
                {
                    case "today":
                        startDate = DateTime.UtcNow.Date;
                        periodLabel = "Today";
                        break;
                    case "week":
                        startDate = GetStartOfWeek(DateTime.UtcNow);
                        periodLabel = "This Week";
                        break;
                    case "month":
                    default:
                        startDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                        periodLabel = "This Month";
                        break;
                }

                var leaderboard = await (
                    from earning in _context.OrderEarnings
                    join partner in _context.TblDeliveryusers on earning.PartnerId equals partner.Id
                    where earning.DeliveredAtUtc >= startDate
                    group earning by new { earning.PartnerId, partner.ContactPerson, partner.Phone } into g
                    select new
                    {
                        PartnerId = g.Key.PartnerId,
                        PartnerName = g.Key.ContactPerson ?? "Unknown Partner",
                        PartnerPhone = g.Key.Phone,
                        TotalEarnings = g.Sum(e => e.EarnAmount),
                        TotalDeliveries = g.Count(),
                        AveragePerDelivery = g.Average(e => e.EarnAmount),
                        BestDay = g.GroupBy(e => e.DeliveredAtUtc.Date)
                               .OrderByDescending(d => d.Sum(e => e.EarnAmount))
                               .Select(d => new { Date = d.Key, Earnings = d.Sum(e => e.EarnAmount) })
                               .FirstOrDefault()
                    })
                    .OrderByDescending(x => x.TotalEarnings)
                    .Take(limit)
                    .ToListAsync();

                var result = leaderboard.Select((partner, index) => new {
                    Rank = index + 1,
                    partner.PartnerId,
                    partner.PartnerName,
                    partner.PartnerPhone,
                    partner.TotalEarnings,
                    partner.TotalDeliveries,
                    partner.AveragePerDelivery,
                    partner.BestDay,
                    Badge = GetPerformanceBadge(index + 1, partner.TotalEarnings, partner.TotalDeliveries)
                }).ToList();

                return Ok(new
                {
                    period = periodLabel,
                    startDate,
                    totalPartners = leaderboard.Count,
                    leaderboard = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to get earnings leaderboard",
                    error = ex.Message
                });
            }
        }

        // POST: api/delivery/earnings/bulk-record
        [HttpPost("bulk-record")]
        public async Task<ActionResult> BulkRecordEarnings([FromBody] List<RecordEarningDto> requests)
        {
            try
            {
                var results = new List<object>();
                var successCount = 0;

                foreach (var request in requests)
                {
                    try
                    {
                        // Verify order exists and is delivered
                        var order = await _context.TblOrdernows
                            .FirstOrDefaultAsync(o => o.Id == request.OrderId &&
                                               o.DeliveryPartnerId == request.PartnerId &&
                                               o.DdeliverredidTime != null);

                        if (order == null)
                        {
                            results.Add(new
                            {
                                OrderId = request.OrderId,
                                Status = "Failed",
                                Error = "Order not found or not delivered"
                            });
                            continue;
                        }

                        // Check if already recorded
                        var exists = await _context.OrderEarnings
                            .AnyAsync(e => e.OrderId == request.OrderId && e.PartnerId == request.PartnerId);

                        if (exists)
                        {
                            results.Add(new
                            {
                                OrderId = request.OrderId,
                                Status = "Skipped",
                                Error = "Already recorded"
                            });
                            continue;
                        }

                        var earningAmount = request.EarnAmount ?? CalculateDeliveryFee(order);

                        var earning = new OrderEarning
                        {
                            PartnerId = request.PartnerId,
                            OrderId = request.OrderId,
                            EarnAmount = earningAmount,
                            DeliveredAtUtc = order.DdeliverredidTime.Value,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.OrderEarnings.Add(earning);
                        await UpdatePartnerWallet(request.PartnerId, earningAmount, $"Delivery earning for order #{order.TrackId}");

                        results.Add(new
                        {
                            OrderId = request.OrderId,
                            Status = "Success",
                            Amount = earningAmount
                        });
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        results.Add(new
                        {
                            OrderId = request.OrderId,
                            Status = "Failed",
                            Error = ex.Message
                        });
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Bulk earnings recording completed. {successCount}/{requests.Count} records processed successfully.",
                    totalRequests = requests.Count,
                    successCount,
                    failedCount = requests.Count - successCount,
                    results
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to bulk record earnings",
                    error = ex.Message
                });
            }
        }

        // Helper Methods
        private decimal CalculateDeliveryFee(TblOrdernow order)
        {
            // Base delivery fee calculation logic
            var baseFee = 25m; 
            var orderValue = order.Deliveryprice * order.Qty;
            // Calculate percentage-based fee (e.g., 5% of order value)
            var percentageFee = orderValue * 0.05m;

            // Use higher of base fee or percentage fee, with a cap
            var calculatedFee = Math.Max(baseFee, Math.Min((byte)percentageFee, 100m));

            return Math.Round(calculatedFee, 2);
        }

        private async Task UpdatePartnerWallet(long partnerId, decimal amount, string description)
        {
            var wallet = await _context.TblDeliveryWallets
                .FirstOrDefaultAsync(w => w.DeliveryPartnerId == partnerId);

            if (wallet == null)
            {
                wallet = new TblDeliveryWallet
                {
                    DeliveryPartnerId = partnerId,
                    Balance = 0,
                    WeeklyEarnings = 0,
                    MonthlyEarnings = 0,
                    TotalEarnings = 0,
                    LastUpdated = DateTime.UtcNow
                };
                _context.TblDeliveryWallets.Add(wallet);
                await _context.SaveChangesAsync();
            }

            // Create transaction
            var transaction = new TblDeliveryTransaction
            {
                DeliveryPartnerId = partnerId,
                Title = "Delivery Earning 📦",
                Description = description,
                Amount = amount,
                Type = TransactionType.Credit,
                Status = TransactionStatus.Completed,
                ReferenceNumber = $"DEL{DateTime.Now:yyyyMMddHHmmss}{partnerId}"
            };

            _context.TblDeliveryTransactions.Add(transaction);

            // Update wallet
            wallet.Balance = (wallet.Balance ?? 0) + amount;
            wallet.TotalEarnings = (wallet.TotalEarnings ?? 0) + amount;
            wallet.LastUpdated = DateTime.UtcNow;
        }

        private DateTime GetStartOfWeek(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        private string GetPerformanceBadge(int rank, decimal earnings, int deliveries)
        {
            return rank switch
            {
                1 => "🏆 Champion",
                2 => "🥈 Runner-up",
                3 => "🥉 Top Performer",
                <= 5 => "⭐ Star Performer",
                <= 10 => "💪 Strong Performer",
                _ => "👍 Good Job"
            };
        }

        private async Task<List<object>> GetDailyChartData(long partnerId, DateTime start, DateTime end)
        {
            return await _context.OrderEarnings
                .Where(e => e.PartnerId == partnerId && e.DeliveredAtUtc >= start && e.DeliveredAtUtc <= end)
                .GroupBy(e => e.DeliveredAtUtc.Date)
                .Select(g => new {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    Earnings = g.Sum(e => e.EarnAmount),
                    Deliveries = g.Count(),
                    AveragePerDelivery = g.Average(e => e.EarnAmount)
                })
                .OrderBy(x => x.Date)
                .Cast<object>()
                .ToListAsync();
        }

        private async Task<List<object>> GetWeeklyChartData(long partnerId, DateTime start)
        {
            var weeks = new List<object>();
            var current = GetStartOfWeek(start);
            var end = DateTime.UtcNow;

            while (current <= end)
            {
                var weekEnd = current.AddDays(7);
                var weekData = await _context.OrderEarnings
                    .Where(e => e.PartnerId == partnerId &&
                               e.DeliveredAtUtc >= current &&
                               e.DeliveredAtUtc < weekEnd)
                    .GroupBy(e => 1)
                    .Select(g => new {
                        Week = $"{current:MMM dd} - {weekEnd.AddDays(-1):MMM dd}",
                        StartDate = current,
                        Earnings = g.Sum(e => e.EarnAmount),
                        Deliveries = g.Count()
                    })
                    .FirstOrDefaultAsync();

                weeks.Add(weekData ?? new
                {
                    Week = $"{current:MMM dd} - {weekEnd.AddDays(-1):MMM dd}",
                    StartDate = current,
                    Earnings = 0m,
                    Deliveries = 0
                });

                current = current.AddDays(7);
            }

            return weeks;
        }

        private async Task<List<object>> GetMonthlyChartData(long partnerId, DateTime start)
        {
            return await _context.OrderEarnings
                .Where(e => e.PartnerId == partnerId && e.DeliveredAtUtc >= start)
                .GroupBy(e => new { e.DeliveredAtUtc.Year, e.DeliveredAtUtc.Month })
                .Select(g => new {
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    Earnings = g.Sum(e => e.EarnAmount),
                    Deliveries = g.Count(),
                    AveragePerDelivery = g.Average(e => e.EarnAmount)
                })
                .OrderBy(x => x.Month)
                .Cast<object>()
                .ToListAsync();
        }

        private async Task<List<object>> GetYearlyChartData(long partnerId, DateTime start)
        {
            return await _context.OrderEarnings
                .Where(e => e.PartnerId == partnerId && e.DeliveredAtUtc >= start)
                .GroupBy(e => e.DeliveredAtUtc.Year)
                .Select(g => new {
                    Year = g.Key,
                    Earnings = g.Sum(e => e.EarnAmount),
                    Deliveries = g.Count(),
                    AveragePerDelivery = g.Average(e => e.EarnAmount)
                })
                .OrderBy(x => x.Year)
                .Cast<object>()
                .ToListAsync();
        }
    }

    // Additional DTOs for Earnings
    public class RecordEarningDto
    {
        public long PartnerId { get; set; }
        public long OrderId { get; set; }
        public decimal? EarnAmount { get; set; } // If null, will be calculated
    }

    public class EarningDetailDto
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public string OrderTrackId { get; set; } = "";
        public decimal EarnAmount { get; set; }
        public DateTime DeliveredAt { get; set; }
        public string ProductName { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string DeliveryAddress { get; set; } = "";
        public decimal? OrderValue { get; set; }
        public decimal? EarningPercentage { get; set; }
    }

    public class EarningSummaryDto
    {
        public long PartnerId { get; set; }
        public DateTime ReportDate { get; set; }

        public decimal TodayEarnings { get; set; }
        public int TodayDeliveries { get; set; }

        public decimal WeeklyEarnings { get; set; }
        public int WeeklyDeliveries { get; set; }

        public decimal MonthlyEarnings { get; set; }
        public int MonthlyDeliveries { get; set; }

        public decimal TotalLifetimeEarnings { get; set; }
        public int TotalLifetimeDeliveries { get; set; }

        public decimal AverageEarningsPerDelivery { get; set; }

        public DateTime? PeakEarningDay { get; set; }
        public decimal PeakEarningAmount { get; set; }

        public decimal DailyGoal { get; set; }
        public decimal DailyProgress { get; set; }

        public decimal WeeklyGoal { get; set; }
        public decimal WeeklyProgress { get; set; }

        public decimal MonthlyGoal { get; set; }
        public decimal MonthlyProgress { get; set; }
    }
}