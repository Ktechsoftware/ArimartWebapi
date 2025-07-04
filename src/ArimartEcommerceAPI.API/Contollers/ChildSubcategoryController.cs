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
    public class ChildSubcategoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ChildSubcategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/childsubcategory
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TblChildSubcategory>>> GetChildSubcategories()
        {
            return await _context.TblChildSubcategories
                .Where(c => !c.IsDeleted && c.IsActive == true)
                .ToListAsync();
        }

        // GET: api/childsubcategory/{id}
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<TblChildSubcategory>> GetChildSubcategory(long id)
        {
            var item = await _context.TblChildSubcategories.FindAsync(id);

            if (item == null || item.IsDeleted)
                return NotFound();

            return item;
        }

        // GET: api/childsubcategory/by-subcategory/3
        [AllowAnonymous]
        [HttpGet("by-subcategory/{subcategoryId}")]
        public async Task<ActionResult<IEnumerable<TblChildSubcategory>>> GetBySubcategory(long subcategoryId)
        {
            return await _context.TblChildSubcategories
                .Where(c => c.Subcategoryid == subcategoryId && !c.IsDeleted && c.IsActive == true)
                .ToListAsync();
        }

        // POST: api/childsubcategory
        [HttpPost]
        public async Task<ActionResult<TblChildSubcategory>> AddChildSubcategory(TblChildSubcategory item)
        {
            item.AddedDate = DateTime.UtcNow;
            item.IsDeleted = false;
            _context.TblChildSubcategories.Add(item);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetChildSubcategory), new { id = item.Id }, item);
        }
    }
}
