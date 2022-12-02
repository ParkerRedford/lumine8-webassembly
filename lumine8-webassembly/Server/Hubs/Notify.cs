using Microsoft.AspNetCore.SignalR;
using lumine8.Server.Data;
using lumine8_GrpcService;
using Image = lumine8_GrpcService.Image;

namespace lumine8.Server.Hubs
{
    public class Notify : Hub
    {
        private readonly ApplicationDbContext applicationDbContext;

        public Notify(ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext;
        }

        public SharedUser GetSharedUser(string UserName = null, string Id = null)
        {
            var user = applicationDbContext.Users.Where(x => x.Username == UserName || x.Id == Id).FirstOrDefault();

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

        public async void SendFriendRequest(Request request)
        {
            var req = applicationDbContext.Requests.Where(x => x.SentToId == request.SentToId && x.SenderId == request.SenderId).FirstOrDefault();
            if (req == null)
            {
                request.RequestId = Guid.NewGuid().ToString();
                applicationDbContext.Requests.Add(request);
                applicationDbContext.SaveChanges();

                var pp = applicationDbContext.ProfilePictures.Where(x => x.UserId == request.SenderId).FirstOrDefault();
                var su = GetSharedUser(Id: request.SenderId);

                var img = new Image();
                if (pp != null)
                    img = applicationDbContext.Images.Where(x => x.ImageId == pp.ImageId).FirstOrDefault();

                await Clients.Group(request.SentToId).SendAsync("FriendRequestSent", request, pp, su, img);
            }

            await Clients.Caller.SendAsync("SentFriendRequest", request);
        }

        public async void SendGroupRequest(GroupRequest request)
        {
            var req = applicationDbContext.GroupRequests.Where(x => x.RequestId == request.RequestId).FirstOrDefault();
            if (req == null)
            {
                request.RequestId = Guid.NewGuid().ToString();
                applicationDbContext.GroupRequests.Add(request);
                applicationDbContext.SaveChanges();
            }

            var pp = applicationDbContext.GroupProfilePictures.Where(x => x.GroupId == request.GroupId).FirstOrDefault();
            var g = applicationDbContext.Groups.Where(x => x.GroupId == request.GroupId).FirstOrDefault();

            var img = new GroupImage();
            if (pp != null)
                img = applicationDbContext.GroupImages.Where(x => x.ImageId == pp.ImageId).FirstOrDefault();

            var u = applicationDbContext.Users.Where(x => x.Id == request.UserId).FirstOrDefault();

            await Clients.Caller.SendAsync("GroupRequestSentCaller", u.Name);
            await Clients.Group(request.UserId).SendAsync("GroupRequestSent", request, g, pp, img);
        }

        public void Announce(string msg)
        {
            Clients.All.SendAsync("AnnounceAll", msg);
        }

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
