using Sorac.Blog.Models;
using Microsoft.EntityFrameworkCore;

namespace Sorac.Blog.Data;

internal class BlogDbContext(DbContextOptions<BlogDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Article>().HasIndex(it => new { it.Directory, it.Name }).IsUnique();
        builder.Entity<Article>().HasIndex(it => it.LastUpdated);
    }


    public DbSet<GitState> GitStates { get; set; } = default!;
    public DbSet<Article> Articles { get; set; } = default!;
    public DbSet<Friend> Friends { get; set; } = default!;
}
