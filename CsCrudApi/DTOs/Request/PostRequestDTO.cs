using CsCrudApi.Models.PostRelated;

namespace CsCrudApi.DTOs;

public class PostRequestDTO
{
    public string? Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<string> Categories { get; set; } = new List<string>();
    public ETypePost Type { get; set; }
    public string? ExternalLink { get; set; }
    public int AreaId { get; set; }

    public static explicit operator Post(PostRequestDTO postRequestDTO)
    {
        return new Post
        {
            AreaId = postRequestDTO.AreaId,
            TextPost = postRequestDTO.Text,
            Type = postRequestDTO.Type,
            ExternalLink = postRequestDTO.ExternalLink,
            DcTitulo = postRequestDTO.Title,
            QuantityLikes = 0,
            PostDate = DateTime.Now,
            UserId = 0,
            Guid = Guid.NewGuid().ToString("N")
        };
    }
}