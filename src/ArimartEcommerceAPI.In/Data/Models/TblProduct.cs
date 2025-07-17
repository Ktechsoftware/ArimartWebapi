using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class TblProduct
{
    public long Id { get; set; }

    public string ProductName { get; set; } = null!;

    public string? Shortdesc { get; set; }

    public string? Longdesc { get; set; }

    public string? Keywords { get; set; }

    public int? Categoryid { get; set; }

    public int? Subcategoryid { get; set; }
    public int? childcategoryid { get; set; }

    public DateTime AddedDate { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public bool? IsActive { get; set; }

    public int? Userid { get; set; }

    public string? PPros { get; set; }
    public string? SpecialTags { get; set; }
}
