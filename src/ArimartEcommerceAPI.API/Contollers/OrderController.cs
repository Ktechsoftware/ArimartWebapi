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
                Sipid = int.TryParse(request.Sipid, out var sipid) ? sipid : (int?)null,
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
                        join p in _context.TblProducts on (int?)order.Pdid equals (int?)p.Id into pJoin
                        from p in pJoin.DefaultIfEmpty()
                        join c in _context.TblCategories on (p != null ? p.Categoryid : (int?)null) equals (int?)c.Id into cJoin
                        from c in cJoin.DefaultIfEmpty()
                        join sc in _context.TblSubcategories on (p != null ? p.Subcategoryid : (int?)null) equals (int?)sc.Id into scJoin
                        from sc in scJoin.DefaultIfEmpty()
                        join cc in _context.TblChildSubcategories on (p != null ? p.childcategoryid : (int?)null) equals (int?)cc.Id into ccJoin
                        from cc in ccJoin.DefaultIfEmpty()
                        where order.Userid == userid && !order.IsDeleted
                        select new
                        {
                            order.TrackId,
                            order.Id,
                            order.AddedDate,
                            order.Qty,
                            order.Deliveryprice,
                            ProductName = p != null ? p.ProductName : null,
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

        return Ok(orders);
    }
    // GET: api/order/history/{userid}/{groupid}
    [HttpGet("history/{userid}/{groupid}")]
    public async Task<IActionResult> GetGroupOrderHistory(int userid, long groupid)
    {
        var orders = await _context.TblOrdernows
            .Where(o => o.Userid == userid && o.Groupid == groupid && o.IsDeleted == false)
            .OrderByDescending(o => o.AddedDate)
            .ToListAsync();

        return Ok(orders);
    }



    [HttpGet("track/{trackId}")]
    public async Task<IActionResult> TrackOrder(string trackId)
    {
        var order = await _context.TblOrdernows
            .Where(o => o.TrackId == trackId && !o.IsDeleted)
            .Select(o => new
            {
                o.TrackId,
                o.Id,
                o.Pid,
                o.Pdid,
                o.Qty,
                o.AddedDate,
                o.DassignidTime,
                o.DvendorpickupTime,
                o.ShipOrderidTime,
                o.DdeliverredidTime,
                o.DuserassginidTime,
                Status = o.DdeliverredidTime != null ? "Delivered"
                        : o.ShipOrderidTime != null ? "Shipped"
                        : o.DvendorpickupTime != null ? "Picked Up"
                        : o.DassignidTime != null ? "Assigned"
                        : "Placed"
            })
            .FirstOrDefaultAsync();

        if (order == null)
            return NotFound(new { message = "Order not found." });

        return Ok(order);
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

    private string GenerateTrackId()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
    }

}
