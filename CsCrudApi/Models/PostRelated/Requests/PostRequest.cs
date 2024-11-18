namespace CsCrudApi.Models.PostRelated.Requests
{
    public class PostRequest
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int? UserId { get; set; }
    }
}
