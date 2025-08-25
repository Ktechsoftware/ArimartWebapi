using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models
{
    public class Affiliate
    {
        public int AffiliateID { get; set; }
        public int UserID { get; set; }
        public string? Status { get; set; } // Pending, Approved, Rejected, Suspended
        public string? ReferralCode { get; set; }
        public DateTime ApplicationDate { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal PendingEarnings { get; set; }
        public string? BankDetails { get; set; } // JSON string
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation property
        public virtual ICollection<AffiliateReferral> AffiliateReferrals { get; set; }
    }
}
