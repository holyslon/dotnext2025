using Microsoft.EntityFrameworkCore;

namespace NetworkingBot.Infrastructure;

internal class MigrationService(IServiceProvider serviceProvider, ILogger<MigrationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var serviceScope = serviceProvider.CreateScope();
            await serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database
                .MigrateAsync(stoppingToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Fail to migrate database");
            throw new ApplicationException("Fail to migrate database", e);
        }
    }
}