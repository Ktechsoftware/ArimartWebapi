using System.ComponentModel.DataAnnotations;
using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArimartEcommerceAPI.API.Contollers
{
        [ApiController]
        [Route("api/[controller]")]
        public class DeliveryWalletController : ControllerBase
        {
            private readonly ApplicationDbContext _context;

            public DeliveryWalletController(ApplicationDbContext context)
            {
                _context = context;
            }

        // GET: api/wallet/{partnerId}
        [HttpGet("{partnerId}")]
        public async Task<ActionResult<WalletDto>> GetWallet(int partnerId)
        {
            var wallet = await _context.TblDeliveryWallets
                .Where(w => w.DeliveryPartnerId == partnerId)
                .Select(w => new WalletDto
                {
                    Id = w.Id,
                    DeliveryPartnerId = w.DeliveryPartnerId,
                    Balance = w.Balance ?? 0,
                    WeeklyEarnings = w.WeeklyEarnings ?? 0,
                    MonthlyEarnings = w.MonthlyEarnings ?? 0,
                    TotalEarnings = w.TotalEarnings ?? 0,
                    TotalReferralEarnings = w.TotalReferralEarnings ?? 0,
                    LastUpdated = w.LastUpdated ?? DateTime.UtcNow
                })
                .FirstOrDefaultAsync();

            if (wallet == null)
            {
                return NotFound($"Wallet not found for partner {partnerId}");
            }

            return Ok(wallet);
        }

        // POST: api/wallet/{partnerId}/deposit
        [HttpPost("{partnerId}/deposit")]
        public async Task<ActionResult<WalletDto>> DepositAmount(int partnerId, DepositRequestDto request)
        {
            try
            {
                // Try to find wallet
                var wallet = await _context.TblDeliveryWallets
                    .FirstOrDefaultAsync(w => w.DeliveryPartnerId == partnerId);

                // If wallet not found, create one
                if (wallet == null)
                {
                    wallet = new TblDeliveryWallet
                    {
                        DeliveryPartnerId = partnerId,
                        Balance = 0,
                        WeeklyEarnings = 0,
                        MonthlyEarnings = 0,
                        TotalEarnings = 0,
                        LastUpdated = DateTime.UtcNow
                    };
                    _context.TblDeliveryWallets.Add(wallet);
                    await _context.SaveChangesAsync(); // Save once to get Wallet.Id
                }

                // Create credit transaction
                var transaction = new TblDeliveryTransaction
                {
                    DeliveryPartnerId = partnerId,
                    Title = string.IsNullOrWhiteSpace(request.Title) ? "Deposit" : request.Title,
                    Description = request.Description,
                    Amount = request.Amount,
                    Type = TransactionType.Credit,
                    Status = TransactionStatus.Completed, // deposit is instant
                    ReferenceNumber = $"DP{DateTime.Now:yyyyMMddHHmmss}{partnerId}"
                };

                _context.TblDeliveryTransactions.Add(transaction);

                // Update wallet balance
                wallet.Balance += request.Amount;
                wallet.TotalEarnings += request.Amount;
                wallet.LastUpdated = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var walletDto = new WalletDto
                {
                    Id = wallet.Id,
                    Balance = wallet.Balance ?? 0,
                    WeeklyEarnings = wallet.WeeklyEarnings ?? 0,
                    MonthlyEarnings = wallet.MonthlyEarnings ?? 0,
                    TotalEarnings = wallet.TotalEarnings ?? 0,
                    LastUpdated = wallet.LastUpdated ?? DateTime.UtcNow
                };

                return Ok(walletDto);
            }
            catch (Exception ex)
            {
                // Log the exception if logging is configured
                // _logger.LogError(ex, "Error occurred while depositing to wallet");

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = "An error occurred while processing the deposit.",
                    Error = ex.Message
                });
            }
        }




        // GET: api/wallet/{partnerId}/transactions
        [HttpGet("{partnerId}/transactions")]
            public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions(
                int partnerId,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 10)
            {
                var transactions = await _context.TblDeliveryTransactions
                    .Where(t => t.DeliveryPartnerId == partnerId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new TransactionDto
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Description = t.Description,
                        Amount = t.Amount,
                        Type = t.Type,
                        Status = t.Status,
                        CreatedAt = t.CreatedAt,
                        CompletedAt = t.CompletedAt,
                        ReferenceNumber = t.ReferenceNumber
                    })
                    .ToListAsync();

                return Ok(transactions);
            }

            // POST: api/wallet/{partnerId}/withdraw
            [HttpPost("{partnerId}/withdraw")]
            public async Task<ActionResult<TblDeliveryWithdrawal>> RequestWithdrawal(int partnerId, WithdrawalRequestDto request)
            {
                // Check if partner exists and has sufficient balance
                var wallet = await _context.TblDeliveryWallets
                    .FirstOrDefaultAsync(w => w.DeliveryPartnerId == partnerId);

                if (wallet == null)
                {
                    return NotFound($"Wallet not found for partner {partnerId}");
                }

                if (wallet.Balance < request.Amount)
                {
                    return BadRequest("Insufficient balance");
                }

                // Calculate processing fee
                decimal processingFee = 0;
                if (Enum.Parse<WithdrawalMethod>(request.Method) == WithdrawalMethod.UPI)
                {
                    processingFee = 2; // ₹2 for UPI
                }

                var withdrawal = new TblDeliveryWithdrawal
                {
                    DeliveryPartnerId = partnerId,
                    Amount = request.Amount,
                    Method = Enum.Parse<WithdrawalMethod>(request.Method),
                    AccountNumber = request.AccountNumber,
                    IfscCode = request.IfscCode,
                    UpiId = request.UpiId,
                    ProcessingFee = processingFee,
                    ReferenceNumber = $"WD{DateTime.Now:yyyyMMddHHmmss}{partnerId}"
                };

                _context.TblDeliveryWithdrawals.Add(withdrawal);

                // Create debit transaction
                var transaction = new TblDeliveryTransaction
                {
                    DeliveryPartnerId = partnerId,
                    Title = "Withdrawal Request",
                    Description = $"Withdrawal via {request.Method}",
                    Amount = -(request.Amount + processingFee),
                    Type = TransactionType.Debit,
                    Status = TransactionStatus.Pending,
                    ReferenceNumber = withdrawal.ReferenceNumber
                };

                _context.TblDeliveryTransactions.Add(transaction);

                // Update wallet balance
                wallet.Balance -= (request.Amount + processingFee);
                wallet.LastUpdated = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetWithdrawal), new { id = withdrawal.Id }, withdrawal);
            }

            // GET: api/wallet/withdrawal/{id}
            [HttpGet("withdrawal/{id}")]
            public async Task<ActionResult<TblDeliveryWithdrawal>> GetWithdrawal(int id)
            {
                var withdrawal = await _context.TblDeliveryWithdrawals.FindAsync(id);

                if (withdrawal == null)
                {
                    return NotFound();
                }

                return Ok(withdrawal);
            }

            // PUT: api/wallet/refresh/{partnerId}
            [HttpPut("refresh/{partnerId}")]
            public async Task<ActionResult<WalletDto>> RefreshWallet(int partnerId)
            {
                var wallet = await _context.TblDeliveryWallets
                    .FirstOrDefaultAsync(w => w.DeliveryPartnerId == partnerId);

                if (wallet == null)
                {
                    return NotFound($"Wallet not found for partner {partnerId}");
                }

                // Recalculate earnings
                var weekStart = DateTime.Now.AddDays(-(int)DateTime.Now.DayOfWeek);
                var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

                var weeklyEarnings = await _context.TblDeliveryTransactions
                    .Where(t => t.DeliveryPartnerId == partnerId
                        && t.Type == TransactionType.Credit
                        && t.Status == TransactionStatus.Completed
                        && t.CreatedAt >= weekStart)
                    .SumAsync(t => t.Amount);

                var monthlyEarnings = await _context.TblDeliveryTransactions
                    .Where(t => t.DeliveryPartnerId == partnerId
                        && t.Type == TransactionType.Credit
                        && t.Status == TransactionStatus.Completed
                        && t.CreatedAt >= monthStart)
                    .SumAsync(t => t.Amount);

                var totalEarnings = await _context.TblDeliveryTransactions
                    .Where(t => t.DeliveryPartnerId == partnerId
                        && t.Type == TransactionType.Credit
                        && t.Status == TransactionStatus.Completed)
                    .SumAsync(t => t.Amount);

                wallet.WeeklyEarnings = weeklyEarnings;
                wallet.MonthlyEarnings = monthlyEarnings;
                wallet.TotalEarnings = totalEarnings;
                wallet.LastUpdated = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var walletDto = new WalletDto
                {
                    Id = wallet.Id,
                    DeliveryPartnerId = wallet.DeliveryPartnerId,
                     Balance = wallet.Balance ?? 0,
                    WeeklyEarnings = wallet.WeeklyEarnings ?? 0,
                    MonthlyEarnings = wallet.MonthlyEarnings ?? 0,
                    TotalEarnings = wallet.TotalEarnings ?? 0,
                    LastUpdated = wallet.LastUpdated ?? DateTime.UtcNow
                };

                return Ok(walletDto);
            }
        }
    }


public class WalletDto
{
    public long Id { get; set; }
    public long DeliveryPartnerId { get; set; }
    public decimal Balance { get; set; }
    public decimal WeeklyEarnings { get; set; }
    public decimal MonthlyEarnings { get; set; }
    public decimal TotalEarnings { get; set; }
    public decimal TotalReferralEarnings { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class TransactionDto
{
    public long Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string ReferenceNumber { get; set; }
}

public class WithdrawalRequestDto
{
    [Required]
    [Range(1, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    [Required]
    public string Method { get; set; } // "BankTransfer" or "UPI"

    public string AccountNumber { get; set; }
    public string IfscCode { get; set; }
    public string UpiId { get; set; }
}

public class DepositRequestDto
{
    [Required]
    [Range(1, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    [StringLength(200)]
    public string Title { get; set; } = "Deposit";

    [StringLength(500)]
    public string Description { get; set; }
}

