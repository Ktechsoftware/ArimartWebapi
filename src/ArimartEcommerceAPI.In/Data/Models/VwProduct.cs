using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class VwProduct
{
    public long Id { get; set; }

    public string ProductName { get; set; } = null!;

    public string? Shortdesc { get; set; }

    public string? Longdesc { get; set; }

    public string? Keywords { get; set; }

    public int? Categoryid { get; set; }
    public string? Unit { get; set; }

    public int? Subcategoryid { get; set; }
    public int? ChildCategoryId { get; set; }

    public DateTime AddedDate { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public bool? IsActive { get; set; }

    public int? Userid { get; set; }

    public string? PPros { get; set; }
    public string? SpecialTags { get; set; }

    public long? Pdid { get; set; }

    public string? CategoryName { get; set; }

    public string? SubcategoryName { get; set; }
    public string? ChildCategoryName { get; set; }

    public string? Image { get; set; }

    public string? Discountprice { get; set; }

    public string? Price { get; set; }

    public string? Netprice { get; set; }

    public string? Totalprice { get; set; }

    public string? Gqty { get; set; }

    public string? Gprice { get; set; }

    public string? Gst { get; set; }

    public string? Wtype { get; set; }

    public string? Wweight { get; set; }

    public string? VendorName { get; set; }

    public string? CompanyName { get; set; }

    public string? ProductNameShort { get; set; }
    public string? GroupCode { get; set; }
}
