using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.DTO;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CartController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("add/user")]
    public async Task<IActionResult> AddToCartByUser([FromBody] AddToCartRequest request)
    {
        if (request.Qty <= 0)
            return BadRequest(new { message = "Quantity must be greater than zero" });

        if (request.Userid == null)
            return BadRequest(new { message = "UserId is required for this operation" });

        var existingCartItem = await _context.TblAddcarts.FirstOrDefaultAsync(c =>
            c.Pdid == request.Pdid &&
            c.Userid == request.Userid &&
            !c.IsDeleted);

        if (existingCartItem != null)
        {
            existingCartItem.Qty += request.Qty;
            existingCartItem.ModifiedDate = DateTime.UtcNow;
        }
        else
        {
            var newItem = new TblAddcart
            {
                Pid = request.Pid,
                Pdid = request.Pdid,
                Qty = request.Qty,
                Userid = request.Userid,
                Price = request.Price,
                AddedDate = DateTime.UtcNow,
                IsDeleted = false,
                IsActive = true
            };

            await _context.TblAddcarts.AddAsync(newItem);
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Product added to cart for user successfully." });
    }

    [HttpPost("add/group")]
    public async Task<IActionResult> AddToCartByGroup([FromBody] AddToCartRequest request)
    {
        if (request.Qty <= 0)
            return BadRequest(new { message = "Quantity must be greater than zero" });

        if (request.Groupid == null)
            return BadRequest(new { message = "GroupId is required for guest cart" });

        var existingCartItem = await _context.TblAddcarts.FirstOrDefaultAsync(c =>
            c.Pdid == request.Pdid &&
            c.Groupid == request.Groupid &&
            !c.IsDeleted);

        if (existingCartItem != null)
        {
            existingCartItem.Qty += request.Qty;
            existingCartItem.ModifiedDate = DateTime.UtcNow;
        }
        else
        {
            var newItem = new TblAddcart
            {
                Pid = request.Pid,
                Pdid = request.Pdid,
                Qty = request.Qty,
                Groupid = request.Groupid,
                Price = request.Price,
                AddedDate = DateTime.UtcNow,
                IsDeleted = false,
                IsActive = true
            };

            await _context.TblAddcarts.AddAsync(newItem);
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Product added to guest cart successfully." });
    }

    // GET: api/cart/user/5
    [HttpGet("user/{userid}")]
    public async Task<IActionResult> GetCartByUserId(int userid)
    {
        var cartItems = await _context.VwCarts
            .Where(c => c.Cuserid == userid && !c.IsDeleted1)
            .ToListAsync();

        if (cartItems == null || cartItems.Count == 0)
            return NotFound(new { message = "No cart items found for this user." });

        return Ok(cartItems);
    }

    // GET: api/cart/usergroup?userid=5&groupid=112233
    [HttpGet("usergroup")]
    public async Task<IActionResult> GetCartByUserAndGroup([FromQuery] int userid, [FromQuery] long groupid)
    {
        var cartItems = await _context.VwCarts
            .Where(c =>
                c.Cuserid == userid &&
                c.Groupid == groupid &&
                !c.IsDeleted1)
            .ToListAsync();

        if (cartItems == null || cartItems.Count == 0)
            return NotFound(new { message = "No cart items found for this user and group." });

        return Ok(cartItems);
    }

    // Additional methods to add to your CartController.cs

    [HttpPut("{cartItemId}")]
    public async Task<IActionResult> UpdateCartItemQuantity(int cartItemId, [FromBody] UpdateQuantityRequest request)
    {
        if (request.Quantity <= 0)
            return BadRequest(new { message = "Quantity must be greater than zero" });

        var cartItem = await _context.TblAddcarts.FirstOrDefaultAsync(c =>
            c.Id == cartItemId && !c.IsDeleted);

        if (cartItem == null)
            return NotFound(new { message = "Cart item not found" });

        cartItem.Qty = request.Quantity;
        cartItem.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Cart item quantity updated successfully" });
    }

    [HttpDelete("{cartItemId}")]
    public async Task<IActionResult> RemoveFromCart(int cartItemId)
    {
        var cartItem = await _context.TblAddcarts.FirstOrDefaultAsync(c =>
            c.Id == cartItemId && !c.IsDeleted);

        if (cartItem == null)
            return NotFound(new { message = "Cart item not found" });

        // Soft delete
        cartItem.IsDeleted = true;
        cartItem.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Item removed from cart successfully" });
    }

    [HttpDelete("user/{userId}")]
    public async Task<IActionResult> ClearCartByUserId(int userId)
    {
        var cartItems = await _context.TblAddcarts
            .Where(c => c.Userid == userId && !c.IsDeleted)
            .ToListAsync();

        if (cartItems.Count == 0)
            return NotFound(new { message = "No cart items found for this user" });

        foreach (var item in cartItems)
        {
            item.IsDeleted = true;
            item.ModifiedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Cart cleared successfully" });
    }

    [HttpGet("count/user/{userId}")]
    public async Task<IActionResult> GetCartItemCount(int userId)
    {
        var count = await _context.TblAddcarts
            .Where(c => c.Userid == userId && !c.IsDeleted)
            .SumAsync(c => c.Qty);

        return Ok(new { count });
    }

    // DTO classes to add to your DTO folder
    public class UpdateQuantityRequest
    {
        public int Quantity { get; set; }
    }

    public class CartSummaryResponse
    {
        public int TotalItems { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
        public List<object> Items { get; set; }
    }
}
