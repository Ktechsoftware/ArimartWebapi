public class DeliveryReferral
{
    public int Id { get; set; }
    public long ReferrerId { get; set; }
    public long RefereeId { get; set; }
    public string? ReferralCode { get; set; }
    public string? Status { get; set; }
    public int CompletedDeliveries { get; set; }
    public int RequiredDeliveries { get; set; }
    public decimal ReferrerReward { get; set; }
    public decimal RefereeReward { get; set; }
    public bool IsReferrerPaid { get; set; }
    public bool IsRefereePaid { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Fix: Navigation properties without ForeignKey attributes
    public virtual TblDeliveryuser? Referrer { get; set; }
    public virtual TblDeliveryuser? Referee { get; set; }
}