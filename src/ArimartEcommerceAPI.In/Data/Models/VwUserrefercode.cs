using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class VwUserrefercode
{
    public long Id { get; set; }

    public string? VendorName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string Refercode { get; set; } = null!;
}
