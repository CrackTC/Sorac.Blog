namespace Sorac.Blog.Models;

internal class GitState
{
    public required int Id { get; set; }
    public required string? LastCommitSha { get; set; }
}
