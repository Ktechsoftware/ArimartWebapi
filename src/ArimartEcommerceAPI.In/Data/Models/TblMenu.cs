using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class TblMenu
{
    public long MenuId { get; set; }

    public long? ParentId { get; set; }

    public string? MenuName { get; set; }

    public string? MenuLink { get; set; }

    public int? Position { get; set; }

    public bool? IsDelete { get; set; }

    public bool IsActive { get; set; }

    public bool IsRights { get; set; }

    public DateTime? CreateTime { get; set; }

    public DateTime? ModifyTime { get; set; }

    public bool? IsAll { get; set; }

    public bool IsCrm { get; set; }
}
