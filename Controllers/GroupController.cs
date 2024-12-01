using Message.Models;
using Message.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;

namespace Message.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GroupController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public GroupController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // DTOs
        public class CreateGroupWithFriendsDto
        {
            public string GroupName { get; set; }
            public string Description { get; set; }
            public List<int> FriendIds { get; set; }
        }

        public class AddMembersDto
        {
            public int GroupId { get; set; }
            public List<int> UserIds { get; set; }
        }

        public class UpdateGroupDto
        {
            public string GroupName { get; set; }
            public string Description { get; set; }
        }

        private async Task<int?> ValidateTokenAndGetUserId(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };

                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);
                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return string.IsNullOrEmpty(userId) ? null : int.Parse(userId);
            }
            catch
            {
                return null;
            }
        }

        [HttpPost("createwithfriends/{token}")]
        public async Task<IActionResult> CreateGroupWithFriends([FromBody] CreateGroupWithFriendsDto groupDto, string token)
        {
            var userId = await ValidateTokenAndGetUserId(token);
            if (!userId.HasValue)
                return Unauthorized("Invalid token");

            // Verify all provided IDs are actually friends
            var friendships = await _context.Friends
                .Where(f => f.UserId == userId.Value && groupDto.FriendIds.Contains(f.FriendId))
                .ToListAsync();

            if (friendships.Count != groupDto.FriendIds.Count)
                return BadRequest("Some of the provided users are not in your friends list");

            if (await _context.Groups.AnyAsync(g => g.GroupName == groupDto.GroupName))
                return BadRequest("Group name already exists");

            var group = new Group
            {
                GroupName = groupDto.GroupName,
                Description = groupDto.Description,
                CreatedAt = DateTime.Now
            };

            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            var userGroups = new List<UserGroup>
            {
                new UserGroup
                {
                    UserId = userId.Value,
                    GroupId = group.GroupId,
                    IsAdmin = true,
                    JoinedAt = DateTime.Now
                }
            };

            userGroups.AddRange(groupDto.FriendIds.Select(friendId => new UserGroup
            {
                UserId = friendId,
                GroupId = group.GroupId,
                IsAdmin = false,
                JoinedAt = DateTime.Now
            }));

            _context.UserGroups.AddRange(userGroups);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Group created successfully with friends", GroupId = group.GroupId });
        }

        [HttpGet("list/{token}")]
        public async Task<IActionResult> GetUserGroups(string token)
        {
            var userId = await ValidateTokenAndGetUserId(token);
            if (!userId.HasValue)
                return Unauthorized("Invalid token");

            var userGroups = await _context.Groups
                .Where(g => g.UserGroups.Any(ug => ug.UserId == userId.Value))
                .Select(g => new
                {
                    g.GroupId,
                    g.GroupName,
                    g.Description,
                    g.CreatedAt,
                    MemberCount = g.UserGroups.Count,
                    IsAdmin = g.UserGroups.Any(ug => ug.UserId == userId.Value && ug.IsAdmin),
                    Members = g.UserGroups.Select(ug => new
                    {
                        UserId = ug.UserId,
                        IsAdmin = ug.IsAdmin,
                        JoinedAt = ug.JoinedAt
                    }).ToList()
                })
                .ToListAsync();

            return Ok(userGroups);
        }

        [HttpPut("update/{groupId}/{token}")]
        public async Task<IActionResult> UpdateGroup(int groupId, [FromBody] UpdateGroupDto updateDto, string token)
        {
            var userId = await ValidateTokenAndGetUserId(token);
            if (!userId.HasValue)
                return Unauthorized("Invalid token");

            var group = await _context.Groups
                .Include(g => g.UserGroups)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);

            if (group == null)
                return NotFound("Group not found");

            if (!group.UserGroups.Any(ug => ug.UserId == userId.Value && ug.IsAdmin))
                return Forbid("Only group admins can update the group");

            if (await _context.Groups.AnyAsync(g => g.GroupName == updateDto.GroupName && g.GroupId != groupId))
                return BadRequest("Group name already exists");

            group.GroupName = updateDto.GroupName;
            group.Description = updateDto.Description;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Group updated successfully" });
        }

        [HttpPost("addmembers/{token}")]
        public async Task<IActionResult> AddMembers([FromBody] AddMembersDto addMembersDto, string token)
        {
            var userId = await ValidateTokenAndGetUserId(token);
            if (!userId.HasValue)
                return Unauthorized("Invalid token");

            var group = await _context.Groups
                .Include(g => g.UserGroups)
                .FirstOrDefaultAsync(g => g.GroupId == addMembersDto.GroupId);

            if (group == null)
                return NotFound("Group not found");

            if (!group.UserGroups.Any(ug => ug.UserId == userId.Value && ug.IsAdmin))
                return Forbid("Only group admins can add members");

            // Check if users are already members
            var existingMembers = await _context.UserGroups
                .Where(ug => ug.GroupId == addMembersDto.GroupId && addMembersDto.UserIds.Contains(ug.UserId))
                .Select(ug => ug.UserId)
                .ToListAsync();

            if (existingMembers.Any())
                return BadRequest($"Users {string.Join(", ", existingMembers)} are already members");

            var newMembers = addMembersDto.UserIds.Select(memberId => new UserGroup
            {
                UserId = memberId,
                GroupId = addMembersDto.GroupId,
                IsAdmin = false,
                JoinedAt = DateTime.Now
            });

            _context.UserGroups.AddRange(newMembers);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Members added successfully" });
        }

        [HttpDelete("leave/{groupId}/{token}")]
        public async Task<IActionResult> LeaveGroup(int groupId, string token)
        {
            var userId = await ValidateTokenAndGetUserId(token);
            if (!userId.HasValue)
                return Unauthorized("Invalid token");

            var userGroup = await _context.UserGroups
                .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId.Value);

            if (userGroup == null)
                return NotFound("You are not a member of this group");

            var isLastAdmin = await _context.UserGroups
                .CountAsync(ug => ug.GroupId == groupId && ug.IsAdmin) == 1 && userGroup.IsAdmin;

            if (isLastAdmin)
                return BadRequest("Cannot leave group as you are the last admin. Transfer admin rights first or delete the group");

            _context.UserGroups.Remove(userGroup);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Successfully left the group" });
        }

        [HttpDelete("delete/{groupId}/{token}")]
        public async Task<IActionResult> DeleteGroup(int groupId, string token)
        {
            var userId = await ValidateTokenAndGetUserId(token);
            if (!userId.HasValue)
                return Unauthorized("Invalid token");

            var group = await _context.Groups
                .Include(g => g.UserGroups)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);

            if (group == null)
                return NotFound("Group not found");

            if (!group.UserGroups.Any(ug => ug.UserId == userId.Value && ug.IsAdmin))
                return Forbid("Only group admins can delete the group");

            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Group deleted successfully" });
        }

        [HttpPost("makeadmin/{groupId}/{memberId}/{token}")]
        public async Task<IActionResult> MakeAdmin(int groupId, int memberId, string token)
        {
            var userId = await ValidateTokenAndGetUserId(token);
            if (!userId.HasValue)
                return Unauthorized("Invalid token");

            var group = await _context.Groups
                .Include(g => g.UserGroups)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);

            if (group == null)
                return NotFound("Group not found");

            if (!group.UserGroups.Any(ug => ug.UserId == userId.Value && ug.IsAdmin))
                return Forbid("Only group admins can promote members to admin");

            var memberUserGroup = await _context.UserGroups
                .FirstOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == memberId);

            if (memberUserGroup == null)
                return NotFound("User is not a member of this group");

            if (memberUserGroup.IsAdmin)
                return BadRequest("User is already an admin");

            memberUserGroup.IsAdmin = true;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "User promoted to admin successfully" });
        }
    }
}
