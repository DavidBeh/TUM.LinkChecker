using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using TUM.LinkChecker;


var uri = new Uri("https://tum.de");
var client = new HttpClient();
var task = new WebDlTask(uri, false, client);
var download = await WebDlImplementation.Download(task);

if (download is WebDlHeadersAndContentResult headersAndContent)
{
    Console.WriteLine(headersAndContent);
    var doc = new HtmlDocument();
    doc.Load(headersAndContent.Content.ContentStream, true);
    doc.DocumentNode.SelectNodes("/html/body//a")
        .Where(node => node.Attributes.Contains("href")).Select(node =>
        {
            var text = Regex.Replace(node.InnerText.Trim(), @"\s+", " "); // Remove multiple whitespaces
            // Remove linebreaks:
            text = text.Replace("\n", " ").Replace("\r", " ");
            
            return $"{node.Attributes["href"].Value}: {text}";
        }).ToList().ForEach(Console.WriteLine);
}
else
{
    Console.WriteLine("No content to analyze");
}