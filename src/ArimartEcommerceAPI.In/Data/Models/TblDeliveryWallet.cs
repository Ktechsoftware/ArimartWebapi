using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models
{
    public class TblDeliveryWallet
    {
        public long Id { get; set; }
        public long DeliveryPartnerId { get; set; }
        public decimal? Balance { get; set; }
        public decimal? WeeklyEarnings { get; set; }
        public decimal? MonthlyEarnings { get; set; }
        public decimal? TotalEarnings { get; set; }
        public decimal? TotalReferralEarnings { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}
