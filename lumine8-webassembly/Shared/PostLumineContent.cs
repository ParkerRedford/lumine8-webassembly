using lumine8_GrpcService;

namespace lumine8.Shared
{
    public class PostLumineContent
    {
        public Message Lumine { get; set; }
        public Room Room { get; set; }
        public List<MessageHashtag> Hashtags { get; set; }
        public string Category { get; set; }
    }
}
