using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models
{
    public class DeliveryShift
    {
        public long ShiftId { get; set; }
        public long PartnerId { get; set; }
        public DateTime StartTimeUtc { get; set; }
        public DateTime? EndTimeUtc { get; set; }
        public decimal? StartLat { get; set; }
        public decimal? StartLng { get; set; }
        public decimal? EndLat { get; set; }
        public decimal? EndLng { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Calculated properties
        public TimeSpan? Duration => EndTimeUtc.HasValue ?
    EndTimeUtc.Value - StartTimeUtc :
    DateTime.UtcNow - StartTimeUtc;  // Current duration for active shifts
        public bool? IsActive => !EndTimeUtc.HasValue;

        // Navigation
        public virtual TblDeliveryuser? DeliveryPartner { get; set; }
    }
}
