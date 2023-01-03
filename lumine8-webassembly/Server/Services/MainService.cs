using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using lumine8.Server.Data;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using NBitcoin;
using Nethereum.HdWallet;
using lumine8_GrpcService;
using Image = lumine8_GrpcService.Image;
using Share = lumine8_GrpcService.Share;
using Microsoft.EntityFrameworkCore;
using lumine8.Aes;

namespace Services
{
    public class MainService : MainProto.MainProtoBase
    {
        private readonly ApplicationDbContext applicationDbContext;
        private readonly string rootPath = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? Path.Combine(Path.DirectorySeparatorChar.ToString(), "var", "www", "lumine8.com", "wwwroot", "p") : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "p");
        public MainService(ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext;
        }

        public SharedUser GetUserFromRequest(ServerCallContext context)
        {
            var Username = context.GetHttpContext().Request.Headers["Username"].FirstOrDefault();
            var u = applicationDbContext.Users.Where(x => x.Username == Username).FirstOrDefault();

            if (u != null)
                return GetSharedUser(Username: u.Username);
            else
                return GetSharedUser();
        }

        /*public override Task<MakePaymentResponse> MakePayment(MakePaymentRequest request, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Find(request.User.Id);
            //var u = applicationDbContext.Users.Where(x => x.Id == request.User.Id).FirstOrDefault();
            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            u.LastPayment = Timestamp.FromDateTime(DateTime.UtcNow);
            applicationDbContext.Users.Update(u);
            applicationDbContext.SaveChanges();

            return Task.FromResult(new MakePaymentResponse());
        }*/

        public bool Authorize(ServerCallContext context, string Username)
        {
            /*var privateKey = context.GetHttpContext().Request.Headers["PrivateKey"].FirstOrDefault();
            var acc = new Nethereum.Web3.Accounts.Account(privateKey);
            return applicationDbContext.Users.Where(x => x.Username == Username && x.PublicKey == acc.PublicKey).FirstOrDefault() != null;*/

            var pass = context.GetHttpContext().Request.Headers["Password"].FirstOrDefault() ?? string.Empty;
            var key = context.GetHttpContext().Request.Headers["PrivateKey"].FirstOrDefault() ?? string.Empty;

            var aes = new CAes(pass, key);
            return applicationDbContext.Users.Where(x => x.Username == Username && x.PublicKey == aes.Encrypt(Username, aes.iv, aes.key).ToString()) != null;
        }

        /*public bool bVerified(string UserId)
        {
            var u = applicationDbContext.Users.Where(x => x.Id == UserId).FirstOrDefault();
            if (DateTime.UtcNow.AddYears(1) > u.LastPayment.ToDateTime())
                return true;
            else
                return false;
        }*/

        public SharedUser GetSharedUser(string Username = "", string Id = "")
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

        public override Task<BoolValue> CheckVersion(Id id, ServerCallContext context)
        {
            return Task.FromResult(new BoolValue { Value = "3d9ca813-6f37-40f9-8735-95e2257f0f6a" == id.Id_ });
        }

        public override Task<Authenticated> Authenticate(LoginUser loginUser, ServerCallContext context)
        {
            var acc = new Nethereum.Web3.Accounts.Account(loginUser.PrivateKey);
            var u = applicationDbContext.Users.Where(x => x.Username == loginUser.Username && x.PublicKey == acc.PublicKey).FirstOrDefault();

            return Task.FromResult(new Authenticated { IsAuthenticated = u != null });
        }

        public override Task<Empty> ChangePassword(LoginUser loginUser, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Where(x => x.Username == loginUser.Username).FirstOrDefault();
            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            var wall = new Wallet(loginUser.Mnemonic, loginUser.Password);
            var acc = wall.GetAccount(0);

            var au = applicationDbContext.Users.Where(x => x.Username == loginUser.Username && x.PublicKey == acc.PublicKey).FirstOrDefault();

            if (au == null)
                throw new UnauthorizedAccessException();

            /*if (loginUser.Password != loginUser.ConfirmPassword)
                throw new UnauthorizedAccessException();

            var u = GetUserFromRequest(context);
            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();*/

            var chWall = new Wallet(loginUser.Mnemonic, loginUser.ConfirmPassword);
            var chAcc = chWall.GetAccount(0);

            au.PublicKey = chAcc.PublicKey;
            applicationDbContext.Users.Update(au);
            applicationDbContext.SaveChanges();

            return Task.FromResult(new Empty());

            /*var au = applicationDbContext.Users.Where(x => x.Id == u.Id).FirstOrDefault();
            
            if (HashString(loginUser.OldPassword, au.PasswordSalt) == au.PasswordStamp)
            {
                var guid = Guid.NewGuid().ToString();

                au.PasswordSalt = guid;
                au.PasswordStamp = HashString(loginUser.Password, guid);
                applicationDbContext.Users.Update(au);
                applicationDbContext.SaveChanges();
                return Task.FromResult(new Empty());
            }
            else
                throw new UnauthorizedAccessException();*/
        }

        public override Task<CreateAccountResponse> CreateAccount(LoginUser loginUser, ServerCallContext context)
        {
            Mnemonic mnemonic = new Mnemonic(Wordlist.English, WordCount.TwentyFour);
            string pw = loginUser.Password;

            var wall = new Wallet(mnemonic.ToString(), pw);
            var acc = wall.GetAccount(0);

            var aes = new CAes(loginUser.Password, loginUser.PrivateKey);

            if (!string.IsNullOrWhiteSpace(loginUser.Username) && applicationDbContext.Users.Where(x => x.Username == loginUser.Username).FirstOrDefault() == null)
            {
                string salt = Guid.NewGuid().ToString("N");

                var user = new ApplicationUser
                {
                    PublicKey = aes.Encrypt(loginUser.Username, aes.iv, aes.key).ToString(),
                    //PasswordSalt = salt,
                    //PasswordStamp = HashString(loginUser.Password, salt),
                    HoursFeed = 168,
                    Username = loginUser.Username,
                    //Email = loginUser.Email,
                    //EmailConfirmed = false,
                    UserSince = Timestamp.FromDateTime(DateTime.UtcNow),
                    Name = loginUser.Username,
                    AllowGroupInvites = true,
                    AllowRequests = true,
                    AllowSharing = true,
                    FriendsFeed = true
                };

                applicationDbContext.Users.Add(user);
                applicationDbContext.SaveChanges();

                /*var greq = new GroupRequest { GroupId = "9986b092-5a46-4d12-b852-951b0f11f93e", UserId = user.Id };
                applicationDbContext.GroupRequests.Add(greq);

                var req = new Request { SenderId = "6b2feb5d-3066-42f1-ac89-00169940cd1b", SentToId = user.Id };
                applicationDbContext.Requests.Add(req);
                applicationDbContext.SaveChanges();*/

                return Task.FromResult(new CreateAccountResponse { SharedUser = GetSharedUser(Id: user.Id), Mnemonic = mnemonic.ToString(), PrivateKey = acc.PrivateKey });
            }

            throw new System.Exception();
        }

        public override Task<SharedUser> GetUser(LoginUser loginUser, ServerCallContext context)
        {
            return Task.FromResult(GetSharedUser(Username: loginUser.Username));
        }

