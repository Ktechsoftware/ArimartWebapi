using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class TblRating
{
    public long Id { get; set; }

    public int? Ratingid { get; set; }

    public long? Userid { get; set; }

    public long? Orderid { get; set; }

    public string? Descr { get; set; }

    public bool? Acctt { get; set; }

    public DateTime AddedDate { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public bool? IsActive { get; set; }

    public long? Pdid { get; set; }
}
