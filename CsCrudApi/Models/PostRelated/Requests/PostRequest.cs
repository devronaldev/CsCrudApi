namespace CsCrudApi.Models.PostRelated.Requests
{
    public class PostRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int? UserId { get; set; }
    }
}
