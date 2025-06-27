using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class TblGroupby
{
    public long Id { get; set; }

    public int? Qty { get; set; }

    public long? Pid { get; set; }

    public long? Pdid { get; set; }

    public int? Userid { get; set; }

    public bool? Acctt { get; set; }

    public DateTime AddedDate { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public bool? IsActive { get; set; }

    public int? Sipid { get; set; }

    public DateOnly? EventSend { get; set; }

    public DateTime? EventSend1 { get; set; }
}
