using HtmlAgilityPack;
using NetSub.Core.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
            var a = await GetPageAsync(1);
            GetPageData(a);
        }
        public static /*IEnumerable<SubflixModel>*/ void GetPageData(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var table = doc.DocumentNode.SelectSingleNode("//table");
            var cc = table.GetElementsWithClass("post-table", "tr");
            var items = cc.ToDownloadItems().Filter();
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
        public static async Task<string> GetdownloadLink(int postid)
        {
            var dict = new Dictionary<string, string>();
            dict.Add("action", "downajax");
            dict.Add("postid", postid.ToString());
            var client = new HttpClient();
            var req = new HttpRequestMessage(HttpMethod.Post, Constants.APIHost + "wp-admin/admin-ajax.php") { Content = new FormUrlEncodedContent(dict) };
            var res = await client.SendAsync(req);
            var response = await res.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<DownloadLinkItem>(response).url;

        }
    }
}
