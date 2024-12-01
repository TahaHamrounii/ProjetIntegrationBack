using Message.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Ensure this is included for EF Core methods
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using Message.Data; // Ensure you have this package installed for password hashing
using Microsoft.AspNetCore.Authorization;

namespace Message.Controllers
{ 

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            // Validate the incoming user data (e.g., check if the email is already taken)
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            {
                return BadRequest("Email is already in use.");
            }
            user.ApplicationDate = DateTime.UtcNow;
            if (string.IsNullOrEmpty(user.Status))
            {
                user.Status = "Suspendu"; // Set default status
            }
            if (string.IsNullOrEmpty(user.University))
            {
                user.University = "Iset Djerba"; // Set default status
            }
           
            // Hash password before saving to the database
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password); // Hashing the password
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(new
            {
                message = "Account is suspended and will be activated soon.",
                status = user.Status
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginRequest.Mail);

            // Verify the password (using hashed passwords)
            if (existingUser == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, existingUser.Password))
            {
                return Unauthorized("Invalid email or password.");
            }

            // Generate JWT token
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenKey = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]); // Use the configured key
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                   
                    new Claim(ClaimTypes.Name, existingUser.Username),
                      new Claim(ClaimTypes.NameIdentifier, existingUser.Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenKey), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return Ok(new { Token = tokenHandler.WriteToken(token),


             UserId = existingUser.Id

            });
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            return Ok(users);
        }

        // DTO for group creation
        public class CreateGroupDto
        {
            public string GroupName { get; set; }
            public string Description { get; set; }
        }

        [HttpPost("creategroup/{token}")]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDto groupDto, string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized("Token is required");
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
                
                try
                {
                    var tokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };

                    var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);
                    var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (string.IsNullOrEmpty(userId))
                    {
                        return Unauthorized("Invalid token");
                    }

                    // Check if group name already exists
                    if (await _context.Groups.AnyAsync(g => g.GroupName == groupDto.GroupName))
                    {
                        return BadRequest("Group name already exists");
                    }

                    // Create new group
                    var group = new Group
                    {
                        GroupName = groupDto.GroupName,
                        Description = groupDto.Description,
                        CreatedAt = DateTime.Now
                    };

                    // Add group to database
                    _context.Groups.Add(group);
                    await _context.SaveChangesAsync();

                    // Add the creator as a group admin
                    var userGroup = new UserGroup
                    {
                        UserId = int.Parse(userId),
                        GroupId = group.GroupId,
                        IsAdmin = true,
                        JoinedAt = DateTime.Now
                    };

                    _context.UserGroups.Add(userGroup);
                    await _context.SaveChangesAsync();

                    return Ok(new { 
                        Message = "Group created successfully",
                        GroupId = group.GroupId,
                        GroupName = group.GroupName
                    });
                }
                catch (SecurityTokenException)
                {
                    return Unauthorized("Invalid token");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("groups/{token}")]
        public async Task<IActionResult> GetAllGroups(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized("Token is required");
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
                
                try
                {
                    var tokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };

                    var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);
                    var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (string.IsNullOrEmpty(userId))
                    {
                        return Unauthorized("Invalid token");
                    }

                    // Get all groups with their member count and creator info
                    var groups = await _context.Groups
                        .Select(g => new
                        {
                            g.GroupId,
                            g.GroupName,
                            g.Description,
                            g.CreatedAt,
                            MemberCount = g.UserGroups.Count,
                            IsUserMember = g.UserGroups.Any(ug => ug.UserId == int.Parse(userId)),
                            IsUserAdmin = g.UserGroups.Any(ug => ug.UserId == int.Parse(userId) && ug.IsAdmin)
                        })
                        .ToListAsync();

                    return Ok(groups);
                }
                catch (SecurityTokenException)
                {
                    return Unauthorized("Invalid token");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }


    public class LoginRequest
    {
        public string Mail { get; set; } // Corresponds to the email
        public string Password { get; set; } // Password field
    }

}
