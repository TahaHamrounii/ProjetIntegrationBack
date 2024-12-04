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
        [ProducesResponseType(typeof(UserSettingsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserSettingsResponse>> GetUserSettings()
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation($"Getting settings for user: {userId}");

                // Get both user and settings
                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId);

                var settings = await _context.UserSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (user == null)
                {
                    return NotFound($"User {userId} not found");
                }

                if (settings == null)
                {
                    // Create default settings if none exist
                    settings = new UserSettings
                    {
                        UserId = userId,
                        IsActive = true,
                        Theme = "light",
                        Language = "en",
                        NotifyMessages = true,
                        NotifyGroups = true,
                        NotifyCalls = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.UserSettings.Add(settings);
                    await _context.SaveChangesAsync();
                }

                // Combine user and settings data
                var response = new UserSettingsResponse
                {
                    Id = settings.Id,
                    UserId = userId,
                    Email = user.Email,
                    Name = user.Name,
                    Username = user.Username,
                    IsActive = settings.IsActive,
                    Theme = settings.Theme,
                    Language = settings.Language,
                    NotifyMessages = settings.NotifyMessages,
                    NotifyGroups = settings.NotifyGroups,
                    NotifyCalls = settings.NotifyCalls,
                    CreatedAt = settings.CreatedAt,
                    UpdatedAt = settings.UpdatedAt
                };

                return Ok(response);
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
        [ProducesResponseType(typeof(UserSettingsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUserSettings([FromBody] UpdateSettingsRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation($"Updating settings for user: {userId}");

                var settings = await _context.UserSettings
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (settings == null)
                {
                    settings = new UserSettings
                    {
                        UserId = userId,
                        IsActive = request.IsActive,
                        Theme = request.Theme,
                        Language = request.Language,
                        NotifyMessages = request.NotifyMessages,
                        NotifyGroups = request.NotifyGroups,
                        NotifyCalls = request.NotifyCalls
                    };
                    _context.UserSettings.Add(settings);
                }
                else
                {
                    settings.IsActive = request.IsActive;
                    settings.Theme = request.Theme;
                    settings.Language = request.Language;
                    settings.NotifyMessages = request.NotifyMessages;
                    settings.NotifyGroups = request.NotifyGroups;
                    settings.NotifyCalls = request.NotifyCalls;
                    settings.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                
                var response = new UserSettingsResponse
                {
                    Id = settings.Id,
                    UserId = settings.UserId,
                    IsActive = settings.IsActive,
                    Theme = settings.Theme,
                    Language = settings.Language,
                    NotifyMessages = settings.NotifyMessages,
                    NotifyGroups = settings.NotifyGroups,
                    NotifyCalls = settings.NotifyCalls,
                    CreatedAt = settings.CreatedAt,
                    UpdatedAt = settings.UpdatedAt
                };

                return Ok(response);
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
        public string Theme { get; set; } = string.Empty;
        /// <summary>
        /// The user's preferred language
        /// </summary>
        public string Language { get; set; } = string.Empty;
        /// <summary>
        /// Whether to notify the user of new messages
        /// </summary>
        public bool NotifyMessages { get; set; }
        /// <summary>
        /// Whether to notify the user of new groups
        /// </summary>
        public bool NotifyGroups { get; set; }
        /// <summary>
        /// Whether to notify the user of new calls
        /// </summary>
        public bool NotifyCalls { get; set; }
    }

    public class UserSettingsResponse
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string Theme { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public bool NotifyMessages { get; set; }
        public bool NotifyGroups { get; set; }
        public bool NotifyCalls { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
