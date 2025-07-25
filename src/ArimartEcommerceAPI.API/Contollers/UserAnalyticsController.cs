using ArimartEcommerceAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArimartEcommerceAPI.API.Contollers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAnalyticsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserAnalyticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary/{userId}")]
        public async Task<IActionResult> GetUserAnalyticsSummary(int userId)
        {
            var now = DateTime.UtcNow;
            var threeMonthsAgo = now.AddMonths(-3);

            // Orders made
            var currentOrders = await _context.TblOrdernows
                .Where(o => o.Userid == userId && o.AddedDate >= threeMonthsAgo && !o.IsDeleted)
                .CountAsync();

            var previousOrders = await _context.TblOrdernows
                .Where(o => o.Userid == userId && o.AddedDate < threeMonthsAgo && o.AddedDate >= threeMonthsAgo.AddMonths(-3) && !o.IsDeleted)
                .CountAsync();

            // Group Buy Joined
            var currentGroupJoins = await _context.TblGroupjoins
                .Where(o => o.Userid == userId && o.IsDeleted == false && o.AddedDate >= threeMonthsAgo)
                .CountAsync();

            var previousGroupJoins = await _context.TblGroupjoins
                .Where(o => o.Userid == userId && o.IsDeleted == false && o.AddedDate < threeMonthsAgo && o.AddedDate >= threeMonthsAgo.AddMonths(-3))
                .CountAsync();

            // Favorite products
            var currentFavorites = await _context.VwWhishlists
                .Where(f => f.Cuserid == userId && f.AddedDate >= threeMonthsAgo && f.IsDeleted == false)
                .CountAsync();

            var previousFavorites = await _context.VwWhishlists
                .Where(f => f.Cuserid == userId && f.AddedDate < threeMonthsAgo && f.AddedDate >= threeMonthsAgo.AddMonths(-3) && f.IsDeleted == false)
                .CountAsync();

            // Referrals
            var currentReferrals = await _context.TblUserReferrals
                .Where(r => r.InviterUserId == userId && r.CreatedAt >= threeMonthsAgo)
                .CountAsync();

            var previousReferrals = await _context.TblUserReferrals
                .Where(r => r.InviterUserId == userId && r.CreatedAt < threeMonthsAgo && r.CreatedAt >= threeMonthsAgo.AddMonths(-3))
                .CountAsync();

            // Utility: % Change Helper
            double PercentChange(int current, int previous)
            {
                if (previous == 0) return current == 0 ? 0 : 100;
                return Math.Round(((double)(current - previous) / previous) * 100, 1);
            }

            return Ok(new
            {
                Orders = new
                {
                    value = currentOrders,
                    change = PercentChange(currentOrders, previousOrders),
                    previous = previousOrders
                },
                GroupBuyJoined = new
                {
                    value = currentGroupJoins,
                    change = PercentChange(currentGroupJoins, previousGroupJoins),
                    previous = previousGroupJoins
                },
                Favorites = new
                {
                    value = currentFavorites,
                    change = PercentChange(currentFavorites, previousFavorites),
                    previous = previousFavorites
                },
                Referrals = new
                {
                    value = currentReferrals,
                    change = PercentChange(currentReferrals, previousReferrals),
                    previous = previousReferrals
                }
            });
        }
    }

}
