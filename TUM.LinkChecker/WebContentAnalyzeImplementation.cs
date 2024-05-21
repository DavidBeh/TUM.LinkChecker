using System.Net.Mime;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using QuikGraph;

namespace TUM.LinkChecker;

public class WebContentAnalyzeImplementation
{
    internal static Task<WebContentAnalyzeResult> Analyze(WebContentAnalyzeTask task)
    {
        BidirectionalGraph<int, Edge<int>> graph = new();
        

        if (task.Download is WebDlHeadersAndContentResult download)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.Load(download.Content.ContentStream, true);
            var hrefs = htmlDocument.DocumentNode.SelectNodes("/html/body//a")
                .Where(node => node.Attributes.Contains("href")).ToList();
            
            
        }
        else
        {
            //return Task.FromResult(new WebContentAnalyzeFailResult(task, new Exception("No content to analyze")));
        }

        throw new NotImplementedException("Not implemented yet");
    }
}

record WebContentAnalyzeTask(
    Uri Uri, 
    WebDlHeadersResult Download,
    CancellationToken CancellationToken);

abstract record WebContentAnalyzeResult(WebContentAnalyzeTask Task);

record WebContentAnalyzeHtmlResult(
    WebContentAnalyzeTask Task,
    WebDlHeadersResult Download,
    HtmlDocument HtmlDocument) : WebContentAnalyzeResult(Task);