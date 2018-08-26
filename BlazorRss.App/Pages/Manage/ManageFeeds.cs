using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            _context.Feeds.Add(new Feed { Url = uri.ToString() });
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
    }
}
