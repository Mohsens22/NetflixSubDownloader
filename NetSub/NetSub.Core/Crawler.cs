using HtmlAgilityPack;
using NetSub.Core.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetSub.Core
{
    public static class Crawler
    {
        public static async void Crawl(int pages)
        {
            for (int i = 1; i < pages; i++)
            {
                var a = await GetPageAsync(i);
                var sata = a.GetPageData();
                sata.Download();
            }


        }
        public static async void Download(this IEnumerable<SubflixModel> items)
        {
            foreach (var item in items)
            {
                var url = await item.GetdownloadLink();
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    var fn = response.Headers["Content-Disposition"].Split(new string[] { "=" }, StringSplitOptions.None)[1];
                    string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
                    Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
                    fn = r.Replace(fn, "");
                    Directory.CreateDirectory(@".\SubRepository");
                    string basePath = @".\SubRepository"; // Change accordingly...
                    var responseStream = response.GetResponseStream();
                    if (!File.Exists(Path.Combine(basePath, fn)))
                        using (var fileStream = File.Create(Path.Combine(basePath, fn)))
                            responseStream.CopyTo(fileStream);

                    else
                        using (var fileStream = File.Create(Path.Combine(basePath, fn + $"_{(Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds}")))
                        {
                            responseStream.CopyTo(fileStream);
                        }

                }
            }

        }
        public static IEnumerable<SubflixModel> GetPageData(this string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var table = doc.DocumentNode.SelectSingleNode("//table");
            var cc = table.GetElementsWithClass("post-table", "tr");
            return cc.ToDownloadItems().Filter();
        }
        private static IEnumerable<SubflixModel> ToDownloadItems(this IEnumerable<HtmlNode> items)
        {
            var list = new List<SubflixModel>();
            foreach (var item in items)
            {
                var r = new SubflixModel();
                r.Language = item.GetElementsWithClass("langTD", "td").FirstOrDefault().InnerText;
                r.Name = item.GetElementsWithClass("titleSub", "span").FirstOrDefault().InnerText;
                r.Id = item.GetElementsWithClass("downloadBtn", "a").FirstOrDefault().Attributes.Where(x => x.Name == "data-postid").FirstOrDefault().Value;
                list.Add(r);
            }
            return list;
        }
        private static IEnumerable<SubflixModel> Filter(this IEnumerable<SubflixModel> items) => items.Where(x => x.Language.ToLower().Contains("english"));
        private static IEnumerable<HtmlNode> GetElementsWithClass(this HtmlNode doc, string className, string tag)
        {

            Regex regex = new Regex("\\b" + Regex.Escape(className) + "\\b", RegexOptions.Compiled);

            return doc
                .Descendants()
                .Where(n => n.NodeType == HtmlNodeType.Element)
                .Where(e => e.Name == tag && regex.IsMatch(e.GetAttributeValue("class", "")));
        }
        public static async Task<string> GetPageAsync(int page)
        {
            var dict = new Dictionary<string, string>();
            dict.Add("action", "get_subs_ajax");
            dict.Add("spage", page.ToString());
            var client = new HttpClient();
            var req = new HttpRequestMessage(HttpMethod.Post, Constants.APIHost + "wp-admin/admin-ajax.php") { Content = new FormUrlEncodedContent(dict) };
            var res = await client.SendAsync(req);
            return await res.Content.ReadAsStringAsync();
        }
        public static async Task<string> GetdownloadLink(this SubflixModel post)
        {
            var dict = new Dictionary<string, string>();
            dict.Add("action", "downajax");
            dict.Add("postid", post.Id);
            var client = new HttpClient();
            var req = new HttpRequestMessage(HttpMethod.Post, Constants.APIHost + "wp-admin/admin-ajax.php") { Content = new FormUrlEncodedContent(dict) };
            var res = await client.SendAsync(req);
            var response = await res.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<DownloadLinkItem>(response).url;

        }
    }
}
