using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class TblShiping
{
    public long Id { get; set; }

    public string? VendorName { get; set; }

    public string? ContactPerson { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }

    public DateTime AddedDate { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public bool? IsActive { get; set; }

    public int? Userid { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Instructions { get; set; }
}
