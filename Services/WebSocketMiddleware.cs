using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Message.Data;
using Message.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

public class WebSocketMiddleware
{
    private readonly RequestDelegate _next;

    public WebSocketMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
    {
        if (context.Request.Path == "/ws" && context.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            await HandleWebSocketCommunication(webSocket, dbContext);
        }
        else
        {
            await _next(context);
        }
    }

    private async Task HandleWebSocketCommunication(WebSocket webSocket, AppDbContext dbContext)
    {
        var buffer = new byte[1024 * 4];
        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
                break;
            }

            var messageJson = Encoding.UTF8.GetString(buffer, 0, result.Count);

            // Log the received message
            Console.WriteLine("Received Message: " + messageJson);

            if (string.IsNullOrEmpty(messageJson))
            {
                Console.WriteLine("Message is empty.");
            }

            // Deserialize the incoming message
            var incomingMessage = System.Text.Json.JsonSerializer.Deserialize<MessageDto>(messageJson);

            if (incomingMessage != null)
            {
                // Save the message to the database
                var newMessage = new Messages
                {
                    SenderId = incomingMessage.SenderId,
                    ReceiverId = incomingMessage.ReceiverId,
                    MessageText = incomingMessage.MessageText,
                    Timestamp = DateTime.UtcNow
                };

                dbContext.Messages.Add(newMessage);
                await dbContext.SaveChangesAsync();

                // Acknowledge receipt
                var response = Encoding.UTF8.GetBytes("Message received and saved.");
                await webSocket.SendAsync(new ArraySegment<byte>(response), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            else
            {
                Console.WriteLine("Failed to deserialize message.");
            }
        }
    }
}

    public class Msg
{
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string MessageText { get; set; }
}
