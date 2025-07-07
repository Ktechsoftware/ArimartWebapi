using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.DTO;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class WishlistController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public WishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/wishlist
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> AddToWishlist([FromBody] AddToWishlistRequest request)
        {
            if (request.Userid == 0 || request.Pdid == 0)
                return BadRequest(new { message = "User ID and Product ID are required." });

            var existing = await _context.TblWishlists.FirstOrDefaultAsync(w =>
                w.Userid == request.Userid && w.Pdid == request.Pdid && !w.IsDeleted);

            if (existing != null)
                return BadRequest(new { message = "Product is already in the wishlist." });

            var wishlistItem = new TblWishlist
            {
                Userid = request.Userid,
                Pdid = request.Pdid
            };

            _context.TblWishlists.Add(wishlistItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Product added to wishlist." });
        }

        // GET: api/wishlist/{userid}
        [AllowAnonymous]
        [HttpGet("{userid}")]
        public async Task<IActionResult> GetWishlist(long userid)
        {
            var wishlist = await _context.VwWhishlists
                .Where(w => w.Cuserid == userid && w.IsDeleted == false && w.IsActive == true)
                .ToListAsync();

            return Ok(wishlist);
        }
    }
}
