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

        public DbSet<Feed> Feeds { get; set; }

        public async Task<List<Feed>> GetAllFeedsAsync()
        {
            return await Feeds
                .Include(x => x.Category)
                .ToListAsync();
        }
    }
}
