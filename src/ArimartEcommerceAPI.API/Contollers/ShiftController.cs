using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.DTO;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace ArimartEcommerceAPI.API.Controllers
{
    [ApiController]
    [Route("api/delivery/[controller]")]
    public class ShiftController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ShiftController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/delivery/shift/start
        [HttpPost("start")]
        public async Task<ActionResult<ShiftResponseDto>> StartShift([FromBody] StartShiftRequestDto request)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Check if partner exists using EF Core
                var partnerExists = await _context.TblDeliveryusers
                    .AnyAsync(p => p.Id == request.PartnerId);

                Console.WriteLine($"Partner exists: {partnerExists}");

                if (!partnerExists)
                    return NotFound(new { message = $"Partner {request.PartnerId} not found" });

                // Check for active shift
                var hasActiveShift = await _context.DeliveryShifts
                    .AnyAsync(s => s.PartnerId == request.PartnerId && s.EndTimeUtc == null);

                if (hasActiveShift)
                    return BadRequest(new { message = "Partner already has an active shift" });

                // Create shift
                var newShift = new DeliveryShift
                {
                    PartnerId = request.PartnerId,
                    StartTimeUtc = DateTime.UtcNow,
                    StartLat = request.StartLatitude,
                    StartLng = request.StartLongitude,
                    CreatedAt = DateTime.UtcNow
                };

                _context.DeliveryShifts.Add(newShift);
                await _context.SaveChangesAsync();

                // Update partner using EF Core
                await _context.TblDeliveryusers
                    .Where(p => p.Id == request.PartnerId)
                    .ExecuteUpdateAsync(p => p
                        .SetProperty(x => x.IsOnline, true)
                        .SetProperty(x => x.CurrentShiftId, newShift.ShiftId));

                await transaction.CommitAsync();

                var response = new ShiftResponseDto
                {
                    ShiftId = newShift.ShiftId,
                    PartnerId = newShift.PartnerId,
                    StartTime = newShift.StartTimeUtc,
                    EndTime = null,
                    StartLatitude = newShift.StartLat,
                    StartLongitude = newShift.StartLng,
                    EndLatitude = null,
                    EndLongitude = null,
                    Duration = FormatDuration(newShift.Duration ?? TimeSpan.Zero),
                    IsActive = true,
                    TotalEarnings = 0,
                    DeliveriesCompleted = 0
                };

                return Ok(new { message = "Shift started!", shift = response });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"ERROR: {ex.Message}");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // POST: api/delivery/shift/end
        [HttpPost("end")]
        public async Task<ActionResult<ShiftResponseDto>> EndShift([FromBody] EndShiftRequestDto request)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var activeShift = await _context.DeliveryShifts
                    .FirstOrDefaultAsync(s => s.PartnerId == request.PartnerId && s.EndTimeUtc == null);

                if (activeShift == null)
                    return NotFound(new { message = "No active shift found for this partner." });

                // End the shift
                activeShift.EndTimeUtc = DateTime.UtcNow;
                activeShift.EndLat = request.EndLatitude;
                activeShift.EndLng = request.EndLongitude;

                // Update partner using EF Core bulk update
                await _context.TblDeliveryusers
                    .Where(p => p.Id == request.PartnerId)
                    .ExecuteUpdateAsync(p => p
                        .SetProperty(x => x.IsOnline, false)
                        .SetProperty(x => x.CurrentShiftId, (long?)null));

                await _context.SaveChangesAsync();

                // Calculate stats
                var shiftStats = await GetShiftStatistics(activeShift.ShiftId);

                var response = new ShiftResponseDto
                {
                    ShiftId = activeShift.ShiftId,
                    PartnerId = activeShift.PartnerId,
                    StartTime = activeShift.StartTimeUtc,
                    EndTime = activeShift.EndTimeUtc,
                    StartLatitude = activeShift.StartLat,
                    StartLongitude = activeShift.StartLng,
                    EndLatitude = activeShift.EndLat,
                    EndLongitude = activeShift.EndLng,
                    Duration = activeShift.Duration.HasValue ? FormatDuration(activeShift.Duration.Value) : "00:00:00",
                    IsActive = false,
                    TotalEarnings = shiftStats.TotalEarnings,
                    DeliveriesCompleted = shiftStats.DeliveriesCompleted
                };

                await ProcessShiftIncentives(request.PartnerId, shiftStats.DeliveriesCompleted);

                await transaction.CommitAsync();

                return Ok(new
                {
                    message = $"Shift ended successfully! 🎉 Duration: {response.Duration}, Earnings: ₹{response.TotalEarnings}",
                    shift = response
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    message = "Failed to end shift",
                    error = ex.Message
                });
            }
        }

        // GET: api/delivery/shift/stats/{partnerId}
        [HttpGet("stats/{partnerId}")]
        public async Task<ActionResult<ShiftStatsDto>> GetShiftStats(long partnerId)
        {
            try
            {
                // Get partner without navigation properties first
                var partner = await _context.TblDeliveryusers
                    .FirstOrDefaultAsync(p => p.Id == partnerId);

                if (partner == null)
                    return NotFound($"Delivery partner with ID {partnerId} not found.");

                // Get current shift directly
                DeliveryShift? currentShift = null;
                if (partner.CurrentShiftId.HasValue)
                {
                    currentShift = await _context.DeliveryShifts
                        .FirstOrDefaultAsync(s => s.ShiftId == partner.CurrentShiftId.Value);
                }

                var today = DateTime.UtcNow.Date;
                var weekStart = GetStartOfWeek(DateTime.UtcNow);
                var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

                // Today's stats
                var todayShifts = await _context.DeliveryShifts
                    .Where(s => s.PartnerId == partnerId && s.StartTimeUtc.Date == today)
                    .ToListAsync();

                var todayLoginHours = CalculateTotalLoginHours(todayShifts);

                // Check if OrderEarnings table exists and has data
                var todayEarnings = 0m;
                var todayDeliveries = 0;
                try
                {
                    todayEarnings = await _context.OrderEarnings
                        .Where(e => e.PartnerId == partnerId && e.DeliveredAtUtc.Date == today)
                        .SumAsync(e => (decimal?)e.EarnAmount) ?? 0;

                    todayDeliveries = await _context.OrderEarnings
                        .CountAsync(e => e.PartnerId == partnerId && e.DeliveredAtUtc.Date == today);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"OrderEarnings query failed: {ex.Message}");
                    // Continue with 0 values
                }

                // Weekly and monthly stats (with similar error handling)
                var weeklyShifts = await _context.DeliveryShifts
                    .Where(s => s.PartnerId == partnerId && s.StartTimeUtc >= weekStart)
                    .ToListAsync();

                var weeklyLoginHours = CalculateTotalLoginHours(weeklyShifts);
                var weeklyEarnings = 0m;
                var weeklyDeliveries = 0;

                try
                {
                    weeklyEarnings = await _context.OrderEarnings
                        .Where(e => e.PartnerId == partnerId && e.DeliveredAtUtc >= weekStart)
                        .SumAsync(e => (decimal?)e.EarnAmount) ?? 0;

                    weeklyDeliveries = await _context.OrderEarnings
                        .CountAsync(e => e.PartnerId == partnerId && e.DeliveredAtUtc >= weekStart);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Weekly OrderEarnings query failed: {ex.Message}");
                }

                var monthlyShifts = await _context.DeliveryShifts
                    .Where(s => s.PartnerId == partnerId && s.StartTimeUtc >= monthStart)
                    .ToListAsync();

                var monthlyLoginHours = CalculateTotalLoginHours(monthlyShifts);
                var monthlyEarnings = 0m;
                var monthlyDeliveries = 0;

                try
                {
                    monthlyEarnings = await _context.OrderEarnings
                        .Where(e => e.PartnerId == partnerId && e.DeliveredAtUtc >= monthStart)
                        .SumAsync(e => (decimal?)e.EarnAmount) ?? 0;

                    monthlyDeliveries = await _context.OrderEarnings
                        .CountAsync(e => e.PartnerId == partnerId && e.DeliveredAtUtc >= monthStart);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Monthly OrderEarnings query failed: {ex.Message}");
                }

                // Current shift stats
                decimal currentShiftEarnings = 0;
                int currentShiftDeliveries = 0;
                string currentShiftDuration = "00:00:00";

                if (currentShift != null)
                {
                    var currentShiftStats = await GetShiftStatistics(currentShift.ShiftId);
                    currentShiftEarnings = currentShiftStats.TotalEarnings;
                    currentShiftDeliveries = currentShiftStats.DeliveriesCompleted;

                    if (currentShift.EndTimeUtc.HasValue)
                    {
                        currentShiftDuration = FormatDuration(currentShift.EndTimeUtc.Value - currentShift.StartTimeUtc);
                    }
                    else
                    {
                        currentShiftDuration = FormatDuration(DateTime.UtcNow - currentShift.StartTimeUtc);
                    }
                }

                // Available incentives
                List<IncentiveDto> incentives = new();
                try
                {
                    incentives = await GetAvailableIncentives(partnerId, todayDeliveries);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Incentives query failed: {ex.Message}");
                }

                var stats = new ShiftStatsDto
                {
                    PartnerId = partnerId,
                    IsCurrentlyOnline = partner.IsOnline,
                    CurrentShiftId = partner.CurrentShiftId,

                    TodayLoginHours = todayLoginHours.HasValue ? FormatDuration(todayLoginHours.Value) : "00:00:00",
                    TodayEarnings = todayEarnings,
                    TodayDeliveries = todayDeliveries,

                    CurrentShiftStart = currentShift?.StartTimeUtc,
                    CurrentShiftDuration = currentShiftDuration,
                    CurrentShiftEarnings = currentShiftEarnings,
                    CurrentShiftDeliveries = currentShiftDeliveries,

                    WeeklyLoginHours = weeklyLoginHours.HasValue ? FormatDuration(weeklyLoginHours.Value) : "00:00:00",
                    WeeklyEarnings = weeklyEarnings,
                    WeeklyDeliveries = weeklyDeliveries,

                    MonthlyLoginHours = monthlyLoginHours.HasValue ? FormatDuration(monthlyLoginHours.Value) : "00:00:00",
                    MonthlyEarnings = monthlyEarnings,
                    MonthlyDeliveries = monthlyDeliveries,

                    AvailableIncentives = incentives
                };

                Console.WriteLine($"Stats created: IsOnline={stats.IsCurrentlyOnline}, CurrentShiftId={stats.CurrentShiftId}");
                return Ok(stats);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetShiftStats ERROR: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new
                {
                    message = "Failed to get shift stats",
                    error = ex.Message
                });
            }
        }

        // GET: api/delivery/shift/history/{partnerId}
        [HttpGet("history/{partnerId}")]
        public async Task<ActionResult<List<ShiftHistoryDto>>> GetShiftHistory(
            long partnerId,
            [FromQuery] int days = 30,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var startDate = DateTime.UtcNow.AddDays(-days);

                var shifts = await _context.DeliveryShifts
                    .Where(s => s.PartnerId == partnerId && s.StartTimeUtc >= startDate)
                    .OrderByDescending(s => s.StartTimeUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var shiftHistory = new List<ShiftHistoryDto>();

                foreach (var shift in shifts)
                {
                    var stats = await GetShiftStatistics(shift.ShiftId);

                    // Fixed duration calculation for history
                    string duration = "00:00:00";
                    if (shift.EndTimeUtc.HasValue)
                    {
                        duration = FormatDuration(shift.EndTimeUtc.Value - shift.StartTimeUtc);
                    }
                    else
                    {
                        // Active shift
                        duration = FormatDuration(DateTime.UtcNow - shift.StartTimeUtc);
                    }

                    var historyItem = new ShiftHistoryDto
                    {
                        ShiftId = shift.ShiftId,
                        StartTime = shift.StartTimeUtc,
                        EndTime = shift.EndTimeUtc,
                        Duration = duration,
                        Earnings = stats.TotalEarnings,
                        DeliveriesCompleted = stats.DeliveriesCompleted,
                        StartLocation = await GetLocationName(shift.StartLat, shift.StartLng),
                        EndLocation = await GetLocationName(shift.EndLat, shift.EndLng)
                    };

                    shiftHistory.Add(historyItem);
                }

                return Ok(shiftHistory);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to get shift history",
                    error = ex.Message
                });
            }
        }

        // GET: api/delivery/shift/online-status/{partnerId}
        [HttpGet("online-status/{partnerId}")]
        public async Task<ActionResult<OnlineStatusDto>> GetOnlineStatus(long partnerId)
        {
            try
            {
                var partner = await _context.TblDeliveryusers
                    .FirstOrDefaultAsync(p => p.Id == partnerId);

                if (partner == null)
                    return NotFound($"Delivery partner with ID {partnerId} not found.");

                // Get current shift directly instead of using navigation
                DeliveryShift? currentShift = null;
                if (partner.CurrentShiftId.HasValue)
                {
                    currentShift = await _context.DeliveryShifts
                        .FirstOrDefaultAsync(s => s.ShiftId == partner.CurrentShiftId.Value);
                }

                // Calculate online duration
                string onlineDuration = "00:00:00";
                if (currentShift != null)
                {
                    if (currentShift.EndTimeUtc.HasValue)
                    {
                        onlineDuration = FormatDuration(currentShift.EndTimeUtc.Value - currentShift.StartTimeUtc);
                    }
                    else
                    {
                        onlineDuration = FormatDuration(DateTime.UtcNow - currentShift.StartTimeUtc);
                    }
                }

                var status = new OnlineStatusDto
                {
                    PartnerId = partnerId,
                    IsOnline = partner.IsOnline,
                    CurrentShiftId = partner.CurrentShiftId,
                    CurrentShiftStart = currentShift?.StartTimeUtc,
                    OnlineDuration = onlineDuration
                };

                return Ok(status);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetOnlineStatus ERROR: {ex.Message}");
                return StatusCode(500, new
                {
                    message = "Failed to get online status",
                    error = ex.Message
                });
            }
        }

        // GET: api/delivery/shift/earnings-report/{partnerId}
        [HttpGet("earnings-report/{partnerId}")]
        public async Task<ActionResult<EarningsReportDto>> GetEarningsReport(
            long partnerId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                var earnings = await _context.OrderEarnings
                    .Where(e => e.PartnerId == partnerId &&
                               e.DeliveredAtUtc >= start &&
                               e.DeliveredAtUtc <= end)
                    .ToListAsync();

                var shifts = await _context.DeliveryShifts
                    .Where(s => s.PartnerId == partnerId &&
                               s.StartTimeUtc >= start &&
                               s.StartTimeUtc <= end)
                    .ToListAsync();

                var totalLoginHours = CalculateTotalLoginHours(shifts);
                var totalEarnings = earnings.Sum(e => e.EarnAmount);
                var deliveryEarnings = totalEarnings;
                var incentiveEarnings = 0m;

                var report = new EarningsReportDto
                {
                    PartnerId = partnerId,
                    ReportDate = DateTime.UtcNow,
                    TotalEarnings = totalEarnings,
                    DeliveryEarnings = deliveryEarnings,
                    IncentiveEarnings = incentiveEarnings,
                    TotalDeliveries = earnings.Count,
                    TotalLoginHours = totalLoginHours.HasValue ? FormatDuration(totalLoginHours.Value) : "00:00:00",
                    TotalShifts = shifts.Count,
                    AverageEarningsPerDelivery = earnings.Count > 0 ? totalEarnings / earnings.Count : 0,
                    AverageEarningsPerHour = totalLoginHours?.TotalHours > 0 ?
                        (decimal)(totalEarnings / (decimal)totalLoginHours.Value.TotalHours) : 0
                };

                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to generate earnings report",
                    error = ex.Message
                });
            }
        }

        // Helper Methods
        private async Task<(decimal TotalEarnings, int DeliveriesCompleted)> GetShiftStatistics(long shiftId)
        {
            var shift = await _context.DeliveryShifts.FindAsync(shiftId);
            if (shift == null) return (0, 0);

            var earnings = await _context.OrderEarnings
                .Where(e => e.PartnerId == shift.PartnerId &&
                           e.DeliveredAtUtc >= shift.StartTimeUtc &&
                           (shift.EndTimeUtc == null || e.DeliveredAtUtc <= shift.EndTimeUtc))
                .ToListAsync();

            return (earnings.Sum(e => e.EarnAmount), earnings.Count);
        }

        private async Task<List<IncentiveDto>> GetAvailableIncentives(long partnerId, int todayDeliveries)
        {
            var today = DateTime.UtcNow.Date;
            var incentiveRules = await _context.IncentiveRules
                .Where(r => r.IsActive && r.EffectiveDate <= today)
                .OrderBy(r => r.MinOrders)
                .ToListAsync();

            var incentives = new List<IncentiveDto>();

            foreach (var rule in incentiveRules)
            {
                var ordersNeeded = Math.Max(0, rule.MinOrders - todayDeliveries);
                var progressPercentage = todayDeliveries >= rule.MinOrders ? 100 :
                    (decimal)todayDeliveries / rule.MinOrders * 100;

                incentives.Add(new IncentiveDto
                {
                    RuleId = rule.RuleId,
                    MinOrders = rule.MinOrders,
                    IncentiveAmount = rule.IncentiveAmount,
                    City = rule.City,
                    CurrentOrders = todayDeliveries,
                    OrdersNeeded = ordersNeeded,
                    IsEligible = todayDeliveries >= rule.MinOrders,
                    ProgressPercentage = Math.Min(100, progressPercentage)
                });
            }

            return incentives;
        }

        private async Task ProcessShiftIncentives(long partnerId, int deliveriesCompleted)
        {
            var today = DateTime.UtcNow.Date;
            var incentiveRules = await _context.IncentiveRules
                .Where(r => r.IsActive && r.EffectiveDate <= today && deliveriesCompleted >= r.MinOrders)
                .ToListAsync();

            foreach (var rule in incentiveRules)
            {
                System.Console.WriteLine($"Partner {partnerId} earned incentive: ₹{rule.IncentiveAmount} for {deliveriesCompleted} deliveries");
            }
        }

        private TimeSpan? CalculateTotalLoginHours(List<DeliveryShift> shifts)
        {
            if (!shifts.Any()) return TimeSpan.Zero;

            var totalMinutes = 0.0;
            foreach (var shift in shifts)
            {
                if (shift.EndTimeUtc.HasValue)
                {
                    // Completed shift
                    totalMinutes += (shift.EndTimeUtc.Value - shift.StartTimeUtc).TotalMinutes;
                }
                else
                {
                    // Active shift
                    totalMinutes += (DateTime.UtcNow - shift.StartTimeUtc).TotalMinutes;
                }
            }

            return TimeSpan.FromMinutes(totalMinutes);
        }

        private string FormatDuration(TimeSpan duration)
        {
            var totalHours = (int)duration.TotalHours;
            var minutes = duration.Minutes;
            var seconds = duration.Seconds;
            return $"{totalHours:D2}:{minutes:D2}:{seconds:D2}";
        }

        private DateTime GetStartOfWeek(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        private async Task<string> GetLocationName(decimal? lat, decimal? lng)
        {
            if (lat == null || lng == null) return "Location not available";
            return $"{lat:F4}, {lng:F4}";
        }
    }
}



