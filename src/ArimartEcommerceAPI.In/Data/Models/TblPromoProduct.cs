using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models
{
    public class TblPromoProduct
    {
        public int Id { get; set; }
        public int PromoId { get; set; }
        public int ProductId { get; set; }
    }
}
