using System.ComponentModel.DataAnnotations;

namespace Sorac.Blog.Models;

public class Article
{
    public int Id { get; set; }

    public required string Directory { get; set; }
    public required string Name { get; set; }

    public string? Title { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime LastUpdated { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; }
}
