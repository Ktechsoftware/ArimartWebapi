using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models
{
    public class FcmDeviceToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? Token { get; set; }
        public string? DeviceType { get; set; } // e.g., "android", "ios", "web"
        public DateTime CreatedAt { get; set; }
    }
    
}
