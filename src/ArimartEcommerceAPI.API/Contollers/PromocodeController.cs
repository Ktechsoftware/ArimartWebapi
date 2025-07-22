using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArimartEcommerceAPI.API.Contollers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PromocodeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PromocodeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/promocode/create
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreatePromoRequest request)
        {
            if (await _context.TblPromocodes.AnyAsync(p => p.Code == request.Code))
                return BadRequest("Promo code already exists.");

            var promo = new TblPromocode
            {
                Code = request.Code,
                Description = request.Description,
                DiscountType = request.DiscountType,
                DiscountValue = request.DiscountValue,
                MinOrderValue = request.MinOrderValue,
                MaxDiscount = request.MaxDiscount,
                UsageLimit = request.UsageLimit,
                PerUserLimit = request.PerUserLimit,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                AddedDate = DateTime.Now,
                IsDeleted = false,
                IsActive = true
            };

            _context.TblPromocodes.Add(promo);
            await _context.SaveChangesAsync();

            return Ok(promo);
        }

        // GET: api/promocode/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var usedPromoIds = await _context.TblPromocodeUsages
                .Where(u => u.UserId == userId)
                .Select(u => u.PromoId)
                .ToListAsync();

            var promos = await _context.TblPromocodes
                .Where(p => !p.IsDeleted && p.IsActive == true && usedPromoIds.Contains(p.PromoId))
                .ToListAsync();

            return Ok(promos);
        }

        // GET: api/promocode/id/{promoId}
        [HttpGet("id/{promoId}")]
        public async Task<IActionResult> GetById(int promoId)
        {
            var promo = await _context.TblPromocodes.FirstOrDefaultAsync(p => p.PromoId == promoId && !p.IsDeleted);
            if (promo == null) return NotFound("Promo not found.");
            return Ok(promo);
        }

        // POST: api/promocode/apply
        [HttpPost("apply")]
        public async Task<IActionResult> ApplyPromo([FromBody] ApplyPromoRequest request)
        {
            var promo = await _context.TblPromocodes.FirstOrDefaultAsync(p => p.Code == request.Code && !p.IsDeleted && p.IsActive == true);
            if (promo == null)
                return BadRequest("Invalid or expired promo code.");

            if (DateTime.Now < promo.StartDate || DateTime.Now > promo.EndDate)
                return BadRequest("Promo code not valid at this time.");

            if (request.OrderAmount < promo.MinOrderValue)
                return BadRequest($"Minimum order value should be ₹{promo.MinOrderValue}");

            var totalUsage = await _context.TblPromocodeUsages.CountAsync(u => u.PromoId == promo.PromoId);
            if (totalUsage >= promo.UsageLimit)
                return BadRequest("Promo code usage limit reached.");

            var userUsage = await _context.TblPromocodeUsages.CountAsync(u => u.PromoId == promo.PromoId && u.UserId == request.UserId);
            if (userUsage >= promo.PerUserLimit)
                return BadRequest("Promo code already used by this user.");

            decimal discount = 0;
            if (promo.DiscountType == "PERCENTAGE")
            {
                discount = request.OrderAmount * (promo.DiscountValue / 100);
                if (promo.MaxDiscount.HasValue && discount > promo.MaxDiscount.Value)
                    discount = promo.MaxDiscount.Value;
            }
            else if (promo.DiscountType == "FLAT")
            {
                discount = promo.DiscountValue;
            }

            _context.TblPromocodeUsages.Add(new TblPromocodeUsage
            {
                PromoId = promo.PromoId,
                UserId = request.UserId,
                UsedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Promo code applied.",
                Discount = discount,
                PayableAmount = request.OrderAmount - discount
            });
        }
    }

    public class CreatePromoRequest
    {
        public int UserId { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? DiscountType { get; set; } // "PERCENTAGE" or "FLAT"
        public decimal DiscountValue { get; set; }
        public decimal MinOrderValue { get; set; }
        public decimal? MaxDiscount { get; set; }
        public int UsageLimit { get; set; } = 1;
        public int PerUserLimit { get; set; } = 1;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class ApplyPromoRequest
    {
        public string? Code { get; set; }
        public int UserId { get; set; }
        public decimal OrderAmount { get; set; }
    }


}
