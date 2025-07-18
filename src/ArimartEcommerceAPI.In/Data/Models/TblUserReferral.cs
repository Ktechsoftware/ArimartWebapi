
using System.ComponentModel.DataAnnotations.Schema;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

[Table("tbl_UserReferral")]
public partial class TblUserReferral
{

    public long Id { get; set; }
    public long InviterUserId { get; set; }
    public long NewUserId { get; set; }
    public string UsedReferralCode { get; set; } = string.Empty;
    public decimal RewardAmount { get; set; } = 0;

    public bool IsRewarded { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    public bool IsActive { get; set; } = true;
}

