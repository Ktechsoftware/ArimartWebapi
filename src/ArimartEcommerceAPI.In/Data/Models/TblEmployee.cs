using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class TblEmployee
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string? EmpCode { get; set; }

    public string Address { get; set; } = null!;

    public string PhoneNo { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public DateTime AddedDate { get; set; }

    public bool IsDeleted { get; set; }

    public string? Type { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string? DepId { get; set; }

    public string? Roles { get; set; }

    public long? RepMgrId { get; set; }

    public string? AssetId { get; set; }

    public string? FrechiseMgrId { get; set; }

    public bool? IsActive { get; set; }
}
