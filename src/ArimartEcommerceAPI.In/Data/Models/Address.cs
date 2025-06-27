using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class Address
{
    public int AdId { get; set; }

    public string AdAddress1 { get; set; } = null!;

    public string AdAddress2 { get; set; } = null!;

    public string AdCity { get; set; } = null!;

    public string AdLandmark { get; set; } = null!;

    public int AdPincode { get; set; }

    public short IsPrimary { get; set; }

    public int UId { get; set; }

    public string AdContact { get; set; } = null!;

    public string AdName { get; set; } = null!;
}
