using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlazorRss.App.Models;
using BlazorRss.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorRss.App.Services
{
    public class FeedRefreshService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<FeedRefreshService> _logger;

        public FeedRefreshService(IServiceProvider services, ILogger<FeedRefreshService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("FeedRefreshService is starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetService<ApplicationDbContext>();
                        await RefreshAllFeeds(context);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred");
                }

                await Task.Delay(TimeSpan.FromMinutes(1));
            }

            _logger.LogInformation("FeedRefreshService is stopping");
        }

        private async Task RefreshAllFeeds(ApplicationDbContext context)
        {
            var feeds = await context.Feeds.ToListAsync();

            _logger.LogInformation($"Refreshing {feeds.Count} feeds");

            foreach (var feed in feeds)
                await RefreshSingleFeedAsync(context, feed);
        }

        private async Task RefreshSingleFeedAsync(ApplicationDbContext context, Feed feed)
        {
            _logger.LogInformation($"Refreshing feed {feed.Name}");

            await Task.Delay(10); // dummy context
        }

    }
}
