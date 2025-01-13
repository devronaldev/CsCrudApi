using CsCrudApi.Models.UserRelated;

namespace CsCrudApi.Models.PostRelated.Requests
{
    public class FeedPost
    {
        public Post Post { get; set; }
        public List<int> Categories { get; set; }
        public object User { get; set; }
    }
}
