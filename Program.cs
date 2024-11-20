using Message.Data;
using Message.Models;
using Message.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);
SQLitePCL.Batteries_V2.Init();

// Add services to the container.
builder.Services.AddSignalR(); // Add SignalR services

#region Database Setup
// Configure DbContext to use SQLite with the connection string defined in appsettings.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
#endregion

#region CORS Configuration
// Configure CORS policy to allow requests from localhost:5173 (front-end) with any method and header
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder.WithOrigins("http://localhost:5173") // Update this if needed
                          .AllowAnyHeader()
                          .AllowAnyMethod());
});
#endregion

#region Authentication & JWT Bearer Setup
// Get JWT settings from configuration
var jwtSettings = builder.Configuration.GetSection("Jwt");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true, // Validate token expiration
        ValidateIssuerSigningKey = true, // Ensure the key used to sign the token is valid
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"])) // Use key from config
    };
});
#endregion

#region Services Registration
// Register application services (e.g., IMessageService) with dependency injection
builder.Services.AddScoped<IMessageService, MessageService>(); // Register MessageService
#endregion

#region Swagger Configuration
// Add Swagger for API documentation (only in development)
builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();
#endregion

#region Controllers Setup
// Add controllers to handle HTTP requests
builder.Services.AddControllers();
#endregion

var app = builder.Build();

#region Development Environment Configuration
// Configure middleware for development environment (Swagger UI, API docs)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
#endregion

#region Middleware Configuration
// Enable HTTPS redirection
app.UseHttpsRedirection();

// Enable CORS with the defined policy
app.UseCors("AllowSpecificOrigin");

// Add authentication and authorization middleware
app.UseAuthentication(); // Checks if a valid JWT token is present
app.UseAuthorization();  // Ensures user is authorized to access the requested resources
#endregion

// Map controllers to API routes
app.MapControllers();

// Run the application
app.Run();
// Use SignalR
app.MapHub<MessagesHub>("/messagesHub");

app.Run();