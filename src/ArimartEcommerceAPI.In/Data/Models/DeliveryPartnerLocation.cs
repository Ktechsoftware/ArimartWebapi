using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models
{
    public class DeliveryPartnerLocation
    {
        public int Id { get; set; }
        public int DeliveryPartnerId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsOnline { get; set; }
    }
}
