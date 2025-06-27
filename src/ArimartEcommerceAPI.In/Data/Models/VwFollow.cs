using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class VwFollow
{
    public long Id { get; set; }

    public long? Vendorid { get; set; }

    public long? Userid { get; set; }

    public bool? Status { get; set; }

    public DateTime AddedDate { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public bool? IsActive { get; set; }

    public string? VendorName { get; set; }

    public string? Image1 { get; set; }

    public string? Companyname { get; set; }
}
