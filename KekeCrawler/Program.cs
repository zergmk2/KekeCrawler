using Abot.Core;
using Abot.Crawler;
using Abot.Poco;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace KekeCrawler
{
    class Program
    {
        private static PoliteWebCrawler crawler;
        private static List<string> DownloadLinkList = new List<string>();
        private static List<string> DownloadMP3LinkList = new List<string>();
        static void Main(string[] args)
        {
            CrawlConfiguration crawlConfig = AbotConfigurationSectionHandler.LoadFromXml().Convert();
            crawlConfig.MaxConcurrentThreads = 5;//this overrides the config value
            crawlConfig.MaxCrawlDepth = 0;
            crawler = new PoliteWebCrawler();
            crawler.PageCrawlStartingAsync += crawler_ProcessPageCrawlStarting;
            crawler.PageCrawlCompletedAsync += crawler_ProcessPageCrawlCompleted;
            crawler.PageCrawlDisallowedAsync += crawler_PageCrawlDisallowed;
            crawler.PageLinksCrawlDisallowedAsync += crawler_PageLinksCrawlDisallowed;

            //var doc = new HtmlDocument();
            //doc.Load(@"C:\Users\lucao\Downloads\keketest.html");
            //var embedNodes = doc.DocumentNode.SelectSingleNode("//script[contains(text(), 'thunder_url')]");
            //var domain = Regex.Match(embedNodes.InnerText, @".*domain.*'(.*)'").Groups[1].ToString();
            //var thunder_url = Regex.Match(embedNodes.InnerText, ".*thunder_url.*\"(.*)\"").Groups[1].ToString();
            //var downloadMp3Link = domain + thunder_url;


            CrawlResult result;
            for (int i = 58; i > 30; i--)
            {
                DownloadLinkList.Clear();
                Thread.Sleep(60000);
                result = crawler.Crawl(new Uri($"http://www.kekenet.com/Article/15410/List_{i}.shtml"));
                if (result.ErrorOccurred)
                    Console.WriteLine("Crawl of {0} completed with error: {1}", result.RootUri.AbsoluteUri, result.ErrorException.Message);
                else
                    Console.WriteLine("Crawl of {0} completed without error.", result.RootUri.AbsoluteUri);

                if (DownloadLinkList.Count > 0)
                {
                    DownloadMP3LinkList.Clear();
                    foreach (var link in DownloadLinkList)
                    {
                        var sub_crawler = new PoliteWebCrawler();
                        sub_crawler.PageCrawlStartingAsync += sub_crawler_ProcessPageCrawlStarting;
                        sub_crawler.PageCrawlCompletedAsync += sub_crawler_ProcessPageCrawlCompleted;
                        sub_crawler.PageCrawlDisallowedAsync += sub_crawler_PageCrawlDisallowed;
                        sub_crawler.PageLinksCrawlDisallowedAsync += sub_crawler_PageLinksCrawlDisallowed;
                        sub_crawler.Crawl(new Uri(link));
                        Thread.Sleep(20000);
                        sub_crawler?.Dispose();
                    }
                }
                //"http://k6.kekenet.com/Sound/2018/01/scad180110.mp3"
                if (DownloadMP3LinkList.Count > 0)
                {
                    foreach (var mp3Link in DownloadMP3LinkList)
                    {
                        WebClient client = new WebClient();
                        Uri ur = new Uri(mp3Link);
                        client.DownloadProgressChanged += WebClientDownloadProgressChanged;
                        client.DownloadDataCompleted += WebClientDownloadCompleted;
                        var file = @"C:\Users\lucao\Downloads\keke\" + mp3Link.Split('/').Last().ToString();
                        client.DownloadFile(ur, file);
                        Thread.Sleep(60000);
                    }
                }
            }

        }

        private static void WebClientDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Console.WriteLine("Download status: {0}%.", e.ProgressPercentage);
        }

        private static void WebClientDownloadCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            Console.WriteLine("Download finished!");
        }

        private static void sub_crawler_PageLinksCrawlDisallowed(object sender, PageLinksCrawlDisallowedArgs e)
        {
        }

        private static void sub_crawler_PageCrawlDisallowed(object sender, PageCrawlDisallowedArgs e)
        {
        }

        private static void sub_crawler_ProcessPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            CrawledPage crawledPage = e.CrawledPage;

            if (crawledPage.WebException != null || crawledPage.HttpWebResponse.StatusCode != HttpStatusCode.OK)
                Console.WriteLine("Crawl of page failed {0}", crawledPage.Uri.AbsoluteUri);
            else
                Console.WriteLine("Crawl of page succeeded {0}", crawledPage.Uri.AbsoluteUri);

            if (string.IsNullOrEmpty(crawledPage.Content.Text))
                Console.WriteLine("Page had no content {0}", crawledPage.Uri.AbsoluteUri);

            var htmlAgilityPackDocument = crawledPage.HtmlDocument; //Html Agility Pack parser
            var embedNodes = htmlAgilityPackDocument.DocumentNode.SelectSingleNode("//script[contains(text(), 'thunder_url')]");
            var domain = Regex.Match(embedNodes.InnerText, @".*domain.*'(.*)'").Groups[1].ToString();
            var thunder_url = Regex.Match(embedNodes.InnerText, ".*thunder_url.*\"(.*)\"").Groups[1].ToString();
            var downloadMp3Link = domain + thunder_url;
            DownloadMP3LinkList.Add(downloadMp3Link);
        }

        private static void sub_crawler_ProcessPageCrawlStarting(object sender, PageCrawlStartingArgs e)
        {
        }

        private static void crawler_PageLinksCrawlDisallowed(object sender, PageLinksCrawlDisallowedArgs e)
        {
        }

        private static void crawler_PageCrawlDisallowed(object sender, PageCrawlDisallowedArgs e)
        {
        }

        private static void crawler_ProcessPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            CrawledPage crawledPage = e.CrawledPage;

            if (crawledPage.WebException != null || crawledPage.HttpWebResponse.StatusCode != HttpStatusCode.OK)
                Console.WriteLine("Crawl of page failed {0}", crawledPage.Uri.AbsoluteUri);
            else
                Console.WriteLine("Crawl of page succeeded {0}", crawledPage.Uri.AbsoluteUri);

            if (string.IsNullOrEmpty(crawledPage.Content.Text))
                Console.WriteLine("Page had no content {0}", crawledPage.Uri.AbsoluteUri);

            var htmlAgilityPackDocument = crawledPage.HtmlDocument; //Html Agility Pack parser
            var angleSharpHtmlDocument = crawledPage.AngleSharpHtmlDocument; //AngleSharp parser

            var list = findAllListForASC(htmlAgilityPackDocument);
            if (list.Count > 0)
            {
                DownloadLinkList = list;
            }
        }

        private static void crawler_ProcessPageCrawlStarting(object sender, PageCrawlStartingArgs e)
        {
        }

        private static List<string> findAllListForASC(HtmlDocument pack)
        {
            List<string> AudioPageList = new List<string>();
            var nodes = pack.DocumentNode.SelectNodes("//ul[@id = 'menu-list']/li");
            if (nodes != null && nodes.Count > 0)
            {
                foreach (var list in nodes)
                {
                    var aNodes = list.SelectSingleNode(".//h2/a");
                    
                    var link = aNodes.GetAttributeValue("href", "");
                    if (!string.IsNullOrEmpty(link))
                    {
                        AudioPageList.Add(link);
                    }
                }
            }
            return AudioPageList;
        }
    }
}
