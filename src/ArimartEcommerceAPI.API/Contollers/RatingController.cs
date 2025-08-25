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

        // Add this method to your existing RatingController class
        [AllowAnonymous]
        [HttpGet("eligibility/{pdid}")]
        public async Task<IActionResult> CheckRatingEligibility(long pdid, [FromQuery] int userId)
        {
            try
            {
                // ✅ Find latest delivered order for product & user
                var order = await _context.TblOrdernows
                    .Where(o => o.Userid == userId && o.Pdid == pdid && o.DdeliverredidTime != null)
                    .OrderByDescending(o => o.DdeliverredidTime)
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    return Ok(new
                    {
                        eligible = false,
                        message = "You can only rate products you have received (delivered)."
                    });
                }

                // ✅ Check if already rated for this specific order
                var existingRating = await _context.TblRatings
                    .FirstOrDefaultAsync(r => r.Userid == userId && r.Pdid == pdid && r.Orderid == order.Id);

                if (existingRating != null)
                {
                    return Ok(new
                    {
                        eligible = false,
                        message = "You have already rated this product from this order.",
                        existingRating = new
                        {
                            rating = existingRating.Ratingid,
                            comment = existingRating.Descr,
                            ratedOn = existingRating.AddedDate
                        }
                    });
                }

                // ✅ User is eligible to rate
                return Ok(new
                {
                    eligible = true,
                    message = "You can rate this product.",
                    orderId = order.Id,
                    deliveredOn = order.DdeliverredidTime
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while checking eligibility." });
            }
        }
        // POST: api/rating
        [HttpPost("rate")]
        public async Task<IActionResult> SubmitRating([FromBody] ProductRatingRequest request)
        {
            // ✅ Find the latest delivered order for the product and user
            var order = await _context.TblOrdernows
                .Where(o => o.Userid == request.UserId && o.Pid == request.Pdid && o.DdeliverredidTime != null)
                .OrderByDescending(o => o.DdeliverredidTime)
                .FirstOrDefaultAsync();

            if (order == null)
                return BadRequest(new { message = "You can only rate products you have received (delivered)." });

            // ✅ Check if already rated
            var existingRating = await _context.TblRatings
                .FirstOrDefaultAsync(r => r.Userid == request.UserId && r.Pdid == request.Pdid && r.Orderid == order.Id);

            if (existingRating != null)
                return BadRequest(new { message = "You have already rated this product from this order." });

            var rating = new TblRating
            {
                Userid = request.UserId,
                Orderid = order.Id,              // ✅ Auto-fetched
                Pdid = request.Pdid,
                Ratingid = request.Rating,
                Descr = request.Comment
            };

            _context.TblRatings.Add(rating);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Rating submitted successfully." });
        }

        // GET: api/rating/analytics/{pdid}
        [AllowAnonymous]
        [HttpGet("analytics/{pdid}")]
        public async Task<IActionResult> GetRatingAnalytics(long pdid)
        {
            var ratings = await _context.TblRatings
                .Where(r => r.Pdid == pdid && !r.IsDeleted && r.IsActive == true)
                .ToListAsync();

            if (!ratings.Any())
            {
                return Ok(new ProductRatingAnalytics
                {
                    ProductId = pdid,
                    TotalReviews = 0,
                    AverageRating = 0,
                    RatingBreakdown = new List<RatingBreakdown>
                    {
                        new RatingBreakdown { Stars = 5, Count = 0, Percentage = 0 },
                        new RatingBreakdown { Stars = 4, Count = 0, Percentage = 0 },
                        new RatingBreakdown { Stars = 3, Count = 0, Percentage = 0 },
                        new RatingBreakdown { Stars = 2, Count = 0, Percentage = 0 },
                        new RatingBreakdown { Stars = 1, Count = 0, Percentage = 0 }
                    }
                });
            }

            var totalReviews = ratings.Count;
            var averageRating = ratings.Average(r => r.Ratingid ?? 0);

            // Group ratings by star count
            var ratingGroups = ratings
                .GroupBy(r => r.Ratingid)
                .ToDictionary(g => g.Key ?? 0, g => g.Count());

            var ratingBreakdown = new List<RatingBreakdown>();
            for (int i = 5; i >= 1; i--)
            {
                var count = ratingGroups.GetValueOrDefault(i, 0);
                var percentage = totalReviews > 0 ? Math.Round((double)count / totalReviews * 100, 1) : 0;

                ratingBreakdown.Add(new RatingBreakdown
                {
                    Stars = i,
                    Count = count,
                    Percentage = percentage
                });
            }

            return Ok(new ProductRatingAnalytics
            {
                ProductId = pdid,
                TotalReviews = totalReviews,
                AverageRating = Math.Round(averageRating, 2),
                RatingBreakdown = ratingBreakdown
            });
        }

        // GET: api/rating/summary/{pdid}
        [AllowAnonymous]
        [HttpGet("summary/{pdid}")]
        public async Task<IActionResult> GetRatingSummary(long pdid)
        {
            var ratings = await _context.TblRatings
                .Where(r => r.Pdid == pdid && !r.IsDeleted && r.IsActive == true)
                .ToListAsync();

            if (!ratings.Any())
            {
                return Ok(new
                {
                    productId = pdid,
                    totalReviews = 0,
                    averageRating = 0.0,
                    fiveStars = 0,
                    fourStars = 0,
                    threeStars = 0,
                    twoStars = 0,
                    oneStar = 0
                });
            }

            var totalReviews = ratings.Count;
            var averageRating = Math.Round(ratings.Average(r => r.Ratingid ?? 0), 2);

            var ratingCounts = ratings
                .GroupBy(r => r.Ratingid)
                .ToDictionary(g => g.Key ?? 0, g => g.Count());

            return Ok(new
            {
                productId = pdid,
                totalReviews = totalReviews,
                averageRating = averageRating,
                fiveStars = ratingCounts.GetValueOrDefault(5, 0),
                fourStars = ratingCounts.GetValueOrDefault(4, 0),
                threeStars = ratingCounts.GetValueOrDefault(3, 0),
                twoStars = ratingCounts.GetValueOrDefault(2, 0),
                oneStar = ratingCounts.GetValueOrDefault(1, 0)
            });
        }

        // GET: api/rating/detailed/{pdid}
        [AllowAnonymous]
        [HttpGet("detailed/{pdid}")]
        public async Task<IActionResult> GetDetailedRatings(long pdid, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] int? filterByStars = null)
        {
            var query = _context.TblRatings
                .Where(r => r.Pdid == pdid && !r.IsDeleted && r.IsActive == true);

            // Filter by specific star rating if provided
            if (filterByStars.HasValue && filterByStars.Value >= 1 && filterByStars.Value <= 5)
            {
                query = query.Where(r => r.Ratingid == filterByStars.Value);
            }

            var totalCount = await query.CountAsync();

            var ratings = await query
                .Join(_context.TblUsers,
                      r => r.Userid,
                      u => u.Id,
                      (r, u) => new DetailedRating
                      {
                          RatingId = r.Id,
                          UserId = r.Userid,
                          Rating = r.Ratingid ?? 0,
                          Description = r.Descr,
                          UserName = u.ContactPerson,
                          RatedOn = r.AddedDate,
                          OrderId = r.Orderid ?? 0
                      })
                .OrderByDescending(r => r.RatedOn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                productId = pdid,
                totalCount = totalCount,
                page = page,
                pageSize = pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                filterByStars = filterByStars,
                ratings = ratings
            });
        }

        // GET: api/rating/average/{pdid} (keeping original for backward compatibility)
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

        // GET: api/rating/withuser/{pdid} (keeping original for backward compatibility)
        [AllowAnonymous]
        [HttpGet("withuser/{pdid}")]
        public async Task<IActionResult> GetRatingsWithUserInfo(long pdid)
        {
            var ratings = await _context.TblRatings
                .Where(r => r.Pdid == pdid && !r.IsDeleted && r.IsActive == true)
                .Join(_context.TblUsers,
                      r => r.Userid,
                      u => u.Id,
                      (r, u) => new {
                          Rating = r.Ratingid,
                          Description = r.Descr,
                          UserName = u.ContactPerson,
                          RatedOn = r.AddedDate
                      })
                .ToListAsync();

            return Ok(ratings);
        }

        // GET: api/rating/product/{pdid} (keeping original for backward compatibility)
        [AllowAnonymous]
        [HttpGet("product/{pdid}")]
        public async Task<IActionResult> GetRatingsByProduct(long pdid)
        {
            var ratings = await _context.TblRatings
                .Where(r => r.Pdid == pdid && !r.IsDeleted && r.IsActive == true)
                .ToListAsync();

            return Ok(ratings);
        }

        // GET: api/rating/stats/overview
        [AllowAnonymous]
        [HttpGet("stats/overview")]
        public async Task<IActionResult> GetOverallStats()
        {
            var allRatings = await _context.TblRatings
                .Where(r => !r.IsDeleted && r.IsActive == true)
                .ToListAsync();

            if (!allRatings.Any())
            {
                return Ok(new
                {
                    totalReviews = 0,
                    averageRating = 0.0,
                    totalProducts = 0,
                    mostReviewedProducts = new List<object>()
                });
            }

            var totalReviews = allRatings.Count;
            var averageRating = Math.Round(allRatings.Average(r => r.Ratingid ?? 0), 2);
            var totalProducts = allRatings.Select(r => r.Pdid).Distinct().Count();

            var mostReviewedProducts = allRatings
                .GroupBy(r => r.Pdid)
                .Select(g => new
                {
                    productId = g.Key,
                    reviewCount = g.Count(),
                    averageRating = Math.Round(g.Average(r => r.Ratingid ?? 0), 2)
                })
                .OrderByDescending(p => p.reviewCount)
                .Take(5)
                .ToList();

            return Ok(new
            {
                totalReviews = totalReviews,
                averageRating = averageRating,
                totalProducts = totalProducts,
                mostReviewedProducts = mostReviewedProducts
            });
        }
    }
}

// Request/Response Models
public class ProductRatingRequest
{
    public int UserId { get; set; }
    public long Pdid { get; set; }
    public int Rating { get; set; } // 1 to 5
    public string? Comment { get; set; }
}


public class ProductRatingAnalytics
{
    public long ProductId { get; set; }
    public int TotalReviews { get; set; }
    public double AverageRating { get; set; }
    public List<RatingBreakdown> RatingBreakdown { get; set; } = new();
}

public class RatingBreakdown
{
    public int Stars { get; set; }
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class DetailedRating
{
    public long RatingId { get; set; }
    public int Rating { get; set; }
    public long? UserId { get; set; }
    public string? Description { get; set; }
    public string? UserName { get; set; }
    public DateTime RatedOn { get; set; }
    public long? OrderId { get; set; }
}