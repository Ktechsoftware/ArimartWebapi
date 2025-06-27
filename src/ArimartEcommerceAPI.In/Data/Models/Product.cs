using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class Product
{
    public int PId { get; set; }

    public string PName { get; set; } = null!;

    public string PDesc { get; set; } = null!;

    public string PPros { get; set; } = null!;

    public short IsAvl { get; set; }

    public string PMeasurement { get; set; } = null!;

    public int CsId { get; set; }

    public double MarketPrice { get; set; }

    public double SellingPrice { get; set; }

    public string PWeightUnit { get; set; } = null!;

    public string BrandName { get; set; } = null!;

    public int UId { get; set; }

    public DateTime AtDate { get; set; }

    public short Oos { get; set; }

    public short Top { get; set; }
}
