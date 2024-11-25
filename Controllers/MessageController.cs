using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Message.Data; // Your DbContext namespace
using Message.Models; // Your models namespace
using System.Collections.Concurrent;

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

                await HandleWebSocketCommunication(userId, webSocket);
            }
            else
            {
                HttpContext.Response.StatusCode = 400; // Bad Request
            }
        }

        private async Task HandleWebSocketCommunication(int userId, WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];

            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var messageJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var chatMessage = JsonSerializer.Deserialize<ChatMessage>(messageJson);

                    if (chatMessage != null)
                    {
                        // Save to database
                        await SaveMessageToDatabase(chatMessage);

                        // Forward the message to the recipient if connected
                        if (ConnectedUsers.TryGetValue(chatMessage.ReceiverId, out var recipientSocket) && recipientSocket.State == WebSocketState.Open)
                        {
                            await recipientSocket.SendAsync(
                                new ArraySegment<byte>(Encoding.UTF8.GetBytes(messageJson)),
                                WebSocketMessageType.Text,
                                true,
                                CancellationToken.None
                            );
                        }
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    ConnectedUsers.TryRemove(userId, out _);
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by user", CancellationToken.None);
                }
            }
        }

        private async Task SaveMessageToDatabase(ChatMessage chatMessage)
        {
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

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessage chatMessage)
        {
            if (chatMessage == null)
            {
                return BadRequest("Invalid message data.");
            }

            // Save message to the database
            await SaveMessageToDatabase(chatMessage);

            // Forward the message to the recipient if connected
            if (ConnectedUsers.TryGetValue(chatMessage.ReceiverId, out var recipientSocket) && recipientSocket.State == WebSocketState.Open)
            {
                var messageJson = JsonSerializer.Serialize(chatMessage);
                await recipientSocket.SendAsync(
                    new ArraySegment<byte>(Encoding.UTF8.GetBytes(messageJson)),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }

            // Respond with success
            return Ok(new { Status = "Message sent successfully" });
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
