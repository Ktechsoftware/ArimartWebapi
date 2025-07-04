using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class TblChildSubcategory
{
    public long Id { get; set; }

    public string ChildcategoryName { get; set; } = null!;

    public int? Categoryid { get; set; }
    public int? Subcategoryid { get; set; }

    public string? Image { get; set; }

    public DateTime AddedDate { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public bool? IsActive { get; set; }
}
