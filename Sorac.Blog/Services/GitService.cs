using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sorac.Blog.Data;
using Sorac.Blog.Models;
using System.Text.RegularExpressions;

namespace Sorac.Blog.Services;

internal partial class GitService(
    IServiceScopeFactory scopeFactory,
    IOptions<BlogConfig> config,
    ILogger<GitService> logger
) : IHostedService
{
    private Repository _repo = null!;


    [GeneratedRegex(@"\.md$", RegexOptions.IgnoreCase)]
    private partial Regex ArticleRegex { get; }

    private async Task<Article?> GetArticleByPathAsync(BlogDbContext dbContext, string path, CancellationToken ct)
    {
        await dbContext.SaveChangesAsync(ct);
        var (dir, name) = (Path.GetDirectoryName(path)!, Path.GetFileName(path)!);
        if (await dbContext.Articles.SingleOrDefaultAsync(it => it.Directory == dir && it.Name == name, ct) is { } article)
            return article;

        logger.LogWarning("Article {Path} not found in the database.", path);
        return null;
    }

    private async Task RollChangeAsync(BlogDbContext dbContext, TreeEntryChanges change, DateTime? time, CancellationToken ct)
    {
        switch (change.Status)
        {
            case ChangeKind.Added:
            case ChangeKind.Copied:
                {
                    logger.LogInformation("Added {Path} at {Time}", change.Path, time);
                    if (!ArticleRegex.IsMatch(change.Path)) return;
                    await dbContext.AddAsync(new Article
                    {
                        Directory = Path.GetDirectoryName(change.Path)!,
                        Name = Path.GetFileName(change.Path)!,
                        LastUpdated = time!.Value,
                        CreatedAt = time!.Value,
                    }, ct);
                    break;
                }
            case ChangeKind.Deleted:
            case ChangeKind.Ignored:
                {
                    logger.LogInformation("Deleted {Path} at {Time}", change.OldPath, time);
                    if (!ArticleRegex.IsMatch(change.OldPath)) return;
                    if (await GetArticleByPathAsync(dbContext, change.OldPath, ct) is { } article)
                        dbContext.Remove(article);
                    break;
                }
            case ChangeKind.Modified:
                {
                    logger.LogInformation("Modified {Path} at {Time}", change.Path, time);
                    if (!ArticleRegex.IsMatch(change.Path)) return;
                    if (await GetArticleByPathAsync(dbContext, change.OldPath, ct) is { } article)
                        article.LastUpdated = time!.Value;
                    break;
                }
            case ChangeKind.Renamed:
                {
                    logger.LogInformation("Renamed {Path} to {NewPath} at {Time}", change.OldPath, change.Path, time);
                    if (!ArticleRegex.IsMatch(change.OldPath) || !ArticleRegex.IsMatch(change.Path)) return;
                    if (await GetArticleByPathAsync(dbContext, change.OldPath, ct) is { } article)
                    {
                        article.Directory = Path.GetDirectoryName(change.Path)!;
                        article.Name = Path.GetFileName(change.Path)!;
                    }
                    break;
                }
            default:
                logger.LogWarning("Unknown change type {ChangeType} for {Path} at {Time}", change.Status, change.Path, time);
                break;
        }
    }

    private async Task RollCommitAsync(BlogDbContext dbContext, Commit? srcCommit, Commit? dstCommit, CancellationToken ct)
    {
        var time = dstCommit?.Author.When.DateTime;
        var changes = _repo.Diff.Compare<TreeChanges>(srcCommit?.Tree, dstCommit?.Tree, new CompareOptions
        {
            Similarity = new SimilarityOptions
            {
                RenameDetectionMode = RenameDetectionMode.Renames,
                WhitespaceMode = WhitespaceMode.IgnoreAllWhitespace,
                RenameLimit = int.MaxValue
            }
        });
        foreach (var change in changes)
            await RollChangeAsync(dbContext, change, time, ct);
    }

    private async Task RollAsync(BlogDbContext dbContext, Commit? oldCommit, bool forward = true, CancellationToken ct = default)
    {
        logger.LogInformation("Rolling {direction}...", forward ? "forward" : "back");

        var branch = _repo.Branches[config.Value.GitLocalBranchName];
        var commits = branch.Commits.TakeWhile(it => it.Sha != oldCommit?.Sha).Append(oldCommit).ToList().AsEnumerable();
        if (forward) commits = commits.Reverse();

        var srcCommit = commits.First();

        foreach (var dstCommit in commits.Skip(1))
        {
            await RollCommitAsync(dbContext, srcCommit, dstCommit, ct);
            srcCommit = dstCommit;
        }

        var newState = new GitState { Id = 1, LastCommitSha = srcCommit?.Sha };
        if (await dbContext.GitStates.SingleOrDefaultAsync(it => it.Id == newState.Id, ct) is { } oldState)
            dbContext.Entry(oldState).CurrentValues.SetValues(newState);
        else
            await dbContext.GitStates.AddAsync(newState, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    private Task RollForwardAsync(BlogDbContext dbContext, Commit? oldCommit, CancellationToken ct) => RollAsync(dbContext, oldCommit, forward: true, ct);
    private Task RollBackAsync(BlogDbContext dbContext, Commit? oldCommit, CancellationToken ct) => RollAsync(dbContext, oldCommit, forward: false, ct);

    private async Task FetchAsync(CancellationToken ct)
    {
        var remote = _repo.Network.Remotes[config.Value.GitRemoteName];
        var refSpecs = remote.FetchRefSpecs.Select(it => it.Specification);
        var fetchOptions = new FetchOptions
        {
            Prune = true,
            OnProgress = _ => !ct.IsCancellationRequested
        };
        await Task.Run(() => Commands.Fetch(_repo, remote.Name, refSpecs, fetchOptions, logMessage: null), ct);
    }

    private async Task CloneAsync(CancellationToken ct)
    {
        var cloneOptions = new CloneOptions(
            new FetchOptions { OnProgress = _ => !ct.IsCancellationRequested }
        );
        await Task.Run(() => Repository.Clone(config.Value.GitRemoteUrl, config.Value.GitLocalPath, cloneOptions));
    }

    public async Task PullAsync(BlogDbContext dbContext, CancellationToken ct)
    {
        logger.LogInformation("Fetching...");
        await FetchAsync(ct);

        logger.LogInformation("Merging...");
        var remoteBranch = _repo.Branches[config.Value.GitRemoteBranchName];
        var localBranch = _repo.Branches[config.Value.GitLocalBranchName];

        var mergeBase = _repo.ObjectDatabase.FindMergeBase(
            remoteBranch.Tip,
            localBranch.Tip
        );

        Commands.Checkout(_repo, localBranch);

        await RollBackAsync(dbContext, mergeBase, ct);

        _repo.Reset(ResetMode.Hard, mergeBase);
        if (_repo.Merge(
                remoteBranch,
                new Signature("Sorac.Blog", "blog@sora.zip", DateTimeOffset.UnixEpoch),
                new() { FastForwardStrategy = FastForwardStrategy.FastForwardOnly }
            ).Status is MergeStatus.Conflicts)
        {
            logger.LogWarning("Merge conflicts detected. Please resolve them manually.");
            return;
        }

        await RollForwardAsync(dbContext, mergeBase, ct);
    }

    private async Task GenerateTitlesAsync(BlogDbContext dbContext, CancellationToken ct)
    {
        foreach (var article in dbContext.Articles.Where(it => it.Title == null))
        {
            var path = Path.Join(config.Value.GitLocalPath, article.Directory, article.Name);

            if (!File.Exists(path))
            {
                logger.LogWarning("Article {Path} not found in the file system.", path);
                continue;
            }

            using var reader = File.OpenText(path);
            while (await reader.ReadLineAsync(ct) is { } line)
            {
                if (line.StartsWith("# "))
                {
                    article.Title = line[2..];
                    break;
                }

                if (!string.IsNullOrWhiteSpace(line))
                {
                    logger.LogWarning("Title not found in {Path}.", path);
                    break;
                }
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }

    public async Task StartAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

        await dbContext.Database.MigrateAsync(ct);

        if (!Repository.IsValid(config.Value.GitLocalPath))
            await CloneAsync(ct);

        _repo = new Repository(config.Value.GitLocalPath);
        Commands.Checkout(_repo, _repo.Branches[config.Value.GitLocalBranchName]);

        if (dbContext.GitStates.AsNoTracking().SingleOrDefault() is not { LastCommitSha: { } oldCommitHash })
            oldCommitHash = null;

        var oldCommit = oldCommitHash is not null ? _repo.Lookup<Commit>(oldCommitHash) : null;
        await RollForwardAsync(dbContext, oldCommit, ct);
        await GenerateTitlesAsync(dbContext, ct);
    }

    public Task StopAsync(CancellationToken ct)
    {
        _repo.Dispose();
        return Task.CompletedTask;
    }
}
