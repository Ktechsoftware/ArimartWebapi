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
            var now = DateTime.UtcNow;
            var groups = await _context.VwGroups
                 .Where(g =>
                    g.EventSend1 != null &&
                    g.EventSend1 > now &&
                    g.IsDeleted1 == false
                )
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

        // ✅ POST: Create a new group deal (Based on tbl_Groupby table)
        [AllowAnonymous]
        [HttpPost("create")]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
        {
            try
            {
                // 1. Create the group
                var newGroup = new TblGroupby
                {
                    Qty = request.QTY,
                    Pid = request.PID,
                    Pdid = request.PDID,
                    Userid = request.userid,
                    Acctt = request.acctt,
                    Sipid = request.sipid
                };

                _context.TblGroupbies.Add(newGroup);
                await _context.SaveChangesAsync();

                // 2. Auto-join: Add creator as a member in TblGroupjoin
                var creatorJoin = new TblGroupjoin
                {
                    Groupid = newGroup.Id, // Use the group Id from the newly created group
                    Userid = request.userid,
                    AddedDate = DateTime.UtcNow,
                    IsDeleted = false,
                    IsActive = true
                };
                _context.TblGroupjoins.Add(creatorJoin);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    RESULT = newGroup.Id,
                    STATUS = 1,
                    Message = "Group created and join."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    RESULT = (long?)null,
                    STATUS = 0,
                    Message = ex.Message
                });
            }
        }

        // ✅ PUT: Update existing group
        [AllowAnonymous]
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateGroup(long id, [FromBody] UpdateGroupRequest request)
        {
            try
            {
                var existingGroup = await _context.TblGroupbies
                    .FirstOrDefaultAsync(g => g.Id == id && g.IsDeleted == false);

                if (existingGroup == null)
                    return NotFound(new
                    {
                        RESULT = (long?)null,
                        STATUS = 0,
                        Message = "Group not found"
                    });

                existingGroup.Qty = request.QTY;
                existingGroup.Pid = request.PID;
                existingGroup.Pdid = request.PDID;
                existingGroup.Userid = request.userid;

                _context.TblGroupbies.Update(existingGroup);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    RESULT = id,
                    STATUS = 1,
                    Message = "Group updated successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    RESULT = (long?)null,
                    STATUS = 0,
                    Message = ex.Message
                });
            }
        }

        // ✅ DELETE: Delete/Soft delete group
        [AllowAnonymous]
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteGroup(long id)
        {
            try
            {
                var group = await _context.TblGroupbies
                    .FirstOrDefaultAsync(g => g.Id == id && g.IsDeleted == false);

                if (group == null)
                    return NotFound(new
                    {
                        RESULT = "Group not found",
                        STATUS = 0
                    });

                group.IsDeleted = true;

                _context.TblGroupbies.Update(group);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    RESULT = "RECORD DELETED SUCCESSFULLY......",
                    STATUS = 1
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    RESULT = ex.Message,
                    STATUS = 0
                });
            }
        }

        // ✅ POST: Join existing group (Based on tbl_Groupjoin table)
        [AllowAnonymous]
        [HttpPost("join")]
        public async Task<IActionResult> JoinGroup([FromBody] JoinGroupRequest request)
        {
            try
            {
                // Check if the user has already joined this group
                var existingJoin = await _context.TblGroupjoins
                    .FirstOrDefaultAsync(j => j.Groupid == request.Groupid &&
                                            j.Userid == request.userid &&
                                            j.IsDeleted == false);

                if (existingJoin != null)
                {
                    return BadRequest(new
                    {
                        RESULT = (long?)null,
                        STATUS = 0,
                        Message = "User already joined this group"
                    });
                }

                var newJoin = new TblGroupjoin
                {
                    Groupid = request.Groupid,
                    Userid = request.userid,
                    AddedDate = DateTime.UtcNow,
                    IsDeleted = false,
                    IsActive = true
                };

                _context.TblGroupjoins.Add(newJoin);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    RESULT = newJoin.Id,
                    STATUS = 1,
                    Message = "Successfully joined the group"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    RESULT = (long?)null,
                    STATUS = 0,
                    Message = ex.Message
                });
            }
        }

        // ✅ DELETE: Leave group
        [AllowAnonymous]
        [HttpDelete("leave")]
        public async Task<IActionResult> LeaveGroup([FromQuery] long groupId, [FromQuery] long userId)
        {
            try
            {
                var join = await _context.TblGroupjoins
                    .FirstOrDefaultAsync(j => j.Groupid == groupId &&
                                            j.Userid == userId &&
                                            j.IsDeleted == false);

                if (join == null)
                    return NotFound(new
                    {
                        RESULT = "Join record not found",
                        STATUS = 0
                    });

                join.IsDeleted = true;
                join.ModifiedDate = DateTime.UtcNow;

                _context.TblGroupjoins.Update(join);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    RESULT = "Left group successfully",
                    STATUS = 1
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    RESULT = ex.Message,
                    STATUS = 0
                });
            }
        }

        // ✅ GET: List of users joined in a group
        [AllowAnonymous]
        [HttpGet("members/{groupId}")]
        public async Task<ActionResult> GetGroupMembers(long groupId)
        {
            var membersWithUserDetails = await _context.TblGroupjoins
                .Where(j => j.Groupid == groupId && !j.IsDeleted)
                .Join(
                    _context.TblUsers,
                    j => j.Userid,
                    u => u.Id,
                    (j, u) => new
                    {
                        groupJoinId = j.Id,
                        groupId = j.Groupid,
                        userId = u.Id,
                        userName = u.VendorName ?? u.ContactPerson,
                        phone = u.Phone,
                        email = u.Email,
                        userType = u.UserType,
                        isActive = j.IsActive,
                        addedDate = j.AddedDate,
                        acctt = j.Acctt,
                        sipid = j.Sipid
                    })
                .ToListAsync();

            return Ok(membersWithUserDetails);
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

        // Referral code endpoints (existing)
        [AllowAnonymous]
        [HttpGet("refercode")]
        public async Task<ActionResult<IEnumerable<VwGrouprefercode>>> GetAllReferCodes()
        {
            var referCodes = await _context.VwGrouprefercodes.ToListAsync();
            return Ok(referCodes);
        }

        [AllowAnonymous]
        [HttpGet("refercode/{id}")]
        public async Task<ActionResult<VwGrouprefercode>> GetReferCodeById(long id)
        {
            var referCode = await _context.VwGrouprefercodes.FirstOrDefaultAsync(r => r.Id == id);

            if (referCode == null)
                return NotFound();

            return Ok(referCode);
        }

        [AllowAnonymous]
        [HttpGet("refercode/by-product/{pid}/{pdid}")]
        public async Task<ActionResult<VwGrouprefercode>> GetReferCodeByProduct(long pid, long pdid)
        {
            var referCode = await _context.VwGrouprefercodes
                .FirstOrDefaultAsync(r => r.Pid == pid && r.Pdid == pdid);

            if (referCode == null)
                return NotFound();

            return Ok(referCode);
        }

        [AllowAnonymous]
        [HttpGet("current-running")]
        public async Task<IActionResult> GetCurrentRunningGroups()
        {
            var now = DateTime.UtcNow;

            var runningGroups = await _context.TblGroupbies
                .Where(g =>
                    g.EventSend1 != null &&
                    g.EventSend1 > now &&
                    g.IsDeleted == false
                )
                .OrderBy(g => g.EventSend1)
                .ToListAsync();
            return Ok(runningGroups);
        }

        [AllowAnonymous]
        [HttpGet("status-short/{gid}")]
        public async Task<IActionResult> GetGroupShortStatus(long gid)
        {
            var group = await _context.VwGroups
                .FirstOrDefaultAsync(g => g.Gid == gid && g.IsDeleted1 == false);
            if (group == null)
            {
                return NotFound(new
                {
                    STATUS = 0,
                    MESSAGE = "Group not found"
                });
            }
            int required = int.TryParse(group.Gqty, out var gqtyParsed) ? gqtyParsed : 0;

            int joined = await _context.TblGroupjoins
    .CountAsync(j => j.Groupid == gid && j.IsDeleted == false && j.IsActive == true);

            int remaining = Math.Max(required - joined, 0);
            string status = remaining == 0 ? "completed" : "pending";

            return Ok(new
            {
                status,
                remainingMembers = remaining
            });

        }



    }

    // Request DTOs
    public class CreateGroupRequest
    {
        public int QTY { get; set; }
        public long PID { get; set; }
        public long PDID { get; set; }
        public int userid { get; set; }
        public bool? acctt { get; set; }
        public int sipid { get; set; }
    }

    public class UpdateGroupRequest
    {
        public int QTY { get; set; }
        public long PID { get; set; }
        public long PDID { get; set; }
        public int userid { get; set; }
    }

    public class JoinGroupRequest
    {
        public long Groupid { get; set; }
        public int userid { get; set; }
    }
}