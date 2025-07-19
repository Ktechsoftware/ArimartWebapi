using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.DTO;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class WalletController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public WalletController(ApplicationDbContext context)
    {
        _context = context;
    }

    [AllowAnonymous]
    [HttpGet("balance/{userid}")]
    public async Task<IActionResult> GetWalletBalance(long userid)
    {
        var walletEntries = await _context.TblWallets
            .Where(w => w.Userid == userid && !w.IsDeleted)
            .ToListAsync();

        var totalRefer = walletEntries.Sum(w => w.ReferAmount ?? 0);
        var totalAmount = walletEntries.Sum(w => w.Amount ?? 0);
        var totalBalance = totalAmount + totalRefer;

        return Ok(new
        {
            userid,
            totalbalance = totalBalance,
            referamount = totalRefer,
            walletamount = totalAmount
        });
    }


    [HttpPost("add")]
    public async Task<IActionResult> AddToWallet([FromBody] AddWalletRequest request)
    {
        var walletEntry = new TblWallet
        {
            Userid = request.Userid,
            Amount = request.Amount,
            AddedDate = DateTime.UtcNow,
            IsDeleted = false,
            IsActive = true,
            Acctt = true
        };

        _context.TblWallets.Add(walletEntry);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Amount added to wallet successfully." });
    }

    // POST: api/wallet/deduct
    [HttpPost("deduct")]
    public async Task<IActionResult> DeductFromWallet([FromBody] DeductWalletRequest request)
    {
        // Get all wallet entries (not just latest)
        var walletEntries = await _context.TblWallets
            .Where(w => w.Userid == request.Userid && !w.IsDeleted)
            .ToListAsync();

        var totalAmount = walletEntries.Sum(w => w.Amount ?? 0);
        var totalRefer = walletEntries.Sum(w => w.ReferAmount ?? 0);
        var totalBalance = totalAmount + totalRefer;

        if (totalBalance < request.Amount)
            return BadRequest(new { message = "Insufficient wallet balance" });

        // Deduct from Amount first, then ReferAmount
        decimal remaining = request.Amount;

        if (totalAmount >= remaining)
        {
            _context.TblWallets.Add(new TblWallet
            {
                Userid = request.Userid,
                Amount = -remaining,
                ReferAmount = 0,
                AddedDate = DateTime.UtcNow,
                IsActive = true,
                IsDeleted = false,
                Acctt = true
            });
        }
        else
        {
            // Deduct full Amount and remaining from ReferAmount
            _context.TblWallets.Add(new TblWallet
            {
                Userid = request.Userid,
                Amount = -totalAmount,
                ReferAmount = -(remaining - totalAmount),
                AddedDate = DateTime.UtcNow,
                IsActive = true,
                IsDeleted = false,
                Acctt = true
            });
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Amount deducted from wallet successfully." });
    }

}
