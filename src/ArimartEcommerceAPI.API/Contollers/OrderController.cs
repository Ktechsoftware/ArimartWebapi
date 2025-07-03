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
            // Parse cart IDs from comma-separated string (now as long)
            var cartIds = request.Addid
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => Convert.ToInt64(id.Trim()))
                .ToList();

            // Fetch matching cart items with qty > 0
            var cartItems = await _context.TblAddcarts
                .Where(c => cartIds.Contains(c.Id) && c.Qty > 0)
                .ToListAsync();

            if (cartItems.Count == 0)
                return BadRequest(new { message = "No valid cart items found for checkout." });

            // Map to order entities
            var newOrders = cartItems.Select(c => new TblOrdernow
            {
                Qty = c.Qty,
                Pid = c.Pid,
                Pdid = c.Pdid,
                Userid = request.Userid,
                Sipid = int.TryParse(request.Sipid, out var sipid) ? sipid : (int?)null,
                Groupid = c.Groupid,
                Deliveryprice = c.Price
            }).ToList();

            await _context.TblOrdernows.AddRangeAsync(newOrders);

            // Update tbl_addcart
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

            return Ok(new { message = "Checkout successful." });
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

        var order = new TblOrdernow
        {
            Qty = request.Qty,
            Pid = request.Pid,
            Pdid = request.Pdid,
            Userid = request.Userid,
            Groupid = null, // This is a normal order
            Deliveryprice = request.Deliveryprice
        };

        await _context.TblOrdernows.AddAsync(order);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Order placed successfully." });
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
    public async Task<IActionResult> GetOrderHistory(int userid)
    {
        var orders = await _context.TblOrdernows
            .Where(o => o.Userid == userid && o.IsDeleted == false)
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



    [HttpGet("track/{orderid}")]
    public async Task<IActionResult> TrackOrder(long orderid)
    {
        var order = await _context.TblOrdernows
            .Where(o => o.Id == orderid && !o.IsDeleted)
            .Select(o => new
            {
                o.Id,
                o.Pid,
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

}
