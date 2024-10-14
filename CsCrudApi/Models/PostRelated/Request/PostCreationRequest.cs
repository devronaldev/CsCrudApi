namespace CsCrudApi.Models.PostRelated.Request
{
    public class PostCreationRequest
    {
        public Post Post { get; set; }
        public List<int> PostAuthorsIds { get; set; }
    }
}
