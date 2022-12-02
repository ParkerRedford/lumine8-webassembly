using Google.Protobuf.WellKnownTypes;
using lumine8.Server.Data;
using lumine8_GrpcService;
using Microsoft.AspNetCore.SignalR;
using Share = lumine8_GrpcService.Share;

namespace lumine8.Server.Hubs
{
    public class PostRealTime : Hub
    {
        private readonly ApplicationDbContext applicationDbContext;

        public PostRealTime(ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext;
        }

        public bool Authorize(LoginUser loginUser, string username)
        {
            var acc = new Nethereum.Web3.Accounts.Account(loginUser.PrivateKey);
            return (applicationDbContext.Users.Where(x => x.Username == username && x.PublicKey == acc.PublicKey).FirstOrDefault() != null);
        }

        public async void Voted(LoginUser loginUser, Vote vote, string room, bool delete)
        {
            var u = applicationDbContext.Users.Where(x => x.Id == vote.UserId).FirstOrDefault();

            if (!Authorize(loginUser, u.Username))
                throw new UnauthorizedAccessException();

            if (delete)
            {
                var v = applicationDbContext.Votes.Where(x => x.VoteId == vote.VoteId).FirstOrDefault();
                applicationDbContext.Votes.Remove(v);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(vote.VoteId))
                {
                    vote.VoteId = Guid.NewGuid().ToString();
                    vote.Date = Timestamp.FromDateTime(DateTime.UtcNow);
                    applicationDbContext.Votes.Add(vote);
                }
                else
                {
                    vote.Date = Timestamp.FromDateTime(DateTime.UtcNow);
                    applicationDbContext.Votes.Update(vote);
                }
            }
            applicationDbContext.SaveChanges();

            var suVote = GetSharedUser(Id: vote.UserId);
            await Clients.Group(room).SendAsync("HasVoted", vote, suVote, delete);
        }

