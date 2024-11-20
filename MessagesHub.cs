using Microsoft.AspNetCore.SignalR;

public class MessagesHub : Hub
{
    public async Task SendMessage(int senderId, int receiverId, string content)
    {
        // You can broadcast the message to other users or to the specific receiver
        await Clients.All.SendAsync("ReceiveMessage", senderId, receiverId, content);
    }
}
