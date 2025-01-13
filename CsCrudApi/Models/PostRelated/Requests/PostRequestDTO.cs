namespace CsCrudApi.Models.PostRelated.Requests
{
    public class PostRequestDTO
    {
        public Post Post { get; set; }
        public List<int>? Categories { get; set; }
    }
}
