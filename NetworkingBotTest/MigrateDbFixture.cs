using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetworkingBot;
using NetworkingBot.Infrastructure;

namespace NetworkBotTest;

public class MigrateDbFixture 
{


    public MigrateDbFixture()
    {
        var networkingBot = new ServiceCollection()
            .AddLogging(opts=>opts.AddSimpleConsole())
            .AddNetworkingBot("Server=127.0.0.1;Port=54321;Userid=postgres;Password=example");

        using var serviceProvider = networkingBot
            .BuildServiceProvider();
        using var serviceScope = serviceProvider.CreateScope();
        serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();
    }
}