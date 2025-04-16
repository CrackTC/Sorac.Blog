namespace Sorac.Blog;

public class BlogConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public required string GitRemoteUrl { get; set; }
    public string GitLocalPath { get; set; } = "blog";
    public string GitRemoteName { get; set; } = "origin";
    public string GitRemoteBranchName { get; set; } = "origin/main";
    public string GitLocalBranchName { get; set; } = "main";
};
