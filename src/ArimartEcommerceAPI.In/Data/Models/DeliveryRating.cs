using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models
{
    public class DeliveryRating
    {
        public long Id { get; set; }
        public long DeliveryBoyId { get; set; }   // Who is being rated
        public long CustomerId { get; set; }      // Who gave the rating
        public long? OrderId { get; set; }        // Optional: Link rating to an order
        public decimal Rating { get; set; }       // 0 to 5 scale
        public string? Feedback { get; set; }     // Optional written feedback
        public DateTime CreatedAt { get; set; }   // Defaults to GETDATE() in DB
    }

}
