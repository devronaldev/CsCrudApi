namespace CsCrudApi.Models.PostRelated.Request
{
    public class PostRequest
    {
        public List<string> PostGUIDs { get; set; }

        public int? IdUser { get; set; }
    }
}
