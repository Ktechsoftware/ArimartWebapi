using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire.Server;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models
{
    public enum TransactionType
    {
        Credit,
        Debit
    }

    public enum TransactionStatus
    {
        Pending,
        Completed,
        Failed,
        Cancelled
    }

    public class TblDeliveryTransaction
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long DeliveryPartnerId { get; set; }

        [Required]
        [StringLength(200)]
        public string? Title { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public TransactionType Type { get; set; }

        [Required]
        public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public int? OrderId { get; set; }
        public long? ReferralId { get; set; }

        [StringLength(100)]
        public string? ReferenceNumber { get; set; }
    }
}
