using Microsoft.EntityFrameworkCore;
using Message.Models;
using Message.Data;  // Add this to reference the AppDbContext
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Message.Services
{
    public class FriendService
    {
        private readonly AppDbContext _context;

        // Injecting the DbContext into the service class
        public FriendService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Friend>> GetFriendListAsync(int userId)
        {
            // Query the Friends table for the given userId and return the list asynchronously
            var friends = await _context.Friends
                                         .Where(f => f.UserId == userId)  // Make sure you filter by UserId
                                         .ToListAsync();  // Use ToListAsync for asynchronous fetching

            return friends;
        }
    }
}
