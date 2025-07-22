using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models
{
    public class TblPromocodeUsage
    {
        [Key]
        public int UsageId { get; set; }

        [Required]
        public int PromoId { get; set; }

        public int UserId { get; set; }

        public DateTime UsedAt { get; set; } = DateTime.Now;
    }
}
