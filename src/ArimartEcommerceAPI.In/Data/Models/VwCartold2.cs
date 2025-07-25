﻿using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class VwCartold2
{
    public int Fid { get; set; }

    public DateTime? Orderdate { get; set; }

    public string? Orderdate1 { get; set; }

    public string? Type { get; set; }

    public string AdName { get; set; } = null!;

    public int? Aid { get; set; }

    public int? Pid { get; set; }

    public string? PName { get; set; }

    public long? Pdid { get; set; }

    public string? Image { get; set; }

    public string? Discountprice { get; set; }

    public double? Price { get; set; }

    public double? Netprice { get; set; }

    public string? Totalprice { get; set; }

    public string? Gqty { get; set; }

    public string? Gprice { get; set; }

    public int? Cuserid { get; set; }

    public int? CQun { get; set; }

    public int? GrpId { get; set; }

    public string? Phone { get; set; }

    public string AdContact { get; set; } = null!;

    public string AdAddress1 { get; set; } = null!;

    public string AdAddress2 { get; set; } = null!;

    public int AdPincode { get; set; }

    public decimal? Qtyprice { get; set; }
}
