using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace CsCrudApi.Models.PostRelated.Requests
{
    public class PostRequest
    {
        public Post Post { get; set; }
        public Category? Category { get; set; }
    }
}
