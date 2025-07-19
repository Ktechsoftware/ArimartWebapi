using System.Linq;
using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.DTO;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OrderController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> CheckoutCart([FromBody] CartCheckoutRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Addid))
            return BadRequest(new { message = "Cart item IDs are required." });

        try
        {
            var cartIds = request.Addid
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => Convert.ToInt64(id.Trim()))
                .ToList();

            var cartItems = await _context.TblAddcarts
                .Where(c => cartIds.Contains(c.Id) && c.Qty > 0)
                .ToListAsync();

            if (cartItems.Count == 0)
                return BadRequest(new { message = "No valid cart items found for checkout." });

            var trackId = GenerateTrackId();

            var newOrders = cartItems.Select(c => new TblOrdernow
            {
                Qty = c.Qty,
                Pid = c.Pid,
                Pdid = c.Pdid,
                Userid = request.Userid,
                Sipid = request.Sipid,
                Groupid = c.Groupid,
                Deliveryprice = c.Price,
                TrackId = trackId,
                AddedDate = DateTime.UtcNow,
                IsDeleted = false,
                IsActive = true
            }).ToList();

            await _context.TblOrdernows.AddRangeAsync(newOrders);

            foreach (var item in cartItems)
            {
                if (item.Groupid != null)
                {
                    item.IsDeleted = false;
                    item.Qty = 0;
                }
                else
                {
                    item.IsDeleted = true;
                }

                item.ModifiedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Checkout successful.", orderid = trackId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Checkout failed.", error = ex.Message });
        }
    }

    // POST: api/order/place
    [HttpPost("place")]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
    {
        if (request.Qty <= 0)
            return BadRequest(new { message = "Quantity must be greater than 0." });

        var trackId = GenerateTrackId();

        var order = new TblOrdernow
        {
            Qty = request.Qty,
            Pid = request.Pid,
            Pdid = request.Pdid,
            Userid = request.Userid,
            Groupid = null,
            Deliveryprice = request.Deliveryprice,
            TrackId = trackId,
            AddedDate = DateTime.UtcNow,
            IsDeleted = false,
            IsActive = true
        };

        await _context.TblOrdernows.AddAsync(order);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Order placed successfully.", orderid = trackId });
    }

    // POST: api/order/place/group
    [HttpPost("place/group")]
    public async Task<IActionResult> PlaceGroupOrder([FromBody] PlaceOrderRequest request)
    {
        if (request.Qty <= 0)
            return BadRequest(new { message = "Quantity must be greater than 0." });

        if (!request.Groupid.HasValue)
            return BadRequest(new { message = "Group ID is required for group order." });

        var order = new TblOrdernow
        {
            Qty = request.Qty,
            Pid = request.Pid,
            Pdid = request.Pdid,
            Userid = request.Userid,
            Groupid = request.Groupid,
            Deliveryprice = request.Deliveryprice,
        };

        await _context.TblOrdernows.AddAsync(order);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Group order placed successfully." });
    }

    // GET: api/order/history/{userid}
    [HttpGet("history/{userid}")]
    public async Task<IActionResult> GetOrderHistory(int userid, [FromQuery] string status = null)
    {
        var baseQuery = from order in _context.TblOrdernows
                        where order.Userid == userid
                              && !order.IsDeleted
                        join p in _context.VwProducts on (int?)order.Pdid equals (int?)p.Pdid into pJoin
                        from p in pJoin.DefaultIfEmpty()
                        join c in _context.TblCategories on (p != null ? p.Categoryid : (int?)null) equals (int?)c.Id into cJoin
                        from c in cJoin.DefaultIfEmpty()
                        join sc in _context.TblSubcategories on (p != null ? p.Subcategoryid : (int?)null) equals (int?)sc.Id into scJoin
                        from sc in scJoin.DefaultIfEmpty()
                        join cc in _context.TblChildSubcategories on (p != null ? p.ChildCategoryId : (int?)null) equals (int?)cc.Id into ccJoin
                        from cc in ccJoin.DefaultIfEmpty()
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
                            Status = order.DdeliverredidTime != null ? "Delivered"
                                    : order.ShipOrderidTime != null ? "Shipped"
                                    : order.DvendorpickupTime != null ? "Picked Up"
                                    : order.DassignidTime != null ? "Assigned"
                                    : "Placed"
                        };

        if (!string.IsNullOrWhiteSpace(status))
        {
            baseQuery = baseQuery.Where(o => o.Status == status);
        }

        var orders = await baseQuery
            .OrderByDescending(o => o.AddedDate)
            .ToListAsync();

        // Group by TrackId to show orders with same track ID together
        var groupedOrders = orders
            .GroupBy(o => o.TrackId)
            .Select(g => new
            {
                TrackId = g.Key,
                OrderDate = g.First().AddedDate,
                TotalItems = g.Count(),
                TotalAmount = g.Sum(o => o.Deliveryprice * o.Qty),
                Status = g.First().Status, // Assuming all items in same track have same status
                Items = g.Select(o => new
                {
                    o.Id,
                    o.Qty,
                    o.Groupid,
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
            .ToList();

        return Ok(groupedOrders);
    }

    [HttpGet("history/{userid}/{groupid}")]
    public async Task<IActionResult> GetGroupOrderHistory(int userid, long groupid)
    {
        var orders = from order in _context.TblOrdernows
                     where order.Userid == userid
                           && order.Groupid == groupid
                           && !order.IsDeleted
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
                         order.AddedDate,
                         order.Qty,
                         order.Deliveryprice,
                         ProductName = p != null ? p.ProductName : null,
                         ProductImage = p != null ? p.Image : null,
                         CategoryName = c != null ? c.CategoryName : null,
                         SubCategoryName = sc != null ? sc.SubcategoryName : null,
                         ChildCategoryName = cc != null ? cc.ChildcategoryName : null,
                         Status = order.DdeliverredidTime != null ? "Delivered"
                                 : order.ShipOrderidTime != null ? "Shipped"
                                 : order.DvendorpickupTime != null ? "Picked Up"
                                 : order.DassignidTime != null ? "Assigned"
                                 : "Placed"
                     };

        var result = await orders.ToListAsync();
        return Ok(result);
    }

    // 🔥 FIXED: Track Order - Now returns ALL items with same track ID
    [HttpGet("track/{trackId}")]
    public async Task<IActionResult> TrackOrder(string trackId)
    {
        var orders = await (
            from order in _context.TblOrdernows
            join p in _context.VwProducts on (int?)order.Pdid equals (int?)p.Pdid into pJoin
            from p in pJoin.DefaultIfEmpty()
            join c in _context.TblCategories on (p != null ? p.Categoryid : (int?)null) equals (int?)c.Id into cJoin
            from c in cJoin.DefaultIfEmpty()
            join sc in _context.TblSubcategories on (p != null ? p.Subcategoryid : (int?)null) equals (int?)sc.Id into scJoin
            from sc in scJoin.DefaultIfEmpty()
            join cc in _context.TblChildSubcategories on (p != null ? p.ChildCategoryId : (int?)null) equals (int?)cc.Id into ccJoin
            from cc in ccJoin.DefaultIfEmpty()
            where order.TrackId == trackId && !order.IsDeleted
            select new
            {
                order.TrackId,
                order.Id,
                order.Pid,
                order.Pdid,
                order.Qty,
                order.Groupid,
                order.Deliveryprice,
                order.AddedDate,
                order.DassignidTime,
                order.DvendorpickupTime,
                order.ShipOrderidTime,
                order.DdeliverredidTime,
                order.DuserassginidTime,

                Status = order.DdeliverredidTime != null ? "Delivered"
                        : order.ShipOrderidTime != null ? "Shipped"
                        : order.DvendorpickupTime != null ? "Picked Up"
                        : order.DassignidTime != null ? "Assigned"
                        : "Placed",

                ProductDetails = new
                {
                    Id = p != null ? p.Id : (long?)null,
                    Name = p != null ? p.ProductName : null,
                    Description = p != null ? p.Longdesc : null,
                    Image = p != null ? p.Image : null,
                    Price = p != null ? p.Price : null,
                    Unit = p != null ? p.Wtype : null,
                    Weight = p != null ? p.Wweight : null
                },

                Category = new
                {
                    Name = c != null ? c.CategoryName : null
                },

                SubCategory = new
                {
                    Name = sc != null ? sc.SubcategoryName : null
                },

                ChildCategory = new
                {
                    Name = cc != null ? cc.ChildcategoryName : null
                }
            })
            .ToListAsync(); // 🔥 Changed from FirstOrDefaultAsync to ToListAsync

        if (!orders.Any())
            return NotFound(new { message = "Order not found." });

        // Return structured response with order summary and all items
        var result = new
        {
            TrackId = trackId,
            OrderDate = orders.First().AddedDate,
            TotalItems = orders.Count,
            TotalAmount = orders.Sum(o => o.Deliveryprice * o.Qty),
            OverallStatus = orders.First().Status, // You might want to calculate this based on all items
            Items = orders.Select(order => new
            {
                order.Id,
                order.Pid,
                order.Pdid,
                order.Qty,
                order.Groupid,
                order.Deliveryprice,
                order.Status,
                order.DassignidTime,
                order.DvendorpickupTime,
                order.ShipOrderidTime,
                order.DdeliverredidTime,
                order.DuserassginidTime,
                order.ProductDetails,
                order.Category,
                order.SubCategory,
                order.ChildCategory
            }).ToList()
        };

        return Ok(result);
    }

    [HttpDelete("{orderid}")]
    public async Task<IActionResult> CancelOrder(long orderid)
    {
        var order = await _context.TblOrdernows.FirstOrDefaultAsync(o => o.Id == orderid && !o.IsDeleted);
        if (order == null)
            return NotFound(new { message = "Order not found or already deleted." });

        order.IsDeleted = true;
        order.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Order cancelled successfully." });
    }

    // 🔥 NEW: Cancel entire order by Track ID (all items)
    [HttpDelete("track/{trackId}")]
    public async Task<IActionResult> CancelOrderByTrackId(string trackId)
    {
        var orders = await _context.TblOrdernows
            .Where(o => o.TrackId == trackId && !o.IsDeleted)
            .ToListAsync();

        if (!orders.Any())
            return NotFound(new { message = "Order not found or already deleted." });

        foreach (var order in orders)
        {
            order.IsDeleted = true;
            order.ModifiedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = $"Order {trackId} cancelled successfully. {orders.Count} items cancelled." });
    }

    private string GenerateTrackId()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }
}