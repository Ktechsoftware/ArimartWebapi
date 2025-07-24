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

        // ✅ Check if the product exists
        var productExists = await _context.TblProducts
            .AnyAsync(p => p.Id == request.ProductId);

        if (!productExists)
        {
            return NotFound(new { message = "Product not found" });
        }

        // ✅ Check if item is already in cart (including optional GroupId if present)
        var existingItem = await _context.TblAddcarts.FirstOrDefaultAsync(c =>
            c.Userid == request.UserId &&
            c.Pdid == request.ProductId &&
            c.Groupid == request.GroupId && // handles null too
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
                Groupid = request.GroupId, // ✅ Set only if provided
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

        // ✅ Filter only items without groupid
        var regularItems = items
            .Where(c => c.Groupid == null || c.Groupid == 0) // null or 0 treated as no group
            .ToList();

        var result = regularItems.Select(c => new
        {
            id = c.Aid,
            pid = c.Pid,
            pdid = c.Pdid,
            name = c.ProductName,
            price = decimal.TryParse(c.Netprice, out var p) ? p : 0,
            netprice = c.Netprice,
            discountprice = c.Discountprice,
            totalprice = c.Totalprice,
            gprice = c.Gprice,
            unittype = c.Unit,
            gqty = c.Gqty,
            image = c.Image,
            categoryId = c.Categoryid,
            categoryName = c.categoryName,
            subcategoryId = c.Subcategoryid,
            subcategoryName = c.SubcategoryName,
            childcategoryName = c.ChildcategoryName,
            quantity = c.Qty ?? 0,
            groupid = c.Groupid,
            vendorName = c.VendorName,
            userPhone = c.Phone,
            qtyprice = c.Qtyprice,
            groupcode = c.GroupCode
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

    // Add these methods to your CartController class

    [HttpDelete("group/{userId}/{groupId}")]
    public async Task<IActionResult> ClearGroupCart(int userId, long groupId)
    {
        var items = await _context.TblAddcarts
            .Where(c => c.Userid == userId && c.Groupid == groupId && !c.IsDeleted)
            .ToListAsync();

        if (!items.Any())
            return NotFound("Group cart already empty");

        foreach (var item in items)
        {
            item.IsDeleted = true;
            item.ModifiedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Group cart cleared" });
    }

    [HttpDelete("allgroups/{userId}")]
    public async Task<IActionResult> ClearAllGroupCarts(int userId)
    {
        var items = await _context.TblAddcarts
            .Where(c => c.Userid == userId && c.Groupid != null && !c.IsDeleted)
            .ToListAsync();

        if (!items.Any())
            return NotFound("No group cart items found");

        foreach (var item in items)
        {
            item.IsDeleted = true;
            item.ModifiedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "All group carts cleared" });
    }

    [HttpGet("usergroup")]
    public async Task<IActionResult> GetGroupCart(int userId, long groupId)
    {
        var items = await _context.VwCarts
            .Where(c => c.Cuserid == userId && c.Groupid == groupId && !c.IsDeleted1)
            .ToListAsync();

        if (!items.Any())
            return Ok(new { items = new List<object>(), totalItems = 0, subtotal = 0 });

        var result = items.Select(c => new
        {
            id = c.Aid,
            pid = c.Pid,
            pdid = c.Pdid,
            name = c.ProductName,
            price = decimal.TryParse(c.Netprice, out var p) ? p : 0,
            unittype = c.Unit,
            netprice = c.Netprice,
            discountprice = c.Discountprice,
            totalprice = c.Totalprice,
            gprice = c.Gprice,
            gqty = c.Gqty,
            image = c.Image,
            categoryId = c.Categoryid,
            categoryName = c.categoryName,
            subcategoryId = c.Subcategoryid,
            subcategoryName = c.SubcategoryName,
            childcategoryName = c.ChildcategoryName,
            quantity = c.Qty ?? 0,
            groupid = c.Groupid,
            vendorName = c.VendorName,
            userPhone = c.Phone,
            qtyprice = c.Qtyprice,
            groupcode = c.GroupCode // Include group code if available
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

    [HttpGet("groupcart")]
    public async Task<IActionResult> GetGroupCart(int userId)
    {
        var items = await _context.VwCarts
            .Where(c => c.Cuserid == userId && c.Groupid != null && !c.IsDeleted1)
            .ToListAsync();

        if (!items.Any())
            return Ok(new { items = new List<object>(), totalItems = 0, subtotal = 0 });

        var result = items.Select(c => {
            decimal.TryParse(c.Netprice, out var netPrice);
            return new
            {
                id = c.Aid,
                pid = c.Pid,
                pdid = c.Pdid,
                name = c.ProductName,
                unittype = c.Unit,
                price = netPrice,
                netprice = c.Netprice,
                discountprice = c.Discountprice,
                totalprice = c.Totalprice,
                gprice = c.Gprice,
                gqty = c.Gqty,
                image = c.Image,
                categoryId = c.Categoryid,
                categoryName = c.categoryName,
                subcategoryId = c.Subcategoryid,
                subcategoryName = c.SubcategoryName,
                childcategoryName = c.ChildcategoryName,
                quantity = c.Qty ?? 0,
                groupid = c.Groupid,
                vendorName = c.VendorName,
                userPhone = c.Phone,
                qtyprice = c.Qtyprice,
                groupcode = c.GroupCode // Include group code if available
            };
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





    // DTOs
    public class AddToCartRequest
    {
        public int UserId { get; set; }
        public long ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public long? GroupId { get; set; } // 👈 Add this for group support
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
