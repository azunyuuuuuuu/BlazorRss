using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlazorRss.App.Models;
using BlazorRss.Shared.Models;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.Services;
using Microsoft.EntityFrameworkCore;

namespace BlazorRss.App.Components
{
    public class ArticleViewBase : BlazorComponent
    {
        // Injected Properties
        [Inject] protected ApplicationDbContext _context { get; set; }
        [Inject] protected IUriHelper _uriHelper { get; set; }

        // Constructor
        public ArticleViewBase() : base()
        {

        }

        // Parameters
        [Parameter] public Guid ArticleId { get; set; }

        public Article _article { get; private set; }

        protected override async Task OnParametersSetAsync()
        {
            if (ArticleId != null && ArticleId != Guid.Empty)
            {
                _article = await _context.Articles
                    .AsNoTracking()
                    .SingleAsync(x => x.ArticleId == ArticleId);
            }
        }
    }
}