public class StartShiftRequestDto
{
    [Required]
    public long PartnerId { get; set; }

    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
    public decimal? StartLatitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
    public decimal? StartLongitude { get; set; }
}

public class EndShiftRequestDto
{
    [Required]
    public long PartnerId { get; set; }

    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
    public decimal? EndLatitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
    public decimal? EndLongitude { get; set; }
}

public class ShiftResponseDto
{
    public long ShiftId { get; set; }
    public long PartnerId { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal? StartLatitude { get; set; }
    public decimal? StartLongitude { get; set; }
    public decimal? EndLatitude { get; set; }
    public decimal? EndLongitude { get; set; }
    public string Duration { get; set; } = "";
    public bool? IsActive { get; set; }
    public decimal? TotalEarnings { get; set; } = 0;
    public int? DeliveriesCompleted { get; set; } = 0;
}

public class ShiftStatsDto
{
    public long PartnerId { get; set; }
    public bool IsCurrentlyOnline { get; set; }
    public long? CurrentShiftId { get; set; }

    // Today's stats
    public string TodayLoginHours { get; set; } = "00:00:00";
    public decimal TodayEarnings { get; set; }
    public int TodayDeliveries { get; set; }

    // Current shift stats
    public DateTime? CurrentShiftStart { get; set; }
    public string CurrentShiftDuration { get; set; } = "00:00:00";
    public decimal CurrentShiftEarnings { get; set; }
    public int CurrentShiftDeliveries { get; set; }

