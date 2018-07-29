using System;

namespace BlazorRss.Shared.Models
{
    public class Feed
    {
        public Guid FeedId { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }

        public Category Category { get; set; }

        public DateTimeOffset TimeAdded { get; set; }
        public DateTimeOffset TimeModified { get; set; }
        public DateTimeOffset TimeLastUpdated { get; set; }
    }
}
