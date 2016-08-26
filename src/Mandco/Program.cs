using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel.Syndication;
using System.Xml;
using Newtonsoft.Json;
using System.Linq;
using System.Threading;

/// <summary>
/// Ref. Mackays : Emma Bullen 
/// Candidate    : Franco Bianchin 
/// Project      : The application pulls UK RSS every hour and creates a file named with the current date where are saved all contextual (by date) news.  
/// </summary>
namespace Mandco
{
    public class Program
    {
        const int interval = 3600000;
        static TimerCallback callback = new TimerCallback(Tick);

        public static void Main(string[] args)
        {
            Timer stateTimer = new Timer(callback, null, 0, interval);
            Console.ReadLine();
        }

        public static void Tick(Object o)
        {
            Console.WriteLine(string.Format("Read rss at {0} ", DateTime.Now.ToString()));
            //manage feeds
            FeedsManager fm = new FeedsManager();
            fm.Reader();
        }
    }


    internal class FeedsManager
    {
        const string bbcUri = "http://feeds.bbci.co.uk/news/uk/rss.xml";

        public void Reader()
        {
            News bbcNews = GetFeeds();

            if (bbcNews.NewsItems.Count > 0)
            {
                var dates = from n in bbcNews.NewsItems
                            group n by n.PubDate.Substring(0, 10) into g
                            select new
                            {
                                Date = g.Key
                            };

                foreach (var d in dates)
                {
                    JsonWriter jw = new JsonWriter(d.Date);
                    int i = jw.Write(bbcNews);
                    Console.WriteLine(string.Format("{0} - inserted {1} new news", d.Date, i));
                }
            }
        }


        /// <summary>
        /// Return rss from bbc
        /// </summary>
        /// <returns></returns>
        private News GetFeeds()
        {
            News news;

            using (XmlReader reader = XmlReader.Create(bbcUri))
            {
                SyndicationFeed list = SyndicationFeed.Load(reader);

                news = new News();
                List<NewsItem> newsItemList = new List<NewsItem>();

                foreach (var item in list.Items)
                {
                    if (item.Title.Text == "BBC News - Home")
                    {
                        news.Title = item.Title.Text;
                        news.Link = item.Id;
                        news.Description = item.Summary.Text;
                    }
                    else
                    {
                        NewsItem newsItem = new NewsItem();
                        newsItem.Title = item.Title.Text;
                        newsItem.Link = item.Id;
                        newsItem.Description = item.Summary.Text;
                        newsItem.PubDate = item.PublishDate.ToString();
                        newsItemList.Add(newsItem);
                    }
                }
                news.NewsItems = newsItemList;
            }
            return news;
        }
    }

    /// <summary>
    /// Creates folder and files and writes rss items 
    /// </summary>
    internal class JsonWriter
    {
        FileInfo fi;
        string date;

        //ctor
        public JsonWriter(string date)
        {
            string folderName = "\\feed";
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + folderName;
            //folder existence
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            this.date = date;
            fi = new FileInfo(folderPath + "\\" + date.Replace('/', '-') + ".json");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="news"></param>
        /// <returns>items processed</returns>
        public int Write(News news)
        {
            int itemsAdded = 0;
            try
            {
                var lastNews = from n in news.NewsItems
                               where n.PubDate.Substring(0, 10) == this.date
                               orderby Convert.ToDateTime(n.PubDate) ascending
                               select n;

                //Check existence file
                List<NewsItem> appendNewsItems = new List<NewsItem>();
                News oldNews = new News()
                {
                    Title = "null",
                    Description = "null",
                    Link = "null",
                    NewsItems = new List<NewsItem>()
                };

                if (fi.Exists)
                {
                    //Read
                    string text = File.ReadAllText(fi.FullName);
                    oldNews = JsonConvert.DeserializeObject<News>(text);


                    if (oldNews != null)
                    {
                        foreach (var newItem in lastNews)
                        {
                            bool exist = false;
                            foreach (var oldItem in oldNews.NewsItems)
                            {
                                if (oldItem.Link == newItem.Link)
                                {
                                    exist = true;
                                    break;
                                }
                            }
                            if (!exist)
                            {
                                appendNewsItems.Add(newItem);
                                itemsAdded += 1;
                            }
                        }

                        if (oldNews.NewsItems.Count > 0)
                        {
                            //add new items
                            oldNews.NewsItems.AddRange(appendNewsItems);
                        }
                        else
                        {
                            return 0;
                        }
                    }
                    else
                    {
                        //Add entire set
                        oldNews.NewsItems.AddRange(lastNews);
                        itemsAdded = oldNews.NewsItems.Count;
                    }
                }
                else
                {
                    //Add entire set
                    oldNews.NewsItems.AddRange(lastNews);
                    itemsAdded = oldNews.NewsItems.Count;
                }

                string jsonFile = JsonConvert.SerializeObject(oldNews, Newtonsoft.Json.Formatting.Indented);

                File.Delete(fi.FullName);
                File.AppendAllText(fi.FullName, jsonFile);

                return itemsAdded;
            }
            catch (Exception ex)
            {

            }
            return 0;
        }
    }
}
