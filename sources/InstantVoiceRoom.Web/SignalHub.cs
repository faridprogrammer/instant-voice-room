namespace InstantVoiceRoom;
using Microsoft.AspNetCore.SignalR;

public class SignalHub : Hub
{
  private static readonly List<(string ConnectionId, string Name)> Users
    = new();

  public override async Task OnDisconnectedAsync(Exception exception)
  {
    lock (Users) { Users.RemoveAll(u => u.ConnectionId == Context.ConnectionId); }
    await BroadcastUserList();
    await base.OnDisconnectedAsync(exception);
  }

  public async Task Join(string displayName)
  {
    lock (Users) { Users.Add((Context.ConnectionId, displayName)); }
    await BroadcastUserList();
    await BrodcastUserJoined(Context.ConnectionId);
    await Clients.Client(Context.ConnectionId).SendAsync("ReceiveConnectionId", Context.ConnectionId);
  }

  public async Task Leave()
  {
    lock (Users) { Users.RemoveAll(u => u.ConnectionId == Context.ConnectionId); }
    await BroadcastUserList();
  }

  private Task BroadcastUserList()
  {
    List<string> names;
    lock (Users) { names = Users.Select(u => u.Name).ToList(); }
    return Clients.All.SendAsync("UsersUpdated", names);
  }

  private async Task BrodcastUserJoined(string userId)
  {
    foreach (var user in Users.Where(u => u.ConnectionId != Context.ConnectionId))
    {
        await Clients.Client(user.ConnectionId).SendAsync("UserJoined", userId);
    }
  }

  public Task SendSignal(string type, string data)
    => Clients.Others.SendAsync("ReceiveSignal", Context.ConnectionId, type, data);

}

