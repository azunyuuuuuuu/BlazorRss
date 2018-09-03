using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using BlazorRss.App.Models;
using BlazorRss.Shared.Models;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlazorRss.App.Pages.Manage
{
    public class ManageFeedsBase : BlazorComponent
    {
        // Injected Properties
        [Inject] protected ApplicationDbContext _context { get; set; }
        [Inject] protected IUriHelper _uriHelper { get; set; }
        [Inject] protected ILogger<ManageFeedsBase> _logger { get; set; }

        // Constructor
        public ManageFeedsBase() : base()
        {
        }

        // Data Containers
        public IReadOnlyList<Feed> feeds { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            await LoadData();
        }

        public async Task LoadData()
        {
            feeds = await _context.Feeds
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        public string NewFeedUrl { get; set; }
        public async Task ActionAddFeed()
        {
            if (string.IsNullOrWhiteSpace(NewFeedUrl))
                return;

            if (!Uri.IsWellFormedUriString(NewFeedUrl, UriKind.Absolute))
                return; // TODO: Implement proper Url checking

            var uri = new Uri(NewFeedUrl);
            _context.Feeds.Add(new Feed
            {
                Url = uri.ToString(),

                DateAdded = DateTimeOffset.Now,
                RefreshInterval = TimeSpan.FromMinutes(10),

                ParserMode = ParserMode.SmartReader
            });
            await _context.SaveChangesAsync();
            NewFeedUrl = "";

            await LoadData();
        }

        public async Task ActionRemoveFeed(Feed feed)
        {
            _context.Articles.RemoveRange(_context.Articles.Where(x => x.Feed == feed));
            _context.Feeds.Remove(feed);

            await _context.SaveChangesAsync();

            await LoadData();
        }

        public void ActionEditFeed(Feed feed)
        {
            _uriHelper.NavigateTo($"/manage/feeds/{feed.FeedId}");
        }

        public string OpmlInput { get; set; }
        public async Task ActionImportOpml()
        {
            var document = XDocument.Parse(OpmlInput);
            var elements = document.Element("opml").Element("body").Elements("outline");

            await ParseOutlineElements(elements);
            OpmlInput = "";

            await LoadData();
        }

        private async Task ParseOutlineElements(IEnumerable<XElement> elements, Category category = null)
        {
            foreach (var element in elements)
            {
                try
                {
                    switch (element.Attribute("type")?.Value)
                    {
                        case "rss":
                            if (0 < await _context.Feeds
                                .AsNoTracking()
                                .Where(x => x.Url == element.Attribute("xmlUrl").Value)
                                .CountAsync())
                                break;

                            var feed = new Feed
                            {
                                Url = element.Attribute("xmlUrl").Value,
                                Name = element.Attribute("text").Value,

                                DateAdded = DateTimeOffset.UtcNow,
                                RefreshInterval = TimeSpan.FromMinutes(10),

                                Category = category,

                                ParserMode = ParserMode.SmartReader
                            };

                            await _context.Feeds.AddAsync(feed);
                            await _context.SaveChangesAsync();

                            break;

                        default:
                            if (0 < await _context.Categories
                                .AsNoTracking()
                                .Where(x => x.Name == element.Attribute("text").Value)
                                .CountAsync())
                            {
                                await ParseOutlineElements(element.Elements("outline"),
                                    await _context.Categories.SingleAsync(x => x.Name == element.Attribute("text").Value));
                            }
                            else
                            {
                                var _category = new Category
                                {
                                    Name = element.Attribute("text").Value
                                };
                                await _context.Categories.AddAsync(_category);
                                await _context.SaveChangesAsync();

                                await ParseOutlineElements(element.Elements("outline"), _category);
                            }

                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while parsing OPML data");
                }
            }
        }
    }
}
