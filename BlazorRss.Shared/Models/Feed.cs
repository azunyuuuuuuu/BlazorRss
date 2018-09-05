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

        public ParserMode ParserMode { get; set; }
        public string ParserTitle { get; set; }
        public string ParserDescription { get; set; }
        public string ParserContent { get; set; }
        public string ParserAuthor { get; set; }
        public string ParserTags { get; set; }
    }

    public enum ParserMode
    {
        SmartReader,
        CssSelector,
        XPathSelector,
        YouTube
    }
}
