using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace CsCrudApi.Models.PostRelated.Requests
{
    public class PostRequest
    {
        public Post Post { get; set; }
        public List<int>? Categories { get; set; }
    }
}
