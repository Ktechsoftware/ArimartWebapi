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
        var wallet = await _context.TblWallets
            .Where(w => w.Userid == userid && !w.IsDeleted)
            .OrderByDescending(w => w.AddedDate)
            .FirstOrDefaultAsync();

        return Ok(new
        {
            userid,
            totalbalance = (wallet?.Amount ?? 0) + (wallet?.ReferAmount ?? 0),
            referamount = wallet?.ReferAmount ?? 0,
            walletamount = wallet?.Amount ?? 0
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
        // Get latest balance
        var wallet = await _context.TblWallets
            .Where(w => w.Userid == request.Userid && !w.IsDeleted)
            .OrderByDescending(w => w.AddedDate)
            .FirstOrDefaultAsync();

        var currentBalance = wallet?.Amount ?? 0;

        if (currentBalance < request.Amount)
            return BadRequest(new { message = "Insufficient balance" });

        var deduction = new TblWallet
        {
            Userid = request.Userid,
            Amount = -request.Amount, // negative for deduction
            AddedDate = DateTime.UtcNow,
            IsDeleted = false,
            IsActive = true,
            Acctt = true
        };

        _context.TblWallets.Add(deduction);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Amount deducted from wallet." });
    }
}
