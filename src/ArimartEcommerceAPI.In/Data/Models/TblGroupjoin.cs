using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class TblGroupjoin
{
    public long Id { get; set; }

    public long? Groupid { get; set; }

    public long? Userid { get; set; }

    public bool? Acctt { get; set; }

    public DateTime AddedDate { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public bool? IsActive { get; set; }

    public int? Sipid { get; set; }
}
