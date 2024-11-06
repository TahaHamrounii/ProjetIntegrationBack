﻿using Message.Models;
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

            // Hash password before saving to the database
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password); // Hashing the password
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok();
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
                   
                    new Claim(ClaimTypes.Name, existingUser.Username)
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenKey), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return Ok(new { Token = tokenHandler.WriteToken(token) });
        }
    }
    public class LoginRequest
    {
        public string Mail { get; set; } // Corresponds to the email
        public string Password { get; set; } // Password field
    }

}
