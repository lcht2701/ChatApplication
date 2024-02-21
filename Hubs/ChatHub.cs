using ChatServer.Models;
using Microsoft.AspNetCore.SignalR;

namespace ChatServer.Hubs;

public class ChatHub : Hub
{
    private readonly IDictionary<string, ChatRoomConnection> _connection;

    public ChatHub(IDictionary<string, ChatRoomConnection> chatRoom)
    {
        _connection = chatRoom;
    }

    //User join a chat room
    public async Task JoinRoom(ChatRoomConnection chatRoomConnection)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, chatRoomConnection.Room!);
        _connection[Context.ConnectionId] = chatRoomConnection;
        await Clients.Group(chatRoomConnection.Room!)
            .SendAsync("ReceiveMessage", "Bot", $"{chatRoomConnection.User} has joined the room.");
        await SendConnectedUser(chatRoomConnection.Room!);
    }

    //Send a message
    public async Task SendMessage(string message)
    {
        if (_connection.TryGetValue(Context.ConnectionId, out ChatRoomConnection chatRoomConnection))
        {
            await Clients.Group(chatRoomConnection.Room!)
                .SendAsync("ReceiveMessage", chatRoomConnection.User, message, DateTime.Now);
        }
    }

    public override Task OnDisconnectedAsync(Exception? ex)
    {
        if (_connection.TryGetValue(Context.ConnectionId, out ChatRoomConnection chatRoomConnection))
        {
            return base.OnDisconnectedAsync(ex);
        }
        
        Clients.Group(chatRoomConnection.Room!)
            .SendAsync("ReceiveMessage", "Bot", $"{chatRoomConnection.User} has left the room.");
        SendConnectedUser(chatRoomConnection.Room!);
        return base.OnDisconnectedAsync(ex);
    }
    
    //Send list of users in a chat room
    public Task SendConnectedUser(string room)
    {
        var users = _connection.Values
            .Where(u => u.Room!.Equals(room))
            .Select(s => s.User);

        return Clients.Group(room).SendAsync("ConnectedUser", users);
    }
    
    
    
}