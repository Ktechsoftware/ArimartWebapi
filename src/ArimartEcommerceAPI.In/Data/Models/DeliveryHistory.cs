using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models
{
    public class DeliveryHistory
    {
        public int Id { get; set; }
        public int DeliveryPartnerId { get; set; }
        public string? TrackId { get; set; }
        public DateTime PickupTime { get; set; }
        public DateTime? DeliveryTime { get; set; }
        public string? Status { get; set; } // picked_up, in_transit, delivered, failed
        public double? DeliveryRating { get; set; }
        public string? CustomerFeedback { get; set; }
        public decimal DeliveryFee { get; set; }
    }
}
