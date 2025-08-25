using System.ComponentModel.DataAnnotations;
using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArimartEcommerceAPI.API.Contollers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeliveryReferralController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DeliveryReferralController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("my-deliverrefcode/{partnerId}")]
        public async Task<IActionResult> GetMyReferralCode(long partnerId)
        {
            var refEntry = await _context.VwDeliverrefercodes
                .FirstOrDefaultAsync(v => v.Id == partnerId);

            if (refEntry == null)
                return NotFound(new { message = "Referral code not found." });

            return Ok(new
            {
                userId = refEntry.Id,
                name = refEntry.ContactPerson,
                phone = refEntry.Phone,
                referCode = refEntry.DeliverRefercode
            });
        }

        // GET: api/referral/stats/{partnerId}
        [HttpGet("stats/{partnerId}")]
        public async Task<ActionResult<ReferralStatsDto>> GetReferralStats(int partnerId)
        {
            var partner = await _context.VwDeliverrefercodes
                .FirstOrDefaultAsync(v => v.Id == partnerId);

            if (partner == null)
            {
                return NotFound("Partner not found");
            }

            var totalReferred = await _context.DeliveryReferrals
                .CountAsync(r => r.ReferrerId == partnerId);

            var totalEarned = await _context.TblDeliveryTransactions
                .Where(t => t.DeliveryPartnerId == partnerId &&
                           t.Type == TransactionType.Credit &&
                           t.Status == TransactionStatus.Completed &&
                           t.ReferralId != null)
                .SumAsync(t => t.Amount);

            var pendingRewards = await _context.DeliveryReferrals
                .Where(r => r.ReferrerId == partnerId &&
                           r.Status == "Pending")
                .SumAsync(r => r.ReferrerReward);

            return Ok(new ReferralStatsDto
            {
                TotalReferred = totalReferred,
                TotalEarned = totalEarned,
                PendingRewards = pendingRewards,
                ReferralCode = partner.DeliverRefercode
            });
        }


        // POST: api/referral/delivery-completed/{partnerId}
        [HttpPost("delivery-completed/{partnerId}")]
        public async Task<IActionResult> DeliveryCompleted(int partnerId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Find active referral where this partner is the referee
                var referral = await _context.DeliveryReferrals
                    .Include(r => r.Referrer)
                    .FirstOrDefaultAsync(r => r.RefereeId == partnerId && r.Status == "Pending");

                if (referral == null)
                {
                    return Ok("No active referral found");
                }

                // Increment completed deliveries
                referral.CompletedDeliveries++;
                referral.UpdatedAt = DateTime.UtcNow;

                // Check if referral is complete
                if (referral.CompletedDeliveries >= referral.RequiredDeliveries)
                {
                    referral.Status = "Completed";
                    referral.CompletedAt = DateTime.UtcNow;

                    // Pay referrer bonus
                    if (!referral.IsReferrerPaid)
                    {
                        await PayReferralBonus(referral.ReferrerId, referral.ReferrerReward, referral.Id, "Referral Bonus", "Referral completed successfully");
                        referral.IsReferrerPaid = true;
                    }

                    // Pay referee bonus
                    if (!referral.IsRefereePaid)
                    {
                        await PayReferralBonus(referral.RefereeId, referral.RefereeReward, referral.Id, "Welcome Bonus", "Bonus for completing referral requirements");
                        referral.IsRefereePaid = true;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok($"Delivery completed. Progress: {referral.CompletedDeliveries}/{referral.RequiredDeliveries}");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task PayReferralBonus(long partnerId, decimal amount, long referralId, string title, string description)
        {
            // Find or create wallet
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
                    TotalReferralEarnings = 0
                };
                _context.TblDeliveryWallets.Add(wallet);
                await _context.SaveChangesAsync();
            }

            // Create transaction
            var transaction = new TblDeliveryTransaction
            {
                DeliveryPartnerId = partnerId,
                Title = title,
                Description = description,
                Amount = amount,
                Type = TransactionType.Credit,
                Status = TransactionStatus.Completed,
                ReferralId = referralId,
                ReferenceNumber = $"REF{DateTime.Now:yyyyMMddHHmmss}{partnerId}",
                CompletedAt = DateTime.UtcNow
            };

            _context.TblDeliveryTransactions.Add(transaction);

            // Update wallet
            wallet.Balance += amount;
            wallet.TotalEarnings += amount;
            wallet.TotalReferralEarnings += amount;
            wallet.LastUpdated = DateTime.UtcNow;
        }
    }

    public class ReferralStatsDto
    {
        public int TotalReferred { get; set; }
        public decimal TotalEarned { get; set; }
        public decimal PendingRewards { get; set; }
        public string ReferralCode { get; set; }
    }

    public class ReferralDto
    {
        public int Id { get; set; }
        public string RefereeName { get; set; }
        public string RefereePhone { get; set; }
        public string Status { get; set; }
        public int CompletedDeliveries { get; set; }
        public int RequiredDeliveries { get; set; }
        public decimal ReferrerReward { get; set; }
        public bool IsReferrerPaid { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class DepositRequestDto
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }
    }

    public class RegisterWithReferralDto
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        public string Address { get; set; }

        public string ReferralCode { get; set; }
    }
}