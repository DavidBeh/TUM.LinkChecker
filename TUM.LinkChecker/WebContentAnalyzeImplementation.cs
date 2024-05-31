using System.Net;
using System.Net.Mime;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using QuikGraph;
using TUM.LinkChecker.Model;

namespace TUM.LinkChecker;

public class WebContentAnalyzeImplementation
{
    public static Task<WebContentAnalyzeResult> Analyze(WebContentAnalyzeTask task)
    {
        WebContentAnalyzeResult result = new WebContentAnalyzeResult(task, null, null);
        if (task.Download is WebDlResultSuccess download)
        {
            result = result with
            {
                
                HeaderData = new WebContentHeaderAnalyzeData(
                    download.Response.Content.Headers.ContentType?.MediaType,
                    download.Response.StatusCode,
                    download.Response.Headers.Location?.ToString())
            };
            if (download.ContentResult is ContentResultSuccess contentResult)
            {
                var a = contentResult.Content.ContentStream;

                try
                {
                    var htmlDocument = new HtmlDocument();
                    htmlDocument.Load(a, true);
                    string? pid = htmlDocument.DocumentNode.SelectSingleNode("//body")?.Attributes["data-pid"]?.Value;
                    result = result with
                    {
                        HtmlContentData = new WebContentHtmlContentAnalyzeData(pid, null, null, null)
                    };
                    var title = htmlDocument.DocumentNode.SelectSingleNode("//title")?.InnerText;
                    var description = htmlDocument.DocumentNode.SelectSingleNode("//meta[@name='description']")
                        ?.Attributes["content"].Value;
                    var links = htmlDocument.DocumentNode.SelectNodes("/html/body//a")?
                        .Where(node =>
                            node.Attributes.Contains("href") &&
                            !node.Attributes["href"].Value.StartsWith("javascript:"))
                        .Select(
                            node =>
                            {
                                var text = Regex.Replace(node.InnerText.Trim(), @"\s+",
                                    " "); // Remove multiple whitespaces
                                // Remove linebreaks:
                                text = text.Replace("\n", " ").Replace("\r", " ");

                                return new LinkData(node.Attributes["href"].Value, text, node.Name);
                            }).ToList();

                    result = result with
                    {
                        HtmlContentData = new WebContentHtmlContentAnalyzeData(pid, title, description, links)
                    };
                }
                catch (Exception e)
                {
                    result = result with
                    {
                        WebContentHtmlException = e
                    };
                }
            }
        }

        return Task.FromResult(result);
    }
}

public record WebContentAnalyzeTask(
    WebDlResult Download,
    CancellationToken CancellationToken = default);

public record WebContentAnalyzeResult(
    
    WebContentAnalyzeTask Task,
    WebContentHeaderAnalyzeData? HeaderData,
    WebContentHtmlContentAnalyzeData? HtmlContentData,
    Exception? WebContentHtmlException = null);

public record WebContentHeaderAnalyzeData(
    string? ContentType,
    HttpStatusCode StatusCode,
    string? RedirectLocation);

public record WebContentHtmlContentAnalyzeData(
    string? Typo3PageId,
    string? Title,
    string? Description,
    List<LinkData>? Links);

public record LinkData(
    string Url,
    string Text,
    string Type);