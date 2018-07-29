using System;
using System.Collections.Generic;

namespace BlazorRss.Shared.Models
{
    public class Category
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; }

        public List<Feed> Feeds { get; set; }
    }
}
