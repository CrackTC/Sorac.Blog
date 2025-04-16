namespace Sorac.Blog.Models;

public class Friend
{
    public int Id { get; set; }

    public required string Name { get; set; }
    public required string Url { get; set; }
    public required string IconUrl { get; set; }
}
