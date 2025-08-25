using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models
{
    public enum WithdrawalStatus
    {
        Requested,
        Processing,
        Completed,
        Failed,
        Cancelled
    }

    public enum WithdrawalMethod
    {
        BankTransfer,
        UPI
    }

    public class TblDeliveryWithdrawal
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DeliveryPartnerId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public WithdrawalMethod Method { get; set; }

        [Required]
        public WithdrawalStatus Status { get; set; } = WithdrawalStatus.Requested;

        [StringLength(100)]
        public string? AccountNumber { get; set; }

        [StringLength(100)]
        public string? IfscCode { get; set; }

        [StringLength(100)]
        public string? UpiId { get; set; }

        [StringLength(100)]
        public string? ReferenceNumber { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ProcessingFee { get; set; } = 0;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedAt { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

    }
}
