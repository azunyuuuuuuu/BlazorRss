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
    }
}
