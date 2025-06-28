using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArimartEcommerceAPI.Infrastructure.Data.DTO
{
    public class AddToWishlistRequest
    {
        public long Userid { get; set; }
        public long Pdid { get; set; }
    }
}
