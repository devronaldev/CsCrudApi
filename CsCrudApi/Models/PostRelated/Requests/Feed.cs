using CsCrudApi.Models.UserRelated;

namespace CsCrudApi.Models.PostRelated.Requests
{
    public class FeedPost
    {
        public PostRequest Post { get; set; }

        public User User { get; set; }
    }
}
