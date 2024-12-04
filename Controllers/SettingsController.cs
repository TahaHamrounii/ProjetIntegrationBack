using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Message.Data;
using Message.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Message.Controllers
{
    /// <summary>
    /// Controller for managing user settings
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(ApplicationDbContext context, ILogger<SettingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private string GetCurrentUserId()
        {
            // Try to get userId from different possible claim types
            var userId = User.FindFirst("nameid")?.Value ?? 
                        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                        User.FindFirst("userId")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }
            return userId;
        }

        /// <summary>
        /// Get the current user's settings. User is identified from their JWT token.
        /// </summary>
        /// <returns>The user's settings</returns>
        /// <response code="200">Returns the user's settings</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="404">If settings are not found for the user</response>
        [HttpGet]
        [ProducesResponseType(typeof(UserSettings), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UserSettings>> GetUserSettings()
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation($"Getting settings for user: {userId}");

                // First try to get settings directly
                var settings = await _context.UserSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (settings != null)
                {
                    return Ok(settings);
                }

                // If settings don't exist, create default settings
                var newSettings = new UserSettings
                {
                    UserId = userId,
                    IsActive = true,
                    Theme = "light",
                    Language = "en",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                try
                {
                    _context.UserSettings.Add(newSettings);
                    await _context.SaveChangesAsync();
                    return Ok(newSettings);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create default settings for user {UserId}", userId);
                    return StatusCode(500, new { message = "Failed to create default settings", error = ex.Message });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user settings");
                return StatusCode(500, new { message = "An error occurred while retrieving user settings", error = ex.Message });
            }
        }

        /// <summary>
        /// Update the current user's settings
        /// </summary>
        /// <param name="request">The settings to update</param>
        /// <returns>The updated settings</returns>
        /// <response code="200">Returns the updated settings</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="404">If the user is not found</response>
        [HttpPut]
        [ProducesResponseType(typeof(UserSettings), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUserSettings([FromBody] UpdateSettingsRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
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
                    _context.Entry(user.Settings).State = EntityState.Modified;
                }

                await _context.SaveChangesAsync();
                return Ok(user.Settings);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt");
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user settings");
                return StatusCode(500, "An error occurred while updating user settings");
            }
        }

        /// <summary>
        /// Update the current user's theme
        /// </summary>
        /// <param name="theme">The new theme</param>
        /// <returns>The updated settings</returns>
        /// <response code="200">Returns the updated settings</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="404">If the user's settings are not found</response>
        [HttpPatch("theme")]
        [ProducesResponseType(typeof(UserSettings), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTheme([FromBody] string theme)
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation($"Updating theme for user: {userId}");

                var settings = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);
                if (settings == null)
                {
                    _logger.LogWarning($"Settings not found for user: {userId}");
                    return NotFound($"Settings for user {userId} not found");
                }

                settings.Theme = theme;
                await _context.SaveChangesAsync();

                return Ok(settings);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt");
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating theme");
                return StatusCode(500, "An error occurred while updating theme");
            }
        }

        /// <summary>
        /// Update the current user's language
        /// </summary>
        /// <param name="language">The new language</param>
        /// <returns>The updated settings</returns>
        /// <response code="200">Returns the updated settings</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="404">If the user's settings are not found</response>
        [HttpPatch("language")]
        [ProducesResponseType(typeof(UserSettings), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateLanguage([FromBody] string language)
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation($"Updating language for user: {userId}");

                var settings = await _context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);
                if (settings == null)
                {
                    _logger.LogWarning($"Settings not found for user: {userId}");
                    return NotFound($"Settings for user {userId} not found");
                }

                settings.Language = language;
                await _context.SaveChangesAsync();

                return Ok(settings);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt");
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating language");
                return StatusCode(500, "An error occurred while updating language");
            }
        }
    }

    /// <summary>
    /// Request model for updating user settings
    /// </summary>
    public class UpdateSettingsRequest
    {
        /// <summary>
        /// Whether the user is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// The user's preferred theme
        /// </summary>
        public string Theme { get; set; } = "light";

        /// <summary>
        /// The user's preferred language
        /// </summary>
        public string Language { get; set; } = "en";
    }
}
