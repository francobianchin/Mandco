using System.Collections.Generic;

namespace Mandco
{
    public class News
    {
        public string Title { get; set; }
        public string Link { get; set; }
        public string Description { get; set; }
        public List<NewsItem> NewsItems { get; set; }
    }

    public class NewsItem
    {
        public string Title { get; set; }
        public string Link { get; set; }
        public string Description { get; set; }
        public string PubDate { get; set; }
    }
}
