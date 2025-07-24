using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models
{
    public class TblPromocode
    {
        [Key]
        public int PromoId { get; set; }

        [Required]
        [MaxLength(50)]
        public string? Code { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(20)]
        public string? DiscountType { get; set; } // "PERCENTAGE" or "FLAT"

        [Required]
        public decimal DiscountValue { get; set; }

        public decimal MinOrderValue { get; set; }

        public decimal? MaxDiscount { get; set; }

        public int UsageLimit { get; set; }

        public int PerUserLimit { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public DateTime AddedDate { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public bool? IsActive { get; set; }
        public string? RewardType { get; set; } = "PROMO_CODE";
        public string? Title { get; set; }


    }
}
