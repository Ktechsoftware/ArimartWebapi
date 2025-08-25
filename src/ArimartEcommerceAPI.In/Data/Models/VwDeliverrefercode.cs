using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class VwDeliverrefercode
{
    public long Id { get; set; }

    public string? ContactPerson { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string DeliverRefercode { get; set; } = null!;
}
