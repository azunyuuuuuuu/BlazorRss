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
                        await PopulateAllArticleContent(context);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred");
                }

                await Task.Delay(TimeSpan.FromSeconds(15));
            }

            _logger.LogInformation("FeedRefreshService is stopping");
        }

        private async Task RefreshAllFeedsAsync(ApplicationDbContext context)
        {
            var feeds = await context.Feeds
                .Include(x => x.Articles)
                .Where(x => x.DateLastUpdate + x.RefreshInterval <= DateTimeOffset.Now)
                .ToListAsync();

            _logger.LogInformation($"Refreshing {feeds.Count} feeds");

            foreach (var feed in feeds)
                await RefreshSingleFeedAsync(context, feed);
        }

        private async Task RefreshSingleFeedAsync(ApplicationDbContext context, Feed feed)
        {
            try
            {
                _logger.LogInformation($"Refreshing feed {feed.Name}");

                var response = await _client.GetAsync(feed.Url);

                using (var xmlreader = System.Xml.XmlReader.Create(await response.Content.ReadAsStreamAsync()))
                    await ParseFeedStream(context, feed, xmlreader);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while refreshing \"{feed.Name}\"");
            }
        }

        private async Task ParseFeedStream(ApplicationDbContext context, Feed feed, System.Xml.XmlReader xmlreader)
        {
            while (xmlreader.Read())
                if (xmlreader.NodeType == System.Xml.XmlNodeType.Element)
                    break;

            var feedreader = GetFeedReader(xmlreader);

            while (await feedreader.Read())
                await ParseReadElement(context, feed, feedreader);

            feed.DateLastUpdate = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();
        }

        private ISyndicationFeedReader GetFeedReader(System.Xml.XmlReader xmlreader)
        {
            switch (xmlreader.Name)
            {
                case "feed": return new Microsoft.SyndicationFeed.Atom.AtomFeedReader(xmlreader);
                case "rss": return new Microsoft.SyndicationFeed.Rss.RssFeedReader(xmlreader);
                default: throw new Exception($"Root element of {xmlreader.Name} could not be recognized as a valid feed type.");
            }
        }

        private async Task ParseReadElement(ApplicationDbContext context, Feed feed, ISyndicationFeedReader feedreader)
        {
            _logger.LogTrace($"Parsing element {feedreader.ElementName}");

            switch (feedreader.ElementType)
            {
                case SyndicationElementType.Item:
                    var item = await feedreader.ReadItem();
                    var itemidentifier = item.Links.FirstOrDefault().Uri.AbsoluteUri;

                    if (feed.Articles
                        .Where(x => x.UniqueId == itemidentifier)
                        .Count() > 0)
                        break;

                    var article = CreateArticleFromItem(feed, item, itemidentifier);

                    context.Articles.Add(article);

                    await context.SaveChangesAsync();
                    break;

                default:
                    SetFeedNameIfNotSet(feed, await feedreader.ReadContent());
                    break;
            }
        }

        private async Task PopulateAllArticleContent(ApplicationDbContext context)
        {
            var articles = await context.Articles
                .Where(x => x.Content == string.Empty)
                .AsTracking()
                .Take(50)
                .ToListAsync();
            _logger.LogDebug($"Populating article content for {articles.Count} articles.");

            foreach (var article in articles)
            {
                _logger.LogDebug($"Populating article {article.UniqueId}");

                await ExtendArticleWithSmartReader(article);
                await context.SaveChangesAsync();
            }
        }

        private async Task ExtendArticleWithSmartReader(Article article)
        {
            try
            {
                var parsedarticlepage = await SmartReader.Reader.ParseArticleAsync(article.ArticleUrl);
                var converter = new ReverseMarkdown.Converter();

                article.Content = converter.Convert(parsedarticlepage.IsReadable ? parsedarticlepage.Content : article.Description);
                // article.Description = parsedarticlepage.Excerpt;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An Error occurred while parsing the article {article.UniqueId}");
                article.Content = $"# error \n\n{ex.ToString()}";
            }
        }

        private Article CreateArticleFromItem(Feed feed, ISyndicationItem item, string itemidentifier)
            => new Article
            {
                Feed = feed,
                UniqueId = itemidentifier,
                // UniqueId = item.Id,
                Title = item.Title,
                Author = item.Contributors.FirstOrDefault()?.Name,
                Description = item.Description,
                Tags = string.Join(", ", item.Categories.Select(x => x.Name)),
                ArticleUrl = item.Links.First()?.Uri.ToString(), // TODO: change this to something proper
                DateAdded = DateTimeOffset.Now,
                DatePublished = item.Published,
                DateUpdated = item.LastUpdated,
                Content = string.Empty
            };

        private void SetFeedNameIfNotSet(Feed feed, ISyndicationContent content)
        {
            if (string.IsNullOrWhiteSpace(feed.Name))
                if (content.Name == "title")
                    feed.Name = content.Value;
        }
    }
}
