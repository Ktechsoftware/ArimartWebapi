using System;
using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArimartEcommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AddressesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AddressesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔹 POST: Add a new Addresses
        [HttpPost("add")]
        public async Task<IActionResult> AddAddresses([FromBody] Address request)
        {
            if (string.IsNullOrWhiteSpace(request.AdAddress1) ||
                string.IsNullOrWhiteSpace(request.AdCity) ||
                string.IsNullOrWhiteSpace(request.AdName) ||
                string.IsNullOrWhiteSpace(request.AdContact) ||
                request.AdPincode <= 0)
            {
                return BadRequest(new { message = "All required fields must be filled." });
            }

            // If setting as primary, unset other primary Addresseses
            if (request.IsPrimary == 1)
            {
                var existing = await _context.Addresses
                    .Where(a => a.UId == request.UId && a.IsPrimary == 1)
                    .ToListAsync();

                foreach (var addr in existing)
                    addr.IsPrimary = 0;
            }

            _context.Addresses.Add(request);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Addresses added successfully.", AddressesId = request.AdId });
        }

        // 🔹 GET: All Addresseses of user
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserAddresseses(int userId)
        {
            var Addresseses = await _context.Addresses
                .Where(a => a.UId == userId)
                .OrderByDescending(a => a.IsPrimary)
                .ToListAsync();

            return Ok(Addresseses);
        }

        // 🔹 GET: Primary Addresses of user
        [HttpGet("primary/{userId}")]
        public async Task<IActionResult> GetPrimaryAddresses(int userId)
        {
            var Addresses = await _context.Addresses
                .FirstOrDefaultAsync(a => a.UId == userId && a.IsPrimary == 1);

            if (Addresses == null)
                return NotFound(new { message = "Primary Addresses not found." });

            return Ok(Addresses);
        }

        // 🔹 PUT: Update an existing Addresses
        [HttpPut("update/{adId}")]
        public async Task<IActionResult> UpdateAddresses(int adId, [FromBody] Address updated)
        {
            var Addresses = await _context.Addresses.FindAsync(adId);
            if (Addresses == null)
                return NotFound(new { message = "Addresses not found." });

            Addresses.AdName = updated.AdName;
            Addresses.AdContact = updated.AdContact;
            Addresses.AdAddress1 = updated.AdAddress1;
            Addresses.AdAddress2 = updated.AdAddress2;
            Addresses.AdCity = updated.AdCity;
            Addresses.AdLandmark = updated.AdLandmark;
            Addresses.AdPincode = updated.AdPincode;

            // If changing to primary, reset others
            if (updated.IsPrimary == 1 && Addresses.IsPrimary == 0)
            {
                var others = await _context.Addresses
                    .Where(a => a.UId == Addresses.UId && a.IsPrimary == 1)
                    .ToListAsync();

                foreach (var other in others)
                    other.IsPrimary = 0;

                Addresses.IsPrimary = 1;
            }
            else
            {
                Addresses.IsPrimary = updated.IsPrimary;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Addresses updated successfully." });
        }

        // 🔹 DELETE: Remove an Addresses
        [HttpDelete("delete/{adId}")]
        public async Task<IActionResult> DeleteAddresses(int adId)
        {
            var Addresses = await _context.Addresses.FindAsync(adId);
            if (Addresses == null)
                return NotFound(new { message = "Addresses not found." });

            _context.Addresses.Remove(Addresses);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Addresses deleted successfully." });
        }

        // 🔹 PUT: Set an Addresses as primary
        [HttpPut("set-primary/{adId}")]
        public async Task<IActionResult> SetPrimaryAddresses(int adId)
        {
            var Addresses = await _context.Addresses.FindAsync(adId);
            if (Addresses == null)
                return NotFound(new { message = "Addresses not found." });

            // Unset other primary Addresseses of the user
            var others = await _context.Addresses
                .Where(a => a.UId == Addresses.UId && a.IsPrimary == 1)
                .ToListAsync();

            foreach (var other in others)
                other.IsPrimary = 0;

            Addresses.IsPrimary = 1;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Primary Addresses set successfully." });
        }
    }
}
