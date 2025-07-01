using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArimartEcommerceAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/products
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VwProduct>>> GetProducts()
        {
            return await _context.VwProducts
                .Where(p => p.IsDeleted == false && p.IsActive == true)
                .OrderByDescending(p => p.AddedDate)
                .ToListAsync();
        }

        // GET: api/products/{id}
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<VwProduct>> GetProduct(long id)
        {
            var product = await _context.VwProducts
                .FirstOrDefaultAsync(p => p.Id == id && p.IsDeleted == false && p.IsActive == true);

            if (product == null)
                return NotFound();

            return product;
        }

        // GET: api/products/search?query=radish
        [AllowAnonymous]
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<VwProduct>>> SearchProducts([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { message = "Search query is required." });

            var results = await _context.VwProducts
                .Where(p => !p.IsDeleted && p.IsActive == true &&
                    (
                        (!string.IsNullOrEmpty(p.ProductName) && p.ProductName.ToLower().Contains(query.ToLower())) ||
                        (!string.IsNullOrEmpty(p.Shortdesc) && p.Shortdesc.ToLower().Contains(query.ToLower())) ||
                        (!string.IsNullOrEmpty(p.PPros) && p.PPros.ToLower().Contains(query.ToLower()))
                    ))
                .OrderByDescending(p => p.AddedDate)
                .ToListAsync();


            return Ok(results);
        }

        // GET: api/products/names?query=veg
        [AllowAnonymous]
        [HttpGet("names")]
        public async Task<ActionResult<IEnumerable<string>>> GetProductNames([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                var defaultNames = await _context.VwProducts
                    .Where(p => !p.IsDeleted && p.IsActive == true && !string.IsNullOrEmpty(p.ProductName))
                    .OrderBy(p => p.ProductName)
                    .Select(p => p.ProductName)
                    .Take(10)
                    .ToListAsync();

                return Ok(defaultNames);
            }

            var productNames = await _context.VwProducts
                .Where(p => !p.IsDeleted && p.IsActive == true &&
                            !string.IsNullOrEmpty(p.ProductName) &&
                            p.ProductName.ToLower().Contains(query.ToLower()))
                .OrderBy(p => p.ProductName)
                .Select(p => p.ProductName)
                .Take(15)
                .ToListAsync();

            return Ok(productNames);
        }
    }
}
