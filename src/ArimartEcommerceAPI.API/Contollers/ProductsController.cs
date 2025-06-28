using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
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
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products.ToListAsync();
        }


        // GET: api/products/{id}
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            return product;
        }

        // GET: api/products/search?query=radish
        [AllowAnonymous]
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Product>>> SearchProducts([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { message = "Search query is required." });

            var results = await _context.Products
                .Where(p => 
                    p.PName.ToLower().Contains(query.ToLower()) ||
                    p.PDesc.ToLower().Contains(query.ToLower()) ||
                    p.PPros.ToLower().Contains(query.ToLower()))
                .ToListAsync();

            return Ok(results);
        }

        // GET: api/products/names
        [AllowAnonymous]
        [HttpGet("names")]
        public async Task<ActionResult<IEnumerable<string>>> GetProductNames([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                // Return first 10 names if query is empty (optional)
                var defaultNames = await _context.Products
                    .Where(p => !string.IsNullOrEmpty(p.PName))
                    .OrderBy(p => p.PName)
                    .Select(p => p.PName)
                    .Take(10)
                    .ToListAsync();

                return Ok(defaultNames);
            }

            var productNames = await _context.Products
                .Where(p => !string.IsNullOrEmpty(p.PName) &&
                            p.PName.ToLower().Contains(query.ToLower()))
                .OrderBy(p => p.PName)
                .Select(p => p.PName)
                .Take(15) 
                .ToListAsync();

            return Ok(productNames);
        }

    }
}
