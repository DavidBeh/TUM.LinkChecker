using System.Net.Mime;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using QuikGraph;

namespace TUM.LinkChecker;

public class WebContentAnalyzeImplementation
{
    internal static Task<WebContentAnalyzeResult> Analyze(WebContentAnalyzeTask task)
    {
        if (task.Download is WebDlResultSuccess download)
        { 
            if (download.ContentResult is ContentResultSuccess contentResult)
            {
                var a = contentResult.Content.ContentStream;
                
                var htmlDocument = new HtmlDocument();
                htmlDocument.Load(a, true);
                var hrefs = htmlDocument.DocumentNode.SelectNodes("/html/body//a")
                    .Where(node => node.Attributes.Contains("href") && !node.Attributes["href"].Value.StartsWith("javascript:")).ToList();
                
            }
        }


        throw new NotImplementedException("Not implemented yet");
    }
}

record WebContentAnalyzeTask(
    Uri Uri, 
    WebDlResult Download,
    CancellationToken CancellationToken);

abstract record WebContentAnalyzeResult(WebContentAnalyzeTask Task);

record WebContentAnalyzeHtmlResult(
    WebContentAnalyzeTask Task,
    HtmlDocument HtmlDocument) : WebContentAnalyzeResult(Task);