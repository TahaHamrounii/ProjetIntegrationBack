using Message.Data;
using Message.Models;
using System.Data.Entity;

namespace Message.Services
{
    public interface IMessageService
    {
        Task SaveMessageAsync(Mssg message);
        Task<IEnumerable<Mssg>> GetMessagesAsync(int senderId, int receiverId);
    }

    public class MessageService : IMessageService
    {
        private readonly AppDbContext _context;

        public MessageService(AppDbContext context)
        {
            _context = context;
        }

        public async Task SaveMessageAsync(Mssg message)
        {
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Mssg>> GetMessagesAsync(int senderId, int receiverId)
        {
            return await _context.Messages
                .Where(m => (m.SenderId == senderId && m.ReceiverId == receiverId) ||
                            (m.SenderId == receiverId && m.ReceiverId == senderId))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }
    }
}
