using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlazorRss.App.Models;
using BlazorRss.Shared.Models;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.Services;

namespace BlazorRss.App.Pages.Main
{
    public class FeedsBase : BlazorComponent
    {
        // Injected Properties
        [Inject] protected ApplicationDbContext _context { get; set; }
        [Inject] protected IUriHelper _uriHelper { get; set; }

        // Constructor
        public FeedsBase() : base()
        {

        }

        // Data Containers
        public IReadOnlyList<Category> Categories { get; private set; }
        public IReadOnlyList<Article> Articles { get; private set; }

        // Parameters
        [Parameter] public Guid FeedId { get; set; }
        [Parameter] public Guid ArticleId { get; set; }

        public Feed _feed { get; private set; }
        public Article _article { get; private set; }

        protected override async Task OnInitAsync()
        {
            Categories = await _context.GetAllCategoriesAsync();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (FeedId != null && FeedId != Guid.Empty)
            {
                _feed = await _context.GetFeed(FeedId);
                Articles = await _context.GetArticlesForFeed(FeedId);
            }

            if (ArticleId != null && ArticleId != Guid.Empty)
            {
                _article = await _context.Articles.FindAsync(ArticleId);
            }
        }

        public void NavigateToFeed(Guid feedid)
        {
            _feed = null;
            _article = null;
            FeedId = Guid.Empty;
            ArticleId = Guid.Empty;

            _uriHelper.NavigateTo($"/feeds/{feedid}");
        }

        public void NavigateToArticle(Guid feedid, Guid articleid)
        {
            // _feed = null;
            // _article = null;
            // FeedId = Guid.Empty;
            // ArticleId = Guid.Empty;

            _uriHelper.NavigateTo($"/feeds/{feedid}/{articleid}");
        }
    }
}
