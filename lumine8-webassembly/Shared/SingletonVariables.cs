using lumine8_GrpcService;
using Image = lumine8_GrpcService.Image;

namespace lumine8
{
    public class SingletonVariables
    {
        //public string grpc { get; private set; } = "https://lumine8.com";
        public string uri { get; private set; } = "https://lumine8.com";
        public string version { get; private set; } = "3d9ca813-6f37-40f9-8735-95e2257f0f6a";
        public event Action OnChange;

        public bool isDesktop { get; set; }

        private ProfilePicture pp = new();
        private Image image = new();

        private bool dAboutMe = false;

        public List<LoginUser> users = new();

        public Image Image
        {
            get { return image; }
            set
            {
                image = value;
                OnChange?.Invoke();
            }
        }

        public ProfilePicture PP
        {
            get { return pp; }
            set
            {
                pp = value;
                OnChange?.Invoke();
            }
        }

        public bool DAboutMe
        {
            get { return dAboutMe; }
            set
            {
                dAboutMe = value;
                OnChange?.Invoke();
            }
        }
    }
}
