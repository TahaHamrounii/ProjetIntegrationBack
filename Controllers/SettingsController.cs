using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Message.Data;
using Message.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Message.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(ApplicationDbContext context, ILogger<SettingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private string CleanUserId(string userId)
        {
            // Handle multiple URL encodings
            while (userId.Contains("%2F"))
            {
                userId = Uri.UnescapeDataString(userId);
            }
            
            // Remove any API prefix
            if (userId.Contains("/api/Settings/"))
            {
                userId = userId.Split(new[] { "/api/Settings/" }, StringSplitOptions.None).Last();
            }
            
            return userId.Trim();
        }

        // GET: api/Settings/{userId}
        [HttpGet("{userId}")]
        public async Task<ActionResult<UserSettings>> GetUserSettings([FromRoute] string userId)
        {
            userId = CleanUserId(userId);
            _logger.LogInformation($"Getting settings for user: {userId}");

            var user = await _context.Users
                .Include(u => u.Settings)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning($"User not found: {userId}");
                return NotFound($"User with ID {userId} not found");
            }

            if (user.Settings == null)
            {
                user.Settings = new UserSettings
                {
                    UserId = userId,
                    IsActive = true,
                    Theme = "light",
                    Language = "en"
                };
                _context.UserSettings.Add(user.Settings);
                await _context.SaveChangesAsync();
            }

            return user.Settings;
        }

        // PUT: api/Settings/{userId}
        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateUserSettings([FromRoute] string userId, [FromBody] UpdateSettingsRequest request)
        {
            userId = CleanUserId(userId);
            _logger.LogInformation($"Updating settings for user: {userId}");

            var user = await _context.Users
                .Include(u => u.Settings)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning($"User not found: {userId}");
                return NotFound($"User with ID {userId} not found");
            }

            if (user.Settings == null)
            {
                user.Settings = new UserSettings
                {
                    UserId = userId,
                    IsActive = request.IsActive,
                    Theme = request.Theme,
                    Language = request.Language
                };
                _context.UserSettings.Add(user.Settings);
            }
            else
            {
                user.Settings.IsActive = request.IsActive;
                user.Settings.Theme = request.Theme;
                user.Settings.Language = request.Language;
            }

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new
                {
                    user.Settings.Id,
                    user.Settings.UserId,
                    user.Settings.IsActive,
                    user.Settings.Theme,
                    user.Settings.Language
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return NotFound();
            }
        }

        // PATCH: api/Settings/{userId}/theme
        [HttpPatch("{userId}/theme")]
        public async Task<IActionResult> UpdateTheme([FromRoute] string userId, [FromBody] string theme)
        {
            userId = CleanUserId(userId);
            _logger.LogInformation($"Updating theme for user: {userId}");

            var user = await _context.Users
                .Include(u => u.Settings)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning($"User not found: {userId}");
                return NotFound($"User with ID {userId} not found");
            }

            if (user.Settings == null)
            {
                user.Settings = new UserSettings
                {
                    UserId = userId,
                    IsActive = true,
                    Theme = theme,
                    Language = "en"
                };
                _context.UserSettings.Add(user.Settings);
            }
            else
            {
                user.Settings.Theme = theme;
            }

            await _context.SaveChangesAsync();
            return Ok(new { Theme = theme });
        }

        // PATCH: api/Settings/{userId}/language
        [HttpPatch("{userId}/language")]
        public async Task<IActionResult> UpdateLanguage([FromRoute] string userId, [FromBody] string language)
        {
            userId = CleanUserId(userId);
            _logger.LogInformation($"Updating language for user: {userId}");

            var user = await _context.Users
                .Include(u => u.Settings)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning($"User not found: {userId}");
                return NotFound($"User with ID {userId} not found");
            }

            if (user.Settings == null)
            {
                user.Settings = new UserSettings
                {
                    UserId = userId,
                    IsActive = true,
                    Theme = "light",
                    Language = language
                };
                _context.UserSettings.Add(user.Settings);
            }
            else
            {
                user.Settings.Language = language;
            }

            await _context.SaveChangesAsync();
            return Ok(new { Language = language });
        }

        public class UpdateSettingsRequest
        {
            public bool IsActive { get; set; }
            public string Theme { get; set; } = string.Empty;
            public string Language { get; set; } = string.Empty;
        }
    }
}