        public async void Commented(Message message, string room)
        {
            await Clients.Group(room).SendAsync("HasCommented", message);
        }
        public void RemoveComment(Message message, string room)
        {
            Clients.Group(room).SendAsync("HasRemovedComment", message);
        }
        public async void Like(Like like)
        {
            if (applicationDbContext.Likes.Where(x => x.UserId == like.UserId && x.MessageId == like.MessageId).FirstOrDefault() == null)
            {
                like.LikeId = Guid.NewGuid().ToString();
                applicationDbContext.Likes.Add(like);
                applicationDbContext.SaveChanges();
                await Clients.Group($"m_{like.MessageId}").SendAsync("Liked", like);
            }
        }
        public void UnLike(Like like)
        {
            var l = applicationDbContext.Likes.Where(x => x.LikeId == like.LikeId).FirstOrDefault();
            applicationDbContext.Likes.Remove(l);
            applicationDbContext.SaveChanges();
            Clients.Group($"m_{like.MessageId}").SendAsync("UnLiked", like);
        }
        public async void Connect(string room)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, room);
        }
        public async void ConnectMessage(string msg)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"m_{msg}");
        }
        public void Disconnect(string room)
        {
            Groups.RemoveFromGroupAsync(Context.ConnectionId, room);
        }
        public void DisconnectMessage(string msg)
        {
            Groups.RemoveFromGroupAsync(Context.ConnectionId, $"m_{msg}");
        }

        public async void AddHashtag(MessageHashtag hashtag)
        {
            var t = applicationDbContext.MessageHashtags.Where(x => x.Name.ToLower() == hashtag.Name.ToLower() && x.RoomId == hashtag.RoomId).FirstOrDefault();
            if (t == null)
            {
                hashtag.MessageHashtagId = Guid.NewGuid().ToString();
                applicationDbContext.MessageHashtags.Add(hashtag);
                applicationDbContext.SaveChanges();

                await Clients.Caller.SendAsync("AddedHashtag", hashtag);
            }
        }

        public void RemoveHashtag(MessageHashtag hashtag)
        {
            applicationDbContext.MessageHashtags.Remove(hashtag);
            applicationDbContext.SaveChanges();

            Clients.Caller.SendAsync("RemovedHashtag", hashtag);
        }

        public async void PostComment(Message message, Message messagePost, string RoomId)
        {
            message.Date = Timestamp.FromDateTime(DateTime.UtcNow);
            message.RoomId = RoomId;

            message.MessageId = Guid.NewGuid().ToString();
            applicationDbContext.Messages.Add(message);
            applicationDbContext.SaveChanges();

            applicationDbContext.MessageOnMessages.Add(new MessageOnMessage { MessageId = message.MessageId, MessageOnId = messagePost.MessageId });
            applicationDbContext.SaveChanges();

            await  Clients.Group(RoomId).SendAsync("PostedComment", message);
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

        private void DeleteMessagesUpdate(Message message)
        {
            var m = applicationDbContext.Messages.Where(x => x.MessageId == message.MessageId).FirstOrDefault();
            var replies = applicationDbContext.Messages.Where(x => applicationDbContext.MessageOnMessages.Any(y => y.MessageOnId == m.MessageId && x.MessageId == y.MessageId)).ToList();

            if (m != null)
            {
                foreach (var r in replies)
                {
                    var lMsg = applicationDbContext.Messages.Where(x => x.MessageId == m.MessageId).FirstOrDefault();
                    var msgOn = applicationDbContext.MessageOnMessages.Where(x => x.MessageOnId == m.MessageId).FirstOrDefault();

                    applicationDbContext.Messages.Remove(lMsg);
                    applicationDbContext.MessageOnMessages.Remove(msgOn);
                    DeleteMessagesUpdate(r);
                }

                var f = applicationDbContext.MessageOnMessages.Where(x => x.MessageId == m.MessageId).FirstOrDefault();

                if (f != null)
                    applicationDbContext.MessageOnMessages.Remove(f);
                applicationDbContext.Messages.Remove(m);
            }
        }

        public List<Message> GetReplies(string Id)
        {
            return applicationDbContext.Messages.Where(x => applicationDbContext.MessageOnMessages.Any(y => y.MessageOnId == Id && x.MessageId == y.MessageId)).ToList();
        }

        public void RemovePost(Message message)
        {
            var rmToMsg = applicationDbContext.RoomToMessages.Where(x => x.MessageId == message.MessageId).FirstOrDefault();
            var r = applicationDbContext.Rooms.Where(x => x.RoomId == rmToMsg.RoomId).FirstOrDefault();
            applicationDbContext.Rooms.Remove(r);

            var m = applicationDbContext.Messages.Where(x => x.MessageId == message.MessageId).FirstOrDefault();
            DeleteMessagesUpdate(m);
            applicationDbContext.Messages.Remove(m);
            
            applicationDbContext.SaveChanges();

            Clients.Group($"m_{message.MessageId}").SendAsync("RemovedPost");
        }

        public async void GetShareInfo(string RoomId, string signedInUserId)
        {
            var shares = applicationDbContext.Shares.Where(x => x.RoomId == RoomId && x.SenderId == signedInUserId);

            await Clients.Caller.SendAsync("GotShareInfo", shares);
        }

        public async void Share(string RoomId, string signedInUserId, string friendId, string name)
        {
            var share = new Share { RoomId = RoomId, SenderId = signedInUserId, UserId = friendId, Date = Timestamp.FromDateTime(DateTime.UtcNow) };
            share.ShareId = Guid.NewGuid().ToString();
            applicationDbContext.Shares.Add(share);
            applicationDbContext.SaveChanges();
            await Clients.Caller.SendAsync("Shared", share, name);
        }

        public void UnShare(string RoomId, string UserId)
        {
            var share = applicationDbContext.Shares.Where(x => x.RoomId == RoomId && x.UserId == UserId).FirstOrDefault();
            applicationDbContext.Shares.Remove(share);
            applicationDbContext.SaveChanges();

            var u = applicationDbContext.Users.Where(x => x.Id == UserId).FirstOrDefault();

            Clients.Caller.SendAsync("UnShared", share, u.Name);
        }

        public async void Reply(Message message, Message messageOn)
        {
            message.Date = Timestamp.FromDateTime(DateTime.UtcNow);
            message.MessageId = Guid.NewGuid().ToString();
            applicationDbContext.Add(message);
            applicationDbContext.SaveChanges();

            var msgOn = new MessageOnMessage { MessageOnId = messageOn.MessageId, MessageId = message.MessageId };
            msgOn.MessageOnMessageId = Guid.NewGuid().ToString();
            applicationDbContext.MessageOnMessages.Add(msgOn);
            applicationDbContext.SaveChanges();

            await Clients.Group($"{messageOn.RoomId}").SendAsync("Commented", message);
            await Clients.Group($"m_{messageOn.MessageId}").SendAsync("Replied", message);
        }
    }
}
