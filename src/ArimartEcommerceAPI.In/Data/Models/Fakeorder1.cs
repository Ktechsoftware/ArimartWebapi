using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class Fakeorder1
{
    public int Fid { get; set; }

    public DateTime? Orderdatetime { get; set; }

    public long? Userid { get; set; }

    public long? Cartid { get; set; }

    public string? Type { get; set; }
}
