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

    [AllowAnonymous]
    [HttpGet("products")]
    public async Task<ActionResult<TopProductsResponse>> GetTopProducts([FromQuery] TopProductsRequest request)
    {
        if (request.Limit < 1 || request.Limit > 50)
            request.Limit = 10;

        IQueryable<VwProduct> query = _context.VwProducts
            .Where(p => p.IsDeleted == false && p.IsActive == true);

        // Exact string match for "9", "99", "999"
        switch (request.Category)
        {
            case 9:
                query = query.Where(p =>
                    p.Price == "9" || p.Price == "9.0" || p.Price == "9.00");
                break;
            case 99:
                query = query.Where(p =>
                    p.Price == "10" || p.Price == "10.0" || p.Price == "10.00" ||
                    (string.Compare(p.Price, "9", StringComparison.OrdinalIgnoreCase) > 0 &&
                     string.Compare(p.Price, "99.99", StringComparison.OrdinalIgnoreCase) <= 0));
                break;
            case 999:
                query = query.Where(p =>
                    string.Compare(p.Price, "99", StringComparison.OrdinalIgnoreCase) > 0 &&
                    string.Compare(p.Price, "999.99", StringComparison.OrdinalIgnoreCase) <= 0);
                break;
            default:
                return BadRequest("Invalid category. Use 9, 99, or 999.");
        }

        var topProducts = await query
            .OrderByDescending(p => p.AddedDate)
            .Take(request.Limit)
            .ToListAsync();

        var response = new TopProductsResponse
        {
            Category = request.Category,
            Products = topProducts,
            Count = topProducts.Count,
            Message = $"Top {topProducts.Count} products under Rs. {request.Category}"
        };

        return Ok(response);
    }

    // Optional: Static Endpoints for each category using exact string matching

    [AllowAnonymous]
    [HttpGet("products/under-9")]
    public async Task<ActionResult<List<VwProduct>>> GetTopProductsUnder9([FromQuery] int limit = 10)
    {
        if (limit < 1 || limit > 50) limit = 10;

        var products = await _context.VwProducts
            .Where(p => p.IsDeleted == false && p.IsActive == true && (
                p.Price == "9" || p.Price == "9.0" || p.Price == "9.00"))
            .OrderByDescending(p => p.AddedDate)
            .Take(limit)
            .ToListAsync();

        return Ok(products);
    }

    [AllowAnonymous]
    [HttpGet("products/under-99")]
    public async Task<ActionResult<List<VwProduct>>> GetTopProductsUnder99([FromQuery] int limit = 10)
    {
        if (limit < 1 || limit > 50) limit = 10;

        var products = await _context.VwProducts
            .Where(p => p.IsDeleted == false && p.IsActive == true &&
                (p.Price == "10" || p.Price == "10.0" || p.Price == "10.00" ||
                 (string.Compare(p.Price, "9", StringComparison.OrdinalIgnoreCase) > 0 &&
                  string.Compare(p.Price, "99.99", StringComparison.OrdinalIgnoreCase) <= 0)))
            .OrderByDescending(p => p.AddedDate)
            .Take(limit)
            .ToListAsync();

        return Ok(products);
    }

    [AllowAnonymous]
    [HttpGet("products/under-999")]
    public async Task<ActionResult<List<VwProduct>>> GetTopProductsUnder999([FromQuery] int limit = 10)
    {
        if (limit < 1 || limit > 50) limit = 10;

        var products = await _context.VwProducts
            .Where(p => p.IsDeleted == false && p.IsActive == true &&
                string.Compare(p.Price, "99", StringComparison.OrdinalIgnoreCase) > 0 &&
                string.Compare(p.Price, "999.99", StringComparison.OrdinalIgnoreCase) <= 0)
            .OrderByDescending(p => p.AddedDate)
            .Take(limit)
            .ToListAsync();

        return Ok(products);
    }

    // Combined Endpoint
    [AllowAnonymous]
    [HttpGet("products/all-categories")]
    public async Task<ActionResult<AllCategoriesResponse>> GetAllTopCategories([FromQuery] int limitPerCategory = 5)
    {
        if (limitPerCategory < 1 || limitPerCategory > 20) limitPerCategory = 5;

        var under9 = await _context.VwProducts
            .Where(p => p.IsDeleted == false && p.IsActive == true &&
                (p.Price == "9" || p.Price == "9.0" || p.Price == "9.00"))
            .OrderByDescending(p => p.AddedDate)
            .Take(limitPerCategory)
            .ToListAsync();

        var under99 = await _context.VwProducts
            .Where(p => p.IsDeleted == false && p.IsActive == true &&
                (p.Price == "10" || p.Price == "10.0" || p.Price == "10.00" ||
                 (string.Compare(p.Price, "9", StringComparison.OrdinalIgnoreCase) > 0 &&
                  string.Compare(p.Price, "99.99", StringComparison.OrdinalIgnoreCase) <= 0)))
            .OrderByDescending(p => p.AddedDate)
            .Take(limitPerCategory)
            .ToListAsync();

        var under999 = await _context.VwProducts
            .Where(p => p.IsDeleted == false && p.IsActive == true &&
                string.Compare(p.Price, "99", StringComparison.OrdinalIgnoreCase) > 0 &&
                string.Compare(p.Price, "999.99", StringComparison.OrdinalIgnoreCase) <= 0)
            .OrderByDescending(p => p.AddedDate)
            .Take(limitPerCategory)
            .ToListAsync();

        var response = new AllCategoriesResponse
        {
            Under9 = under9,
            Under99 = under99,
            Under999 = under999,
            LimitPerCategory = limitPerCategory
        };

        return Ok(response);
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

    public class AllCategoriesResponse
    {
        public List<VwProduct> Under9 { get; set; }
        public List<VwProduct> Under99 { get; set; }
        public List<VwProduct> Under999 { get; set; }
        public int LimitPerCategory { get; set; }
    }

}