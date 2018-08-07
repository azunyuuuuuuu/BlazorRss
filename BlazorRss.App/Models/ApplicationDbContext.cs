using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlazorRss.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorRss.App.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Feed> Feeds { get; set; }
        public DbSet<Article> Articles { get; set; }

        public async Task<List<Feed>> GetAllFeedsAsync()
            => await Feeds
                .Include(x => x.Category)
                .ToListAsync();


        public async Task<List<Category>> GetAllCategoriesAsync()
            => await Categories
                .Include(x => x.Feeds)
                .ThenInclude(x => x.Articles)
                .ToListAsync();


        public async Task<Feed> GetFeed(Guid id)
            => await Feeds
                .FindAsync(id);

    }
}
