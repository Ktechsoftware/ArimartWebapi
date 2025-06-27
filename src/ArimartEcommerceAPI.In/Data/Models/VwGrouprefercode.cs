using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class VwGrouprefercode
{
    public long Id { get; set; }

    public long? Pid { get; set; }

    public long? Pdid { get; set; }

    public string Refercode { get; set; } = null!;
}
