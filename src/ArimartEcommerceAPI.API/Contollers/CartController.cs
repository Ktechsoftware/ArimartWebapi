using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/cart")]
public class CartController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CartController(ApplicationDbContext context)
    {
        _context = context;
    }

    // 1. ✅ Add to Cart
    [HttpPost("add/user")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
    {
        if (request.UserId <= 0 || request.ProductId <= 0 || request.Quantity <= 0)
            return BadRequest("Invalid input");

        var existingItem = await _context.TblAddcarts.FirstOrDefaultAsync(c =>
            c.Userid == request.UserId &&
            c.Pdid == request.ProductId &&
            !c.IsDeleted);

        if (existingItem != null)
        {
            existingItem.Qty += request.Quantity;
            existingItem.ModifiedDate = DateTime.UtcNow;
        }
        else
        {
            var item = new TblAddcart
            {
                Userid = request.UserId,
                Pdid = request.ProductId,
                Qty = request.Quantity,
                Price = request.Price,
                AddedDate = DateTime.UtcNow,
                IsDeleted = false,
                IsActive = true
            };
            await _context.TblAddcarts.AddAsync(item);
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Item added to cart" });
    }

    // 2. ✅ Get Cart Items by User
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetCart(int userId)
    {
        var items = await _context.VwCarts
            .Where(c => c.Cuserid == userId && !c.IsDeleted1)
            .ToListAsync();

        if (!items.Any())
            return Ok(new { items = new List<object>(), totalItems = 0, subtotal = 0 });

        var result = items.Select(c => new
        {
            id = c.Aid,
            name = c.ProductName,
            price = decimal.TryParse(c.Netprice, out var p) ? p : 0,
            image = c.Image,
            categoryName = c.Categoryid,
            subcategoryName = c.Subcategoryid,
            quantity = c.Qty ?? 0
        }).ToList();

        var totalItems = result.Sum(i => i.quantity);
        var subtotal = result.Sum(i => i.price * i.quantity);

        return Ok(new
        {
            items = result,
            totalItems,
            subtotal
        });
    }

    // 3. ✅ Update Quantity
    [HttpPut("update")]
    public async Task<IActionResult> UpdateQuantity([FromBody] UpdateCartRequest request)
    {
        var cartItem = await _context.TblAddcarts.FirstOrDefaultAsync(c =>
            c.Userid == request.UserId && c.Id == request.ItemId && !c.IsDeleted);

        if (cartItem == null)
            return NotFound("Cart item not found");

        cartItem.Qty = request.Quantity;
        cartItem.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Quantity updated" });
    }

    // 4. ✅ Remove Item from Cart
    [HttpDelete("remove")]
    public async Task<IActionResult> RemoveFromCart([FromBody] RemoveCartRequest request)
    {
        var cartItem = await _context.TblAddcarts.FirstOrDefaultAsync(c =>
            c.Userid == request.UserId && c.Id == request.ItemId && !c.IsDeleted);

        if (cartItem == null)
            return NotFound("Item not found");

        cartItem.IsDeleted = true;
        cartItem.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Item removed" });
    }

    // 5. ✅ Sync Entire Cart
    [HttpPost("sync")]
    public async Task<IActionResult> SyncCart([FromBody] SyncCartRequest request)
    {
        if (request.UserId <= 0)
            return BadRequest("Invalid UserId");

        // Soft delete all existing cart items
        var oldItems = await _context.TblAddcarts
            .Where(c => c.Userid == request.UserId && !c.IsDeleted)
            .ToListAsync();

        foreach (var item in oldItems)
        {
            item.IsDeleted = true;
            item.ModifiedDate = DateTime.UtcNow;
        }

        // Add new items
        foreach (var item in request.Items)
        {
            await _context.TblAddcarts.AddAsync(new TblAddcart
            {
                Userid = request.UserId,
                Pdid = item.ProductId,
                Qty = item.Quantity,
                Price = item.Price,
                AddedDate = DateTime.UtcNow,
                IsDeleted = false,
                IsActive = true
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Cart synced" });
    }

    // 6. ✅ Clear Cart
    [HttpDelete("user/{userId}")]
    public async Task<IActionResult> ClearCart(int userId)
    {
        var items = await _context.TblAddcarts
            .Where(c => c.Userid == userId && !c.IsDeleted)
            .ToListAsync();

        if (!items.Any())
            return NotFound("Cart already empty");

        foreach (var item in items)
        {
            item.IsDeleted = true;
            item.ModifiedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Cart cleared" });
    }

    // DTOs
    public class AddToCartRequest
    {
        public int UserId { get; set; }
        public long ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class UpdateCartRequest
    {
        public int UserId { get; set; }
        public int ItemId { get; set; }
        public int Quantity { get; set; }
    }

    public class RemoveCartRequest
    {
        public int UserId { get; set; }
        public int ItemId { get; set; }
    }

    public class SyncCartRequest
    {
        public int UserId { get; set; }
        public List<SyncCartItem> Items { get; set; }
    }

    public class SyncCartItem
    {
        public long ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
