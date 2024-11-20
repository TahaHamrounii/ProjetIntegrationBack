using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Message.Models;
using Message.Services;  // Add the correct namespace for MessageService
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;

namespace Message.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]  
    public class MessagesController : ControllerBase
    {

        private readonly IMessageService _messageService;
        private readonly IHubContext<MessagesHub> _hubContext;

        // Inject IMessageService in the constructor
        public MessagesController(IMessageService messageService, IHubContext<MessagesHub> hubContext)
        {
            _messageService = messageService;
            _hubContext = hubContext;

        }

        // Endpoint to save a message
        [HttpPost("save")]
        public async Task<IActionResult> SaveMessage([FromBody] Mssg message)
        {
            if (message == null || string.IsNullOrWhiteSpace(message.Content))
            {
                return BadRequest(new { Error = "Message content cannot be empty." });
            }

            message.Timestamp = DateTime.UtcNow;

            // Use the service to save the message
            await _messageService.SaveMessageAsync(message);

            await _hubContext.Clients.All.SendAsync("ReceiveMessage", message.SenderId, message.ReceiverId, message.Content);

            return Ok(new { Message = "Message saved successfully." });
        }

        // Get messages between two users
        [HttpGet("conversation")]
        public async Task<IActionResult> GetMessages([FromQuery] int senderId, [FromQuery] int receiverId)
        {
            if (senderId <= 0 || receiverId <= 0)
            {
                return BadRequest(new { Error = "Invalid sender or receiver ID." });
            }

            // Use the service to retrieve messages
            var messages = await _messageService.GetMessagesAsync(senderId, receiverId);

            if (!messages.Any())
            {
                return NotFound(new { Error = "No messages found." });
            }

            return Ok(messages);
        }
    }
}