    // This week stats
    public string WeeklyLoginHours { get; set; } = "00:00:00";
    public decimal WeeklyEarnings { get; set; }
    public int WeeklyDeliveries { get; set; }

    // This month stats
    public string MonthlyLoginHours { get; set; } = "00:00:00";
    public decimal MonthlyEarnings { get; set; }
    public int MonthlyDeliveries { get; set; }

    // Available incentives
    public List<IncentiveDto> AvailableIncentives { get; set; } = new();
}

public class IncentiveDto
{
    public long RuleId { get; set; }
    public int MinOrders { get; set; }
    public decimal IncentiveAmount { get; set; }
    public string? City { get; set; }
    public int CurrentOrders { get; set; }
    public int OrdersNeeded { get; set; }
    public bool IsEligible { get; set; }
    public decimal ProgressPercentage { get; set; }
}

public class ShiftHistoryDto
{
    public long ShiftId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Duration { get; set; } = "";
    public decimal Earnings { get; set; }
    public int DeliveriesCompleted { get; set; }
    public string StartLocation { get; set; } = "";
    public string EndLocation { get; set; } = "";
    public List<IncentiveEarnedDto> IncentivesEarned { get; set; } = new();
}

public class IncentiveEarnedDto
{
    public long RuleId { get; set; }
    public decimal Amount { get; set; }
    public int OrdersCompleted { get; set; }
    public string Description { get; set; } = "";
}

public class OnlineStatusDto
{
    public long PartnerId { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastStatusUpdate { get; set; }
    public long? CurrentShiftId { get; set; }
    public DateTime? CurrentShiftStart { get; set; }
    public string OnlineDuration { get; set; } = "00:00:00";
}

public class EarningsReportDto
{
    public long PartnerId { get; set; }
    public DateTime ReportDate { get; set; }
    public decimal TotalEarnings { get; set; }
    public decimal DeliveryEarnings { get; set; }
    public decimal IncentiveEarnings { get; set; }
    public int TotalDeliveries { get; set; }
    public string TotalLoginHours { get; set; } = "00:00:00";
    public int TotalShifts { get; set; }
    public decimal AverageEarningsPerDelivery { get; set; }
    public decimal AverageEarningsPerHour { get; set; }
}