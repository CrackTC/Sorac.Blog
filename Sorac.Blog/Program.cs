using Microsoft.Extensions.Logging.Console;
using Microsoft.EntityFrameworkCore;
using Sorac.Blog.Data;
using Sorac.Blog.Components;
using Sorac.Blog;
using Sorac.Blog.Services;
using Markdig;
using Markdig.Extensions.EmphasisExtras;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services
    .AddDbContext<BlogDbContext>(options =>
    {
        options.UseSqlite(builder.Configuration.GetConnectionString(nameof(BlogDbContext)));
    })
    .AddHostedService<GitService>()
    .Configure<BlogConfig>(builder.Configuration.GetRequiredSection("Blog"))
    .AddJSComponents()
    .AddSingleton(
        new MarkdownPipelineBuilder()
            .UseAutoLinks()
            .UsePipeTables()
            .UseBootstrap()
            .UseEmphasisExtras(EmphasisExtraOptions.Strikethrough)
            .UseMathematics()
            .Build()
    );

builder.Logging.AddSimpleConsole(option =>
{
    option.IncludeScopes = true;
    option.TimestampFormat = "MM-dd HH:mm:ss ";
    option.ColorBehavior = LoggerColorBehavior.Enabled;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Sorac.Blog.Client._Imports).Assembly);

var path = Path.Combine(
    Directory.GetCurrentDirectory(),
    app.Services.GetRequiredService<IOptions<BlogConfig>>().Value.GitLocalPath
);

if (!Directory.Exists(path))
{
    Directory.CreateDirectory(path);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(path),
    RequestPath = "/static"
});

app.MapGet("/api/update", async (
    [FromQuery(Name = "api_key")] string apiKey,
    IOptions<BlogConfig> config,
    BlogDbContext context,
    [FromServices] GitService gitService,
    CancellationToken ct) =>
{
    if (string.Equals(apiKey, config.Value.ApiKey))
        await gitService.PullAsync(context, ct);
    return Results.Ok();
});

app.Run();
