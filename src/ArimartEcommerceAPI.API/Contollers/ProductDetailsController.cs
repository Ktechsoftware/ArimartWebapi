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
    public class ProductDetailsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductDetailsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/productdetails
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TblProductdetail>>> GetProductDetails()
        {
            var details = await _context.TblProductdetails
                .Where(p => !p.IsDeleted && p.IsActive == true)
                .ToListAsync();

            return Ok(details);
        }

        // GET: api/productdetails/{id}
        [AllowAnonymous]
        [HttpGet("{productId}")]
        public async Task<ActionResult<TblProductdetail>> GetProductDetailByProductId(long productId)
        {
            var detail = await _context.TblProductdetails
                .Where(d => d.Productid == productId && !d.IsDeleted && d.IsActive == true)
                .FirstOrDefaultAsync();

            if (detail == null)
                return NotFound(new { message = "No product detail found for the given product ID." });

            return Ok(detail);
        }


    }
}
