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
using Message.Data;
using Message.Services; // Ensure you have this package installed for password hashing

namespace Message.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public UserController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            return Ok(users);
        }
        [HttpDelete("user/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return Ok(user);
        }
        [HttpPut("user/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] User updatedUser)
        {
            if (id != updatedUser.Id)
            {
                return BadRequest();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            if (!string.IsNullOrEmpty(updatedUser.Username))
            {
                user.Username = updatedUser.Username;
            }
            if (!string.IsNullOrEmpty(updatedUser.Email))
            {
                user.Email = updatedUser.Email;
            }
            if (!string.IsNullOrEmpty(updatedUser.Password))
            {
                // Hash the password before updating if necessary
                user.Password = updatedUser.Password;
            }
            if (updatedUser.ApplicationDate != default)
            {
                user.ApplicationDate = updatedUser.ApplicationDate;
            }
            if (!string.IsNullOrEmpty(updatedUser.University))
            {
                user.University = updatedUser.University;
            }
            if (updatedUser.Status != null) // Assuming Status is a nullable type (e.g., `int?` or `string?`)
            {
                user.Status = updatedUser.Status;
            }
            if (!string.IsNullOrEmpty(updatedUser.Role))
            {
                user.Role = updatedUser.Role;
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Users.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }


        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            var totalUsers = await _context.Users.CountAsync();

            return Ok(new
            {
                TotalUsers = totalUsers,
            });
        }

        [HttpPatch("user/{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] string status)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var subject = "requête d'inscription";
            var content = "";
            if (status == "accepted")
            {
                content = "We are happy to inform you that you have been selected for the position at our site.We look forward to welcoming you aboard and working together";
            }
            else
            {
                content = "We regret to inform you that, after careful consideration, we are unable to offer you an account at our site at this time. Thank you for your interest, and we wish you all the best in your future endeavors";
            }
            EmailService.sendEmail(user, subject,content);

            user.Status = status;

            _context.Entry(user).Property(u => u.Status).IsModified = true;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Users.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }


    }
}
