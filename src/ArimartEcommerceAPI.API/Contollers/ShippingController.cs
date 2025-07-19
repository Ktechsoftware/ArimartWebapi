using System;
using System.Linq;
using System.Threading.Tasks;
using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArimartEcommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShippingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ShippingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔹 POST: Add a new shipping address
        [HttpPost("add")]
        public async Task<IActionResult> AddShipping([FromBody] TblShiping request)
        {
            if (string.IsNullOrWhiteSpace(request.Address) ||
                string.IsNullOrWhiteSpace(request.City) ||
                string.IsNullOrWhiteSpace(request.ContactPerson) ||
                string.IsNullOrWhiteSpace(request.Phone) ||
                string.IsNullOrWhiteSpace(request.PostalCode))
            {
                return BadRequest(new { message = "All required fields must be filled." });
            }

            // 🔹 Check for valid pincode range
            if (int.TryParse(request.PostalCode, out int pincode))
            {
                if (pincode < 800001 || pincode > 800030)
                {
                    return BadRequest(new { message = "Sorry, we are not available at this pincode." });
                }
            }
            else
            {
                return BadRequest(new { message = "Invalid pincode format." });
            }

            request.AddedDate = DateTime.UtcNow;
            request.IsDeleted = false;
            request.IsActive = true;

            _context.TblShipings.Add(request);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Shipping address added successfully.", sipid = request.Id });
        }


        // 🔹 GET: All shipping addresses of user
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserShipping(int userId)
        {
            var list = await _context.TblShipings
                .Where(s => s.Userid == userId && !s.IsDeleted)
                .OrderByDescending(s => s.AddedDate)
                .ToListAsync();

            return Ok(list);
        }

        // 🔹 GET: Get a shipping address by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetShippingById(long id)
        {
            var ship = await _context.TblShipings
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (ship == null)
                return NotFound(new { message = "Shipping address not found." });

            return Ok(ship);
        }

        // 🔹 PUT: Update a shipping address
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateShipping(long id, [FromBody] TblShiping updated)
        {
            var existing = await _context.TblShipings.FindAsync(id);
            if (existing == null || existing.IsDeleted)
                return NotFound(new { message = "Shipping address not found." });

            // 🔹 Check for valid pincode range
            if (int.TryParse(updated.PostalCode, out int pincode))
            {
                if (pincode < 800001 || pincode > 800030)
                {
                    return BadRequest(new { message = "Sorry, we are not available at this pincode." });
                }
            }
            else
            {
                return BadRequest(new { message = "Invalid pincode format." });
            }

            existing.VendorName = updated.VendorName;
            existing.ContactPerson = updated.ContactPerson;
            existing.Email = updated.Email;
            existing.Phone = updated.Phone;
            existing.Address = updated.Address;
            existing.City = updated.City;
            existing.State = updated.State;
            existing.PostalCode = updated.PostalCode;
            existing.Country = updated.Country;
            existing.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Shipping address updated successfully." });
        }


        // 🔹 DELETE: Soft delete a shipping address
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteShipping(long id)
        {
            var existing = await _context.TblShipings.FindAsync(id);
            if (existing == null || existing.IsDeleted)
                return NotFound(new { message = "Shipping address not found." });

            existing.IsDeleted = true;
            existing.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Shipping address deleted successfully." });
        }
    }
}
