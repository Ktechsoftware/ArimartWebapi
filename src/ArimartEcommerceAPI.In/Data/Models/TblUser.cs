using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class TblUser
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

    public string? UserType { get; set; }

    public string? CompanyName { get; set; }

    public int? BusinessCategory { get; set; }

    public string? Gst { get; set; }

    public string? Pan { get; set; }

    public string? AadharCardNo { get; set; }

    public string? BusinessLocation { get; set; }

    public string? BankName { get; set; }

    public string? AccountNo { get; set; }

    public string? Ifsccode { get; set; }

    public string? BusinessLicense { get; set; }

    public string? Idproof { get; set; }

    public int? Reject { get; set; }

    public string? RejectRemark { get; set; }

    public string? Pass { get; set; }

    public long? Refid { get; set; }

    public string? Image { get; set; }

    public string? Gender { get; set; }

    public string? ClientName { get; set; }

    public string? CreditReference { get; set; }

    public int? ExposerLimit { get; set; }
}
