using Google.Protobuf.WellKnownTypes;
using lumine8.Server.Data;
using lumine8_GrpcService;
using Microsoft.AspNetCore.SignalR;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace lumine8.Server.Hubs
{
    public class MainHub : Hub
    {
        private readonly ApplicationDbContext applicationDbContext;
        private readonly string rootPath = (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
           ? Path.Combine(Path.DirectorySeparatorChar.ToString(), "var", "www", "lumine8", "wwwroot", "p") : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "p");

        public MainHub(ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext;
        }

        public bool Authorize(HubCallerContext context, string Username)
        {
            var privateKey = context.GetHttpContext().Request.Headers["PrivateKey"].FirstOrDefault();
            var acc = new Nethereum.Web3.Accounts.Account(privateKey);
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

        /*******Connections*******/
        public async void Connect(string modelId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, modelId);
        }

        public void Disconnect(string modelId)
        {
            Groups.RemoveFromGroupAsync(Context.ConnectionId, modelId);
        }

        public async void GetSize()
        {
            long size = 0;
            var dir = new DirectoryInfo(Path.Combine(rootPath, "v"));
            FileInfo[] fis = dir.GetFiles();

            foreach (var f in fis)
            {
                size += f.Length;
            }

            await Clients.Caller.SendAsync("VideoProgress", size);
        }
    }
}
