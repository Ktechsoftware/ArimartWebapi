using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class TblOrdernow
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

    public long? Groupid { get; set; }

    public int? Dassignid { get; set; }

    public int? Dvendorpickup { get; set; }

    public int? ShipOrderid { get; set; }

    public int? Ddeliverredid { get; set; }

    public int? Userratid { get; set; }

    public int? DeliveryboysAssginid { get; set; }

    public long? DeliveryboysShipid { get; set; }

    public long? DeliveryboyAssginid1 { get; set; }

    public int? Duserassginid { get; set; }

    public DateTime? DvendorpickupTime { get; set; }

    public DateTime? ShipOrderidTime { get; set; }

    public DateTime? DdeliverredidTime { get; set; }

    public DateTime? DuserassginidTime { get; set; }

    public DateTime? DassignidTime { get; set; }

    public decimal? Deliveryprice { get; set; }
}
