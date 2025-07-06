using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArimartEcommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GroupController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GroupController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ GET: All Active Group Deals (For Listing)
        [AllowAnonymous]
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<VwGroup>>> GetAllGroups()
        {
            var groups = await _context.VwGroups
                .Where(g => g.IsDeleted1 == false)
                .OrderByDescending(g => g.AddedDate)
                .ToListAsync();

            return Ok(groups);
        }

        // ✅ GET: Single Group by Gid
        [AllowAnonymous]
        [HttpGet("{gid}")]
        public async Task<ActionResult<VwGroup>> GetGroupById(long gid)
        {
            var group = await _context.VwGroups
                .FirstOrDefaultAsync(g => g.Gid == gid && g.IsDeleted1 == false);

            if (group == null)
                return NotFound();

            return Ok(group);
        }

        // ✅ POST: Create a new group deal
        [AllowAnonymous]
        [HttpPost("create")]
        public async Task<IActionResult> CreateGroup([FromBody] TblGroupjoin groupJoin)
        {
            groupJoin.AddedDate = DateTime.UtcNow;
            groupJoin.IsDeleted = false;
            groupJoin.IsActive = true;

            _context.TblGroupjoins.Add(groupJoin);
            await _context.SaveChangesAsync();

            return Ok(groupJoin);
        }

        // ✅ PUT: Join existing group
        [AllowAnonymous]
        [HttpPost("join")]
        public async Task<IActionResult> JoinGroup([FromBody] TblGroupjoin join)
        {
            var exists = await _context.TblGroupjoins
                .FirstOrDefaultAsync(j => j.Groupid == join.Groupid && j.Userid == join.Userid && j.IsDeleted == false);

            if (exists != null)
                return BadRequest("User already joined this group.");

            join.AddedDate = DateTime.UtcNow;
            join.IsDeleted = false;
            join.IsActive = true;

            _context.TblGroupjoins.Add(join);
            await _context.SaveChangesAsync();

            return Ok(join);
        }

        // ✅ DELETE: Leave group
        [AllowAnonymous]
        [HttpDelete("leave")]
        public async Task<IActionResult> LeaveGroup([FromQuery] long groupId, [FromQuery] long userId)
        {
            var join = await _context.TblGroupjoins
                .FirstOrDefaultAsync(j => j.Groupid == groupId && j.Userid == userId && j.IsDeleted == false);

            if (join == null)
                return NotFound("Join not found");

            join.IsDeleted = true;
            join.ModifiedDate = DateTime.UtcNow;

            _context.TblGroupjoins.Update(join);
            await _context.SaveChangesAsync();

            return Ok("Left group successfully");
        }

        // ✅ GET: List of users joined in a group
        [AllowAnonymous]
        [HttpGet("members/{groupId}")]
        public async Task<ActionResult<IEnumerable<TblGroupjoin>>> GetGroupMembers(long groupId)
        {
            var members = await _context.TblGroupjoins
                .Where(j => j.Groupid == groupId && j.IsDeleted == false)
                .ToListAsync();

            return Ok(members);
        }

        // ✅ GET: My joined groups
        [AllowAnonymous]
        [HttpGet("my-joined/{userId}")]
        public async Task<ActionResult<IEnumerable<VwGroup>>> GetMyJoinedGroups(int userId)
        {
            var groupIds = await _context.TblGroupjoins
                .Where(j => j.Userid == userId && j.IsDeleted == false)
                .Select(j => j.Groupid)
                .ToListAsync();

            var groups = await _context.VwGroups
                .Where(g => groupIds.Contains(g.Gid) && g.IsDeleted1 == false)
                .ToListAsync();

            return Ok(groups);
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VwGrouprefercode>>> GetAll()
        {
            var referCodes = await _context.VwGrouprefercodes.ToListAsync();
            return Ok(referCodes);
        }

        // GET: api/grouprefercode/5
        [AllowAnonymous]
        [HttpGet("grouprefercode/{id}")]
        public async Task<ActionResult<VwGrouprefercode>> GetById(long id)
        {
            var referCode = await _context.VwGrouprefercodes.FirstOrDefaultAsync(r => r.Id == id);

            if (referCode == null)
                return NotFound();

            return Ok(referCode);
        }

        // GET: api/grouprefercode/by-product/101/202
        [AllowAnonymous]
        [HttpGet("grouprefercode/by-product/{pid}/{pdid}")]
        public async Task<ActionResult<VwGrouprefercode>> GetByProduct(long pid, long pdid)
        {
            var referCode = await _context.VwGrouprefercodes
                .FirstOrDefaultAsync(r => r.Pid == pid && r.Pdid == pdid);

            if (referCode == null)
                return NotFound();

            return Ok(referCode);
        }
    }
}
