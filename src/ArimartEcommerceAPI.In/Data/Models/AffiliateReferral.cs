using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models
{
    public class AffiliateReferral
    {
        public int ReferralID { get; set; }
        public int AffiliateID { get; set; }
        public int ReferredUserID { get; set; }
        public DateTime InstallDate { get; set; }
        public DateTime? ConversionDate { get; set; }
        public decimal CommissionAmount { get; set; }
        public string? Status { get; set; } // Pending, Confirmed, Paid
        public DateTime CreatedAt { get; set; }

        // Navigation property
        public virtual Affiliate? Affiliate { get; set; }
    }
}
