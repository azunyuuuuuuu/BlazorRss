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
using System.Net.Http;
using Microsoft.SyndicationFeed;

namespace BlazorRss.App.Services
{
    public class FeedRefreshService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<FeedRefreshService> _logger;
        private readonly HttpClient _client;

        public FeedRefreshService(IServiceProvider services, ILogger<FeedRefreshService> logger, IHttpClientFactory clientFactory)
        {
            _services = services;
            _logger = logger;
            _client = clientFactory.CreateClient();
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
                        await RefreshAllFeedsAsync(context);
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

        private async Task RefreshAllFeedsAsync(ApplicationDbContext context)
        {
            var feeds = await context.Feeds
                .Include(x => x.Articles)
                .ToListAsync();

            _logger.LogInformation($"Refreshing {feeds.Count} feeds");

            foreach (var feed in feeds)
                await RefreshSingleFeedAsync(context, feed);
        }

        private async Task RefreshSingleFeedAsync(ApplicationDbContext context, Feed feed)
        {
            if (DateTimeOffset.Now < feed.DateLastUpdate + feed.RefreshInterval)
                return;

            try
            {
                _logger.LogInformation($"Refreshing feed {feed.Name}");

                var response = await _client.GetAsync(feed.Url);

                using (var xmlreader = System.Xml.XmlReader.Create(await response.Content.ReadAsStreamAsync()))
                {
                    while (xmlreader.Read())
                    {
                        if (xmlreader.NodeType != System.Xml.XmlNodeType.Element)
                            continue;
                        break;
                    }

                    ISyndicationFeedReader feedreader;

                    switch (xmlreader.Name)
                    {
                        case "feed":
                            feedreader = new Microsoft.SyndicationFeed.Atom.AtomFeedReader(xmlreader);
                            break;

                        case "rss":
                            feedreader = new Microsoft.SyndicationFeed.Rss.RssFeedReader(xmlreader);
                            break;

                        default:
                            throw new Exception($"Root element of {xmlreader.Name} could not be recognized as a valid feed type.");
                    }

                    while (await feedreader.Read())
                    {
                        switch (feedreader.ElementType)
                        {
                            case SyndicationElementType.Item:
                                var item = await feedreader.ReadItem();

                                if (feed.Articles.Where(x => x.UniqueId == item.Id && x.DateUpdated == item.LastUpdated).Count() > 0)
                                    break;

                                var article = new Article
                                {
                                    Feed = feed,
                                    UniqueId = item.Id,
                                    Title = item.Title,
                                    Author = item.Contributors.FirstOrDefault()?.Name,
                                    Description = item.Description,
                                    Tags = string.Join(", ", item.Categories.Select(x=>x.Name)),
                                    ArticleUrl = item.Links.First().Uri.ToString(), // TODO: change this to something proper
                                    DatePublished = item.Published,
                                    DateUpdated = item.LastUpdated
                                };

                                context.Articles.Add(article);

                                await context.SaveChangesAsync();

                                break;

                            default:
                                var content = await feedreader.ReadContent();
                                break;
                        }
                    }

                    feed.DateLastUpdate = DateTimeOffset.UtcNow;
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while refreshing \"{feed.Name}\"");
            }
        }
    }
}
