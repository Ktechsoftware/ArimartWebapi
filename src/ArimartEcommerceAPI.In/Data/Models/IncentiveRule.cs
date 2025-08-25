using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models
{
    public class IncentiveRule
    {
        public long RuleId { get; set; }
        public DateTime EffectiveDate { get; set; }
        public string? City { get; set; }
        public int MinOrders { get; set; }
        public decimal IncentiveAmount { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
