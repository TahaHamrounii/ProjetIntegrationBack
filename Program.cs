using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Message.Data;
using Message.Models;
using System.IdentityModel.Tokens.Jwt;
using Message.Services;

var builder = WebApplication.CreateBuilder(args);
SQLitePCL.Batteries_V2.Init();

// Add services to the containerAuthorization: Bearer <your-jwt-token>
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder.WithOrigins("http://localhost:5173") // Allow Vue.js app origin
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials()); // This allows cookies and credentials
});

// Authentication setup
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});
builder.Services.AddScoped<FriendService>();
// Add controllers and other configurations
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigin"); // Enable CORS

// Add authentication middleware before authorization
app.UseAuthentication();
app.UseAuthorization();

// Enable WebSockets
app.UseWebSockets();

// WebSocket middleware to handle incoming WebSocket connections
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws" && context.WebSockets.IsWebSocketRequest)
    {
        // Get the token from Authorization header
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (string.IsNullOrEmpty(token))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Token is missing.");
            return;
        }

        // Validate the token
        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var claims = jwtToken?.Claims;
            // Optionally, you can validate claims here if necessary
        }
        catch (Exception)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid token.");
            return;
        }

        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await HandleWebSocket(context, webSocket);
    }
    else
    {
        await next();
    }
});

// Map controllers
app.MapControllers();

app.Run();

// WebSocket handling logic
async Task HandleWebSocket(HttpContext context, System.Net.WebSockets.WebSocket webSocket)
{
    var buffer = new byte[1024 * 4];
    var dbContext = context.RequestServices.GetRequiredService<AppDbContext>(); // Inject AppDbContext

    while (webSocket.State == System.Net.WebSockets.WebSocketState.Open)
    {
        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
        {
            await webSocket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
        }
        else
        {
            var messageJson = Encoding.UTF8.GetString(buffer, 0, result.Count);

            try
            {
                // Deserialize the message
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
                    await webSocket.SendAsync(new ArraySegment<byte>(response), System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                // Log the exception and send an error message
                var errorMessage = $"Error processing message: {ex.Message}";
                var errorResponse = Encoding.UTF8.GetBytes(errorMessage);
                await webSocket.SendAsync(new ArraySegment<byte>(errorResponse), System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
}

// Message DTO for WebSockets
public class MessageDto
{
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string MessageText { get; set; }
}
