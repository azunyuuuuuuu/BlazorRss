using System;
using System.Threading.Tasks;
using BlazorRss.App.Models;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlazorRss.Server
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = BuildWebHost(args);

            using (var scope = host.Services.CreateScope())
                await ExecuteAditionalServiceSetupAsync(scope);

            host.Run();
        }

        private static async Task ExecuteAditionalServiceSetupAsync(IServiceScope scope)
        {
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<Program>>();

            await SeedWithSampleDataAsync(services.GetRequiredService<ApplicationDbContext>(), logger);
        }

        private static async Task SeedWithSampleDataAsync(ApplicationDbContext context, ILogger<Program> logger)
        {
            try
            {
                await context.Database.EnsureCreatedAsync();

                if ((await context.Feeds.CountAsync()) > 0)
                    return;

                await context.Feeds.AddRangeAsync(
                    new Shared.Models.Feed { Name = "Test Feed 1" },
                    new Shared.Models.Feed { Name = "Test Feed 2" }
                );

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error during database initialization occurred");
            }
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseConfiguration(new ConfigurationBuilder()
                    .AddCommandLine(args)
                    .Build())
                .UseStartup<Startup>()
                .UseUrls("http://*:5000/")
                .Build();
    }
}
