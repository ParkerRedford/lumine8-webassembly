using Microsoft.AspNetCore.Mvc;
using lumine8.Server.Data;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;
using Google.Protobuf.WellKnownTypes;
using lumine8_GrpcService;
using Microsoft.AspNetCore.Http;

namespace lumine8.Server.Controllers
{
    [Route("api/g/[action]")]
    public class GroupsController : Controller
    {
        private readonly ApplicationDbContext applicationDbContext;
        private readonly string rootPath = (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        ? Path.Combine(Path.DirectorySeparatorChar.ToString(), "var", "www", "lumine8", "wwwroot", "p") : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "p");
        public GroupsController(ApplicationDbContext applicationDbContext)
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

        private SharedUser GetUserFromRequest(HttpRequest re)
        {
            var username = re.Headers["UserName"].FirstOrDefault();
            var u = applicationDbContext.Users.Where(x => x.Username == username).FirstOrDefault();

            if (u != null)
                return GetSharedUser(UserName: u.Username);
            else
                return GetSharedUser();
        }

        /******Page******/
        [Route("{GroupId}")]
        public async Task<IActionResult> UploadPP([FromForm] IFormFile file, string GroupId)
        {
            string guid = Guid.NewGuid().ToString();
            string fileType = file.FileName.Split('.')[1];
            string fileName = $"{guid}.{fileType}";

            string aPath = Path.Combine(rootPath, "g", GroupId);

            if (!Directory.Exists(aPath))
                Directory.CreateDirectory(aPath);

            string path = Path.Combine(aPath, fileName);

            using MemoryStream ms = new();
            await file.CopyToAsync(ms);
            await System.IO.File.WriteAllBytesAsync(path, ms.ToArray());

            var image = new GroupImage
            {
                ImageId = Guid.NewGuid().ToString(),
                UserId = GetUserFromRequest(Request).Id,
                GroupId = GroupId,
                Category = "Profile Pictures",
                FileName = fileName,
                CreateDate = Timestamp.FromDateTime(DateTime.UtcNow)
            };
            applicationDbContext.GroupImages.Add(image);
            applicationDbContext.SaveChanges();

            var ppl = applicationDbContext.GroupImages.Where(x => x.GroupId == GroupId).FirstOrDefault();

            if (ppl == null)
                applicationDbContext.GroupProfilePictures.Add(new GroupProfilePicture { ProfilePictureId = Guid.NewGuid().ToString(), GroupId = GroupId, ImageId = image.ImageId });

            applicationDbContext.SaveChanges();

            return Ok(image);

        }

        /*****Pictures******/
        [Route("{GroupId}/{Category}")]
        public async Task<IActionResult> UploadPictures([FromForm] List<IFormFile> files, string GroupId, string Category)
        {
            var u = GetUserFromRequest(Request);

            var images = new List<GroupImage>();

            foreach (var f in files)
            {
                string guid = Guid.NewGuid().ToString();
                string fileType = f.FileName.Split('.')[1];
                string fileName = $"{guid}.{fileType}";

                string aPath = Path.Combine(rootPath, "g", GroupId);

                if (!Directory.Exists(aPath))
                    Directory.CreateDirectory(aPath);

                string path = Path.Combine(aPath, fileName);

                using MemoryStream ms = new();
                await f.CopyToAsync(ms);
                await System.IO.File.WriteAllBytesAsync(path, ms.ToArray());

                var image = new GroupImage
                {
                    ImageId = Guid.NewGuid().ToString(),
                    CreateDate = Timestamp.FromDateTime(DateTime.UtcNow),
                    UserId = u.Id,
                    GroupId = GroupId,
                    Category = Category,
                    FileName = fileName
                };

                applicationDbContext.GroupImages.Add(image);
                applicationDbContext.SaveChanges();

                images.Add(image);
            }

            return Ok(images);
        }
    }
}
