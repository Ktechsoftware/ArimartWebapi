using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class TblOrder
{
    public long Id { get; set; }

    public string Productid { get; set; } = null!;

    public decimal? Productdetails { get; set; }

    public int? Orderqty { get; set; }

    public DateTime AddedDate { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public bool? IsActive { get; set; }

    public int? Userid { get; set; }
}
