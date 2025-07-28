using System.ComponentModel.DataAnnotations;
using System.Linq;
using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.DTO;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TopController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TopController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/top/orders/combined/{userid}
    [HttpGet("orders/combined/{userid}")]
    public async Task<IActionResult> GetTopRecentOrdersCombined(int userid)
    {
        try
        {
            // Get both individual and group orders for the user
            var allOrders = await (
                from order in _context.TblOrdernows
                where order.Userid == userid && !order.IsDeleted
                join p in _context.VwProducts on (int?)order.Pdid equals (int?)p.Pdid into pJoin
                from p in pJoin.DefaultIfEmpty()
                join c in _context.TblCategories on (p != null ? p.Categoryid : (int?)null) equals (int?)c.Id into cJoin
                from c in cJoin.DefaultIfEmpty()
                join sc in _context.TblSubcategories on (p != null ? p.Subcategoryid : (int?)null) equals (int?)sc.Id into scJoin
                from sc in scJoin.DefaultIfEmpty()
                join cc in _context.TblChildSubcategories on (p != null ? p.ChildCategoryId : (int?)null) equals (int?)cc.Id into ccJoin
                from cc in ccJoin.DefaultIfEmpty()
                orderby order.AddedDate descending
                select new
                {
                    order.TrackId,
                    order.Id,
                    order.Groupid,
                    order.AddedDate,
                    order.Qty,
                    order.Deliveryprice,
                    ProductName = p != null ? p.ProductName : null,
                    ProductImage = p != null ? p.Image : null,
                    CategoryName = c != null ? c.CategoryName : null,
                    SubCategoryName = sc != null ? sc.SubcategoryName : null,
                    ChildCategoryName = cc != null ? cc.ChildcategoryName : null,
                    OrderType = order.Groupid.HasValue ? "Group" : "Individual",
                    Status = order.DdeliverredidTime != null ? "Delivered"
                            : order.ShipOrderidTime != null ? "Shipped"
                            : order.DvendorpickupTime != null ? "Picked Up"
                            : order.DassignidTime != null ? "Assigned"
                            : "Placed"
                })
                .Take(10) // Take more to ensure we get 5 unique track IDs
                .ToListAsync();

            // Group by TrackId and take top 5
            var groupedOrders = allOrders
                .GroupBy(o => o.TrackId)
                .Select(g => new
                {
                    TrackId = g.Key,
                    OrderDate = g.First().AddedDate,
                    OrderType = g.First().OrderType,
                    GroupId = g.First().Groupid,
                    TotalItems = g.Count(),
                    TotalAmount = g.Sum(o => o.Deliveryprice * o.Qty),
                    Status = g.First().Status,
                    Items = g.Select(o => new
                    {
                        o.Id,
                        o.Qty,
                        o.Deliveryprice,
                        o.ProductName,
                        o.ProductImage,
                        o.CategoryName,
                        o.SubCategoryName,
                        o.ChildCategoryName,
                        o.Status
                    }).ToList()
                })
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToList();

            return Ok(new
            {
                message = "Top 5 recent orders (individual and group) retrieved successfully.",
                userid = userid,
                orders = groupedOrders
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to retrieve recent orders.", error = ex.Message });
        }
    }

    // Optional: Static Endpoints for each category using exact string matching

    [AllowAnonymous]
    [HttpGet("products/under-9")]
    public async Task<ActionResult<List<VwTopProducts>>> GetTopProductsUnder9([FromQuery] int limit = 10)
    {
        if (limit < 1 || limit > 50) limit = 10;

        var products = await _context.VwTopProducts
            .Where(p => p.IsDeleted == false && p.IsActive == true && p.CastedPrice <= 9)
            .OrderByDescending(p => p.AddedDate)
            .Take(limit)
            .ToListAsync();

        return Ok(products);
    }

    [AllowAnonymous]
    [HttpGet("products/under-49")]
    public async Task<ActionResult<List<VwTopProducts>>> GetTopProductsUnder49([FromQuery] int limit = 10)
    {
        if (limit < 1 || limit > 50) limit = 10;

        var products = await _context.VwTopProducts
            .Where(p => p.IsDeleted == false && p.IsActive == true &&
                        p.CastedPrice > 9 && p.CastedPrice <= 49)
            .OrderByDescending(p => p.AddedDate)
            .Take(limit)
            .ToListAsync();

        return Ok(products);
    }
    [AllowAnonymous]
    [HttpGet("products/under-99")]
    public async Task<ActionResult<List<VwTopProducts>>> GetTopProductsUnder99([FromQuery] int limit = 10)
    {
        if (limit < 1 || limit > 50) limit = 10;

        var products = await _context.VwTopProducts
            .Where(p => p.IsDeleted == false && p.IsActive == true &&
                        p.CastedPrice > 49 && p.CastedPrice <= 99)
            .OrderByDescending(p => p.AddedDate)
            .Take(limit)
            .ToListAsync();

        return Ok(products);
    }

    [AllowAnonymous]
    [HttpGet("products/under-999")]
    public async Task<ActionResult<List<VwTopProducts>>> GetTopProductsUnder999([FromQuery] int limit = 10)
    {
        if (limit < 1 || limit > 50) limit = 10;

        var products = await _context.VwTopProducts
            .Where(p => p.IsDeleted == false && p.IsActive == true &&
                        p.CastedPrice > 99 && p.CastedPrice <= 999)
            .OrderByDescending(p => p.AddedDate)
            .Take(limit)
            .ToListAsync();

        return Ok(products);
    }

    // Request/Response Models

    public class TopProductsRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Category must be 9, 99, or 999")]
        public int Category { get; set; }

        [Range(1, 50, ErrorMessage = "Limit must be between 1 and 50")]
        public int Limit { get; set; } = 10;
    }

    public class TopProductsResponse
    {
        public int Category { get; set; }
        public List<VwProduct> Products { get; set; }
        public int Count { get; set; }
        public string Message { get; set; }
    }


}