        private void DeleteMessagesUpdate(Message message)
        {
            var m = applicationDbContext.Messages.Find(message.MessageId);
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

        //Not tested. Might break the platform if used
        public override Task<Empty> DeleteAccount(LoginUser loginUser, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Where(x => x.Username == loginUser.Username).FirstOrDefault();

            var g = applicationDbContext.Groups.Where(x => x.OwnerId == u.Id).ToList();
            var r = applicationDbContext.RoomGroups.Where(x => g.Any(y => y.GroupId == x.GroupId)).ToList();
            var gm = applicationDbContext.RoomToMessages.Where(x => r.Any(y => y.RoomId == x.RoomId)).ToList();
            var msg = applicationDbContext.Messages.Where(x => x.SenderId == u.Id || gm.Any(y => y.MessageId == x.MessageId)).ToList();
            foreach (var m in msg)
                DeleteMessagesUpdate(m);

            var chats = applicationDbContext.UserRooms.Where(x => x.UserId == u.Id || x.OtherId == u.Id).ToList();
            var rChats = applicationDbContext.ChatMessages.Where(x => chats.Any(y => y.RoomId == x.RoomId)).ToList();
            applicationDbContext.ChatMessages.RemoveRange(rChats);

            throw new UnauthorizedAccessException();

            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            string uPath = Path.Combine(rootPath, "u", u.Id);

            if (File.Exists(uPath))
                File.Delete(uPath);

            var groups = applicationDbContext.Groups.Where(x => x.OwnerId == u.Id).ToList();

            foreach (var gr in groups)
            {
                string path = Path.Combine(rootPath, "g", gr.GroupId);

                if (File.Exists(path))
                    File.Delete(path);
            }

            applicationDbContext.Users.Remove(u);
            applicationDbContext.SaveChanges();

            return Task.FromResult(new Empty());
        }

        /*****Profile*****/
        public override Task<ChatPersonModel> GetChatPersonModel(UserId id, ServerCallContext context)
        {
            var u = GetUserFromRequest(context);
            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            var f = GetSharedUser(Id: id.UserId_);
            var ur = applicationDbContext.UserRooms.Where(x => x.UserId == u.Id && x.OtherId == id.UserId_).FirstOrDefault();

            if (ur == null)
            {
                var rId = Guid.NewGuid().ToString();
                var urr = new UserRoom { UserId = u.Id, OtherId = id.UserId_, RoomId = rId };
                var fr = new UserRoom { UserId = id.UserId_, OtherId = u.Id, RoomId = rId };

                applicationDbContext.UserRooms.Add(urr);
                applicationDbContext.UserRooms.Add(fr);
                applicationDbContext.SaveChanges();

                ur = applicationDbContext.UserRooms.Where(x => x.UserId == u.Id && x.OtherId == id.UserId_).FirstOrDefault();
            }

            var ms = applicationDbContext.ChatMessages.Where(x => x.RoomId == ur.RoomId).ToList();

            var model = new ChatPersonModel { User = f, UserRoom = ur };
            model.ChatMessages.AddRange(ms);

            return Task.FromResult(model);
        }

        public override Task<IntroductionPageModel> GetIntroductionPageModel(Empty Empty, ServerCallContext context)
        {
            var u = GetUserFromRequest(context); ;
            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            var signedInUser = GetSharedUser(Username: u.Username);

            var profileSecurity = applicationDbContext.ProfileSecurities.Where(x => x.UserId == signedInUser.Id).FirstOrDefault();
            var privateProfile = applicationDbContext.PrivateProfiles.Where(x => x.UserId == signedInUser.Id);
            var aboutMe = applicationDbContext.AboutMe.Where(x => x.UserId == signedInUser.Id).FirstOrDefault();

            if (aboutMe == null)
            {
                aboutMe = new AboutMe { UserId = signedInUser.Id };
                applicationDbContext.AboutMe.Add(aboutMe);
                applicationDbContext.SaveChanges();
            }

            if (profileSecurity == null)
            {
                profileSecurity = new ProfileSecurity
                {
                    UserId = signedInUser.Id,
                    AboutMe = SecurityLevel.PublicLevel,
                    Friends = SecurityLevel.PublicLevel,
                    DOB = SecurityLevel.PublicLevel,
                    Education = SecurityLevel.PublicLevel,
                    Groups = SecurityLevel.PublicLevel,
                    Interests = SecurityLevel.PublicLevel,
                    Lumine = SecurityLevel.PublicLevel,
                    Pictures = SecurityLevel.PublicLevel,
                    PlacesLived = SecurityLevel.PublicLevel,
                    Relationship = SecurityLevel.PublicLevel,
                    Sex = SecurityLevel.PublicLevel,
                    WorkHistory = SecurityLevel.PublicLevel
                };

                applicationDbContext.ProfileSecurities.Add(profileSecurity);
                applicationDbContext.SaveChanges();
            }

            var pp = applicationDbContext.ProfilePictures.Where(x => x.UserId == signedInUser.Id).FirstOrDefault();
            var pps = applicationDbContext.Images.Where(x => x.Category == "Profile Pictures" && x.UserId == signedInUser.Id);

            var model = new IntroductionPageModel { SignedInUser = signedInUser, ProfileSecurity = profileSecurity, AboutMe = aboutMe, ProfilePicture = pp };
            model.PrivateProfile.AddRange(privateProfile);
            model.ProfilePictures.AddRange(pps);

            return Task.FromResult(model);
        }

        public override Task<Empty> UpdateAboutMe(AboutMe aboutMe, ServerCallContext context)
        {
            var user = applicationDbContext.Users.Find(aboutMe.UserId);

            if (!Authorize(context, user.Username))
                throw new UnauthorizedAccessException();

            applicationDbContext.AboutMe.Update(aboutMe);
            applicationDbContext.SaveChanges();

            return Task.FromResult(new Empty());
        }

        public override Task<CategoryPageModel> GetCategoryModel(CategoryPageModelRequest cm, ServerCallContext context)
        {
            var signedInUser = GetUserFromRequest(context);

            var u = GetSharedUser(Username: cm.Username);
            var images = applicationDbContext.Images.Where(x => x.Category == cm.Category && x.UserId == u.Id).ToList();

            var model = new CategoryPageModel { SignedInUser = signedInUser, User = u };
            model.Images.AddRange(images);

            return Task.FromResult(model);
        }

        public override Task<Image> DeleteCategory(Image img, ServerCallContext context)
        {
            var limg = applicationDbContext.Images.Find(img.ImageId);
            var imgs = applicationDbContext.Images.Where(x => x.Category == limg.Category && x.ImageId == limg.ImageId).ToList();

            if (imgs.Count() == 1)
            {
                var rmCat = applicationDbContext.CategoryRooms.Where(x => x.Category == limg.Category).ToList();
                if (rmCat.Count() > 0)
                {
                    foreach (var r in rmCat)
                        applicationDbContext.CategoryRooms.Remove(r);
                }
                applicationDbContext.SaveChanges();
            }

            string path = Path.Combine(rootPath, "u", limg.ImageId, limg.FileName);

            if (File.Exists(path))
                File.Delete(path);

            var c = applicationDbContext.UserComments.Where(x => x.ImageId == limg.ImageId).ToList();
            applicationDbContext.UserComments.RemoveRange(c);

            applicationDbContext.Images.Remove(limg);
            applicationDbContext.SaveChanges();

            return Task.FromResult(img);
        }

        public override Task<Image> EditImage(Image img, ServerCallContext context)
        {
            applicationDbContext.Images.Update(img);
            applicationDbContext.SaveChanges();

            return Task.FromResult(img);
        }

        public override Task<PicturesPageModel> GetPicturesPageModel(Id id, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Where(x => x.Username == id.Id_).FirstOrDefault();
            var signedInUser = applicationDbContext.Users.Where(x => x.Username == GetUserFromRequest(context).Username).FirstOrDefault();

            var images = applicationDbContext.Images.Where(x => x.UserId == u.Id).ToList();

            var path = Path.Combine(rootPath, "u", u.Id);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var directories = new DirectoryInfo(path).EnumerateDirectories().Select(x => x.FullName).ToList();
            var rFiles = new FileNamesRequest { UserId = u.Id };
            rFiles.CurrentDirectories.AddRange(directories);
            var files = GetFileNamesModel(rFiles, context);
            //var files = GetFileNames(new CGetFiles { cd = directories }, u.Id);

            Dictionary<string, string> dict = new();

            for (int i = 0; directories.Count > i; i++)
                directories[i] = directories[i].Replace(path, string.Empty);

            var model = new PicturesPageModel { User = GetSharedUser(Id: u.Id), SignedInUser = GetSharedUser(Id: signedInUser.Id) };
            model.Directories.AddRange(directories);
            model.Files.AddRange(files.Result.Files);
            model.Images.AddRange(images);

            return Task.FromResult(model);
        }

        public override Task<FileNamesModel> GetFileNamesModel(FileNamesRequest get, ServerCallContext context)
        {
            string str = string.Empty;
            if (get.CurrentDirectories != null)
            {
                foreach (var s in get.CurrentDirectories)
                    str += s + Path.DirectorySeparatorChar;
            }

            var path = Path.Combine(rootPath, "u", get.UserId);
            DirectoryInfo files = new DirectoryInfo(Path.Combine(path, str));

            List<string> sFiles = files.EnumerateFiles().Select(x => x.FullName).ToList();

            for (int i = 0; sFiles.Count() > i; i++)
                sFiles[i] = sFiles[i].Replace(path, string.Empty);

            var directories = new DirectoryInfo(Path.Combine(path, str)).EnumerateDirectories().Select(x => x.FullName).ToList();

            for (int i = 0; directories.Count > i; i++)
                directories[i] = directories[i].Replace(Path.Combine(path, str), string.Empty);

            var model = new FileNamesModel();
            model.Files.AddRange(sFiles);
            model.Directories.AddRange(directories);
            model.CurrentDirectories.AddRange(get.CurrentDirectories);

            return Task.FromResult(model);
        }

        public override Task<ProfilePageModel> GetProfilePageModel(Id id, ServerCallContext context)
        {
            var user = GetSharedUser(Username: id.Id_);
            var signedInUser = GetUserFromRequest(context);

            var links = applicationDbContext.Links.Where(x => x.UserId == user.Id).ToList();

            var profileSecurity = applicationDbContext.ProfileSecurities.Find(user.Id);
            var privateProfile = applicationDbContext.PrivateProfiles.Where(x => x.UserId == user.Id).ToList();
            var aboutMe = applicationDbContext.AboutMe.Find(user.Id);

            if (aboutMe == null)
            {
                aboutMe = new AboutMe { UserId = user.Id };
                applicationDbContext.AboutMe.Add(aboutMe);
                applicationDbContext.SaveChanges();
            }

            if (profileSecurity == null)
            {
                profileSecurity = new ProfileSecurity
                {
                    UserId = user.Id,
                    AboutMe = SecurityLevel.PublicLevel,
                    Friends = SecurityLevel.PublicLevel,
                    DOB = SecurityLevel.PublicLevel,
                    Education = SecurityLevel.PublicLevel,
                    Groups = SecurityLevel.PublicLevel,
                    Interests = SecurityLevel.PublicLevel,
                    Lumine = SecurityLevel.PublicLevel,
                    Pictures = SecurityLevel.PublicLevel,
                    PlacesLived = SecurityLevel.PublicLevel,
                    Relationship = SecurityLevel.PublicLevel,
                    Sex = SecurityLevel.PublicLevel,
                    WorkHistory = SecurityLevel.PublicLevel
                };

                applicationDbContext.ProfileSecurities.Add(profileSecurity);
                applicationDbContext.SaveChanges();
            }

            //var placesLived = applicationDbContext.PlacesLived.Where(x => x.UserId == user.Id).ToList();
            var workHistories = applicationDbContext.WorkHistory.Where(x => x.UserId == user.Id).ToList();
            var educationList = applicationDbContext.EducationList.Where(x => x.UserId == user.Id).ToList();

            var rooms = applicationDbContext.Rooms.Where(x => x.OwnerId == user.Id && !applicationDbContext.RoomGroups.Any(r => r.RoomId == x.RoomId)).ToList();

            var friends = applicationDbContext.Friends.Where(x => x.UserId == user.Id).ToList();
            var groups = applicationDbContext.Groups.Where(x => applicationDbContext.Roles.Any(r => r.GroupId == x.GroupId && r.UserId == user.Id)).ToList();
            var roles = applicationDbContext.Roles.Where(x => x.UserId == user.Id).ToList();

            var friend = applicationDbContext.Friends.Where(x => x.UserId == signedInUser.Id && x.FriendId == user.Id).FirstOrDefault();

            var myFriends = applicationDbContext.Friends.Where(x => x.UserId == signedInUser.Id).Select(x => x.FriendId);
            var mutualFriends = friends.Select(x => x.FriendId).Intersect(myFriends).ToList();

            var suFriends = new List<SharedUser>();
            friends.ForEach(x => suFriends.Add(GetSharedUser(Id: x.FriendId)));

            var pp = applicationDbContext.ProfilePictures.Where(x => x.UserId == user.Id).FirstOrDefault();
            var pps = applicationDbContext.Images.Where(x => x.Category == "Profile Pictures" && x.UserId == signedInUser.Id).ToList();

            var request = applicationDbContext.Requests.Where(x => x.SenderId == user.Id && x.SentToId == signedInUser.Id).FirstOrDefault() ?? new Request();

            var petitions = applicationDbContext.Petitions.Where(x => x.CreatedById == user.Id).ToList();

            var model = new ProfilePageModel { AboutMe = aboutMe, Friend = friend, ProfilePicture = pp, ProfileSecurity = profileSecurity, Request = request, SignedInUser = signedInUser, User = user };
            model.EducationList.AddRange(educationList);
            model.Friends.AddRange(friends);
            model.SuFriends.AddRange(suFriends);
            model.Groups.AddRange(groups);
            model.Links.AddRange(links);
            model.MutualFriends.AddRange(mutualFriends);
            model.ProfilePictures.AddRange(pps);
            model.PrivateProfiles.AddRange(privateProfile);
            model.Roles.AddRange(roles);
            model.Rooms.AddRange(rooms);
            model.WorkHistories.AddRange(workHistories);
            model.Petitions.AddRange(petitions);

            return Task.FromResult(model);
        }

        public override Task<CreateGroupResponse> CreateGroup(GroupModel group, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Find(group.OwnerId);

            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            if (string.IsNullOrWhiteSpace(group.Name))
                group.Name = "Untitled";

            group.CreateDate = Timestamp.FromDateTime(DateTime.UtcNow);
            applicationDbContext.Groups.Add(group);
            applicationDbContext.SaveChanges();

            var role = new Role { RoleType = RoleType.Owner, UserId = group.OwnerId, GroupId = group.GroupId };

            applicationDbContext.SectionRoles.Add(new SectionRoles
            {
                GroupId = group.GroupId,
                Description = RoleType.Owner,
                Permissions = RoleType.Owner,
                PermissionsOneBelow = RoleType.Owner,
                Pictures = RoleType.Member,
                RemoveMember = RoleType.Administrator,
                Ban = RoleType.Administrator,
                RemoveBan = RoleType.Administrator,
                RemovePictures = RoleType.Moderator,
                RemoveLumine = RoleType.Moderator,
                SendInvites = RoleType.Moderator,
                ShareLumine = RoleType.Member,

                CheckLumine = RoleType.Moderator,
                PostLumine = RoleType.Member,
                RequestLumine = RoleType.NoRole,
                SeeLumine = RoleType.NoRole,
                UpdatePictures = RoleType.Moderator,
                UploadPictures = RoleType.Member,
                Hashtags = RoleType.Owner,
            });
            applicationDbContext.Roles.Add(role);
            applicationDbContext.SaveChanges();

            return Task.FromResult(new CreateGroupResponse { Group = group, Role = role });
        }

        public override Task<Room> GetPagePost(Id id, ServerCallContext context)
        {
            return Task.FromResult(applicationDbContext.Rooms.Where(x => x.RoomId == id.Id_).FirstOrDefault());
        }

        public override Task<ModalModel> GetModalModel(Id id, ServerCallContext context)
        {
            var comments = applicationDbContext.UserComments.Where(x => x.ImageId == id.Id_).ToList();
            var users = applicationDbContext.Users.Where(x => comments.Any(y => y.UserId == x.Id)).ToList();
            var suUsers = new List<SharedUser>();
            foreach (var u in users)
                suUsers.Add(GetSharedUser(Id: u.Id));

            var model = new ModalModel();
            model.UserComments.AddRange(comments);
            model.Users.AddRange(suUsers);

            return Task.FromResult(model);
        }

        public override Task<AddCommentResponse> AddComment(UserComment comment, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Find(comment.UserId);
            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            comment.Date = Timestamp.FromDateTime(DateTime.UtcNow);
            applicationDbContext.UserComments.Add(comment);
            applicationDbContext.SaveChanges();

            return Task.FromResult(new AddCommentResponse { User = GetSharedUser(Id: comment.UserCommentId), UserComment = comment });
        }

        public override Task<UserComment> DeleteComment(UserComment comment, ServerCallContext context)
        {
            var c = applicationDbContext.UserComments.Find(comment.UserCommentId);

            var u = applicationDbContext.Users.Find(c.UserId);
            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            applicationDbContext.UserComments.Remove(c);
            applicationDbContext.SaveChanges();

            return Task.FromResult(comment);
        }

        public override Task<SearchPageModel> GetSearchPageModel(Empty Empty, ServerCallContext context)
        {
            var signedInUser = GetUserFromRequest(context);
            var users = applicationDbContext.Users.Where(x => x.Id != signedInUser.Id).ToList().Take(100);

            var suUsers = new List<SharedUser>();
            users.ToList().ForEach(x => suUsers.Add(GetSharedUser(Id: x.Id)));

            var pps = applicationDbContext.ProfilePictures.AsEnumerable().Where(x => users.Any(y => y.Id == x.UserId)).ToList();
            var images = applicationDbContext.Images.AsEnumerable().Where(x => pps.Any(y => y.ImageId == x.ImageId)).ToList();

            var groups = applicationDbContext.Groups.ToList().Take(50);

            var gpps = applicationDbContext.GroupProfilePictures.AsEnumerable().Where(x => groups.Any(y => y.GroupId == x.GroupId)).ToList();
            var gImages = applicationDbContext.GroupImages.AsEnumerable().Where(x => pps.Any(y => y.ImageId == x.ImageId)).ToList();

            var model = new SearchPageModel { SignedInUser = signedInUser };
            model.Users.AddRange(suUsers);
            model.ProfilePictures.AddRange(pps);
            model.Images.AddRange(images);
            model.Groups.AddRange(groups);
            model.GroupProfilePictures.AddRange(gpps);
            model.GroupImages.AddRange(gImages);

            return Task.FromResult(model);
        }

        public override Task<GetInterestsResponse> GetInterests(Id id, ServerCallContext context)
        {
            var interests = applicationDbContext.Interests.Where(x => x.InterestName.ToLower().Contains(id.Id_.ToLower())).ToList();
            var model = new GetInterestsResponse();
            model.Interests.AddRange(interests);

            return Task.FromResult(model);
        }

        public override Task<SearchResponse> GetSearchModel(SearchModel s, ServerCallContext context)
        {
            var users = applicationDbContext.Users.Where(x => (x.Name.ToLower().Contains(s.Name.ToLower()) || x.Username.ToLower().Contains(s.Name.ToLower()))
        && x.Username != s.SignedInUser.Id).ToList();

            if (s.BInterest && s.Interests.Any())
                users = users.Where(x => s.Interests.Any(y => applicationDbContext.Interests.Any(p => p.InterestName.ToLower().Contains(y.InterestName.ToLower()) && p.UserId == x.Id))).ToList();

            //About
            if (s.BAbout)
            {
                var about = applicationDbContext.About.Where(x => x.MartialStatus == s.About.MartialStatus && x.Sex == s.About.Sex);
                if (about.Any())
                    users = users.Where(x => about.Any(y => y.AboutId == x.Id && y.AboutId != s.SignedInUser.Id)).ToList();
            }

            //Places Lives
            /*if (s.bPlace)
            {
                s.Live.State = (!string.IsNullOrWhiteSpace(s.Live.State)) ? s.Live.State : "";
                s.Live.City = (!string.IsNullOrWhiteSpace(s.Live.City)) ? s.Live.City : "";
                s.Live.County = (!string.IsNullOrWhiteSpace(s.Live.County)) ? s.Live.County : "";
                s.Live.PostalCode = (!string.IsNullOrWhiteSpace(s.Live.PostalCode)) ? s.Live.PostalCode : "";

                var places = applicationDbContext.PlacesLived.Where(x =>
                   x.Country == s.Live.Country &&
                   x.State.ToLower().Contains(s.Live.State.ToLower()) &&
                   x.City.ToLower().Contains(s.Live.City.ToLower()) &&
                   x.County.ToLower().Contains(s.Live.County.ToLower()) &&
                   x.PostalCode.ToLower().Contains(s.Live.PostalCode.ToLower()) &&
                   x.UserId != s.signedInUser.Id).ToList();

                if (places.Any())
                {
                    users = users.Where(x => places.Any(p => p.UserId == x.Id)).ToList();
                }
            }*/

            //Education
            if (s.BEducation)
            {
                s.Education.SchoolName = !string.IsNullOrWhiteSpace(s.Education.SchoolName) ? s.Education.SchoolName : "";
                s.Education.Major = !string.IsNullOrWhiteSpace(s.Education.Major) ? s.Education.Major : "";
                s.Education.Minor = !string.IsNullOrWhiteSpace(s.Education.Minor) ? s.Education.Minor : "";

                var educationList = applicationDbContext.EducationList.Where(x =>
                x.SchoolName.ToLower().Contains(s.Education.SchoolName.ToLower()) &&
                x.Degree == s.Education.Degree &&
                x.Major.ToLower().Contains(s.Education.Major.ToLower()) &&
                x.Minor.ToLower().Contains(s.Education.Minor.ToLower()) &&
                x.UserId != s.SignedInUser.Id);

                if (educationList.Any())
                {
                    users = users.Where(x => educationList.Any(e => e.UserId == x.Id)).ToList();
                }
            }

            var suUsers = new List<SharedUser>();
            users.ForEach(x => suUsers.Add(GetSharedUser(Id: x.Id)));

            var pps = applicationDbContext.ProfilePictures.AsEnumerable().Where(x => users.Any(y => y.Id == x.UserId)).ToList();
            var images = applicationDbContext.Images.AsEnumerable().Where(x => pps.Any(y => y.ImageId == x.ImageId)).ToList();

            var model = new SearchResponse();
            model.Users.AddRange(suUsers);
            model.ProfilePictures.AddRange(pps);
            model.Images.AddRange(images);

            return Task.FromResult(model);
        }

        public override Task<SearchGroupResponse> GetSearchGroup(SearchGroupModel search, ServerCallContext context)
        {
            var groupTags = applicationDbContext.Hashtags.AsEnumerable().Where(x => search.Tags.Any(y => x.Name.ToLower().Contains(y.ToLower()))).ToList();
            var groups = new List<GroupModel>();
            if (groupTags.Count() > 0)
                groups = applicationDbContext.Groups.AsEnumerable().Where(x => x.Name.ToLower().Contains(search.Name.ToLower()) && x.GroupJoin == search.Join && groupTags.Any(y => y.GroupId == x.GroupId)).ToList();
            else
            {
                if (!string.IsNullOrWhiteSpace(search.Name))
                    groups = applicationDbContext.Groups.Where(x => x.Name.ToLower().Contains(search.Name.ToLower()) && x.GroupJoin == search.Join).ToList();
                else
                    groups = applicationDbContext.Groups.AsEnumerable().Where(x => x.GroupJoin == search.Join).ToList();
            }

            var pps = applicationDbContext.GroupProfilePictures.AsEnumerable().Where(x => groups.Any(y => y.GroupId == x.GroupId)).ToList();
            var images = applicationDbContext.GroupImages.AsEnumerable().Where(x => pps.Any(y => y.ImageId == x.ImageId)).ToList();

            var model = new SearchGroupResponse();
            model.Groups.AddRange(groups);
            model.GroupProfilePictures.AddRange(pps);
            model.Images.AddRange(images);

            return Task.FromResult(model);
        }

        public override Task<SecurityPageModel> GetSecurityPageModel(Empty Empty, ServerCallContext context)
        {
            var ur = GetUserFromRequest(context);
            var profileSecurity = applicationDbContext.ProfileSecurities.Find(ur.Id);

            var exceptions = applicationDbContext.PrivateProfiles.Where(x => x.UserId == ur.Id).ToList();
            var uExceptions = applicationDbContext.Users.AsEnumerable().Where(x => exceptions.Any(y => y.UserId == x.Id)).ToList();

            var suExceptions = new List<SharedUser>();
            uExceptions.ForEach(x => suExceptions.Add(GetSharedUser(Id: x.Id)));

            var friends = applicationDbContext.Friends.Where(x => x.UserId == ur.Id).ToList();

            var model = new SecurityPageModel { User = ur, ProfileSecurity = profileSecurity };
            model.PrivateProfiles.AddRange(exceptions);
            model.Users.AddRange(suExceptions);
            model.Friends.AddRange(friends);

            return Task.FromResult(model);
        }

        public override Task<PrivateProfile> AddException(SharedUser user, ServerCallContext context)
        {
            var u = GetUserFromRequest(context);
            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            var pp = applicationDbContext.PrivateProfiles.Where(x => x.UserId == u.Id && x.WhoId == user.Id).FirstOrDefault();
            var lpp = new PrivateProfile { UserId = u.Id, WhoId = user.Id };

            if (pp == null)
            {
                applicationDbContext.PrivateProfiles.Add(lpp);
                applicationDbContext.SaveChanges();

                return Task.FromResult(lpp);
            }
            else
                throw new System.Exception();
        }

        public override Task<Empty> UpdateException(PrivateProfile privateProfile, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Where(x => x.Id == privateProfile.UserId).FirstOrDefault();

            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            applicationDbContext.PrivateProfiles.Update(privateProfile);
            applicationDbContext.SaveChanges();

            return Task.FromResult(new Empty());
        }

        public override Task<PrivateProfile> DeleteException(PrivateProfile privateProfile, ServerCallContext context)
        {
            var pp = applicationDbContext.PrivateProfiles.Where(x => x.PrivateProfileId == privateProfile.UserId).FirstOrDefault();
            var u = applicationDbContext.Users.Where(x => x.Id == pp.UserId).FirstOrDefault();
            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            applicationDbContext.PrivateProfiles.Remove(pp);
            applicationDbContext.SaveChanges();

            return Task.FromResult(privateProfile);
        }

        public override Task<PeopleModel> GetPeople(Id id, ServerCallContext context)
        {
            var people = applicationDbContext.Users.Where(x => x.Name.ToLower().Contains(id.Id_.ToLower())).ToList();

            var suPeople = new List<SharedUser>();
            people.ForEach(x => suPeople.Add(GetSharedUser(Id: x.Id)));

            var model = new PeopleModel();
            model.Users.AddRange(suPeople);

            return Task.FromResult(model);
        }

        public override Task<ProfileSecurity> UpdateSecurity(ProfileSecurity profileSecurity, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Find(profileSecurity.UserId);
            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            applicationDbContext.ProfileSecurities.Update(profileSecurity);
            applicationDbContext.SaveChanges();

            return Task.FromResult(profileSecurity);
        }

        public override Task<SharedUser> UpdateUserSettings(SharedUser user, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Find(user.Id);
            if (!Authorize(context, user.Username))
                throw new UnauthorizedAccessException();

            u.AllowRequests = user.AllowRequests;
            u.AllowGroupInvites = user.AllowGroupInvites;
            u.AllowSharing = user.AllowSharing;
            u.FriendsFeed = user.FriendsFeed;

            applicationDbContext.Users.Update(u);
            applicationDbContext.SaveChanges();

            return Task.FromResult(user);
        }

        public override Task<Image> DeleteProfilePicture(Image image, ServerCallContext context)
        {
            var lp = applicationDbContext.Images.Find(image.ImageId);

            var com = applicationDbContext.UserComments.Where(x => x.ImageId == lp.ImageId);
            applicationDbContext.UserComments.RemoveRange(com);

            var pp = applicationDbContext.ProfilePictures.Where(x => x.ImageId == lp.ImageId).FirstOrDefault();
            if (pp != null && pp.ImageId == lp.ImageId)
                applicationDbContext.ProfilePictures.Remove(pp);

            applicationDbContext.Images.Remove(lp);

            string path = Path.Combine(rootPath, "u", lp.UserId, lp.FileName);

            if (File.Exists(path))
                File.Delete(path);

            applicationDbContext.SaveChanges();

            return Task.FromResult(image);
        }

        public override Task<ChangeCategoriesResponse> ChangeCategories(ChangeCategoriesRequest ch, ServerCallContext context)
        {
            var u = GetUserFromRequest(context);

            var imgs = applicationDbContext.Images.Where(x => x.Category == ch.From && x.UserId == u.Id).ToList();
            var limgs = new List<Image>();
            foreach (var ca in imgs)
            {
                ca.Category = ch.To;
                applicationDbContext.Images.Update(ca);
                limgs.Add(ca);
            }
            applicationDbContext.SaveChanges();

            var model = new ChangeCategoriesResponse { From = ch.From, To = ch.To };
            model.Images.AddRange(limgs);

            return Task.FromResult(model);
        }

        public override Task<FriendDuo> ChangePriority(FriendDuo friend, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Find(friend.UserId);

            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            applicationDbContext.Friends.Update(friend);
            applicationDbContext.SaveChanges();

            return Task.FromResult(friend);
        }

        public override Task<SharedUser> UnFriend(Id id, ServerCallContext context)
        {
            var signedInUser = GetUserFromRequest(context);
            var u = GetSharedUser(Id: id.Id_);

            if (!Authorize(context, signedInUser.Username))
                throw new UnauthorizedAccessException();

            var f1 = applicationDbContext.Friends.Where(x => x.UserId == id.Id_ && x.FriendId == signedInUser.Id).FirstOrDefault();
            var f2 = applicationDbContext.Friends.Where(x => x.UserId == signedInUser.Id && x.FriendId == id.Id_).FirstOrDefault();
            applicationDbContext.Friends.Remove(f1);
            applicationDbContext.Friends.Remove(f2);
            applicationDbContext.SaveChanges();

            var r1 = applicationDbContext.UserRooms.Where(x => x.UserId == signedInUser.Id && x.OtherId == id.Id_).FirstOrDefault();
            var r2 = applicationDbContext.UserRooms.Where(x => x.UserId == id.Id_ && x.OtherId == signedInUser.Id).FirstOrDefault();

            var msgs = applicationDbContext.ChatMessages.Where(x => x.RoomId == r1.RoomId);

            applicationDbContext.ChatMessages.RemoveRange(msgs);
            applicationDbContext.UserRooms.Remove(r1);
            applicationDbContext.UserRooms.Remove(r2);

            applicationDbContext.SaveChanges();

            return Task.FromResult(GetSharedUser(Id: u.Id));
        }

        public override Task<SharedUser> AcceptFriendRequestProfile(Request r, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Where(x => x.Id == r.SenderId).FirstOrDefault();

            bool isFriend = applicationDbContext.Friends.Where(x => x.UserId == r.SenderId && x.FriendId == r.SentToId).FirstOrDefault() == null ||
                         applicationDbContext.Friends.Where(x => x.UserId == r.SentToId && x.FriendId == r.SenderId).FirstOrDefault() == null;

            if (isFriend)
            {
                applicationDbContext.Friends.Add(new FriendDuo { UserId = r.SenderId, FriendId = r.SentToId });
                applicationDbContext.Friends.Add(new FriendDuo { UserId = r.SentToId, FriendId = r.SenderId });
            }

            applicationDbContext.Requests.Remove(r);
            applicationDbContext.SaveChanges();

            return Task.FromResult(GetSharedUser(Id: u.Id));
        }

        public override Task<Link> DeleteLink(Link link, ServerCallContext context)
        {
            var li = applicationDbContext.Links.Find(link.LinkId);
            var u = applicationDbContext.Users.Find(link.UserId);

            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            var l = applicationDbContext.Links.Find(link.LinkId);
            applicationDbContext.Links.Remove(l);
            applicationDbContext.SaveChanges();

            return Task.FromResult(link);
        }

        public override Task<Link> UpdateLink(Link link, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Find(link.UserId);

            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            applicationDbContext.Links.Update(link);
            applicationDbContext.SaveChanges();

            return Task.FromResult(link);
        }

        public override Task<Link> CreateLink(Link link, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Find(link.UserId);

            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            applicationDbContext.Links.Add(link);
            applicationDbContext.SaveChanges();

            return Task.FromResult(link);
        }

        public override Task<CreatePostByCategoryResponse> CreatePostByCategory(CreatePostByCategoryRequest re, ServerCallContext context)
        {
            re.Message.Date = Timestamp.FromDateTime(DateTime.UtcNow);
            applicationDbContext.Messages.Add(re.Message);

            var room = new Room { OwnerId = re.Message.SenderId, Date = Timestamp.FromDateTime(DateTime.UtcNow) };
            applicationDbContext.Rooms.Add(room);
            applicationDbContext.SaveChanges();

            applicationDbContext.RoomToMessages.Add(new RoomToMessage { MessageId = re.Message.MessageId, RoomId = room.RoomId });
            applicationDbContext.SaveChanges();

            applicationDbContext.CategoryRooms.Add(new CategoryRooms { Category = re.Category, RoomId = room.RoomId });
            applicationDbContext.SaveChanges();

            return Task.FromResult(new CreatePostByCategoryResponse { Category = re.Category });
        }

        public override Task<Image> DeleteImage(Image image, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Find(image.UserId);

            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            var limg = applicationDbContext.Images.Find(image.ImageId);
            var imgs = applicationDbContext.Images.Where(x => x.Category == limg.Category);
            if (imgs.Count() == 1)
            {
                var rmCat = applicationDbContext.CategoryRooms.Where(x => x.Category == limg.Category);
                if (rmCat.Count() > 0)
                {
                    foreach (var rc in rmCat)
                    {
                        var r = applicationDbContext.Rooms.Where(x => x.OwnerId == limg.UserId && x.RoomId == rc.RoomId).FirstOrDefault();
                        if (r != null)
                            applicationDbContext.CategoryRooms.Remove(rc);
                    }
                }
            }

            string path = Path.Combine(rootPath, "u", limg.UserId, limg.FileName);

            if (File.Exists(path))
                File.Delete(path);

            applicationDbContext.Images.Remove(limg);
            applicationDbContext.SaveChanges();

            return Task.FromResult(image);
        }

        /*****Group*****/
        public override Task<GroupPageModel> GetGroupPageModel(GroupId id, ServerCallContext context)
        {
            var signedInUsername = context.GetHttpContext().Request.Headers["Username"].FirstOrDefault();

            var group = applicationDbContext.Groups.Find(id.GroupId_);

            var signedInUser = GetSharedUser(Username: signedInUsername);
            var signedInUserRole = new Role();

            if (signedInUser == null)
            {
                signedInUser = new SharedUser { Id = string.Empty };
                signedInUserRole = new Role { RoleType = RoleType.NoRole };
            }
            else
                signedInUserRole = applicationDbContext.Roles.Where(x => x.UserId == signedInUser.Id && x.GroupId == group.GroupId).FirstOrDefault();

            var roles = applicationDbContext.Roles.Where(x => x.GroupId == group.GroupId);
            var sections = applicationDbContext.SectionRoles.Where(x => x.GroupId == group.GroupId).FirstOrDefault();

            var links = new List<GroupLink>();
            var lLinks = applicationDbContext.GroupLinks.Where(x => x.GroupId == group.GroupId).ToList();
            links.AddRange(lLinks);

            var owners = roles.Where(a => a.RoleType == RoleType.Owner).ToList();
            var adminstrators = roles.Where(a => a.RoleType == RoleType.Administrator).ToList();
            var moderators = roles.Where(m => m.RoleType == RoleType.Moderator).ToList();
            var members = roles.Where(m => m.RoleType == RoleType.Member).ToList();

            bool valid = true;
            if (applicationDbContext.Bans.Where(x => x.UserId == signedInUser.Id && x.GroupId == group.GroupId).FirstOrDefault() != null)
                valid = false;

            if (signedInUserRole == null)
                signedInUserRole = new Role { UserId = signedInUser.Id, RoleType = RoleType.NoRole, GroupId = group.GroupId };

            bool security;
            if (group.Security == Security.PrivateSecurity)
                security = true;
            else
                security = false;

            var hashtags = applicationDbContext.Hashtags.Where(x => x.GroupId == group.GroupId).ToList();

            var rooms = applicationDbContext.Rooms.Where(x => applicationDbContext.RoomGroups.Any(r => r.RoomId == x.RoomId && r.GroupId == group.GroupId)).ToList();

            var pp = applicationDbContext.GroupProfilePictures.Where(x => x.GroupId == id.GroupId_).FirstOrDefault();
            var pps = applicationDbContext.GroupImages.Where(x => x.Category == "Profile Pictures" && x.GroupId == id.GroupId_).ToList();

            var suMembersMan = applicationDbContext.Users.Where(x => roles.Any(y => y.UserId == x.Id)).ToList();
            var suMembers = new List<SharedUser>();
            suMembersMan.ForEach(x => suMembers.Add(GetSharedUser(Id: x.Id)));

            var model = new GroupPageModel();
            model.Rooms.AddRange(rooms);
            model.Hashtags.AddRange(hashtags);
            model.Owners.AddRange(owners);
            model.Administrators.AddRange(adminstrators);
            model.Moderators.AddRange(moderators);
            model.Members.AddRange(members);
            model.GroupModel = group;
            model.Links.AddRange(links);
            model.Pp = pp;
            model.Pps.AddRange(pps);
            model.SuMembers.AddRange(suMembers);
            model.SignedInUser = signedInUser;
            model.SignedInUserRole = signedInUserRole;
            model.Roles.AddRange(roles);
            model.SectionRoles = sections;
            model.Security = security;
            model.Valid = valid;

            return Task.FromResult(model);
        }

        public override Task<Role> SetPriority(Role role, ServerCallContext context)
        {
            var u = GetUserFromRequest(context);
            var g = applicationDbContext.Groups.Find(role.GroupId);
            if (!Authorize(context, u.Username) && g.OwnerId == u.Id)
                throw new UnauthorizedAccessException();

            role.Priority = !role.Priority;
            applicationDbContext.Roles.Update(role);
            applicationDbContext.SaveChanges();

            return Task.FromResult(role);
        }

        public override Task<GroupSecurity> SetSecurity(GroupSecurity gs, ServerCallContext context)
        {
            var u = GetUserFromRequest(context);
            var g = applicationDbContext.Groups.Find(gs.Group.GroupId);
            if (!Authorize(context, u.Username) && g.OwnerId == u.Id)
                throw new UnauthorizedAccessException();

            gs.BSecurity = !gs.BSecurity;

            if (gs.BSecurity)
                gs.Security = Security.PrivateSecurity;
            else
                gs.Security = Security.PublicSecurity;

            applicationDbContext.Groups.Update(gs.Group);
            applicationDbContext.SaveChanges();

            return Task.FromResult(gs);
        }

        public override Task<GroupModel> SetGroupJoin(GroupModel g, ServerCallContext context)
        {
            var u = GetUserFromRequest(context);
            if (!Authorize(context, u.Username) && g.OwnerId == u.Id)
                throw new UnauthorizedAccessException();

            applicationDbContext.Groups.Update(g);
            applicationDbContext.SaveChanges();

            return Task.FromResult(g);
        }

        public override Task<ServerMessage> GroupJoin(GroupModel g, ServerCallContext context)
        {
            var u = GetUserFromRequest(context);
            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            var role = new Role { UserId = u.Id, GroupId = g.GroupId };

            if (role == null)
            {
                if (g.GroupJoin == Join.Anonymous)
                {
                    role.RoleType = RoleType.Member;
                    applicationDbContext.Roles.Add(role);
                    applicationDbContext.SaveChanges();

                    return Task.FromResult(new ServerMessage { Message = "Joined group" });
                }
                else if (g.GroupJoin == Join.Approval)
                {
                    if (applicationDbContext.GroupApprovals.Where(x => x.UserId == role.UserId && x.GroupId == g.GroupId).FirstOrDefault() == null)
                    {
                        applicationDbContext.GroupApprovals.Add(new GroupApproval { GroupId = g.GroupId, UserId = role.UserId });
                        applicationDbContext.SaveChanges();

                        return Task.FromResult(new ServerMessage { Message = "Sent request" });
                    }
                }
            }
            throw new System.Exception();
        }

        public override Task<GroupModel> Update(GroupModel g, ServerCallContext context)
        {
            var u = GetUserFromRequest(context);
            if (!Authorize(context, u.Username) && g.OwnerId == u.Id)
                throw new UnauthorizedAccessException();

            applicationDbContext.Groups.Update(g);
            applicationDbContext.SaveChanges();

            return Task.FromResult(g);
        }

        public override Task<GroupLink> DeleteGroupLink(GroupLink l, ServerCallContext context)
        {
            var lLink = applicationDbContext.GroupLinks.Find(l.LinkId);
            applicationDbContext.GroupLinks.Remove(lLink);
            applicationDbContext.SaveChanges();

            return Task.FromResult(l);
        }

        public override Task<GroupLink> UpdateGroupLink(GroupLink l, ServerCallContext context)
        {
            applicationDbContext.GroupLinks.Update(l);
            applicationDbContext.SaveChanges();

            return Task.FromResult(l);
        }

        public override Task<GroupLink> CreateGroupLink(GroupModel g, ServerCallContext context)
        {
            var link = new GroupLink { GroupId = g.GroupId };
            applicationDbContext.GroupLinks.Add(link);
            applicationDbContext.SaveChanges();

            return Task.FromResult(link);
        }

        public override Task<Hashtag> CreateHashtag(Hashtag ht, ServerCallContext context)
        {
            applicationDbContext.Hashtags.Add(ht);
            applicationDbContext.SaveChanges();

            return Task.FromResult(ht);
        }

        public override Task<Hashtag> DeleteHashtag(Hashtag ht, ServerCallContext context)
        {
            var h = applicationDbContext.Hashtags.Find(ht.HashtagId);
            applicationDbContext.Hashtags.Remove(h);

            return Task.FromResult(ht);
        }

        public override Task<Room> PostLumine(PostModel pm, ServerCallContext context)
        {
            pm.Room.Date = Timestamp.FromDateTime(DateTime.UtcNow);
            applicationDbContext.Rooms.Add(pm.Room);
            applicationDbContext.SaveChanges();

            foreach (var h in pm.MHashtags)
            {
                h.RoomId = pm.Room.RoomId;
                applicationDbContext.MessageHashtags.Add(h);
            }

            pm.Message.Date = Timestamp.FromDateTime(DateTime.UtcNow);
            pm.Message.RoomId = pm.Room.RoomId;
            applicationDbContext.Messages.Add(pm.Message);
            applicationDbContext.SaveChanges();

            var rtm = new RoomToMessage { MessageId = pm.Message.MessageId, RoomId = pm.Room.RoomId };
            applicationDbContext.RoomToMessages.Add(rtm);
            applicationDbContext.SaveChanges();

            var rg = new RoomGroup { RoomId = rtm.RoomId, GroupId = pm.GroupId };
            applicationDbContext.RoomGroups.Add(rg);
            applicationDbContext.SaveChanges();

            return Task.FromResult(pm.Room);
        }

        public override Task<ProfilePicture> SwitchProfilePicture(Image pp, ServerCallContext context)
        {
            var lpp = applicationDbContext.ProfilePictures.Where(x => x.UserId == pp.UserId).FirstOrDefault();

            if (lpp == null)
            {
                lpp = new ProfilePicture { ImageId = pp.ImageId, UserId = pp.UserId };
                applicationDbContext.ProfilePictures.Add(lpp);
            }
            else
            {
                lpp.ImageId = pp.ImageId;
                applicationDbContext.ProfilePictures.Update(lpp);
            }
            applicationDbContext.SaveChanges();

            return Task.FromResult(lpp);
        }

        public override Task<GroupProfilePicture> SwitchGroupProfilePicture(GroupImage pp, ServerCallContext context)
        {
            var lpp = applicationDbContext.GroupProfilePictures.Where(x => x.GroupId == pp.GroupId).FirstOrDefault();

            if (lpp == null)
            {
                lpp = new GroupProfilePicture { ImageId = pp.ImageId, GroupId = pp.GroupId };
                applicationDbContext.GroupProfilePictures.Add(lpp);
            }
            else
            {
                lpp.ImageId = pp.ImageId;
                applicationDbContext.GroupProfilePictures.Update(lpp);
            }
            applicationDbContext.SaveChanges();

            return Task.FromResult(lpp);
        }

        public override Task<GroupImage> DeleteGroupProfilePicture(GroupImage pp, ServerCallContext context)
        {
            var lp = applicationDbContext.GroupImages.Find(pp.ImageId);

            var com = applicationDbContext.GroupUserComments.Where(x => x.ImageId == lp.ImageId).ToList();
            applicationDbContext.GroupUserComments.RemoveRange(com);

            var lpp = applicationDbContext.GroupProfilePictures.Where(x => x.ImageId == lp.ImageId).FirstOrDefault();
            if (lpp != null && lpp.ImageId == lp.ImageId)
                applicationDbContext.GroupProfilePictures.Remove(lpp);

            applicationDbContext.GroupImages.Remove(lp);

            string path = Path.Combine(rootPath, "g", lp.GroupId, lp.FileName);

            if (File.Exists(path))
                File.Delete(path);

            applicationDbContext.SaveChanges();

            return Task.FromResult(pp);
        }

        public override Task<PCM> GetPCM(GetPCMRequest pcm, ServerCallContext context)
        {
            var u = GetUserFromRequest(context);
            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            var suSignedInUser = GetSharedUser(Username: u.Username);
            var signedInUserRole = applicationDbContext.Roles.Where(x => x.UserId == suSignedInUser.Id && x.GroupId == pcm.GroupId).FirstOrDefault();

            var group = applicationDbContext.Groups.Find(pcm.GroupId);
            var sections = applicationDbContext.SectionRoles.Where(x => x.GroupId == pcm.GroupId).FirstOrDefault();
            var banned = applicationDbContext.Bans.Where(x => x.UserId == suSignedInUser.Id && x.GroupId == pcm.GroupId).FirstOrDefault() != null;
            var isRole = signedInUserRole.RoleType < sections.Pictures;

            var images = applicationDbContext.GroupImages.Where(x => x.Category == pcm.Cagetory && x.UserId == pcm.GroupId).AsQueryable();

            var lpcm = new PCM { SignedInUser = suSignedInUser, SignedInUserRole = signedInUserRole, Group = group, SectionRoles = sections, IsRole = isRole, Banned = banned };
            lpcm.Images.AddRange(images);

            return Task.FromResult(lpcm);
        }

        public override Task<GroupImage> DeletePictureCategory(GroupImage img, ServerCallContext context)
        {
            var limg = applicationDbContext.GroupImages.Find(img.ImageId);
            var imgs = applicationDbContext.GroupImages.Where(x => x.Category == limg.Category && x.GroupId == limg.GroupId).ToList();

            if (imgs.Count() == 1)
            {
                var rmCat = applicationDbContext.CategoryRooms.Where(x => x.Category == limg.Category).ToList();
                if (rmCat.Count() > 0)
                {
                    foreach (var r in rmCat)
                    {
                        var rmGrp = applicationDbContext.RoomGroups.Where(x => x.GroupId == limg.GroupId && x.RoomId == r.RoomId).FirstOrDefault();
                        if (rmGrp != null)
                            applicationDbContext.CategoryRooms.Remove(r);
                    }
                }
                applicationDbContext.SaveChanges();
            }

            string path = Path.Combine(rootPath, "g", limg.GroupId, limg.FileName);

            if (File.Exists(path))
                File.Delete(path);

            var c = applicationDbContext.GroupUserComments.Where(x => x.ImageId == limg.ImageId).ToList();
            applicationDbContext.GroupUserComments.RemoveRange(c);

            applicationDbContext.GroupImages.Remove(limg);
            applicationDbContext.SaveChanges();

            return Task.FromResult(img);
        }

        public override Task<GroupImage> EditGroupImage(GroupImage img, ServerCallContext context)
        {
            applicationDbContext.Update(img);
            applicationDbContext.SaveChanges();

            return Task.FromResult(img);
        }

        public override Task<GroupModal> GetGroupModalModel(GroupImage img, ServerCallContext context)
        {
            var comments = applicationDbContext.GroupUserComments.Where(x => x.ImageId == img.ImageId).ToList();
            var users = applicationDbContext.Users.Where(x => comments.Any(y => y.UserId == x.Id)).ToList();
            var suUsers = new List<SharedUser>();
            users.ForEach(x => suUsers.Add(GetSharedUser(Id: x.Id)));

            var modal = new GroupModal();
            modal.Comments.AddRange(comments);
            modal.Users.AddRange(suUsers);

            return Task.FromResult(modal);
        }

        public override Task<GroupUserComment> DeleteGroupComment(GroupUserComment comment, ServerCallContext context)
        {
            var c = applicationDbContext.GroupUserComments.Find(comment.CommentId);
            applicationDbContext.GroupUserComments.Remove(c);
            applicationDbContext.SaveChanges();

            return Task.FromResult(comment);
        }

        public override Task<AddGroupCommentReturn> AddGroupComment(GroupUserComment comment, ServerCallContext context)
        {
            comment.CreateDate = Timestamp.FromDateTime(DateTime.UtcNow);
            applicationDbContext.GroupUserComments.Add(comment);
            applicationDbContext.SaveChanges();

            var user = GetSharedUser(Id: comment.UserId);

            var adr = new AddGroupCommentReturn { Comment = comment, User = user };

            return Task.FromResult(adr);
        }

        public override Task<ProfilePictureModel> GetGroupProfilePicture(GroupModel group, ServerCallContext context)
        {
            var pp = applicationDbContext.GroupProfilePictures.Where(x => x.GroupId == group.GroupId).FirstOrDefault();
            GroupImage image = null;

            if (pp != null)
                image = applicationDbContext.GroupImages.Where(x => x.ImageId == pp.ImageId).FirstOrDefault();

            var rpp = new ProfilePictureModel { Image = image, ProfilePicture = pp };

            return Task.FromResult(rpp);
        }

        public override Task<MainGroupProfilePictureModel> GetMainGroupProfilePicture(GroupModel group, ServerCallContext context)
        {
            var pp = applicationDbContext.GroupProfilePictures.Where(x => x.GroupId == group.GroupId).FirstOrDefault();
            GroupImage image = (pp != null) ? applicationDbContext.GroupImages.Find(pp.ImageId) : new GroupImage();

            var model = new MainGroupProfilePictureModel
            {
                ProfilePicture = pp,
                Image = image
            };

            return Task.FromResult(model);
        }

        public override Task<MembershipModel> GetMembershipModel(GroupId id, ServerCallContext context)
        {
            var signedInUser = GetUserFromRequest(context);

            var group = applicationDbContext.Groups.Find(id.GroupId_);

            //var signedInUser = applicationDbContext.Users.Where(x => x.Username == user).FirstOrDefault();
            var su = GetSharedUser(Id: signedInUser.Id);

            var owner = applicationDbContext.Users.Find(group.OwnerId);
            var suOwner = GetSharedUser(Id: owner.Id);

            var signedInUserRole = applicationDbContext.Roles.Where(x => x.UserId == signedInUser.Id && x.GroupId == group.GroupId).FirstOrDefault();
            var sections = applicationDbContext.SectionRoles.Where(x => x.GroupId == group.GroupId).FirstOrDefault();

            var roles = applicationDbContext.Roles.Where(x => x.GroupId == group.GroupId && x.UserId != signedInUser.Id && x.UserId != owner.Id).ToList();
            var suMembers = new List<SharedUser>();
            roles.ForEach(x => suMembers.Add(GetSharedUser(Id: x.UserId)));

            var bans = applicationDbContext.Bans.Where(x => x.GroupId == group.GroupId).ToList();

            var friends = applicationDbContext.Friends.Where(x => x.UserId == signedInUser.Id).ToList();

            var model = new MembershipModel { SignedInUser = signedInUser, Group = group, Owner = suOwner, SignedInUserRole = signedInUserRole, SectionRoles = sections };
            model.Bans.AddRange(bans);
            model.Friends.AddRange(friends);
            model.Members.AddRange(suMembers);
            model.Roles.AddRange(roles);

            return Task.FromResult(model);
        }

        public override Task<GroupModel> Leave(Role role, ServerCallContext context)
        {
            var r = applicationDbContext.Roles.Find(role.RoleId);
            var u = applicationDbContext.Users.Find(r.UserId);
            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            applicationDbContext.Roles.Remove(role);
            applicationDbContext.SaveChanges();

            var g = applicationDbContext.Groups.Find(role.GroupId);

            return Task.FromResult(g);
        }

        public override Task<SearchInvitesResponse> SearchInvites(SearchInvitesRequest search, ServerCallContext context)
        {
            var UserId = GetUserFromRequest(context).Id;

            var roles = applicationDbContext.Roles.Where(x => x.GroupId == search.GroupId).ToList();
            var bans = applicationDbContext.Bans.Where(x => x.GroupId == search.GroupId).ToList();
            var us = applicationDbContext.Users.AsEnumerable().Where(x => x.Name.ToLower().Contains(search.Value.ToLower())).ToList();
            var users = us.Where(x => x.Id != UserId &&
            !roles.AsEnumerable().Any(y => y.GroupId == search.GroupId && y.UserId == x.Id) &&
            !bans.AsEnumerable().Any(y => y.UserId == x.Id && y.GroupId == search.GroupId)).ToList();

            var su = new List<SharedUser>();
            users.ForEach(x => su.Add(GetSharedUser(Id: x.Id) ));

            var r = new SearchInvitesResponse();
            r.Users.AddRange(su);

            return Task.FromResult(r);
        }

        public override Task<Role> Promote(Role role, ServerCallContext context)
        {
            var u = GetUserFromRequest(context);
            if (!Authorize(context, u.Username))
                throw new System.Exception();

            var signedInUserRole = applicationDbContext.Roles.Where(x => x.UserId == u.Id && x.GroupId == role.GroupId).FirstOrDefault();
            var sectionRoles = applicationDbContext.SectionRoles.Where(x => x.GroupId == role.GroupId).FirstOrDefault();

            if (signedInUserRole.RoleType >= role.RoleType)
            {
                applicationDbContext.Roles.Update(role);
                applicationDbContext.SaveChanges();
            }
            else
                throw new System.Exception();

            return Task.FromResult(role);
        }

        public override Task<Role> PromoteLite(Role role, ServerCallContext context)
        {
            var u = GetUserFromRequest(context);
            if (!Authorize(context, u.Username))
                throw new System.Exception();

            var signedInUserRole = applicationDbContext.Roles.Where(x => x.UserId == u.Id && x.GroupId == role.GroupId).FirstOrDefault();
            var sectionRoles = applicationDbContext.SectionRoles.Where(x => x.GroupId == role.GroupId).FirstOrDefault();

            if (signedInUserRole.RoleType > role.RoleType)
            {
                applicationDbContext.Roles.Update(role);
                applicationDbContext.SaveChanges();
            }
            else
                throw new System.Exception();

            return Task.FromResult(role);
        }

        public override Task<Role> RemoveMember(Role role, ServerCallContext context)
        {
            var u = GetUserFromRequest(context);
            if (!Authorize(context, u.Username))
                throw new System.Exception();

            var r = applicationDbContext.Roles.Find(role.RoleId);

            var signedInUserRole = applicationDbContext.Roles.Where(x => x.UserId == u.Id && x.GroupId == r.GroupId).FirstOrDefault();
            var sectionRoles = applicationDbContext.SectionRoles.Where(x => x.GroupId == r.GroupId).FirstOrDefault();

            if (signedInUserRole.RoleType >= sectionRoles.RemoveMember && signedInUserRole.RoleType > r.RoleType)
            {
                applicationDbContext.Roles.Remove(r);
                applicationDbContext.SaveChanges();
            }
            else
                throw new System.Exception();

            return Task.FromResult(role);
        }

        public override Task<Role> BanUser(Role role, ServerCallContext context)
        {
            var r = applicationDbContext.Roles.Find(role.RoleId);
            var u = applicationDbContext.Users.Find(r.UserId);
            if (!Authorize(context, u.Username))
                throw new System.Exception();

            var b = new Ban { GroupId = role.GroupId, UserId = role.UserId };

            var signedInUserRole = applicationDbContext.Roles.Where(x => x.UserId == u.Id && x.GroupId == r.GroupId).FirstOrDefault();
            var sectionRoles = applicationDbContext.SectionRoles.Where(x => x.GroupId == r.GroupId).FirstOrDefault();

            if (signedInUserRole.RoleType >= sectionRoles.Ban && signedInUserRole.RoleType > r.RoleType)
            {
                applicationDbContext.Bans.Add(b);
                applicationDbContext.Roles.Remove(r);
                applicationDbContext.SaveChanges();
            }
            else
                throw new System.Exception();

            return Task.FromResult(role);
        }

        public override Task<SearchBanResponse> SearchBan(SearchBanRequest search, ServerCallContext context)
        {
            var us = GetUserFromRequest(context);

            var bans = applicationDbContext.Bans.Where(x => x.GroupId == search.GroupId).ToList();
            var users = applicationDbContext.Users.Where(x => x.Name.ToLower().Contains(search.Value.ToLower()) && x.Id != us.Id && !bans.Any(y => y.UserId == x.Id)).ToList();
            var suBans = new List<SharedUser>();
            users.ForEach(x => suBans.Add(GetSharedUser(Id: x.Id)));

            var model = new SearchBanResponse();
            model.Users.AddRange(suBans);

            return Task.FromResult(model);
        }

        public override Task<Ban> RemoveBan(Ban ban, ServerCallContext context)
        {
            var u = GetUserFromRequest(context);

            if (!Authorize(context, u.Username))
                throw new System.Exception();

            var b = applicationDbContext.Bans.Find(ban.BanId);

            var signedInUserRole = applicationDbContext.Roles.Where(x => x.UserId == u.Id && x.GroupId == b.GroupId).FirstOrDefault();
            var sectionRoles = applicationDbContext.SectionRoles.Where(x => x.GroupId == b.GroupId).FirstOrDefault();

            if (signedInUserRole.RoleType >= sectionRoles.RemoveBan)
            {
                applicationDbContext.Bans.Remove(b);
                applicationDbContext.SaveChanges();
            }
            else
                throw new System.Exception();

            return Task.FromResult(ban);
        }

        public override Task<SectionRoles> UpdateSectionRoles(SectionRoles sections, ServerCallContext context)
        {
            var g = applicationDbContext.Groups.Find(sections.GroupId);
            var u = applicationDbContext.Users.Find(g.OwnerId);

            if (!Authorize(context, u.Username))
                throw new System.Exception();

            applicationDbContext.SectionRoles.Update(sections);
            applicationDbContext.SaveChanges();

            return Task.FromResult(sections);
        }

        public override Task<GroupPicturesPageModel> GetGroupPicturesPageModel(GroupId id, ServerCallContext context)
        {
            var u = GetUserFromRequest(context);
            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            var signedInUser = applicationDbContext.Users.Where(x => x.Username == u.Username).FirstOrDefault();
            var suUser = GetSharedUser(Id: signedInUser.Id);

            var signedInUserRole = applicationDbContext.Roles.Where(x => x.UserId == signedInUser.Id && x.GroupId == id.GroupId_).FirstOrDefault();

            var group = applicationDbContext.Groups.Find(id.GroupId_);
            var roles = applicationDbContext.Roles.Where(x => x.GroupId == id.GroupId_).ToList();
            var sections = applicationDbContext.SectionRoles.Where(x => x.GroupId == id.GroupId_).FirstOrDefault();

            bool banned = false;

            if (applicationDbContext.Bans.Where(x => x.UserId == signedInUser.Id && x.GroupId == id.GroupId_).FirstOrDefault() != null)
                banned = true;

            if (signedInUserRole == null)
                signedInUserRole = new Role { UserId = signedInUser.Id, RoleType = RoleType.NoRole, GroupId = id.GroupId_ };

            var images = applicationDbContext.GroupImages.Where(x => x.GroupId == id.GroupId_).ToList();

            var model = new GroupPicturesPageModel { Banned = banned, Group = group, SectionRoles = sections, SignedInUser = suUser, SignedInUserRole = signedInUserRole };
            model.Images.AddRange(images);
            model.Roles.AddRange(roles);

            return Task.FromResult(model);
        }

        public override Task<ChangeGroupCategoriesReponse> ChangeGroupCategories(ChangeGroupCategoriesRequest ch, ServerCallContext context)
        {
            var imgs = applicationDbContext.GroupImages.Where(x => x.Category == ch.From && x.GroupId == ch.GroupId).ToList();
            var limgs = new List<GroupImage>();
            foreach (var ca in imgs)
            {
                ca.Category = ch.To;
                applicationDbContext.GroupImages.Update(ca);
                limgs.Add(ca);
            }
            applicationDbContext.SaveChanges();

            var model = new ChangeGroupCategoriesReponse { From = ch.From, To = ch.To };
            model.Images.AddRange(limgs);

            return Task.FromResult(model);
        }

        public override Task<CreateGroupPostByCategoryResponse> CreateGroupPostByCategory(CreateGroupPostByCategoryRequest r, ServerCallContext context)
        {
            r.Message.Date = Timestamp.FromDateTime(DateTime.UtcNow);
            applicationDbContext.Messages.Add(r.Message);

            var room = new Room { OwnerId = r.Message.SenderId, Date = Timestamp.FromDateTime(DateTime.UtcNow) };
            applicationDbContext.Rooms.Add(room);
            applicationDbContext.SaveChanges();

            applicationDbContext.RoomGroups.Add(new RoomGroup { GroupId = r.GroupId, RoomId = room.RoomId });
            applicationDbContext.SaveChanges();

            applicationDbContext.RoomToMessages.Add(new RoomToMessage { MessageId = r.Message.MessageId, RoomId = room.RoomId });
            applicationDbContext.SaveChanges();

            applicationDbContext.CategoryRooms.Add(new CategoryRooms { Category = r.Category, RoomId = room.RoomId });
            applicationDbContext.SaveChanges();

            return Task.FromResult(new CreateGroupPostByCategoryResponse { Category = r.Category });
        }

        public override Task<GroupImage> DeleteGroupImage(GroupImage img, ServerCallContext context)
        {
            var limg = applicationDbContext.GroupImages.Find(img.ImageId);
            var imgs = applicationDbContext.GroupImages.Where(x => x.Category == limg.Category && x.GroupId == limg.GroupId).ToList();

            if (imgs.Count() == 1)
            {
                var rmCat = applicationDbContext.CategoryRooms.Where(x => x.Category == limg.Category).ToList();
                if (rmCat.Count() > 0)
                {
                    foreach (var r in rmCat)
                    {
                        var rmGrp = applicationDbContext.RoomGroups.Where(x => x.GroupId == limg.GroupId && x.RoomId == r.RoomId).FirstOrDefault();
                        if (rmGrp != null)
                            applicationDbContext.CategoryRooms.Remove(r);
                    }
                }
                applicationDbContext.SaveChanges();
            }

            string path = Path.Combine(rootPath, "g", limg.GroupId, limg.FileName);

            if (File.Exists(path))
                File.Delete(path);

            var c = applicationDbContext.GroupUserComments.Where(x => x.ImageId == limg.ImageId).ToList();
            applicationDbContext.GroupUserComments.RemoveRange(c);

            applicationDbContext.GroupImages.Remove(limg);
            applicationDbContext.SaveChanges();

            return Task.FromResult(img);
        }

        /*****Home*****/
        public override Task<IndexPageModel> GetIndexModel(Empty Empty, ServerCallContext context)
        {
            var ur = GetUserFromRequest(context);

            if (!Authorize(context, ur.Username))
                throw new UnauthorizedAccessException();

            var user = applicationDbContext.Users.Where(x => x.Username == ur.Username).FirstOrDefault();
            var u = GetSharedUser(Id: user.Id);

            var friends = applicationDbContext.Friends.Where(x => x.UserId == user.Id).ToList();
            var suFriends = new List<SharedUser>();
            friends.ForEach(x => suFriends.Add(GetSharedUser(Id: x.FriendId)));

            var tagsFeeds = applicationDbContext.TagsFeeds.Where(x => x.UserId == user.Id).ToList();

            var rolesFilter = applicationDbContext.Roles.Where(x => x.UserId == user.Id && x.Filter).ToList();
            var rolesBlacklist = applicationDbContext.Roles.Where(x => x.UserId == user.Id && x.Blacklist).ToList();

            var roles = applicationDbContext.Roles.Where(x => x.UserId == user.Id).ToList();
            var groups = applicationDbContext.Groups.AsEnumerable().Where(x => roles.Any(y => y.GroupId == x.GroupId)).ToList();

            IndexPageModel model = new IndexPageModel();
            model.Groups.AddRange(groups);
            model.Roles.AddRange(roles);
            model.TagsFeeds.AddRange(tagsFeeds);
            model.RolesBlacklist.AddRange(rolesBlacklist);
            model.RolesFilter.AddRange(rolesFilter);
            model.Friends.AddRange(friends);
            model.SuFriends.AddRange(suFriends);
            model.User = u;

            return Task.FromResult(model);
        }

        public override Task<TagsFeed> AddTag(TagsFeed tag, ServerCallContext context)
        {
            applicationDbContext.TagsFeeds.Add(tag);
            applicationDbContext.SaveChanges();

            return Task.FromResult(tag);
        }

        public override Task<TagsFeed> BlacklistTag(TagsFeed tag, ServerCallContext context)
        {
            tag.Blacklist = true;
            applicationDbContext.TagsFeeds.Update(tag);
            applicationDbContext.SaveChanges();

            return Task.FromResult(tag);
        }

        public override Task<TagsFeed> UpdateCheckTag(TagsFeed tag, ServerCallContext context)
        {
            tag.Checked = !tag.Checked;
            applicationDbContext.TagsFeeds.Update(tag);
            applicationDbContext.SaveChanges();

            return Task.FromResult(tag);
        }

        public override Task<TagsFeed> DeleteTagFeed(TagsFeed tag, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Find(tag.UserId);

            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            var lTag = applicationDbContext.TagsFeeds.Find(tag.TagsFeedId);
            applicationDbContext.TagsFeeds.Remove(lTag);
            applicationDbContext.SaveChanges();

            return Task.FromResult(tag);
        }

        public override Task<TagsFeed> RemoveBlacklistTag(TagsFeed tag, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Find(tag.UserId);

            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            tag.Blacklist = false;
            applicationDbContext.TagsFeeds.Update(tag);
            applicationDbContext.SaveChanges();

            return Task.FromResult(tag);
        }

        public override Task<FriendDuo> RemoveFriendFilter(FriendDuo friend, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Find(friend.UserId);

            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            friend.Filter = false;
            applicationDbContext.Friends.Update(friend);
            applicationDbContext.SaveChanges();

            return Task.FromResult(friend);
        }

        public override Task<FriendDuo> RemoveFriendBlacklist(FriendDuo friend, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Find(friend.UserId);

            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            friend.Blacklist = false;
            applicationDbContext.Friends.Update(friend);
            applicationDbContext.SaveChanges();

            return Task.FromResult(friend);
        }

        public override Task<FriendDuo> AddFriendFilter(FriendDuo friend, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Find(friend.UserId);

            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            friend.Filter = true;
            applicationDbContext.Friends.Update(friend);
            applicationDbContext.SaveChanges();

            return Task.FromResult(friend);
        }

        public override Task<FriendDuo> AddBlacklistFriend(FriendDuo friend, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Find(friend.UserId);

            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            friend.Blacklist = true;
            applicationDbContext.Friends.Update(friend);
            applicationDbContext.SaveChanges();

            return Task.FromResult(friend);
        }

        public override Task<Role> GroupRemoveFilter(Role role, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Find(role.UserId);

            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            role.Filter = false;
            applicationDbContext.Roles.Update(role);
            applicationDbContext.SaveChanges();

            return Task.FromResult(role);
        }

        public override Task<Role> GroupRemoveBlacklist(Role role, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Find(role.UserId);

            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            role.Blacklist = false;
            applicationDbContext.Roles.Update(role);
            applicationDbContext.SaveChanges();

            return Task.FromResult(role);
        }

        public override Task<Role> GroupAddFilter(Role role, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Find(role.UserId);

            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            role.Filter = true;
            applicationDbContext.Roles.Update(role);
            applicationDbContext.SaveChanges();

            return Task.FromResult(role);
        }

        public override Task<Rooms> GetRooms(IndexRooms indexRooms, ServerCallContext context)
        {
            List<Room> all = new();
            List<Room> high = new();
            List<Room> low = new();

            var user = applicationDbContext.Users.Find(indexRooms.UserId);
            var shares = applicationDbContext.Shares.Where(x => x.UserId == user.Id).ToList();
            var friends = applicationDbContext.Friends.Where(x => x.UserId == user.Id).ToList();
            var tagsFeeds = applicationDbContext.TagsFeeds.Where(x => x.UserId == user.Id).ToList();

            if (indexRooms.You)
            {
                var s = applicationDbContext.Groups.ToList();
                high = applicationDbContext.Rooms.ToList().Where(x => x.OwnerId == user.Id && DateTime.UtcNow < x.Date.ToDateTime().AddHours(user.HoursFeed) && !applicationDbContext.RoomGroups.ToList().Any(y => y.RoomId == x.RoomId)).ToList();
            }

            if (indexRooms.BShares)
            {
                foreach (var s in shares)
                {
                    var f = applicationDbContext.Friends.Where(x => x.UserId == user.Id && x.FriendId == s.SenderId).FirstOrDefault();
                    var r = applicationDbContext.Rooms.Find(s.RoomId);

                    if (DateTime.UtcNow < r.Date.ToDateTime().AddHours(user.HoursFeed))
                    {
                        if (f.Priority)
                            high.Add(r);
                        else
                            low.Add(r);
                    }
                }
            }

            foreach (var r in applicationDbContext.Roles.Where(x => x.UserId == user.Id).ToList())
            {
                var gr = applicationDbContext.RoomGroups.Where(x => x.GroupId == r.GroupId).ToList();
                foreach (var g in gr)
                {
                    var role = applicationDbContext.Roles.Where(x => x.GroupId == g.GroupId && x.UserId == user.Id).FirstOrDefault();
                    var room = applicationDbContext.Rooms.Find(g.RoomId);

                    if (room != null && !role.Blacklist)
                    {
                        if (DateTime.UtcNow < room.Date.ToDateTime().AddHours(user.HoursFeed))
                        {
                            if (indexRooms.FGroups && role.Filter || !indexRooms.FGroups)
                            {
                                if (role.Priority)
                                    high.Add(room);
                                else
                                    low.Add(room);
                            }
                        }
                    }
                }
            }

            foreach (var f in friends)
            {
                var friend = applicationDbContext.Users.Find(f.FriendId);
                if (friend.FriendsFeed && !f.Blacklist)
                {
                    foreach (var r in applicationDbContext.Rooms.Where(x => x.OwnerId == f.FriendId).ToList())
                    {
                        if (DateTime.UtcNow < r.Date.ToDateTime().AddHours(user.HoursFeed))
                        {
                            var rmGrp = applicationDbContext.RoomGroups.Where(x => x.RoomId == r.RoomId).FirstOrDefault();

                            if (rmGrp == null)
                            {
                                if (indexRooms.FFriends && f.Filter || !indexRooms.FFriends)
                                {
                                    if (f.Priority)
                                        high.Add(r);
                                    else
                                        low.Add(r);
                                }
                            }
                        }
                    }
                }
            }

            foreach (var d in high.Distinct())
            {
                if (DateTime.UtcNow < d.Date.ToDateTime().AddHours(user.HoursFeed) && all.Where(x => x.RoomId == d.RoomId).FirstOrDefault() == null)
                {
                    var msg = applicationDbContext.Messages.Where(x => applicationDbContext.RoomToMessages.Any(m => m.MessageId == x.MessageId && m.RoomId == d.RoomId)).FirstOrDefault();
                    if (indexRooms.TagOptions == TagOptions.NoFilter)
                    {
                        all.Add(d);
                    }
                    else
                    {
                        foreach (var t in tagsFeeds)
                        {
                            if (msg.MessageString.ToString().ToLower().Contains(t.Tag.ToLower()))
                            {
                                if (!t.Blacklist)
                                {
                                    if (indexRooms.TagOptions == TagOptions.FilterBoth)
                                    {
                                        break;
                                    }
                                    else if (indexRooms.TagOptions == TagOptions.FilterChecked)
                                    {
                                        if (t.Checked)
                                            break;
                                        else
                                            all.Add(d);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            foreach (var d in low.Distinct())
            {
                if (DateTime.UtcNow < d.Date.ToDateTime().AddHours(user.HoursFeed) && all.Where(x => x.RoomId == d.RoomId).FirstOrDefault() == null)
                {
                    var msg = applicationDbContext.Messages.Where(x => applicationDbContext.RoomToMessages.Any(m => m.MessageId == x.MessageId && m.RoomId == d.RoomId)).FirstOrDefault();
                    if (indexRooms.TagOptions == TagOptions.NoFilter)
                    {
                        all.Add(d);
                    }
                    else
                    {
                        foreach (var t in tagsFeeds)
                        {
                            if (msg.MessageString.ToString().ToLower().Contains(t.Tag.ToLower()))
                            {
                                if (!t.Blacklist)
                                {
                                    if (indexRooms.TagOptions == TagOptions.FilterBoth)
                                    {
                                        break;
                                    }
                                    else if (indexRooms.TagOptions == TagOptions.FilterChecked)
                                    {
                                        if (t.Checked)
                                            break;
                                        else
                                            all.Add(d);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            var rooms = new Rooms();
            rooms.Rooms_.AddRange(all.Distinct().OrderByDescending(x => x.Date));

            return Task.FromResult(rooms);
        }

        public override Task<UpdatedHours> UpdateHours(SharedUser user, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Find(user.Id);

            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            if (u.HoursFeed < 0)
                u.HoursFeed = 0;
            if (user.HoursFeed > 168)
                u.HoursFeed = 168;

            u.HoursFeed = u.HoursFeed;

            applicationDbContext.Users.Update(u);
            applicationDbContext.SaveChanges();

            return Task.FromResult(new UpdatedHours() { Hours = u.HoursFeed });
        }

        public override Task<AboutPageModel> GetAboutPageModel(Id id, ServerCallContext context)
        {
            var signedInUser = GetUserFromRequest(context);

            var about = applicationDbContext.About.Find(id.Id_);
            if (about == null)
                about = new About { AboutId = signedInUser.Id };

            FriendDuo friend = new FriendDuo();
            bool isFriend = false;

            if (id.Id_ != signedInUser.Id)
            {
                friend = applicationDbContext.Friends.Where(x => x.UserId == signedInUser.Id && x.FriendId == id.Id_).FirstOrDefault();

                if (friend != null)
                    isFriend = true;
            }

            var profileSecurity = applicationDbContext.ProfileSecurities.Where(x => x.UserId == id.Id_).FirstOrDefault();
            if (profileSecurity == null)
            {
                applicationDbContext.ProfileSecurities.Add(new ProfileSecurity
                {
                    UserId = id.Id_,
                    AboutMe = SecurityLevel.PublicLevel,
                    Friends = SecurityLevel.PublicLevel,
                    DOB = SecurityLevel.PublicLevel,
                    Education = SecurityLevel.PublicLevel,
                    Groups = SecurityLevel.PublicLevel,
                    Interests = SecurityLevel.PublicLevel,
                    Lumine = SecurityLevel.PublicLevel,
                    Pictures = SecurityLevel.PublicLevel,
                    PlacesLived = SecurityLevel.PublicLevel,
                    Relationship = SecurityLevel.PublicLevel,
                    Sex = SecurityLevel.PublicLevel,
                    WorkHistory = SecurityLevel.PublicLevel
                });
                applicationDbContext.SaveChanges();

                profileSecurity = applicationDbContext.ProfileSecurities.Find(id.Id_);
            }
            var privateProfile = applicationDbContext.PrivateProfiles.Where(x => x.UserId == id.Id_).ToList();

            var model = new AboutPageModel { About = about, IsFriend = isFriend, ProfileSecurity = profileSecurity };
            model.PrivateProfiles.AddRange(privateProfile);

            return Task.FromResult(model);
        }

        public override Task<Empty> UpdateAbout(UpdateAboutRequest about, ServerCallContext context)
        {
            if (applicationDbContext.About.Find(about.User.Id) == null)
            {
                about.About.DOB = new Timestamp();
                about.About.AboutId = about.User.Id;
                applicationDbContext.About.Add(about.About);
            }
            else
            {
                var a = applicationDbContext.About.Find(about.User.Id);
                a.MartialStatus = about.About.MartialStatus;
                a.Sex = about.About.Sex;
                applicationDbContext.About.Update(a);
            }

            var u = applicationDbContext.Users.Find(about.User.Id);
            u.Name = about.User.Name;
            applicationDbContext.Users.Update(u);

            applicationDbContext.SaveChanges();

            return Task.FromResult(new Empty());
        }

        public override Task<EducationRepsonse> GetEducation(Id id, ServerCallContext context)
        {
            var model = new EducationRepsonse();
            model.EducationList.AddRange(applicationDbContext.EducationList.Where(x => x.UserId == id.Id_));

            return Task.FromResult(model);
        }

        public override Task<Education> CreateEducation(Education education, ServerCallContext context)
        {
            applicationDbContext.EducationList.Add(education);
            applicationDbContext.SaveChanges();

            return Task.FromResult(education);
        }

        public override Task<Education> DeleteEducation(Education education, ServerCallContext context)
        {
            var e = applicationDbContext.EducationList.Find(education.EducationId);

            applicationDbContext.EducationList.Remove(e);
            applicationDbContext.SaveChanges();

            return Task.FromResult(education);
        }

        public override Task<Empty> UpdateEducation(Education education, ServerCallContext context)
        {
            applicationDbContext.EducationList.Update(education);
            applicationDbContext.SaveChanges();

            return Task.FromResult(new Empty());
        }

        public override Task<InterestPageModel> GetInterestPageModel(Id id, ServerCallContext context)
        {
            var profileSecurity = applicationDbContext.ProfileSecurities.Find(id.Id_);
            var interests = applicationDbContext.Interests.Where(x => x.UserId == id.Id_).ToList();
            var b = applicationDbContext.Friends.Where(x => x.UserId == GetUserFromRequest(context).Id && x.FriendId == id.Id_).FirstOrDefault() == null;

            var model = new InterestPageModel { ProfileSecurity = profileSecurity, IsFriend = b };
            model.Interests.AddRange(interests);

            return Task.FromResult(model);
        }

        public override Task<Interest> AddInterest(Interest interest, ServerCallContext context)
        {
            if (applicationDbContext.Interests.Where(x => x.InterestName.ToLower().Contains(interest.InterestName.ToLower()) && x.UserId == interest.UserId).FirstOrDefault() == null)
            {
                applicationDbContext.Interests.Add(interest);
                applicationDbContext.SaveChanges();

                return Task.FromResult(interest);
            }
            else
                throw new System.Exception();
        }

        public override Task<Interest> DeleteInterest(Id id, ServerCallContext context)
        {
            var interest = applicationDbContext.Interests.Find(id.Id_);
            applicationDbContext.Interests.Remove(interest);
            applicationDbContext.SaveChanges();

            return Task.FromResult(interest);
        }

        public override Task<InterestsModel> FindInterests(Id id, ServerCallContext context)
        {
            var interests = new InterestsModel();
            interests.Interests.AddRange(applicationDbContext.Interests.Where(x => x.InterestName.ToLower().Contains(id.Id_.ToLower())));

            return Task.FromResult(interests);
        }

        public override Task<MainLayoutModel> GetMainLayoutModel(Empty Empty, ServerCallContext context)
        {
            var u = GetUserFromRequest(context);

            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            var requests = applicationDbContext.Requests.Where(x => x.SentToId == u.Id).ToList();
            var grRequests = applicationDbContext.GroupRequests.AsEnumerable().Where(x => x.UserId == u.Id).ToList();
            var notifications = applicationDbContext.Notifications.Where(x => x.UserId == u.Id).ToList();

            var uRequests = applicationDbContext.Users.AsEnumerable().Where(x => requests.Any(y => y.SenderId == x.Id)).ToList();
            var suRequests = new List<SharedUser>();
            uRequests.ForEach(x => suRequests.Add(GetSharedUser(Id: x.Id)));

            var ppRequests = applicationDbContext.ProfilePictures.AsEnumerable().Where(x => requests.Any(y => y.SenderId == x.UserId)).ToList();
            var uImgRequests = applicationDbContext.Images.AsEnumerable().Where(x => ppRequests.Any(y => y.ImageId == x.ImageId)).ToList();

            var gRequests = applicationDbContext.Groups.AsEnumerable().Where(x => grRequests.Any(y => y.GroupId == x.GroupId)).ToList();
            var gppRequests = applicationDbContext.GroupProfilePictures.AsEnumerable().Where(x => gRequests.Any(y => y.GroupId == x.GroupId)).ToList();
            var gImgRequests = applicationDbContext.GroupImages.AsEnumerable().Where(x => gppRequests.Any(y => y.GroupId == x.GroupId)).ToList();

            var friends = applicationDbContext.Friends.Where(x => x.UserId == u.Id).ToList();
            var suFriends = new List<SharedUser>();
            friends.ForEach(x => suFriends.Add(GetSharedUser(Id: x.FriendId)));

            var rms = applicationDbContext.UserRooms.Where(x => x.UserId == u.Id).ToList();
            var notFriends = applicationDbContext.Users.AsEnumerable().Where(x => rms.Any(y => y.OtherId == x.Id) && !friends.Any(y => y.FriendId == x.Id)).ToList();
            var suNFriends = new List<SharedUser>();
            foreach (var f in notFriends)
            {
                var r = rms.Where(x => x.UserId == u.Id && x.OtherId == f.Id).FirstOrDefault();
                var msgs = applicationDbContext.ChatMessages.Where(x => x.RoomId == r.RoomId).Count();

                if (msgs > 0)
                    suNFriends.Add(GetSharedUser(Id: f.Id));
            }

            var model = new MainLayoutModel { SignedInUser = u };
            model.Requests.AddRange(requests);
            model.GroupRequests.AddRange(grRequests);
            model.Notifications.AddRange(notifications);
            model.SuRequests.AddRange(suRequests);
            model.ProfilePictureRequests.AddRange(ppRequests);
            model.Images.AddRange(uImgRequests);
            model.Groups.AddRange(gRequests);
            model.GroupProfilePictureRequests.AddRange(gppRequests);
            model.GroupImages.AddRange(gImgRequests);
            model.Friends.AddRange(suFriends);
            model.NotFriends.AddRange(suNFriends);

            return Task.FromResult(model);
        }

        public override Task<Empty> DeleteNotifications(Empty Empty, ServerCallContext context)
        {
            var u = GetUserFromRequest(context);
            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            var ns = applicationDbContext.Notifications.Where(x => x.UserId == u.Id).ToList();
            applicationDbContext.Notifications.RemoveRange(ns);
            applicationDbContext.SaveChanges();

            return Task.FromResult(new Empty());
        }

        public override Task<Notification> DeleteNotification(Notification notification, ServerCallContext context)
        {
            var n = applicationDbContext.Notifications.Find(notification.NotificationId);
            var u = applicationDbContext.Users.Find(n.UserId);

            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            applicationDbContext.Notifications.Remove(n);
            applicationDbContext.SaveChanges();

            return Task.FromResult(notification);
        }

        public override Task<Request> AcceptFriendRequest(Request request, ServerCallContext context)
        {
            var r = applicationDbContext.Requests.Find(request.RequestId);

            bool isFriend = applicationDbContext.Friends.Where(x => x.UserId == r.SenderId && x.FriendId == r.SentToId).FirstOrDefault() == null ||
                         applicationDbContext.Friends.Where(x => x.UserId == r.SentToId && x.FriendId == r.SenderId).FirstOrDefault() == null;

            if (isFriend)
            {
                applicationDbContext.Friends.Add(new FriendDuo { UserId = r.SenderId, FriendId = r.SentToId });
                applicationDbContext.Friends.Add(new FriendDuo { UserId = r.SentToId, FriendId = r.SenderId });
            }

            applicationDbContext.Requests.Remove(r);
            applicationDbContext.SaveChanges();

            return Task.FromResult(request);
        }

        public override Task<Request> DeclineFriendRequest(Request request, ServerCallContext context)
        {
            var r = applicationDbContext.Requests.Find(request.RequestId);

            applicationDbContext.Requests.Remove(r);
            applicationDbContext.SaveChanges();

            return Task.FromResult(request);
        }

        public override Task<GroupRequest> AcceptGroupRequest(GroupRequest request, ServerCallContext context)
        {
            var r = applicationDbContext.GroupRequests.Find(request.RequestId);

            var role = new Role { GroupId = request.GroupId, UserId = request.UserId, RoleType = RoleType.Member };

            applicationDbContext.Roles.Add(role);
            applicationDbContext.GroupRequests.Remove(r);
            applicationDbContext.SaveChanges();

            return Task.FromResult(request);
        }

        public override Task<GroupRequest> DeclineGroupRequest(GroupRequest request, ServerCallContext context)
        {
            var r = applicationDbContext.GroupRequests.Find(request.RequestId);

            applicationDbContext.GroupRequests.Remove(r);
            applicationDbContext.SaveChanges();

            return Task.FromResult(request);
        }

        public override Task<MainProfilePictureModel> GetMainProfilePicture(Id id, ServerCallContext context)
        {
            var su = GetSharedUser(Id: id.Id_);

            var pp = applicationDbContext.ProfilePictures.Where(x => x.UserId == su.Id).FirstOrDefault();

            Image img = (pp != null) ? applicationDbContext.Images.Find(pp.ImageId) : new Image();

            return Task.FromResult(new MainProfilePictureModel { Image = img, ProfilePicture = pp, User = su });
        }

        public override Task<PlacesLivedResponse> GetPlacesLived(Id id, ServerCallContext context)
        {
            var countries = applicationDbContext.Countries.ToList();
            var places = applicationDbContext.PlacesLived.Where(x => x.UserId == id.Id_).ToList();

            var states = applicationDbContext.States.AsEnumerable().Where(x => places.Any(y => y.StateId == x.Id)).ToList();
            var cities = applicationDbContext.Cities.AsEnumerable().Where(x => places.Any(y => y.CityId == x.Id)).ToList();

            var m = new PlacesLivedResponse();
            m.PlacesLived.AddRange(places);
            m.Countries.AddRange(countries);
            m.States.AddRange(states);
            m.Cities.AddRange(cities);

            return Task.FromResult(m);
        }

        public override Task<Lived> CreatePlace(Lived lived, ServerCallContext context)
        {
            applicationDbContext.PlacesLived.Add(lived);
            applicationDbContext.SaveChanges();

            return Task.FromResult(lived);
        }

        public override Task<Lived> DeletePlace(Lived lived, ServerCallContext context)
        {
            var p = applicationDbContext.PlacesLived.Find(lived.PlaceLivedId);
            applicationDbContext.PlacesLived.Remove(p);
            applicationDbContext.SaveChanges();

            return Task.FromResult(lived);
        }

        public override Task<Lived> UpdatePlace(Lived lived, ServerCallContext context)
        {
            applicationDbContext.PlacesLived.Update(lived);
            applicationDbContext.SaveChanges();

            return Task.FromResult(lived);
        }

        public override Task<PostPageModel> GetPostModel(Room room, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Find(room.OwnerId);
            var owner = GetSharedUser(Id: room.OwnerId);
            var signedInUser = GetUserFromRequest(context);

            var rmGrp = applicationDbContext.RoomGroups.Where(x => x.RoomId == room.RoomId).FirstOrDefault() ?? new RoomGroup();

            GroupModel group = new GroupModel();
            Role signedInUserRole = new Role();
            SectionRoles sectionRoles = new SectionRoles();

            if (applicationDbContext.RoomGroups.Where(x => x.RoomId == room.RoomId).FirstOrDefault() != null)
            {
                group = applicationDbContext.Groups.Find(rmGrp.GroupId);

                signedInUserRole = applicationDbContext.Roles.Where(x => x.GroupId == rmGrp.GroupId && x.UserId == signedInUser.Id).FirstOrDefault();
                sectionRoles = applicationDbContext.SectionRoles.Where(x => x.GroupId == rmGrp.GroupId).FirstOrDefault();

                if (signedInUserRole == null)
                {
                    signedInUserRole = new Role { UserId = signedInUser.Id, RoleType = RoleType.NoRole };
                }
            }

            var categoryRoom = applicationDbContext.CategoryRooms.Where(x => x.RoomId == room.RoomId).FirstOrDefault();
            if (categoryRoom == null)
                categoryRoom = new CategoryRooms { RoomId = room.RoomId, Category = "" };

            var rmToMsg = applicationDbContext.RoomToMessages.Where(x => x.RoomId == room.RoomId).FirstOrDefault();

            var votes = applicationDbContext.Votes.Where(x => x.RoomId == room.RoomId).ToList();
            var uVotes = new List<SharedUser>();
            votes.ForEach(x => uVotes.Add(GetSharedUser(Id: x.UserId)));

            Like like = (rmToMsg != null) ? applicationDbContext.Likes.Where(x => x.UserId == signedInUser.Id && x.MessageId == rmToMsg.MessageId).FirstOrDefault() : new Like();

            var imgRms = applicationDbContext.ImageRooms.Where(x => x.RoomId == room.RoomId).ToList();
            var images = applicationDbContext.Images.Where(x => x.UserId == signedInUser.Id).ToList();

            var msg = applicationDbContext.Messages.AsEnumerable().Where(x => rmToMsg.RoomId == room.RoomId && rmToMsg.MessageId == x.MessageId).FirstOrDefault();

            var msgOn = applicationDbContext.MessageOnMessages.Where(x => x.MessageOnId == msg.MessageId).ToList();
            var messages = applicationDbContext.Messages.AsEnumerable().Where(x => msgOn.Any(y => x.MessageId == y.MessageId)).ToList();
            var msgCount = applicationDbContext.Messages.AsEnumerable().Where(x => x.RoomId == rmToMsg.RoomId).Count();

            

            if (string.IsNullOrWhiteSpace(msg.SenderId))
            {
                msg.SenderId = room.OwnerId;
                applicationDbContext.Messages.Update(msg);
                applicationDbContext.SaveChanges();
            }

            ProfilePicture pp = applicationDbContext.ProfilePictures.Where(x => x.UserId == msg.SenderId).FirstOrDefault();
            Image pic = (pp != null && !string.IsNullOrWhiteSpace(pp.ProfilePictureId)) ? applicationDbContext.Images.Find(pp.ImageId) : new Image();

            var hashtags = applicationDbContext.MessageHashtags.ToList().Where(x => x.RoomId == rmToMsg.RoomId).ToList();

            var cat = applicationDbContext.Images.Where(x => x.Category == categoryRoom.Category).ToList();
            var imgIndex = applicationDbContext.Images.Where(x => x.UserId == msg.SenderId && x.Category == categoryRoom.Category).ToList();

            var groupImages = applicationDbContext.GroupImages.Where(x => x.GroupId == rmGrp.GroupId && x.Category == categoryRoom.Category).ToList();
            var grpImgCat = applicationDbContext.GroupImages.Where(x => x.Category == categoryRoom.Category).ToList();

            var imgsFromRms = applicationDbContext.Images.AsEnumerable().Where(x => imgRms.AsEnumerable().Any(y => y.ImageId == x.ImageId)).ToList();

            var friends = new List<FriendDuo>();
            if (messages.Count() > 0)
                friends = applicationDbContext.Friends.AsEnumerable().Where(x => x.UserId == signedInUser.Id && messages.Any(y => y.SenderId == x.FriendId)).ToList();
            var uFriends = applicationDbContext.Friends.Where(x => x.UserId == signedInUser.Id).ToList();
            var myFriends = new List<SharedUser>();
            uFriends.ForEach(x => myFriends.Add(GetSharedUser(Id: x.FriendId)));

            var shares = applicationDbContext.Shares.Where(x => x.RoomId == room.RoomId && applicationDbContext.Friends.Any(y => y.UserId == signedInUser.Id && x.UserId == y.FriendId)).ToList();

            var model = new PostPageModel
            {
                Room = room,
                Owner = owner,
                RoomGroup = rmGrp,
                Group = group,
                SignedInUser = signedInUser,
                SignedInUserRole = signedInUserRole,
                SectionRoles = sectionRoles,
                CategoryRooms = categoryRoom,
                RoomToMessage = rmToMsg,
                Like = like,
                Message = msg,
                ProfilePicture = pp,
                Picture = pic,
                MessageCount = msgCount
            };
            model.Votes.AddRange(votes);
            model.SuVotes.AddRange(uVotes);
            model.ImageRooms.AddRange(imgRms);
            model.Images.AddRange(images);
            model.Messages.AddRange(messages);
            model.Categories.AddRange(cat);
            model.ImageIndex.AddRange(imgIndex);
            model.GroupImages.AddRange(groupImages);
            model.GroupImagesCategory.AddRange(grpImgCat);
            model.ImagesFromRoom.AddRange(imgsFromRms);
            model.Friends.AddRange(friends);
            model.MyFriends.AddRange(myFriends);
            model.Shares.AddRange(shares);
            model.MessageHashtags.AddRange(hashtags);

            return Task.FromResult(model);
        }

        public override Task<Message> SaveEdit(SaveEditRequest saveEdit, ServerCallContext context)
        {
            if (saveEdit.Message.Date < Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(30)))
                throw new AccessViolationException();

            foreach (var t in saveEdit.Hashtags)
            {
                var tag = applicationDbContext.MessageHashtags.Where(x => x.Name == t.Name && x.RoomId == saveEdit.Message.RoomId).FirstOrDefault();
                if (tag == null)
                    applicationDbContext.MessageHashtags.Add(t);
            }

            applicationDbContext.Messages.Update(saveEdit.Message);
            applicationDbContext.SaveChanges();

            return Task.FromResult(saveEdit.Message);
        }

        public override Task<ProfilePicturePageModel> GetProfilePicture(Id id, ServerCallContext context)
        {
            var u = GetUserFromRequest(context);

            var pp = applicationDbContext.ProfilePictures.Where(x => x.UserId == id.Id_).FirstOrDefault();

            Image img = (pp != null) ? applicationDbContext.Images.Find(pp.ImageId) : new();

            FriendDuo friend = (u.Id != id.Id_) ? applicationDbContext.Friends.Where(x => x.UserId == u.Id && x.FriendId == id.Id_).FirstOrDefault() : null;
            bool isFriend = false;
            if (friend != null)
                isFriend = true;

            return Task.FromResult(new ProfilePicturePageModel { ProfilePicture = pp, Image = img, IsFriend = isFriend });
        }

        public override Task<ReplyModel> GetReplyModel(Message message, ServerCallContext context)
        {
            var m = applicationDbContext.Messages.Find(message.MessageId);

            var u = applicationDbContext.Users.Find(m.SenderId);
            SharedUser user = (u != null && !string.IsNullOrWhiteSpace(u.Id)) ? GetSharedUser(Id: u.Id) : new();

            var messageOns = applicationDbContext.MessageOnMessages.Where(x => x.MessageOnId == m.MessageId).ToList();
            var messages = applicationDbContext.Messages.AsEnumerable().Where(x => messageOns.Any(y => x.MessageId == y.MessageId)).OrderByDescending(x => x.Date).ToList();

            var like = applicationDbContext.Likes.Where(x => x.MessageId == m.MessageId && x.UserId == GetUserFromRequest(context).Id).FirstOrDefault();
            var likes = applicationDbContext.Likes.Where(x => x.MessageId == m.MessageId).ToList();

            var model = new ReplyModel { User = user, Like = like, Message = message };
            model.Messages.AddRange(messages);
            model.MessageOnMessages.AddRange(messageOns);
            model.Likes.AddRange(likes);

            return Task.FromResult(model);
        }

        public override Task<ShareResponse> Share(ShareRequest s, ServerCallContext context)
        {
            var u = GetUserFromRequest(context);

            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            var share = new Share { RoomId = s.Room.RoomId, SenderId = u.Id, UserId = s.Friend.Id, Date = Timestamp.FromDateTime(DateTime.UtcNow) };
            applicationDbContext.Shares.Add(share);
            applicationDbContext.SaveChanges();

            return Task.FromResult(new ShareResponse { Name = s.Friend.Name, Share = share });
        }

        public override Task<ShareResponse> UnShare(ShareRequest s, ServerCallContext context)
        {
            var share = applicationDbContext.Shares.Where(x => x.RoomId == s.Room.RoomId && x.UserId == s.Friend.Id).FirstOrDefault();
            applicationDbContext.Shares.Remove(share);
            applicationDbContext.SaveChanges();

            return Task.FromResult(new ShareResponse { Name = s.Friend.Name, Share = share });
        }

        public override Task<WorkHistoryResponse> GetWorkHistory(Id id, ServerCallContext context)
        {
            var countries = applicationDbContext.Countries.ToList();
            var workHistories = applicationDbContext.WorkHistory.Where(x => x.UserId == id.Id_).ToList();

            var states = applicationDbContext.States.AsEnumerable().Where(x => workHistories.Any(y => y.StateId == x.Id)).ToList();
            var cities = applicationDbContext.Cities.AsEnumerable().Where(x => workHistories.Any(y => y.CityId == x.Id)).ToList();

            var m = new WorkHistoryResponse();
            m.Countries.AddRange(countries);
            m.WorkHistories.AddRange(workHistories);
            m.States.AddRange(states);
            m.Cities.AddRange(cities);

            return Task.FromResult(m);
        }

        public override Task<WorkHistory> CreateWork(WorkHistory work, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Find(work.UserId);

            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            applicationDbContext.WorkHistory.Add(work);
            applicationDbContext.SaveChanges();

            return Task.FromResult(work);
        }

        public override Task<WorkHistory> UpdateWork(WorkHistory work, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Find(work.UserId);

            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            applicationDbContext.WorkHistory.Update(work);
            applicationDbContext.SaveChanges();

            return Task.FromResult(work);
        }

        public override Task<WorkHistory> DeleteWork(WorkHistory work, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Find(work.UserId);

            if (!Authorize(context, u.Username))
                throw new UnauthorizedAccessException();

            var w = applicationDbContext.WorkHistory.Find(work.WorkId);
            applicationDbContext.WorkHistory.Remove(w);
            applicationDbContext.SaveChanges();

            return Task.FromResult(work);
        }

        public override Task<StatesResponse> GetStates(IdNumber id, ServerCallContext context)
        {
            var states = applicationDbContext.States.Where(x => x.CountryId == id.Id).ToList();
            var m = new StatesResponse();
            m.States.AddRange(states);

            return Task.FromResult(m);
        }

        public override Task<CitiesResponse> GetCities(IdNumber id, ServerCallContext context)
        {
            var cities = applicationDbContext.Cities.Where(x => x.StateId == id.Id).ToList();
            var m = new CitiesResponse();
            m.Cities.AddRange(cities);

            return Task.FromResult(m);
        }

        public override Task<PetitionsPageModel> GetPetitions(Empty Empty, ServerCallContext context)
        {
            var petitions = applicationDbContext.Petitions.ToList();
            var sigs = applicationDbContext.PetitionSigs.ToList();
            var users = applicationDbContext.Users.ToList().Where(x => petitions.Any(y => y.CreatedById == x.Id)).ToList();

            
            var u = GetUserFromRequest(context);
            var petitionAddress = applicationDbContext.PetitionAddresses.Where(x => x.UserId == u.Id).FirstOrDefault();

            var model = new PetitionsPageModel();
            model.User = u;
            model.Petitions.AddRange(petitions);
            model.PetitionSigs.AddRange(sigs);

            if (petitionAddress != null && !string.IsNullOrWhiteSpace(petitionAddress.UserId))
            {
                var p = applicationDbContext.PlacesLived.Find(petitionAddress.LivedId);
                model.Lived = p;
                model.Country = applicationDbContext.Countries.Find(p.CountryId);
                model.State = applicationDbContext.States.Find(p.StateId);
                model.City = applicationDbContext.Cities.Find(p.CityId);
            }
            else
            {
                model.Lived = new Lived();
                model.Country = new Country();
                model.State = new State();
                model.City = new City();
            }

            users.ForEach(x => model.Users.Add(GetSharedUser(Id: x.Id)));

            return Task.FromResult(model);
        }

        public override Task<PetitionPageModel> GetPetition(Id id, ServerCallContext context)
        {
            var p = applicationDbContext.Petitions.Find(id.Id_);
            var sigs = applicationDbContext.PetitionSigs.Where(x => x.PetitionId == id.Id_).ToList();
            var users = applicationDbContext.Users.ToList().Where(x => sigs.Any(y => y.UserId == x.Id)).ToList();

            var model = new PetitionPageModel();
            model.User = GetUserFromRequest(context);
            model.Petition = p;
            model.Sigs.AddRange(sigs);

            return Task.FromResult(model);
        }

        public override Task<PetitionModel> CreatePetition(PetitionModel model, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Find(model.CreatedById);
            if (!Authorize(context, u.Username))
                throw new System.Exception();

            applicationDbContext.Petitions.Add(model);
            applicationDbContext.SaveChanges();

            return Task.FromResult(model);
        }

        public override Task<PetitionModel> DeletePetition(PetitionModel model, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Find(model.CreatedById);
            if (!Authorize(context, u.Username))
                throw new System.Exception();

            var m = applicationDbContext.Petitions.Where(x => x.PetitionId == model.PetitionId).FirstOrDefault();
            applicationDbContext.Petitions.Remove(m);
            applicationDbContext.SaveChanges();

            return Task.FromResult(model);
        }

        public override Task<PetitionSig> SignPetition(PetitionSig sig, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Find(sig.UserId);
            //var u = applicationDbContext.Users.Where(x => x.Id == sig.UserId).FirstOrDefault();
            if (!Authorize(context, u.Username))
                throw new System.Exception();

            if (applicationDbContext.PetitionSigs.Where(x => x.UserId == u.Id && x.PetitionId == sig.PetitionId).FirstOrDefault() == null)
            {
                applicationDbContext.PetitionSigs.Add(sig);
                applicationDbContext.SaveChanges();
                return Task.FromResult(sig);
            }
            else
                throw new System.Exception();
        }

        public override Task<PetitionSig> UnsignPetition(PetitionSig sig, ServerCallContext context)
        {
            var u = GetUserFromRequest(context);
            if (!Authorize(context, u.Username))
                throw new System.Exception();

            var s = applicationDbContext.PetitionSigs.Where(x => x.PetitionId == sig.PetitionId && x.UserId == u.Id).FirstOrDefault();
            if (s != null)
            {
                applicationDbContext.PetitionSigs.Remove(s);
                applicationDbContext.SaveChanges();

                return Task.FromResult(sig);
            }
            else
                throw new System.Exception();
        }

        public override Task<Empty> UpdateAddressForPetition(Lived lived, ServerCallContext context)
        {
            var u = applicationDbContext.Users.Find(lived.UserId);

            if (!Authorize(context, u.Username))
                throw new System.Exception();

            var a = applicationDbContext.PetitionAddresses.Find(u.Id);
            if (a == null)
                applicationDbContext.PetitionAddresses.Add(new PetitionAddress { LivedId = lived.PlaceLivedId, UserId = u.Id });
            else
            {
                a.LivedId = lived.PlaceLivedId;
                applicationDbContext.PetitionAddresses.Update(a);
            }

            applicationDbContext.SaveChanges();

            return Task.FromResult(new Empty());
        }

        public override Task<VideoHomePage> GetVideoHomePage(Empty Empty, ServerCallContext context)
        {
            var likes = applicationDbContext.VideoLikes.Where(x => x.Like && x.LikeDate.ToDateTime() <= DateTime.Now.AddHours(24)).GroupBy(x => x.VideoId).Select(x => x.FirstOrDefault()).Take(200).ToList();
            var vids = applicationDbContext.Videos.Where(x => likes.Any(y => y.VideoId == x.VideoId)).ToList();

            var model = new VideoHomePage();
            model.Videos.AddRange(vids);
            model.Likes.AddRange(likes);

            return Task.FromResult(model);
        }
    }
}
