using ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class TblDeliveryuser
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
    public int? CurrentStep { get; set; }
    public string? RejectRemark { get; set; }
    public string? Pass { get; set; }
    public long? Refid { get; set; }
    public string? Image { get; set; }
    public string? RegistrationStatus { get; set; }
    public bool? PersonalInfoComplete { get; set; }
    public bool? DocumentsUploaded { get; set; }
    public bool? ProfileComplete { get; set; }
    public string? VehicleType { get; set; } // bike, car, truck
    public string? VehicleNumber { get; set; }
    public bool IsAvailable { get; set; }
    public bool Emergencydetail { get; set; }
    public bool Bankdetail { get; set; }
    public bool Vehicledetail { get; set; }
    public decimal Rating { get; set; }
    public int TotalDeliveries { get; set; }
    public DateTime? Dob { get; set; }
    public string? WhatsappNo { get; set; }
    public string? AlterMobile { get; set; }
    public string? BloodGroup { get; set; }
    public string? LanguageKnown { get; set; }
    public string? BranchName { get; set; }
    public string? AccountHolderName { get; set; }
    public string? AccountType { get; set; }
    public string? UpiId { get; set; }
   
    public string? FatherName { get; set; }

    public string? PrimaryContactName { get; set; }
    public string? PrimaryContactPhone { get; set; }
    public string? PrimaryContactRelation { get; set; }
    public string? SecondaryContactName { get; set; }
    public string? SecondaryContactPhone { get; set; }
    public string? SecondaryContactRelation { get; set; }
    public string? MedicalConditions { get; set; }
    public string? Allergies { get; set; }
    public string? EmergencyAddress { get; set; }

    // Vehicle Details (additional to existing VehicleType and VehicleNumber)
    public string? VehicleBrand { get; set; }
    public string? VehicleModel { get; set; }
    public string? VehicleYear { get; set; }
    public string? VehicleColor { get; set; }
    public DateTime? RegistrationDate { get; set; }
    public string? InsuranceNumber { get; set; }
    public string? DrivingLicenseNumber { get; set; }
    public bool IsOnline { get; set; } = false;
    public long? CurrentShiftId { get; set; }

    public virtual DeliveryShift? CurrentShift { get; set; }
    public virtual ICollection<DeliveryShift> DeliveryShifts { get; set; } = new List<DeliveryShift>();
    public virtual ICollection<OrderEarning> OrderEarnings { get; set; } = new List<OrderEarning>();

}