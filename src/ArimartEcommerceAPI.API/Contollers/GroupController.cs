using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using ArimartEcommerceAPI.Services.Services;
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
        private readonly IFcmPushService _fcmPushService;

        public GroupController(ApplicationDbContext context, IFcmPushService fcmPushService)
        {
            _context = context;
            _fcmPushService = fcmPushService;
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
            var now = DateTime.UtcNow; // or DateTime.Now if you want local time

            var group = await _context.VwGroups
                .FirstOrDefaultAsync(g =>
                    g.Gid == gid &&
                    g.IsDeleted1 == false &&
                    g.EventSend1 > now // Only running groups
                );

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

        private async Task CheckAndCompleteGroup(long groupId)
        {
            var group = await _context.VwGroups.FirstOrDefaultAsync(g => g.Gid == groupId);
            if (group == null) return;

            int required = int.TryParse(group.Gqty, out var qty) ? qty : 0;
            int joined = await _context.TblGroupjoins.CountAsync(j => j.Groupid == groupId && !j.IsDeleted);

            if (joined >= required)
            {
                // Activate all pending orders for this group
                var pendingOrders = await _context.TblOrdernows
                    .Where(o => o.Groupid == groupId && o.DassignidTime == null && !o.IsDeleted)
                    .ToListAsync();

                foreach (var order in pendingOrders)
                {
                    order.DassignidTime = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                // Notify all group members
                var memberIds = await _context.TblGroupjoins
                    .Where(j => j.Groupid == groupId && !j.IsDeleted)
                    .Select(j => j.Userid)
                    .ToListAsync();

                foreach (var memberId in memberIds)
                {
                    await SendGroupCompletedNotification(memberId, groupId);
                }
            }
        }
        private async Task CleanupExpiredGroupItems()
        {
            var expiredCartItems = await _context.TblAddcarts
                .Where(c => c.Groupid != null && !c.IsDeleted)
                .Join(_context.VwGroups,
                    c => c.Groupid,
                    g => g.Gid,
                    (c, g) => new { cart = c, group = g })
                .Where(x => x.group.EventSend1 <= DateTime.UtcNow)
                .Select(x => x.cart)
                .ToListAsync();

            foreach (var item in expiredCartItems)
            {
                item.IsDeleted = true;
                item.ModifiedDate = DateTime.UtcNow;
            }

            if (expiredCartItems.Any())
            {
                await _context.SaveChangesAsync();

                var userIds = expiredCartItems.Select(i => i.Userid).Distinct();
                foreach (var userId in userIds)
                {
                    if (userId.HasValue) // Check if not null
                    {
                        await SendExpiredGroupNotification(userId.Value);
                    }
                }
            }
        }

        // ✅ POST: Join existing group (Based on tbl_Groupjoin table)
        [AllowAnonymous]
        [HttpPost("join")]
        public async Task<IActionResult> JoinGroup([FromBody] JoinGroupRequest request)
        {
            await CleanupExpiredGroupItems();
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
                await CheckAndCompleteGroup(request.Groupid);

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
        public async Task<IActionResult> GetCurrentRunningGroups(
       [FromQuery] int page = 1,
       [FromQuery] int pageSize = 10)
        {
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Prevent excessive page sizes

            var now = DateTime.UtcNow;

            // Get total count for pagination metadata
            var totalCount = await _context.VwGroups
                .Where(g =>
                    g.EventSend1 != null &&
                    g.EventSend1 > now &&
                    g.IsDeleted == false
                )
                .CountAsync();

            // Get paginated results
            var runningGroups = await _context.VwGroups
                .Where(g =>
                    g.EventSend1 != null &&
                    g.EventSend1 > now &&
                    g.IsDeleted == false
                )
                .OrderBy(g => g.EventSend1)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Calculate pagination metadata
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var response = new
            {
                Data = runningGroups,
                Pagination = new
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    HasPreviousPage = page > 1,
                    HasNextPage = page < totalPages
                }
            };

            return Ok(response);
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

        // search by group code
        [AllowAnonymous]
        [HttpGet("by-refercode/{referCode}")]
        public async Task<ActionResult> GetGroupByReferCode(string referCode)
        {
            var referCodeData = await _context.VwGrouprefercodes
                .FirstOrDefaultAsync(r => r.Refercode == referCode);

            if (referCodeData == null)
            {
                return Ok(new
                {
                    gid = (long?)null,
                    message = "Group code is not match please check code and search again",
                    STATUS = 0
                });
            }

            // 🛠️ Try to find group from PID/PDID if GID is not available
            var group = await _context.VwGroups
                .FirstOrDefaultAsync(g =>
                    g.Pid == referCodeData.Pid &&
                    g.Pdid == referCodeData.Pdid &&
                    g.IsDeleted == false);

            if (group == null)
            {
                return Ok(new
                {
                    gid = (long?)null,
                    message = "Group not found for this group code",
                    STATUS = 0
                });
            }

            return Ok(new
            {
                gid = group.Gid,
                message = "Awesome! Group found.",
                STATUS = 1
            });
        }

        // ===================== Send notification =========================
        // Group Expired Notification
        private async Task<string> SendExpiredGroupNotification(int userId)
        {
            return await SendNotificationAsync(
                userId,
                "⏰ Group Deal Expired",
                "Some items were removed from your cart. You can reorder them individually!"
            );
        }

        // Group Completed Notification
        // Change your notification method signature to accept long
        private async Task<string> SendGroupCompletedNotification(long? userId, long groupId)
        {
            return await SendNotificationAsync(
                (int)userId.Value, // Convert here if your FCM method needs int
                "🎉 Group Deal Complete!",
                "Congratulations! Your group order is now being processed."
            );
        }

        // New Member Joined Notification
        private async Task<string> SendNewMemberJoinedNotification(int userId, string memberName)
        {
            return await SendNotificationAsync(
                userId,
                "👥 New Member Joined!",
                $"{memberName} joined your group deal. Share with more friends to complete faster!"
            );
        }

        // Group Almost Complete Notification
        private async Task<string> SendGroupAlmostCompleteNotification(int userId, int remainingMembers)
        {
            return await SendNotificationAsync(
                userId,
                "🔥 Group Almost Complete!",
                $"Only {remainingMembers} more member(s) needed! Share to unlock the deal."
            );
        }

        // Cart Item Removed Notification
        private async Task<string> SendCartItemRemovedNotification(int userId, string productName)
        {
            return await SendNotificationAsync(
                userId,
                "🛒 Cart Updated",
                $"{productName} was removed from your cart due to group expiry. You can add it again!"
            );
        }

        // Group Order Processing Notification
        private async Task<string> SendGroupOrderProcessingNotification(int userId, string trackId)
        {
            return await SendNotificationAsync(
                userId,
                "📦 Group Order Processing",
                $"Your group order {trackId} is now being processed. Group deal completed successfully!"
            );
        }

        // Reorder Available Notification
        private async Task<string> SendReorderAvailableNotification(int userId, string productName)
        {
            return await SendNotificationAsync(
                userId,
                "🔄 Reorder Available",
                $"{productName} is now available for individual purchase. Order now!"
            );
        }

        // Group Failed Notification
        private async Task<string> SendGroupFailedNotification(int userId, string productName)
        {
            return await SendNotificationAsync(
                userId,
                "❌ Group Deal Failed",
                $"Group deal for {productName} didn't complete in time. You can still order individually!"
            );
        }

        // Group Member Limit Reached
        private async Task<string> SendGroupLimitReachedNotification(int userId)
        {
            return await SendNotificationAsync(
                userId,
                "✅ Group Limit Reached!",
                "Amazing! Your group deal reached maximum members. Orders are being processed now."
            );
        }

        // Time Running Out Notification
        private async Task<string> SendTimeRunningOutNotification(int userId, int hoursLeft)
        {
            return await SendNotificationAsync(
                userId,
                "⏳ Time Running Out!",
                $"Only {hoursLeft} hours left for your group deal. Invite friends now!"
            );
        }
        private async Task<string> SendNotificationAsync(int userId, string title, string message)
        {
            var fcmToken = await _context.FcmDeviceTokens
                .Where(t => t.UserId == userId)
                .Select(t => t.Token)
                .FirstOrDefaultAsync();

            string fcmStatus = "FCM not sent";

            try
            {
                if (!string.IsNullOrEmpty(fcmToken))
                {
                    var (success, error) = await _fcmPushService.SendNotificationAsync(
                        fcmToken,
                        title,
                        message
                    );
                    fcmStatus = success ? "✅ FCM sent successfully." : $"❌ FCM failed: {error}";
                }
                else
                {
                    fcmStatus = "❌ FCM token not found for user.";
                }
            }
            catch (Exception ex)
            {
                fcmStatus = $"❌ FCM exception: {ex.Message}";
            }

            return fcmStatus;
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