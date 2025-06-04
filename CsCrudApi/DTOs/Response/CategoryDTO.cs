using CsCrudApi.Models.PostRelated;

namespace CsCrudApi.DTOs;

public record CategoryDTO(Category c)
{
    public int Id { get; set; } = c.Id;
    public string Hashtag { get; set; } = c.Description;
    public int Uses { get; set; } = c.Quantity;
};