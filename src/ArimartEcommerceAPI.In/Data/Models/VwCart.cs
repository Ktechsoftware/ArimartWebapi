using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class VwCart
{
    public long? Id { get; set; }

    public string? ProductName { get; set; }

    public string? Shortdesc { get; set; }

    public string? Longdesc { get; set; }

    public string? Keywords { get; set; }

    public int? Categoryid { get; set; }

    public int? Subcategoryid { get; set; }
    public string? categoryName { get; set; }
    public string? SubcategoryName { get; set; }
    public string? ChildcategoryName { get; set; }

    public DateTime? AddedDate { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public bool? IsActive { get; set; }

    public int? Userid { get; set; }

    public string? PPros { get; set; }

    public long? Pid { get; set; }

    public long? Pdid { get; set; }
    public string? Unit { get; set; }
    public string? Image { get; set; }

    public string? Discountprice { get; set; }

    public string? Price { get; set; }

    public string? Netprice { get; set; }

    public string? Totalprice { get; set; }

    public string? Gqty { get; set; }

    public string? Gprice { get; set; }

    public long Aid { get; set; }

    public bool IsDeleted1 { get; set; }

    public int? Cuserid { get; set; }

    public int? Qty { get; set; }

    public long? Groupid { get; set; }

    public string? VendorName { get; set; }

    public string? Phone { get; set; }

    public decimal? Qtyprice { get; set; }
    public string? GroupCode { get; set; }
}
