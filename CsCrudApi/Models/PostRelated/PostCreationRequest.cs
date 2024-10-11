namespace CsCrudApi.Models.PostRelated
{
    public class PostCreationRequest
    {
        public Post Post { get; set; }
        public List<int> PostAuthorsIds { get; set; }
    }
}
