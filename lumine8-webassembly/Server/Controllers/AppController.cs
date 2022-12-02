using Google.Protobuf.WellKnownTypes;
using lumine8.Server.Data;
using lumine8.Shared;
using lumine8_GrpcService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using Exception = System.Exception;
using FileResult = Microsoft.AspNetCore.Mvc.FileResult;
using Image = lumine8_GrpcService.Image;

namespace lumine8_web.Controllers
{
    public class Petitions
    {
        public IEnumerable<PetitionModel>? PetitionModels { get; set; }
        public IEnumerable<PetitionSig>? PetitionSigs { get; set; }
        public IEnumerable<SharedUser>? Users { get; set; }
    }

    [Route("api/[action]")]
    public class AppController : ControllerBase
    {
        private readonly ApplicationDbContext applicationDbContext;
        private readonly string rootPath = (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        ? Path.Combine(Path.DirectorySeparatorChar.ToString(), "var", "www", "lumine8", "wwwroot") : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        public AppController(ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext;
        }

        [HttpGet]
        public Petitions GetPetitions()
        {
            var petitions = applicationDbContext.Petitions.AsEnumerable();
            var sigs = applicationDbContext.PetitionSigs.AsEnumerable();

            var users = applicationDbContext.Users.Where(x => petitions.Any(y => y.CreatedById == x.Id)).AsEnumerable();
            var suUsers = new List<SharedUser>();
            foreach (var u in users)
                suUsers.Add(new SharedUser { Id = u.Id, Username = u.Username, Name = u.Name });
            
            return new Petitions { PetitionModels = petitions, PetitionSigs = sigs, Users = suUsers };
        }

        [HttpGet]
        [Route("{id}")]
        public long GetPetitionCount(string id)
        {
            return applicationDbContext.PetitionSigs.Where(x => x.PetitionId == id).Count();
        }

        public FileResult Android()
        {
            var file = Path.Combine(rootPath, "downloads", "com.lumine8.lumine8_maui-Signed.apk");
            var data = System.IO.File.ReadAllBytes(file);

            return File(data, "application/vnd.android.package-archive", "com.lumine8.lumine8_maui-Signed.apk");
        }

        public FileResult Windows()
        {
            var file = Path.Combine(rootPath, "downloads", "lumine8.exe");
            var data = System.IO.File.ReadAllBytes(file);

            return File(data, "application/octet-stream", "lumine8.exe");
        }

        public bool Authorize(HttpRequest context, string Username)
        {
            var privateKey = context.Headers["PrivateKey"].FirstOrDefault();
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

        private SharedUser GetUserFromRequest(HttpRequest re)
        {
            var Username = re.Headers["Username"].FirstOrDefault();
            var u = applicationDbContext.Users.Where(x => x.Username == Username).FirstOrDefault();

            if (u != null)
                return GetSharedUser(Username: u.Username);
            else
                return GetSharedUser();
        }

        public string TestEmail()
        {
            try
            {
                MailMessage message = new MailMessage();
                message.From = new MailAddress("no-reply@lumine8.com");
                message.To.Add(new MailAddress("redrevyol@gmail.com"));
                message.Subject = "Subject test";
                message.Body = Guid.NewGuid().ToString();

                var client = new SmtpClient();
                client.Host = "localhost";
                client.Port = 25;
                client.EnableSsl = true;
                client.Send(message);

                return "Test Passed";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<IActionResult?> UploadPP([FromForm] IFormFile file)
        {
            var u = GetUserFromRequest(Request);

            if (!Authorize(Request, u.Username))
                throw new AuthenticationException();

            string guid = Guid.NewGuid().ToString();

            string path = Path.Combine(rootPath, "p", "u", u.Id);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            if (file != null)
            {
                string fileType = file.FileName.Split('.')[1];
                string fileName = $"{guid}.{fileType}";
                string fPath = Path.Combine(path, fileName);

                using MemoryStream ms = new();
                await file.CopyToAsync(ms);
                await System.IO.File.WriteAllBytesAsync(fPath, ms.ToArray());

                var image = new Image
                {
                    ImageId = Guid.NewGuid().ToString(),
                    UserId = u.Id,
                    Category = "Profile Pictures",
                    FileName = fileName,
                    UploadDate = Timestamp.FromDateTime(DateTime.UtcNow)
                };
                applicationDbContext.Images.Add(image);
                applicationDbContext.SaveChanges();

                var ppl = applicationDbContext.ProfilePictures.Where(x => x.UserId == u.Id).FirstOrDefault();

                if (ppl == null)
                    applicationDbContext.ProfilePictures.Add(new ProfilePicture { ProfilePictureId = Guid.NewGuid().ToString(), UserId = u.Id, ImageId = image.ImageId });

                applicationDbContext.SaveChanges();

                return Ok(image);
            }

            return null;
        }

        public async Task<IActionResult> PostLumine([FromForm] IFormFile file)
        {
            var u = GetUserFromRequest(Request);
            if (!Authorize(Request, u.Username))
                throw new AuthenticationException();

            var json = Request.Headers["PostJson"].FirstOrDefault();
            var post = JsonConvert.DeserializeObject<PostLumineContent>(json);

            post.Room.RoomId = Guid.NewGuid().ToString();
            post.Room.Date = Timestamp.FromDateTime(DateTime.UtcNow);
            applicationDbContext.Rooms.Add(post.Room);
            applicationDbContext.Entry(post.Room).State = Microsoft.EntityFrameworkCore.EntityState.Added;
            applicationDbContext.SaveChanges();

            post.Lumine.MessageId = Guid.NewGuid().ToString();
            post.Lumine.Date = Timestamp.FromDateTime(DateTime.UtcNow);
            post.Lumine.RoomId = post.Room.RoomId;

            applicationDbContext.Messages.Add(post.Lumine);
            applicationDbContext.SaveChanges();

            applicationDbContext.RoomToMessages.Add(new RoomToMessage { RoomToMessageId = Guid.NewGuid().ToString(), MessageId = post.Lumine.MessageId, RoomId = post.Room.RoomId });

            if (post.Hashtags != null)
            {
                foreach (var h in post.Hashtags)
                {
                    h.RoomId = post.Room.RoomId;
                    h.MessageHashtagId = Guid.NewGuid().ToString();
                    applicationDbContext.MessageHashtags.Add(h);
                }
            }

            applicationDbContext.SaveChanges();

            if (file != null)
            {
                string guid = Guid.NewGuid().ToString();
                string fileType = file.FileName.Split('.')[1];
                string fileName = $"{guid}.{fileType}";

                string aPath = Path.Combine(rootPath, "p", "u", post.Lumine.SenderId);

                if (!Directory.Exists(aPath))
                    Directory.CreateDirectory(aPath);

                string path = Path.Combine(aPath, fileName);

                using MemoryStream ms = new();
                await file.CopyToAsync(ms);
                await System.IO.File.WriteAllBytesAsync(path, ms.ToArray());

                var image = new Image
                {
                    ImageId = Guid.NewGuid().ToString(),
                    UserId = post.Lumine.SenderId,
                    Category = post.Category,
                    FileName = fileName,
                    UploadDate = Timestamp.FromDateTime(DateTime.UtcNow)
                };

                applicationDbContext.Images.Add(image);
                applicationDbContext.SaveChanges();

                applicationDbContext.ImageRooms.Add(new ImageRoom { ImageRoomId = Guid.NewGuid().ToString(), ImageId = image.ImageId, RoomId = post.Room.RoomId });
                applicationDbContext.SaveChanges();
            }

            return Ok(post.Room);
        }

        /*****Pictures*****/
        [Route("{category}")]
        public async Task<IActionResult> UploadPictures([FromForm] List<IFormFile> files, string category)
        {
            var u = GetUserFromRequest(Request);
            var images = new List<Image>();

            string aPath = Path.Combine(rootPath, "p", "u", u.Id);

            if (!Directory.Exists(aPath))
                Directory.CreateDirectory(aPath);

            foreach (var f in files)
            {
                string guid = Guid.NewGuid().ToString();
                string fileType = f.FileName.Split('.')[1];
                string fileName = $"{guid}.{fileType}";

                string path = Path.Combine(aPath, fileName);

                using MemoryStream ms = new();
                await f.CopyToAsync(ms);
                await System.IO.File.WriteAllBytesAsync(path, ms.ToArray());

                var image = new Image
                {
                    ImageId = Guid.NewGuid().ToString(),
                    UploadDate = Timestamp.FromDateTime(DateTime.UtcNow),
                    UserId = u.Id,
                    Category = category,
                    FileName = fileName
                };

                applicationDbContext.Images.Add(image);
                applicationDbContext.SaveChanges();

                images.Add(image);
            }

            return Ok(images);
        }
    }
}