using Microsoft.AspNetCore.SignalR;
using lumine8.Server.Data;
using System.Text;
using System.Security.Cryptography;
using System.Security.Authentication;
using Google.Protobuf.WellKnownTypes;
using lumine8_GrpcService;

namespace lumine8.Server.Hubs
{
    public class Chat : Hub
    {
        private readonly ApplicationDbContext applicationDbContext;

        public Chat(ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext;
        }

        private SharedUser GetUserFromService(LoginUser service)
        {
            var username = service.Username;
            //var Username = re.GetHttpContext().Request.Query["Username"].FirstOrDefault();
            var u = applicationDbContext.Users.Where(x => x.Username == username).FirstOrDefault();

            if (u != null)
                return GetSharedUser(Username: u.Username);
            else
                return GetSharedUser();
        }

        public bool Authorize(LoginUser user, string Username)
        {
            //var privateKey = context.GetHttpContext().Request.Headers["PrivateKey"].FirstOrDefault();
            var acc = new Nethereum.Web3.Accounts.Account(user.PrivateKey);
            return (applicationDbContext.Users.Where(x => x.Username == Username && x.PublicKey == acc.PublicKey).FirstOrDefault() != null);
        }

        public SharedUser GetSharedUser(string Username = null, string Id = null)
        {
            var user = applicationDbContext.Users.Where(x => x.Username == Username || x.Id == Id).FirstOrDefault();

            if (user != null)
            {
                return new SharedUser
                {
                    Id = user.Id,
                    Name = user.Name,
                    AllowGroupInvites = user.AllowGroupInvites,
                    AllowRequests = user.AllowRequests,
                    AllowSharing = user.AllowSharing,
                    FriendsFeed = user.FriendsFeed,
                    Username = user.Username,
                    HoursFeed = user.HoursFeed,
                    UserSince = user.UserSince
                };
            }

            return new SharedUser();
        }

        public async void SendMessage(ChatMessage message, string signedInUserId, string UserId)
        {
            message.MessageId = Guid.NewGuid().ToString();
            message.Date = Timestamp.FromDateTime(DateTime.UtcNow);
            applicationDbContext.ChatMessages.Add(message);
            applicationDbContext.SaveChanges();

            await Clients.Group(signedInUserId).SendAsync("MessageReceived", message);
            await Clients.Group(UserId).SendAsync("MessageReceived", message);

            var signedInUser = applicationDbContext.Users.Where(x => x.Id == signedInUserId).FirstOrDefault();
            var notification = new Notification { NotificationId = Guid.NewGuid().ToString(), UserId = UserId, Message = $"<div><a href=\"u/{signedInUser.Username}\">{signedInUser.Name}</a><span> has sent you a message:</span><br /><span>{message.Message}</span></div>" };

            applicationDbContext.Notifications.Add(notification);
            applicationDbContext.SaveChanges();
            await Clients.Groups(UserId).SendAsync("SentNotification", notification, signedInUser.Id);
        }

        public async void Writing(string UserId, string RoomId, bool w)
        {
            await Clients.Group(UserId).SendAsync("IsWriting", RoomId, w);
        }

        public async void OpenRoom(LoginUser auth, string UserId)
        {
            /*var re = Context.GetHttpContext().Request.Headers;
            var Username = Context.GetHttpContext().Request.Headers["Username"];
            var token = Context.GetHttpContext().Request.Headers["Token"].FirstOrDefault();
            var password = Context.GetHttpContext().Request.Headers["Password"].FirstOrDefault();*/

            var u = GetUserFromService(auth);
            if(!Authorize(auth, u.Username))
                throw new AuthenticationException();

            string RoomId = string.Empty;
            var r = applicationDbContext.UserRooms.Where(x => x.UserId == u.Id && x.OtherId == UserId).FirstOrDefault();

            if (r == null)
            {
                var rId = Guid.NewGuid().ToString();
                var urr = new UserRoom { UserRoomId = Guid.NewGuid().ToString(), UserId = u.Id, OtherId = UserId, RoomId = rId };
                var fr = new UserRoom { UserRoomId = Guid.NewGuid().ToString(), UserId = UserId, OtherId = u.Id, RoomId = rId };

                applicationDbContext.UserRooms.Add(urr);
                applicationDbContext.UserRooms.Add(fr);
                applicationDbContext.SaveChanges();
            }

            await Clients.Group(u.Id).SendAsync("OpenedRoom", UserId);
        }

        /*public async void Online(string user)
        {
            ConnectedUsers.connectedUsers.Add(new Shared.Chat.ConnectedUser { ConnectionId = Context.ConnectionId, UserId = user });

            var friends = applicationDbContext.Friends.Where(x => x.UserId == user);
            foreach (var f in friends)
                await Clients.Group(f.FriendId).SendAsync("IsOnline", user, Context.ConnectionId);
        }*/

        /*public override Task OnDisconnectedAsync(System.Exception exception)
        {
            var u = ConnectedUsers.connectedUsers.Where(x => x.ConnectionId == Context.ConnectionId).FirstOrDefault();
            if (u != null)
            {
                ConnectedUsers.connectedUsers.Remove(u);

                var friends = applicationDbContext.Friends.Where(x => x.UserId == u.UserId);
                foreach (var f in friends)
                    Clients.Group(f.FriendId).SendAsync("IsOffline", Context.ConnectionId);
            }

            return base.OnDisconnectedAsync(exception);
        }*/

        public async void Connect(string Id)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, Id);
        }

        public void Disconnect(string Id)
        {
            Groups.RemoveFromGroupAsync(Context.ConnectionId, Id);
        }
    }
}
