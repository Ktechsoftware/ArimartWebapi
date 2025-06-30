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
    public class CategoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/category
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TblCategory>>> GetCategories()
        {
            return await _context.TblCategories
                .Where(c => !c.IsDeleted && c.IsActive == true)
                .ToListAsync();
        }

        // GET: api/category/{id}
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<TblCategory>> GetCategory(long id)
        {
            var category = await _context.TblCategories.FindAsync(id);

            if (category == null || category.IsDeleted)
                return NotFound();

            return category;
        }

        // POST: api/category
        [HttpPost]
        public async Task<ActionResult<TblCategory>> AddCategory(TblCategory category)
        {
            category.AddedDate = DateTime.UtcNow;
            category.IsDeleted = false;
            _context.TblCategories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
        }
    }
}
