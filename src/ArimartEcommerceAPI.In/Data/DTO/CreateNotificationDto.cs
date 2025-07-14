using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArimartEcommerceAPI.Infrastructure.Data.DTO
{
    public class CreateNotificationDto
    {
        public long UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Urlt { get; set; } = null!;
        public string? Message { get; set; }
        public int? Sipid { get; set; }
    }
}
