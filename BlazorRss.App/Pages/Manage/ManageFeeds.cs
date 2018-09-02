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

namespace BlazorRss.App.Pages.Manage
{
    public class ManageFeedsBase : BlazorComponent
    {
        // Injected Properties
        [Inject] protected ApplicationDbContext _context { get; set; }
        [Inject] protected IUriHelper _uriHelper { get; set; }

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
                RefreshInterval = TimeSpan.FromMinutes(10)
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
                switch (element.Attribute("type")?.Value)
                {
                    case "rss":
                        var feed = await _context.Feeds
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x => x.Url == element.Attribute("xmlUrl").Value);

                        if (feed != null)
                            break;

                        feed = new Feed
                        {
                            Name = element.Attribute("text").Value,
                            Url = element.Attribute("xmlUrl").Value,
                            RefreshInterval = TimeSpan.FromMinutes(10),
                            DateAdded = DateTimeOffset.UtcNow,
                            Category = category
                        };

                        await _context.Feeds.AddAsync(feed);
                        await _context.SaveChangesAsync();

                        break;

                    default:
                        category = _context.Categories
                            .AsNoTracking()
                            .FirstOrDefault(x => x.Name == element.Attribute("text").Value);

                        if (category == null)
                        {
                            category = new Category { Name = element.Attribute("text").Value };
                            await _context.Categories.AddAsync(category);
                            await _context.SaveChangesAsync();
                        }

                        await ParseOutlineElements(element.Elements("outline"), category);

                        break;
                }
        }
    }
}
