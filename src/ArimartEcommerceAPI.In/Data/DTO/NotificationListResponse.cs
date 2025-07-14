using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArimartEcommerceAPI.Infrastructure.Data.DTO
{
    public class NotificationListResponse
    {
        public List<NotificationDto> Notifications { get; set; } = new List<NotificationDto>();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public bool HasMore { get; set; }
    }
}
