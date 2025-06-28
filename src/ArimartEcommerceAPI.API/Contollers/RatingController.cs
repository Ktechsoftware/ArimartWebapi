using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ArimartEcommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RatingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RatingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/rating
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> SubmitRating([FromBody] SubmitRatingRequest request)
        {
            if (request.Ratingid < 1 || request.Ratingid > 5)
                return BadRequest(new { message = "Rating must be between 1 and 5." });

            // Check if user already rated this product
            var existingRating = await _context.TblRatings.FirstOrDefaultAsync(r =>
                r.Userid == request.Userid &&
                r.Pdid == request.Pdid &&
                !r.IsDeleted &&
                r.IsActive == true);

            if (existingRating != null)
            {
                return BadRequest(new { message = "You have already rated this product." });
            }

            var rating = new TblRating
            {
                Ratingid = request.Ratingid,
                Userid = request.Userid,
                Orderid = request.Orderid,
                Descr = request.Descr,
                Pdid = request.Pdid,
                AddedDate = DateTime.UtcNow,
                IsDeleted = false,
                IsActive = true
            };

            _context.TblRatings.Add(rating);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Rating submitted successfully." });
        }

        [AllowAnonymous]
        [HttpGet("average/{pdid}")]
        public async Task<IActionResult> GetAverageRating(long pdid)
        {
            var ratings = await _context.TblRatings
                .Where(r => r.Pdid == pdid && !r.IsDeleted && r.IsActive == true)
                .ToListAsync();

            if (!ratings.Any())
                return Ok(new { averageRating = 0, totalRatings = 0 });

            var average = ratings.Average(r => r.Ratingid ?? 0);

            return Ok(new
            {
                averageRating = Math.Round(average, 2),
                totalRatings = ratings.Count
            });
        }

        [AllowAnonymous]
        [HttpGet("withuser/{pdid}")]
        public async Task<IActionResult> GetRatingsWithUserInfo(long pdid)
        {
            var ratings = await _context.TblRatings
                .Where(r => r.Pdid == pdid && !r.IsDeleted && r.IsActive == true)
                .Join(_context.Users,
                      r => r.Userid,
                      u => u.UserId,
                      (r, u) => new {
                          Rating = r.Ratingid,
                          Description = r.Descr,
                          UserName = u.FullName,
                          RatedOn = r.AddedDate
                      })
                .ToListAsync();

            return Ok(ratings);
        }


        [AllowAnonymous]
        [HttpGet("product/{pdid}")]
        public async Task<IActionResult> GetRatingsByProduct(long pdid)
        {
            var ratings = await _context.TblRatings
                .Where(r => r.Pdid == pdid && !r.IsDeleted && r.IsActive == true)
                .ToListAsync();

            return Ok(ratings);
        }

    }
}

public class SubmitRatingRequest
{
    public int Ratingid { get; set; }      // e.g., 1 to 5
    public long Userid { get; set; }       // User giving the rating
    public long Orderid { get; set; }      // Optional: to track which order this came from
    public string? Descr { get; set; }     // Optional review text
    public long Pdid { get; set; }         // ProductDetail ID (or Product ID depending on usage)
}

