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
    public class ManageSingleFeedBase : BlazorComponent
    {
        // Injected Properties
        [Inject] protected ApplicationDbContext _context { get; set; }
        [Inject] protected IUriHelper _uriHelper { get; set; }

        // Constructor
        public ManageSingleFeedBase() : base()
        {
        }

        // Data Containers
        public Feed feed { get; set; }
        public IReadOnlyList<Category> categories { get; set; }

        // Parameters
        [Parameter] public Guid FeedId { get; set; }

        public string categoryid { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            await LoadData();
        }

        public async Task LoadData()
        {
            feed = await _context.Feeds
                .Include(x => x.Category)
                .SingleOrDefaultAsync(x => x.FeedId == FeedId);

            if (feed.Category != null)
                categoryid = feed.Category.CategoryId.ToString();

            categories = await _context.Categories
                .ToListAsync();
        }

        public async Task SaveData()
        {
            if (categoryid != null)
                feed.Category = categories.Single(x => x.CategoryId == Guid.Parse(categoryid));

            _context.Update(feed);

            await _context.SaveChangesAsync();
        }

        public async Task CleanArticleContents()
        {
            foreach (var article in await _context.Articles
                .Include(x => x.Feed)
                .Where(x => x.Feed == feed)
                .ToListAsync())
                article.Content = string.Empty;

            await _context.SaveChangesAsync();
        }
    }
}
