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
    public class FeedRefreshService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<FeedRefreshService> _logger;

        public FeedRefreshService(IServiceProvider services, ILogger<FeedRefreshService> logger)
        {
            _services = services;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service started");

            Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                    await DoWork(cancellationToken);
            }, cancellationToken);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service stopped");
            
            return Task.CompletedTask;
        }

        private async Task DoWork(CancellationToken cancellationToken)
        {
            await Task.Delay(1000);

            using (var scope = _services.CreateScope())
            {
                var context = scope.ServiceProvider.GetService<ApplicationDbContext>();
                await RefreshAllFeeds(context);
            }

            _logger.LogDebug("ProcessingLoop completed");
        }

        private async Task RefreshAllFeeds(ApplicationDbContext context)
        {
            var feeds = await context.Feeds.ToListAsync();

            _logger.LogDebug($"Refreshing {feeds.Count} feeds");

            foreach (var feed in feeds)
            {
                await RefreshSingleFeedAsync(feed);
            }
        }

        private async Task RefreshSingleFeedAsync(Feed feed)
        {
            _logger.LogDebug($"Refreshing feed {feed.Name}");

            await Task.Delay(10); // dummy context
        }

        public void Dispose()
        {

        }
    }
}
