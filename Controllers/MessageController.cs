using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Message.Data; // Your DbContext namespace
using Message.Models; // Your models namespace
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
namespace Message.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly AppDbContext _context;
        private static readonly ConcurrentDictionary<int, WebSocket> ConnectedUsers = new();

        public MessageController(AppDbContext context)
        {

            _context = context;


        }

        [HttpGet("connect/{userId}")]
        public async Task ConnectWebSocket(int userId)
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {

                var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                ConnectedUsers[userId] = webSocket;
                var message = "You're Connected!";
                var bytes = Encoding.UTF8.GetBytes(message);
                var arraySegment = new ArraySegment<byte>(bytes, 0, bytes.Length);
                while (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);

                    await Task.Delay(TimeSpan.FromSeconds(30)); // Ping every 30 seconds
                    await webSocket.SendAsync(Encoding.UTF8.GetBytes("Ping"), WebSocketMessageType.Text, true, CancellationToken.None);
                }


            }
        }


        private async Task SaveMessageToDatabase(ChatMessage chatMessage)
        {

            chatMessage.Timestamp = DateTime.UtcNow;
            var message = new Messages
            {
                SenderId = chatMessage.SenderId,
                ReceiverId = chatMessage.ReceiverId,
                MessageText = chatMessage.Content,
                Timestamp = chatMessage.Timestamp
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
        }

        [HttpGet("history/{senderId}/{receiverId}")]
        public async Task<IActionResult> GetMessageHistory(int senderId, int receiverId)
        {
            var messages = await _context.Messages
                .Where(m => (m.SenderId == senderId && m.ReceiverId == receiverId) ||
                            (m.SenderId == receiverId && m.ReceiverId == senderId))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            return Ok(messages);
        }


        [HttpGet("history/all")]
        public async Task<IActionResult> GetALLMessageHistory()
        {
            var messages = await _context.Messages
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

            return Ok(messages);
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessage chatMessage)
        {
            if (chatMessage == null)
            {
                return BadRequest("Invalid message data.");
            }

            // Save message to the database
            await SaveMessageToDatabase(chatMessage);

                System.Diagnostics.Debug.WriteLine("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" + chatMessage);
                var receiverSocket = ConnectedUsers[chatMessage.ReceiverId];
                var messageJson = JsonSerializer.Serialize(chatMessage);
            var arraySegment = new ArraySegment<byte>(Encoding.UTF8.GetBytes(messageJson));

                if (receiverSocket.State == WebSocketState.Open)
                {
                    await receiverSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            

            // Respond with success
            return Ok(new { Status = "Message sent successfully" });
        }

        //this for searching all users 
        [HttpGet("searchFriend")]
        public async Task<IActionResult> SearchUsers([FromQuery] string query)
        {
            if (string.IsNullOrEmpty(query))
                return BadRequest("Search query cannot be empty.");

            // Search users by name or email (case-insensitive)
            var users = await _context.Users
                .Where(u => u.Username.Contains(query) || u.Email.Contains(query))
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Status // Optional fields for the user
                })
                .ToListAsync();

            return Ok(users);
        }

    }


    public class ChatMessage
    {
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
