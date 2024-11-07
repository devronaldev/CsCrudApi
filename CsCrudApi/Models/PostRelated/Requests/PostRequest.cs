namespace CsCrudApi.Models.PostRelated.Requests
{
    public class PostRequest
    {
        public List<string> PostGUIDs { get; set; }
        public int? UserId { get; set; }
    }
}
