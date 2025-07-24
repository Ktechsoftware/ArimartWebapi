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
                RewardType = request.RewardType, // ✅ NEW
                AddedDate = DateTime.Now,
                IsDeleted = false,
                IsActive = true
            };

            _context.TblPromocodes.Add(promo);
            await _context.SaveChangesAsync();

            return Ok(promo);
        }

        [HttpPut("update/{promoId}")]
        public async Task<IActionResult> Update(int promoId, [FromBody] CreatePromoRequest request)
        {
            var promo = await _context.TblPromocodes.FindAsync(promoId);
            if (promo == null || promo.IsDeleted) return NotFound("Promo not found.");

            promo.Code = request.Code;
            promo.Description = request.Description;
            promo.DiscountType = request.DiscountType;
            promo.DiscountValue = request.DiscountValue;
            promo.MinOrderValue = request.MinOrderValue;
            promo.MaxDiscount = request.MaxDiscount;
            promo.UsageLimit = request.UsageLimit;
            promo.PerUserLimit = request.PerUserLimit;
            promo.StartDate = request.StartDate;
            promo.EndDate = request.EndDate;
            promo.RewardType = request.RewardType; // ✅ NEW
            promo.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(promo);
        }

        // ================= ADMIN ==================
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var promos = await _context.TblPromocodes
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.AddedDate)
                .ToListAsync();

            return Ok(promos);
        }

        [HttpDelete("delete/{promoId}")]
        public async Task<IActionResult> Delete(int promoId)
        {
            var promo = await _context.TblPromocodes.FindAsync(promoId);
            if (promo == null || promo.IsDeleted) return NotFound("Promo not found.");

            promo.IsDeleted = true;
            promo.IsActive = false;
            promo.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok("Promo deleted.");
        }

        [HttpPost("assign-products")]
        public async Task<IActionResult> AssignToProducts([FromBody] PromoProductAssignRequest request)
        {
            var promo = await _context.TblPromocodes.FirstOrDefaultAsync(p => p.PromoId == request.PromoId);
            if (promo == null || promo.IsDeleted) return NotFound("Promo not found.");

            var existing = _context.TblPromoProducts
                .Where(p => p.PromoId == request.PromoId);
            _context.TblPromoProducts.RemoveRange(existing); // Reset old links

            foreach (var productId in request.ProductIds)
            {
                _context.TblPromoProducts.Add(new TblPromoProduct
                {
                    PromoId = request.PromoId,
                    ProductId = productId
                });
            }

            await _context.SaveChangesAsync();
            return Ok("Products assigned to promo.");
        }

        [HttpGet("products/{promoId}")]
        public async Task<IActionResult> GetAssignedProducts(int promoId)
        {
            var productIds = await _context.TblPromoProducts
                .Where(p => p.PromoId == promoId)
                .Select(p => p.ProductId)
                .ToListAsync();

            return Ok(productIds);
        }

        [HttpPost("validate-product")]
        public async Task<IActionResult> ValidatePromoForProduct([FromBody] PromoProductValidationRequest request)
        {
            var promo = await _context.TblPromocodes
                .FirstOrDefaultAsync(p => p.Code == request.Code && !p.IsDeleted && p.IsActive == true);
            if (promo == null) return BadRequest("Invalid promo.");

            var isProductAllowed = await _context.TblPromoProducts
                .AnyAsync(p => p.PromoId == promo.PromoId && p.ProductId == request.ProductId);

            if (!isProductAllowed)
                return BadRequest("Promo not valid for this product.");

            return Ok("Promo valid for this product.");
        }

        [HttpPut("toggle/{promoId}")]
        public async Task<IActionResult> ToggleStatus(int promoId)
        {
            var promo = await _context.TblPromocodes.FindAsync(promoId);
            if (promo == null || promo.IsDeleted) return NotFound("Promo not found.");

            promo.IsActive = !promo.IsActive;
            promo.ModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok($"Promo {(promo.IsActive == true ? "activated" : "deactivated")}.");
        }

        [HttpGet("available")]
        public async Task<IActionResult> GetAvailablePromos()
        {
            var now = DateTime.Now;
            var promos = await _context.TblPromocodes
                .Where(p => !p.IsDeleted && p.IsActive == true && now >= p.StartDate && now <= p.EndDate)
                .ToListAsync();

            return Ok(promos);
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

        // GET: api/promocode/my-rewards/{userId}
        [HttpGet("my-rewards/{userId}")]
        public async Task<IActionResult> GetMyRewards(int userId)
        {
            var now = DateTime.Now;

            var allPromos = await _context.TblPromocodes
                .Where(p => !p.IsDeleted)
                .Select(p => new PromoViewModel
                {
                    PromoId = p.PromoId,
                    Code = p.Code,
                    Description = p.Description,
                    DiscountType = p.DiscountType,
                    DiscountValue = p.DiscountValue,
                    RewardType = p.RewardType,
                    MinOrderValue = p.MinOrderValue,
                    MaxDiscount = p.MaxDiscount,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    IsActive = p.IsActive ?? false,
                    UsageLimit = p.UsageLimit,
                    PerUserLimit = p.PerUserLimit,
                    TotalUsage = _context.TblPromocodeUsages.Count(u => u.PromoId == p.PromoId),
                    UserUsage = _context.TblPromocodeUsages.Count(u => u.PromoId == p.PromoId && u.UserId == userId)
                })
                .ToListAsync();

            var rewards = allPromos.Select(promo => new
            {
                promo.PromoId,
                promo.Code,
                promo.Description,
                promo.DiscountType,
                promo.DiscountValue,
                promo.RewardType,
                promo.MinOrderValue,
                promo.MaxDiscount,
                promo.StartDate,
                promo.EndDate,
                Status = GetPromoStatus(promo, now)
            });

            return Ok(rewards);
        }

        private string GetPromoStatus(PromoViewModel promo, DateTime now)
        {
            if (!promo.IsActive || now > promo.EndDate)
                return "Expired";
            if (promo.UserUsage >= promo.PerUserLimit)
                return "Used";
            if (promo.TotalUsage >= promo.UsageLimit)
                return "Used";
            if (now >= promo.StartDate && now <= promo.EndDate)
                return "Available";
            return "Expired";
        }

        // update scratchcard status
        [HttpPost("mark-scratch")]
        public async Task<IActionResult> MarkAsScratched([FromBody] ScratchPromoRequest request)
        {
            var usage = await _context.TblPromocodeUsages
                .FirstOrDefaultAsync(x => x.PromoId == request.PromoId && x.UserId == request.UserId);

            if (usage == null)
                return NotFound("Promo not used yet.");

            if (usage.ScratchRevealed)
                return Ok("Already marked as scratched.");

            usage.ScratchRevealed = true;
            _context.TblPromocodeUsages.Update(usage);
            await _context.SaveChangesAsync();

            return Ok("Marked as scratched.");
        }

        public class ScratchPromoRequest
        {
            public int PromoId { get; set; }
            public int UserId { get; set; }
        }


        // POST: api/promocode/apply
        [HttpPost("apply")]
        public async Task<IActionResult> ApplyPromo([FromBody] ApplyPromoRequest request)
        {
            var promo = await _context.TblPromocodes.FirstOrDefaultAsync(p =>
                p.Code == request.Code && !p.IsDeleted && p.IsActive == true);

            if (promo == null)
                return BadRequest("Invalid or expired promo code.");

            if (DateTime.Now < promo.StartDate || DateTime.Now > promo.EndDate)
                return BadRequest("Promo code not valid at this time.");

            if (request.OrderAmount < promo.MinOrderValue)
                return BadRequest($"Minimum order value should be ₹{promo.MinOrderValue}");

            var totalUsage = await _context.TblPromocodeUsages.CountAsync(u => u.PromoId == promo.PromoId);
            if (totalUsage >= promo.UsageLimit)
                return BadRequest("Promo code usage limit reached.");

            var userUsage = await _context.TblPromocodeUsages.CountAsync(u =>
                u.PromoId == promo.PromoId && u.UserId == request.UserId);
            if (userUsage >= promo.PerUserLimit)
                return BadRequest("Promo code already used by you.");

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

            if (discount >= request.OrderAmount)
                return BadRequest("Promo code cannot be applied on this order.");

            return Ok(new
            {
                Message = "Promo code applied.",
                Discount = discount,
                PayableAmount = request.OrderAmount - discount,
                PromoId = promo.PromoId,
                OrderId = request.OrderId,
                Code = promo.Code
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
        public string RewardType { get; set; } = "PROMO_CODE";
    }

    public class ApplyPromoRequest
    {
        public string? Code { get; set; }
        public int UserId { get; set; }
        public decimal OrderAmount { get; set; }
        public int? OrderId { get; set; }
    }

    public class PromoProductAssignRequest
    {
        public int PromoId { get; set; }
        public List<int> ProductIds { get; set; } = new();
    }


    public class PromoProductValidationRequest
    {
        public string? Code { get; set; }
        public int ProductId { get; set; }
    }

    public class PromoViewModel
    {
        public int PromoId { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public string? RewardType { get; set; }
        public decimal MinOrderValue { get; set; }
        public decimal? MaxDiscount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public int UsageLimit { get; set; }
        public int PerUserLimit { get; set; }
        public int? TotalUsage { get; set; }
        public int? UserUsage { get; set; }
    }

}
