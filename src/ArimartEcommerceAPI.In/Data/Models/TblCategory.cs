using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class TblCategory
{
    public long Id { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Image { get; set; }
    public string? IconLabel { get; set; }

    public DateTime AddedDate { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public bool? IsActive { get; set; }
}
