using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class TblProductdetail
{
    public long Id { get; set; }

    public string? Colorcode { get; set; }

    public string? Price { get; set; }

    public string? Discountprice { get; set; }

    public string? StockQty { get; set; }

    public long? Productid { get; set; }

    public DateTime AddedDate { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public bool? IsActive { get; set; }

    public int? Userid { get; set; }

    public string? Gst { get; set; }

    public string? Totalprice { get; set; }

    public string? Netprice { get; set; }

    public string? Gqty { get; set; }

    public string? Gprice { get; set; }

    public string? Wtype { get; set; }

    public string? Wweight { get; set; }

    public string? Hsncode { get; set; }
}
