using CsCrudApi.Models.PostRelated;
using CsCrudApi.Models.UserRelated;

namespace CsCrudApi.DTOs;

public record SearchedPostDTO
{
    public string? Title { get; set; }
    public string Text { get; set; }
    public string Guid { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; }

    public SearchedPostDTO(Post p)
    {
        Guid = p.Guid;
        Text = p.TextPost;
        Title = p.DcTitulo;
        UserId = p.UserId;
    }
};