using Microsoft.Extensions.DependencyInjection;

namespace ComputerRepairSystem.customer.Services;

public class SyncBackgroundService
{
    private readonly IServiceProvider _services;

    public SyncBackgroundService(IServiceProvider services)
    {
        _services = services;
    }

    public async Task RunOnceAsync()
    {
        using var scope = _services.CreateScope();

        var syncService =
            scope.ServiceProvider
                .GetRequiredService<SyncService>();

        await syncService.SyncPendingAsync();
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"SYNC ERROR: {ex.Message}");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(30),
                cancellationToken);
        }
    }
}