using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.DTO;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using ArimartEcommerceAPI.Services.Services;
using System.ComponentModel.DataAnnotations;

namespace ArimartEcommerceAPI.API.Controllers
{
    [ApiController]
    [Route("api/delivery/[controller]")]
    public class IncentiveController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IFcmPushService _fcmPushService;

        public IncentiveController(ApplicationDbContext context, IFcmPushService fcmPushService)
        {
            _context = context;
            _fcmPushService = fcmPushService;
        }

        // POST: api/delivery/incentive/rules
        [HttpPost("rules")]
        public async Task<ActionResult<IncentiveRule>> CreateIncentiveRule([FromBody] CreateIncentiveRuleDto request)
        {
            try
            {
                var rule = new IncentiveRule
                {
                    EffectiveDate = request.EffectiveDate.Date,
                    City = request.City?.Trim(),
                    MinOrders = request.MinOrders,
                    IncentiveAmount = request.IncentiveAmount,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.IncentiveRules.Add(rule);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetIncentiveRule), new { id = rule.RuleId }, rule);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to create incentive rule",
                    error = ex.Message
                });
            }
        }

        // GET: api/delivery/incentive/rules/{id}
        [HttpGet("rules/{id}")]
        public async Task<ActionResult<IncentiveRule>> GetIncentiveRule(long id)
        {
            var rule = await _context.IncentiveRules.FindAsync(id);

            if (rule == null)
                return NotFound($"Incentive rule with ID {id} not found.");

            return Ok(rule);
        }

        // GET: api/delivery/incentive/rules
        [HttpGet("rules")]
        public async Task<ActionResult<List<IncentiveRule>>> GetAllIncentiveRules(
            [FromQuery] bool activeOnly = true,
            [FromQuery] string? city = null)
        {
            var query = _context.IncentiveRules.AsQueryable();

            if (activeOnly)
                query = query.Where(r => r.IsActive);

            if (!string.IsNullOrWhiteSpace(city))
                query = query.Where(r => r.City == null || r.City.ToLower() == city.ToLower());

            var rules = await query
                .OrderBy(r => r.MinOrders)
                .ThenByDescending(r => r.EffectiveDate)
                .ToListAsync();

            return Ok(rules);
        }

        // PUT: api/delivery/incentive/rules/{id}
        [HttpPut("rules/{id}")]
        public async Task<ActionResult> UpdateIncentiveRule(long id, [FromBody] CreateIncentiveRuleDto request)
        {
            try
            {
                var rule = await _context.IncentiveRules.FindAsync(id);
                if (rule == null)
                    return NotFound($"Incentive rule with ID {id} not found.");

                rule.EffectiveDate = request.EffectiveDate.Date;
                rule.City = request.City?.Trim();
                rule.MinOrders = request.MinOrders;
                rule.IncentiveAmount = request.IncentiveAmount;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Incentive rule updated successfully.", rule });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to update incentive rule",
                    error = ex.Message
                });
            }
        }

        // DELETE: api/delivery/incentive/rules/{id}
        [HttpDelete("rules/{id}")]
        public async Task<ActionResult> DeleteIncentiveRule(long id)
        {
            try
            {
                var rule = await _context.IncentiveRules.FindAsync(id);
                if (rule == null)
                    return NotFound($"Incentive rule with ID {id} not found.");

                rule.IsActive = false; // Soft delete
                await _context.SaveChangesAsync();

                return Ok(new { message = "Incentive rule deactivated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to delete incentive rule",
                    error = ex.Message
                });
            }
        }

        // GET: api/delivery/incentive/available/{partnerId}
        [HttpGet("available/{partnerId}")]
        public async Task<ActionResult<List<IncentiveDto>>> GetAvailableIncentives(long partnerId)
        {
            try
            {
                var today = DateTime.UtcNow.Date;

                // Get partner's deliveries today
                var todayDeliveries = await _context.OrderEarnings
                    .CountAsync(e => e.PartnerId == partnerId && e.DeliveredAtUtc.Date == today);

                // Get active incentive rules
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

                return Ok(incentives);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to get available incentives",
                    error = ex.Message
                });
            }
        }

        // POST: api/delivery/incentive/calculate/{partnerId}
        [HttpPost("calculate/{partnerId}")]
        public async Task<ActionResult<decimal>> CalculateDailyIncentives(long partnerId, [FromQuery] DateTime? date = null)
        {
            try
            {
                var targetDate = date?.Date ?? DateTime.UtcNow.Date;

                // Get deliveries for the specified date
                var deliveries = await _context.OrderEarnings
                    .CountAsync(e => e.PartnerId == partnerId && e.DeliveredAtUtc.Date == targetDate);

                // Get applicable incentive rules
                var eligibleRules = await _context.IncentiveRules
                    .Where(r => r.IsActive &&
                               r.EffectiveDate <= targetDate &&
                               deliveries >= r.MinOrders)
                    .OrderByDescending(r => r.IncentiveAmount) // Get highest incentive
                    .FirstOrDefaultAsync();

                var incentiveAmount = eligibleRules?.IncentiveAmount ?? 0;

                return Ok(new
                {
                    partnerId,
                    date = targetDate,
                    deliveriesCompleted = deliveries,
                    incentiveAmount,
                    ruleApplied = eligibleRules != null ? new
                    {
                        eligibleRules.RuleId,
                        eligibleRules.MinOrders,
                        eligibleRules.IncentiveAmount,
                        eligibleRules.City
                    } : null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to calculate incentives",
                    error = ex.Message
                });
            }
        }

        // POST: api/delivery/incentive/process-daily
        [HttpPost("process-daily")]
        public async Task<ActionResult> ProcessDailyIncentives([FromQuery] DateTime? date = null)
        {
            try
            {
                var targetDate = date?.Date ?? DateTime.UtcNow.Date.AddDays(-1); // Yesterday by default

                // Get all active partners who made deliveries on target date
                var partnersWithDeliveries = await _context.OrderEarnings
                    .Where(e => e.DeliveredAtUtc.Date == targetDate)
                    .GroupBy(e => e.PartnerId)
                    .Select(g => new { PartnerId = g.Key, DeliveryCount = g.Count() })
                    .ToListAsync();

                var processedCount = 0;
                var totalIncentivesPaid = 0m;

                foreach (var partner in partnersWithDeliveries)
                {
                    // Find best applicable incentive rule
                    var bestRule = await _context.IncentiveRules
                        .Where(r => r.IsActive &&
                                   r.EffectiveDate <= targetDate &&
                                   partner.DeliveryCount >= r.MinOrders)
                        .OrderByDescending(r => r.IncentiveAmount)
                        .FirstOrDefaultAsync();

                    if (bestRule != null)
                    {
                        // Add to wallet (assuming you have a wallet system)
                        await AddIncentiveToWallet(partner.PartnerId, bestRule.IncentiveAmount,
                            $"Daily incentive for {partner.DeliveryCount} deliveries", bestRule.RuleId);

                        // Send notification
                        await SendIncentiveNotification(partner.PartnerId, bestRule.IncentiveAmount, partner.DeliveryCount);

                        processedCount++;
                        totalIncentivesPaid += bestRule.IncentiveAmount;
                    }
                }

                return Ok(new
                {
                    message = $"Daily incentives processed successfully for {targetDate:yyyy-MM-dd}",
                    date = targetDate,
                    partnersProcessed = processedCount,
                    totalIncentivesPaid,
                    totalPartners = partnersWithDeliveries.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to process daily incentives",
                    error = ex.Message
                });
            }
        }

        // GET: api/delivery/incentive/leaderboard
        [HttpGet("leaderboard")]
        public async Task<ActionResult> GetIncentiveLeaderboard(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int limit = 10)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.Date.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow.Date;

                var leaderboard = await (
                    from earning in _context.OrderEarnings
                    join partner in _context.TblDeliveryusers on earning.PartnerId equals partner.Id
                    where earning.DeliveredAtUtc >= start && earning.DeliveredAtUtc <= end
                    group earning by new { earning.PartnerId, partner.ContactPerson, partner.Phone } into g
                    select new
                    {
                        PartnerId = g.Key.PartnerId,
                        PartnerName = g.Key.ContactPerson,
                        PartnerPhone = g.Key.Phone,
                        TotalDeliveries = g.Count(),
                        TotalEarnings = g.Sum(e => e.EarnAmount),
                        DailyAverage = g.Count() / (decimal)(end - start).Days,
                        BestDay = g.GroupBy(e => e.DeliveredAtUtc.Date)
                               .OrderByDescending(d => d.Count())
                               .First()
                               .Count()
                    })
                    .OrderByDescending(x => x.TotalDeliveries)
                    .Take(limit)
                    .ToListAsync();

                // Calculate potential incentives for each partner
                var leaderboardWithIncentives = new List<object>();

                foreach (var partner in leaderboard)
                {
                    var potentialIncentive = await CalculatePotentialIncentiveForPeriod(
                        partner.PartnerId, start, end);

                    leaderboardWithIncentives.Add(new
                    {
                        partner.PartnerId,
                        partner.PartnerName,
                        partner.PartnerPhone,
                        partner.TotalDeliveries,
                        partner.TotalEarnings,
                        partner.DailyAverage,
                        partner.BestDay,
                        PotentialIncentive = potentialIncentive,
                        Rank = leaderboard.IndexOf(partner) + 1
                    });
                }

                return Ok(new
                {
                    period = new { startDate = start, endDate = end },
                    leaderboard = leaderboardWithIncentives
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to get incentive leaderboard",
                    error = ex.Message
                });
            }
        }

        // Helper Methods
        private async Task AddIncentiveToWallet(long partnerId, decimal amount, string description, long ruleId)
        {
            // Check if wallet exists, create if not
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
                await _context.SaveChangesAsync(); // Save to get ID
            }

            // Create incentive transaction
            var transaction = new TblDeliveryTransaction
            {
                DeliveryPartnerId = partnerId,
                Title = "Daily Incentive 🎯",
                Description = description,
                Amount = amount,
                Type = TransactionType.Credit,
                Status = TransactionStatus.Completed,
                ReferenceNumber = $"INC{DateTime.Now:yyyyMMddHHmmss}{partnerId}"
            };

            _context.TblDeliveryTransactions.Add(transaction);

            // Update wallet balance
            wallet.Balance = (wallet.Balance ?? 0) + amount;
            wallet.TotalEarnings = (wallet.TotalEarnings ?? 0) + amount;
            wallet.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        private async Task SendIncentiveNotification(long partnerId, decimal amount, int deliveries)
        {
            try
            {
                var partner = await _context.TblDeliveryusers.FindAsync(partnerId);
                if (partner?.Id == null) return;

                var title = "Incentive Earned! 🎉";
                var body = $"Congratulations! You've earned ₹{amount} incentive for completing {deliveries} deliveries today!";

                await _fcmPushService.SendNotificationAsync(partner.Id.ToString(), title, body);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the process
                System.Console.WriteLine($"Failed to send incentive notification: {ex.Message}");
            }
        }

        private async Task<decimal> CalculatePotentialIncentiveForPeriod(long partnerId, DateTime start, DateTime end)
        {
            var dailyDeliveries = await _context.OrderEarnings
                .Where(e => e.PartnerId == partnerId && e.DeliveredAtUtc >= start && e.DeliveredAtUtc <= end)
                .GroupBy(e => e.DeliveredAtUtc.Date)
                .Select(g => g.Count())
                .ToListAsync();

            var totalIncentive = 0m;

            foreach (var dayDeliveries in dailyDeliveries)
            {
                var bestRule = await _context.IncentiveRules
                    .Where(r => r.IsActive && dayDeliveries >= r.MinOrders)
                    .OrderByDescending(r => r.IncentiveAmount)
                    .FirstOrDefaultAsync();

                if (bestRule != null)
                    totalIncentive += bestRule.IncentiveAmount;
            }

            return totalIncentive;
        }
    }
}
public class CreateIncentiveRuleDto
{
    [Required]
    public DateTime EffectiveDate { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Minimum orders must be at least 1")]
    public int MinOrders { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Incentive amount must be greater than 0")]
    public decimal IncentiveAmount { get; set; }
}