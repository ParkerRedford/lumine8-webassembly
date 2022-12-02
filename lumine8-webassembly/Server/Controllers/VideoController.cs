using Google.Protobuf.WellKnownTypes;
using lumine8.Server.Data;
using lumine8_GrpcService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;

namespace lumine8.Server.Controllers
{
    [Route("api/v/[action]")]
    public class VideoController : Controller
    {
        private readonly ApplicationDbContext applicationDbContext;
        private readonly string rootPath = (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            ? Path.Combine(Path.DirectorySeparatorChar.ToString(), "var", "www", "lumine8", "wwwroot", "p") : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "p");
        public VideoController(ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext;
        }

        /*string HashString(string text, string salt = "")
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            using (var sha = new SHA256Managed())
            {
                byte[] textBytes = Encoding.UTF8.GetBytes(text + salt);
                byte[] hashBytes = sha.ComputeHash(textBytes);

                string hash = BitConverter
                    .ToString(hashBytes)
                    .Replace("-", string.Empty);
                return hash;
            }
        }*/

        public bool Authorize(HttpRequest context, string Username)
        {
            var privateKey = context.Headers["PrivateKey"].FirstOrDefault();
            var acc = new Nethereum.Web3.Accounts.Account(privateKey);
            return (applicationDbContext.Users.Where(x => x.Username == Username && x.PublicKey == acc.PublicKey).FirstOrDefault() != null);

            /*
            var Username = context.GetHttpContext().Request.Headers["Username"].FirstOrDefault();
            var token = context.GetHttpContext().Request.Headers["Token"].FirstOrDefault();
            
            *//*var Username = login.Username;
            var token = login.Token;
            var password = login.Password;*//*

            if (Username == Username)
            {
                var u = applicationDbContext.Users.Where(x => x.Username == Username).FirstOrDefault();
                if (string.IsNullOrWhiteSpace(password))
                {
                    var t = applicationDbContext.Tokens.Where(x => x.UserId == u.Id && x.TokenId == token).FirstOrDefault();
                    if (t != null)
                        return true;
                }
                else
                    return CheckPassword(new LoginUser { Username = Username, Token = token, Password = password });
            }

            return false;*/
        }

        /*public bool CheckPassword(LoginUser loginUser)
        {
            var u = applicationDbContext.Users.Where(x => x.Username == loginUser.Username).FirstOrDefault();
            return (HashString(loginUser.Password, u.PasswordSalt) == u.PasswordStamp);
        }*/

        /*private bool Authorize(HttpRequest re, string Username)
        {
            var username = re.Headers["Username"].FirstOrDefault();
            var token = re.Headers["Token"].FirstOrDefault();
            var password = re.Headers["Password"].FirstOrDefault();

            if (Username == username)
            {
                var u = applicationDbContext.Users.Where(x => x.Username == Username).FirstOrDefault();
                if (string.IsNullOrWhiteSpace(password))
                {
                    var t = applicationDbContext.Tokens.Where(x => x.UserId == u.Id && x.TokenId == token).FirstOrDefault();
                    if (t != null)
                        return true;
                }
                else
                    return CheckPassword(new LoginUser { Username = Username, Token = token, Password = password });
            }

            return false;
        }*/

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

        public async Task<IActionResult> Video([FromForm] IFormFile file)
        {
            var u = GetUserFromRequest(Request);

            if (!Authorize(Request, u.Username))
                throw new UnauthorizedAccessException();

            var split = file.FileName.Split('.');
            var path = Path.Combine(rootPath, "v", u.Id, $"{Guid.NewGuid()}.{split[1]}");

            using MemoryStream ms = new();
            await file.CopyToAsync(ms);
            await System.IO.File.WriteAllBytesAsync(path, ms.ToArray());

            return Ok();
        }

        public Video[] GetVideos()
        {
            return applicationDbContext.Videos.ToArray();
        }
    }
}
