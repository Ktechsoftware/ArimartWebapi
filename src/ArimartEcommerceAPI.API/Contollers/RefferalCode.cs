using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class ReferralController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReferralController(ApplicationDbContext context)
    {
        _context = context;
    }

    // 2️⃣: Get a user's referral code (from view)
    [HttpGet("my-refcode/{userId}")]
    public async Task<IActionResult> GetMyReferralCode(long userId)
    {
        var refEntry = await _context.VwUserrefercodes
            .FirstOrDefaultAsync(v => v.Id == userId);

        if (refEntry == null)
            return NotFound(new { message = "Referral code not found." });

        return Ok(new
        {
            userId = refEntry.Id,
            name = refEntry.VendorName,
            phone = refEntry.Phone,
            referCode = refEntry.Refercode
        });
    }

    // 3️⃣: Get referral stats (total installs and refer earnings)
    [HttpGet("stats/{userId}")]
    public async Task<IActionResult> GetReferralStats(long userId)
    {
        var totalInstalled = await _context.TblUserReferrals
            .CountAsync(r => r.InviterUserId == userId);

        var referAmount = await _context.TblWallets
            .Where(w => w.Userid == userId)
            .Select(w => w.ReferAmount)
            .FirstOrDefaultAsync();

        return Ok(new
        {
            totalInstalled,
            totalEarned = referAmount ?? 0
        });
    }

}
