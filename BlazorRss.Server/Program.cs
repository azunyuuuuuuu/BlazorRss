using System;
using System.Collections.Generic;
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

                if ((await context.Categories.CountAsync()) > 0)
                    return;

                await context.Categories.AddRangeAsync(
                    new Shared.Models.Category
                    {
                        Name = "Test Category",
                        Feeds = new List<Shared.Models.Feed>
                        {
                            new Shared.Models.Feed {
                                Name = "YouTube: LiveOverflow channel feed",
                                Url = "https://www.youtube.com/feeds/videos.xml?channel_id=UClcE-kVhqyiHCcjYwcpfj9w",
                                RefreshInterval = TimeSpan.FromSeconds(10),
                                DateAdded = DateTimeOffset.UtcNow
                                },
                            new Shared.Models.Feed {
                                Name = "Ars Technica",
                                Url = "http://feeds.arstechnica.com/arstechnica/index/",
                                RefreshInterval = TimeSpan.FromSeconds(10),
                                DateAdded = DateTimeOffset.UtcNow
                                },
                            new Shared.Models.Feed {
                                Name = "Neowin",
                                Url = "http://feeds.feedburner.com/neowin-main",
                                RefreshInterval = TimeSpan.FromSeconds(10),
                                DateAdded = DateTimeOffset.UtcNow
                                }
                        }
                    }
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
                    .AddJsonFile("appsettings.json", false)
                    // .AddJsonFile("appsettings.json", true)
                    .Build())
                .UseStartup<Startup>()
                .UseUrls("http://*:5000/")
                .Build();
    }
}
