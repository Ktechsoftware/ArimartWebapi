using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models
{
    public class OrderEarning
    {
        public long Id { get; set; }
        public long PartnerId { get; set; }
        public long OrderId { get; set; }
        public decimal EarnAmount { get; set; }
        public DateTime DeliveredAtUtc { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual TblDeliveryuser? DeliveryPartner { get; set; }
        public virtual TblOrdernow? Order { get; set; }
    }
}
