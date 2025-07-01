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

}
