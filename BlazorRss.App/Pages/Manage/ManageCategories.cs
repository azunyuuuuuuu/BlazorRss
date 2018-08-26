using System.Collections.Generic;
using System.Threading.Tasks;
using BlazorRss.App.Models;
using BlazorRss.Shared.Models;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.EntityFrameworkCore;

namespace BlazorRss.App.Pages.Manage
{
    public class ManageCategoriesBase : BlazorComponent
    {
        // Injected Properties
        [Inject] protected ApplicationDbContext _context { get; set; }

        // Constructor
        public ManageCategoriesBase() : base()
        {
        }

        // Data Containers
        public IReadOnlyList<Category> categories { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            await LoadData();
        }

        public async Task LoadData()
        {
            categories = await _context.Categories
                .AsNoTracking()
                .ToListAsync();
        }

        public string NewCategoryName { get; set; }
        public async Task ActionAddCategory()
        {
            if (string.IsNullOrWhiteSpace(NewCategoryName))
                return;

            _context.Categories.Add(new Category { Name = NewCategoryName });
            await _context.SaveChangesAsync();
            NewCategoryName = "";

            await LoadData();
        }

        public async Task ActionRemoveCategory(Category category)
        {
            _context.Categories.Remove(category);

            await _context.SaveChangesAsync();

            await LoadData();
        }
    }
}
