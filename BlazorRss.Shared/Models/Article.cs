using System;

namespace BlazorRss.Shared.Models
{
    public class Article
    {
        public Guid ArticleId { get; set; }
        public Feed Feed { get; set; }
        public string UniqueIdentifier { get; set; }

        public bool Read { get; set; }

        public DateTimeOffset DatePublished { get; set; }
        public DateTimeOffset DateUpdated { get; set; }

        public string ArticleUrl { get; set; }
        public string Author { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }

        public bool Deleted { get; set; }
    }
}
