using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class Cart
{
    public int CId { get; set; }

    public int PId { get; set; }

    public int UId { get; set; }

    public int CQun { get; set; }

    public DateTime AtDate { get; set; }

    public int GrpId { get; set; }

    public short SaveLeter { get; set; }

    public short HasOrdered { get; set; }
}
