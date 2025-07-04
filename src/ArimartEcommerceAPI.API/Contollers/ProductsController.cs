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

        // GET: api/products?page=1&pageSize=10
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<ProductsResponse>> GetProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            // Ensure reasonable pagination limits
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 10;

            var query = _context.VwProducts
                .Where(p => p.IsDeleted == false && p.IsActive == true)
                .OrderByDescending(p => p.AddedDate);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new ProductsResponse
            {
                Products = products,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };

            return Ok(response);
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

        // GET: api/products/search?query=radish&page=1&pageSize=10
        [AllowAnonymous]
        [HttpGet("search")]
        public async Task<ActionResult<ProductsResponse>> SearchProducts([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(new { message = "Search query is required." });

            // Ensure reasonable pagination limits
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 10;

            var searchQuery = _context.VwProducts
                .Where(p => !p.IsDeleted && p.IsActive == true &&
                    (
                        (!string.IsNullOrEmpty(p.ProductName) && p.ProductName.ToLower().Contains(query.ToLower())) ||
                        (!string.IsNullOrEmpty(p.Shortdesc) && p.Shortdesc.ToLower().Contains(query.ToLower())) ||
                        (!string.IsNullOrEmpty(p.PPros) && p.PPros.ToLower().Contains(query.ToLower()))
                    ))
                .OrderByDescending(p => p.AddedDate);

            var totalCount = await searchQuery.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var products = await searchQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new ProductsResponse
            {
                Products = products,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1,
                SearchQuery = query
            };

            return Ok(response);
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

        // GET: api/products/by-childcategory/5?page=1&pageSize=10
        [AllowAnonymous]
        [HttpGet("by-childcategory/{childCategoryId}")]
        public async Task<ActionResult<ProductsResponse>> GetProductsByChildCategory(
            int childCategoryId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 50) pageSize = 10;

            var query = _context.VwProducts
               .Where(p => p.ChildCategoryId == childCategoryId && !p.IsDeleted && p.IsActive == true)
                .OrderByDescending(p => p.AddedDate);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new ProductsResponse
            {
                Products = products,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };

            return Ok(response);
        }


    }

    // Response model for paginated products
    public class ProductsResponse
    {
        public IEnumerable<VwProduct> Products { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        public string SearchQuery { get; set; }
    }
}