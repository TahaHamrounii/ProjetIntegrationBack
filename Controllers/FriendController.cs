using Message.Data;
using Message.Models;
using Message.Services;
using Microsoft.AspNetCore.Mvc;
using System.Data.Entity;

namespace Message.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FriendController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly FriendService _friendService;

       

        public FriendController(AppDbContext context , FriendService friendService)
        {
            _context = context;
            _friendService = friendService;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddFriend([FromBody] AddFriendRequest request)
        {
            if (request.UserId == request.FriendId)
            {
                return BadRequest(new { message = "You cannot add yourself as a friend." });
            }

            // Check if the users exist
            var userExists = _context.Users.AsQueryable().Any(u => u.Id == request.UserId);
            var friendExists = _context.Users.AsQueryable().Any(u => u.Id == request.FriendId);

            if (!userExists || !friendExists)
            {
                return NotFound(new { message = "User or friend not found." });
            }

            // Check if friendship already exists
            var friendshipExists = _context.Friends
                .AsQueryable()
                .Any(f => (f.UserId == request.UserId && f.FriendId == request.FriendId) ||
                          (f.UserId == request.FriendId && f.FriendId == request.UserId));

            if (friendshipExists)
            {
                return Conflict(new { message = "Friendship already exists." });
            }

            // Add friendship
            var friendship = new Friend
            {
                UserId = request.UserId,
                FriendId = request.FriendId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Friends.Add(friendship);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Friend added successfully!" });
        }
        // GET: api/Friend/list/{userId}
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetFriendList(int userId)
        {
            try
            {
                // Call the service method to get the friend list asynchronously
                var friendList = await _friendService.GetFriendListAsync(userId);

                // If the friend list is null or empty, return a 404 (Not Found)
                if (friendList == null || friendList.Count == 0)
                {
                    return NotFound("No friends found for this user.");
                }

                // Return the friend list with a 200 OK status
                return Ok(friendList);
            }
            catch (Exception ex)
            {
                // Log the exception (optional) and return a 500 (Internal Server Error)
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }

    public class AddFriendRequest
    {
        public int UserId { get; set; }
        public int FriendId { get; set; }
    }

}
