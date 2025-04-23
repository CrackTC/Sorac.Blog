namespace Sorac.Blog;

public class BlogConfig
{
    public string ApiKey { get; init; } = string.Empty;
    public required string GitRemoteUrl { get; init; }
    public string GitLocalPath { get; init; } = "blog";
    public string GitRemoteName { get; init; } = "origin";
    public string GitRemoteBranchName { get; init; } = "origin/main";
    public string GitLocalBranchName { get; init; } = "main";

    public int ArticlesPerPage { get; init; } = 30;
};
