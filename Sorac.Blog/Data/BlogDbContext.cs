using Sorac.Blog.Models;
using Microsoft.EntityFrameworkCore;

namespace Sorac.Blog.Data;

internal class BlogDbContext(DbContextOptions<BlogDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        var article = builder.Entity<Article>();
        article.HasIndex(it => new { it.Directory, it.Name }).IsUnique();
        article.HasIndex(it => it.CreatedAt);
    }


    public DbSet<GitState> GitStates { get; set; } = default!;
    public DbSet<Article> Articles { get; set; } = default!;
    public DbSet<Friend> Friends { get; set; } = default!;
}
