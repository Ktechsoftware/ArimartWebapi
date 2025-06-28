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
    public class SubcategoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SubcategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/subcategory
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TblSubcategory>>> GetSubcategories()
        {
            return await _context.TblSubcategories
                .Where(s => !s.IsDeleted && s.IsActive == true)
                .ToListAsync();
        }

        // GET: api/subcategory/by-category/{categoryId}
        [AllowAnonymous]
        [HttpGet("by-category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<TblSubcategory>>> GetSubcategoriesByCategoryId(long categoryId)
        {
            return await _context.TblSubcategories
                .Where(s => s.Categoryid == categoryId && !s.IsDeleted && s.IsActive == true)
                .ToListAsync();
        }

    }
}
