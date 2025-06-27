using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class ProductsImg
{
    public int PiId { get; set; }

    public string IName { get; set; } = null!;

    public int PId { get; set; }
}
