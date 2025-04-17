
namespace Sorac.Blog.Services;

internal class BootstrapService<T>(T service) : IHostedService where T : IHostedService
{
    public Task StartAsync(CancellationToken ct) => service.StartAsync(ct);
    public Task StopAsync(CancellationToken ct) => service.StopAsync(ct);
}
