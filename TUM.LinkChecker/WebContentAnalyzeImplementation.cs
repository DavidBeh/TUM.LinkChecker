using System.Net.Mime;
using HtmlAgilityPack;

namespace TUM.LinkChecker;

public class WebContentAnalyzeImplementation
{
    internal static Task<WebContentAnalyzeResult> Analyze(WebContentAnalyzeTask task)
    {
        
        if (task.Download is WebDlHeadersAndContentResult download)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.Load(download.Content.ContentStream, true);
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