using System.ComponentModel.DataAnnotations;

namespace Sorac.Blog.Models;

public class Article
{
    public int Id { get; set; }

    public required string Directory { get; set; }
    public required string Name { get; set; }

    [DataType(DataType.Date)]
    public DateTime LastUpdated { get; set; }
}
