using ArimartEcommerceAPI.Infrastructure.Data.Models;
using ArimartEcommerceAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArimartEcommerceAPI.API.Contollers
{
    // Controllers/AffiliateController.cs
    [ApiController]
    [Route("api/[controller]")]
    public class AffiliateController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AffiliateController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/affiliate/status/{userId}
        [HttpGet("status/{userId}")]
        public async Task<ActionResult<AffiliateDashboardDto>> GetAffiliateStatus(int userId)
        {
            var affiliate = await _context.Affiliates
                .Include(a => a.AffiliateReferrals)
                .FirstOrDefaultAsync(a => a.UserID == userId);

            if (affiliate == null)
            {
                return Ok(new { IsAffiliate = false });
            }

            var dashboard = new AffiliateDashboardDto
            {
                Status = affiliate.Status,
                ReferralCode = affiliate.ReferralCode,
                ReferralLink = !string.IsNullOrEmpty(affiliate.ReferralCode) ?
                              $"https://yourapp.com/download?ref={affiliate.ReferralCode}" : null,
                TotalEarnings = affiliate.TotalEarnings,
                PendingEarnings = affiliate.PendingEarnings,
                TotalReferrals = affiliate.AffiliateReferrals.Count,
                PendingReferrals = affiliate.AffiliateReferrals.Count(r => r.Status == "Pending"),
                ApplicationDate = affiliate.ApplicationDate,
                ApprovalDate = affiliate.ApprovalDate
            };

            return Ok(new { IsAffiliate = true, Data = dashboard });
        }

        // POST: api/affiliate/apply
        [HttpPost("apply")]
        public async Task<ActionResult> ApplyForAffiliate(AffiliateApplicationDto application, int userId)
        {
            var existingAffiliate = await _context.Affiliates
                .FirstOrDefaultAsync(a => a.UserID == userId);

            if (existingAffiliate != null)
            {
                return BadRequest(new { Message = "User has already applied for affiliate program" });
            }

            // Create bank details JSON
            var bankDetails = new
            {
                AccountNumber = application.BankAccountNumber,
                BankName = application.BankName,
                AccountHolderName = application.AccountHolderName
            };

            var affiliate = new Affiliate
            {
                UserID = userId,
                Status = "Pending",
                ApplicationDate = DateTime.UtcNow,
                TotalEarnings = 0,
                PendingEarnings = 0,
                BankDetails = System.Text.Json.JsonSerializer.Serialize(bankDetails),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Affiliates.Add(affiliate);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Application submitted successfully", AffiliateID = affiliate.AffiliateID });
        }

        // GET: api/affiliate/referrals/{userId}
        [HttpGet("referrals/{userId}")]
        public async Task<ActionResult> GetReferrals(int userId)
        {
            var affiliate = await _context.Affiliates
                .FirstOrDefaultAsync(a => a.UserID == userId);

            if (affiliate == null)
            {
                return NotFound(new { Message = "Affiliate not found" });
            }

            var referrals = await _context.AffiliateReferrals
                .Where(r => r.AffiliateID == affiliate.AffiliateID)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.ReferralID,
                    r.InstallDate,
                    r.ConversionDate,
                    r.CommissionAmount,
                    r.Status,
                    r.CreatedAt
                })
                .ToListAsync();

            return Ok(referrals);
        }

        // POST: api/affiliate/track-install
        [HttpPost("track-install")]
        public async Task<ActionResult> TrackInstall([FromBody] TrackInstallDto trackData)
        {
            if (string.IsNullOrEmpty(trackData.ReferralCode))
            {
                return BadRequest(new { Message = "Referral code is required" });
            }

            // Find affiliate by referral code
            var affiliate = await _context.Affiliates
                .FirstOrDefaultAsync(a => a.ReferralCode == trackData.ReferralCode && a.Status == "Approved");

            if (affiliate == null)
            {
                return NotFound(new { Message = "Invalid referral code" });
            }

            // Check if this user was already referred
            var existingReferral = await _context.AffiliateReferrals
                .FirstOrDefaultAsync(r => r.ReferredUserID == trackData.NewUserId);

            if (existingReferral != null)
            {
                return BadRequest(new { Message = "User already tracked" });
            }

            // Create new referral record
            var referral = new AffiliateReferral
            {
                AffiliateID = affiliate.AffiliateID,
                ReferredUserID = trackData.NewUserId,
                InstallDate = DateTime.UtcNow,
                CommissionAmount = 10.00m, // Set your commission amount
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.AffiliateReferrals.Add(referral);

            // Update affiliate pending earnings
            affiliate.PendingEarnings += referral.CommissionAmount;
            affiliate.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Install tracked successfully" });
        }
    }

    // DTO for tracking installs
    public class TrackInstallDto
    {
        public string? ReferralCode { get; set; }
        public int NewUserId { get; set; }
    }
}


// DTOs/AffiliateApplicationDto.cs
public class AffiliateApplicationDto
{
    public string? SocialMediaHandles { get; set; }
    public string? AudienceDescription { get; set; }
    public string? WhyJoin { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankName { get; set; }
    public string? AccountHolderName { get; set; }
    public bool AcceptTerms { get; set; }
}

// DTOs/AffiliateDashboardDto.cs
public class AffiliateDashboardDto
{
    public string? Status { get; set; }
    public string? ReferralCode { get; set; }
    public string? ReferralLink { get; set; }
    public decimal TotalEarnings { get; set; }
    public decimal PendingEarnings { get; set; }
    public int TotalReferrals { get; set; }
    public int PendingReferrals { get; set; }
    public DateTime? ApplicationDate { get; set; }
    public DateTime? ApprovalDate { get; set; }
}