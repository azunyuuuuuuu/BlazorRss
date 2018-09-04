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
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;

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

                await Task.Delay(TimeSpan.FromMinutes(1));
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

            try
            {
                switch (feedreader.ElementType)
                {
                    case SyndicationElementType.Item:
                        var item = await feedreader.ReadItem();
                        var itemidentifier = item.Links.FirstOrDefault().Uri.AbsoluteUri;

                        if (await context.Articles
                            .Where(x => x.UniqueId == itemidentifier)
                            .AsNoTracking()
                            .CountAsync() > 0)
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
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while refreshing \"{feed.Name}\"");
            }
        }

        private async Task PopulateAllArticleContent(ApplicationDbContext context)
        {
            var articles = await context.Articles
                .Where(x => x.RawContent == string.Empty || x.Content == string.Empty)
                .Include(x => x.Feed)
                .OrderByDescending(x => x.DateAdded)
                .AsTracking()
                .Take(250)
                .ToListAsync();
            _logger.LogDebug($"Downloading article content for {articles.Count} articles.");

            foreach (var article in articles)
            {
                _logger.LogDebug($"Downloading article {article.UniqueId}");

                await DownloadArticleRawContent(context, article);
                await ProcessArticleRawContent(context, article);
            }
        }

        private async Task ProcessArticleRawContent(ApplicationDbContext context, Article article)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(article.Content))
                    return;

                var converter = new ReverseMarkdown.Converter();

                switch (article.Feed.ParserMode)
                {
                    default:
                    case ParserMode.SmartReader:
                        ProcessArticleRawContentSmartReader(article, converter);
                        break;

                    case ParserMode.CssSelector:
                        ProcessArticleRawContentCssSelector(article, converter);
                        break;

                    case ParserMode.XPathSelector:
                        ProcessArticleRawContentXPathSelector(article, converter);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while processing {article.ArticleUrl}");
                article.Content = $"# Error\n\n{ex.ToString()}";
            }
            finally
            {
                await context.SaveChangesAsync();
            }
        }

        private static void ProcessArticleRawContentXPathSelector(Article article, ReverseMarkdown.Converter converter)
        {
            var html = new HtmlDocument();
            html.LoadHtml(article.RawContent);
            var document = html.DocumentNode;

            article.Title = document.SelectSingleNode(article.Feed.ParserTitle)?.InnerText;
            article.Description = document.SelectSingleNode(article.Feed.ParserDescription)?.InnerText;
            article.Content = converter.Convert(
                string.Join("", document.SelectNodes(article.Feed.ParserContent)
                    ?.Select(x => x.OuterHtml))
                );
            article.Author = document.SelectSingleNode(article.Feed.ParserAuthor)?.InnerText;
            article.Tags = string.Join(", ", document.SelectNodes(article.Feed.ParserTags)?.Select(x => x.InnerText));
        }

        private static void ProcessArticleRawContentCssSelector(Article article, ReverseMarkdown.Converter converter)
        {
            var html = new HtmlDocument();
            html.LoadHtml(article.RawContent);
            var document = html.DocumentNode;

            article.Title = document.QuerySelector(article.Feed.ParserTitle)?.InnerText;
            article.Description = document.QuerySelector(article.Feed.ParserDescription)?.InnerText;
            article.Content = converter.Convert(document.QuerySelector(article.Feed.ParserContent)?.InnerHtml);
            var temp4 = document.QuerySelector(article.Feed.ParserAuthor);
            var temp5 = document.QuerySelector(article.Feed.ParserTags);
        }

        private void ProcessArticleRawContentSmartReader(Article article, ReverseMarkdown.Converter converter)
        {
            _logger.LogTrace($"Parsing article content for {article.ArticleUrl}");

            var parsedarticle = SmartReader.Reader.ParseArticle(article.ArticleUrl, article.RawContent);

            article.Content = converter.Convert(parsedarticle.IsReadable ? parsedarticle.Content : article.Description);
            if (string.IsNullOrWhiteSpace(article.Content))
                article.Content = "###### no content";
        }

        private async Task DownloadArticleRawContent(ApplicationDbContext context, Article article)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(article.RawContent))
                    return;
                _logger.LogTrace($"Downloading raw article from {article.ArticleUrl}");

                article.RawContent = await _client.GetStringAsync(article.ArticleUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while downloading {article.ArticleUrl}");
                article.RawContent = $"# Error\n\n{ex.ToString()}";
            }
            finally
            {
                await context.SaveChangesAsync();
            }
        }

        private Article CreateArticleFromItem(Feed feed, ISyndicationItem item, string itemidentifier)
            => new Article
            {
                Feed = feed,
                UniqueId = itemidentifier,

                Title = item.Title,
                Author = item.Contributors.FirstOrDefault()?.Name,

                DateAdded = DateTimeOffset.Now,
                DatePublished = item.Published,
                DateUpdated = item.LastUpdated,

                ArticleUrl = item.Links.First()?.Uri.ToString(), // TODO: change this to something proper
                Description = item.Description,
                RawContent = string.Empty,
                Content = string.Empty,
                Tags = string.Join(", ", item.Categories.Select(x => x.Name)),

                Read = false,
                IsDeleted = false,
                IsSponsored = false,
                IsReadable = false
            };

        private void SetFeedNameIfNotSet(Feed feed, ISyndicationContent content)
        {
            if (string.IsNullOrWhiteSpace(feed.Name))
                if (content.Name == "title")
                    feed.Name = content.Value;
        }
    }
}
