using System;
using System.Collections.Generic;

namespace BlazorRss.Shared.Models
{
    public class Feed
    {
        public Guid FeedId { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }

        public Category Category { get; set; }

        public DateTimeOffset DateAdded { get; set; }
        public DateTimeOffset DateModified { get; set; }
        public DateTimeOffset DateLastUpdate { get; set; }
        public TimeSpan RefreshInterval { get; set; }

        public List<Article> Articles { get; set; }
    }
}
