using System.Net;
using QuikGraph;

namespace TUM.LinkChecker.Model;

public class Website2(Uri uri)
{

    public Uri? Uri { get; set; } = uri;
    public AnalysisData? AnalysisData { get; set; }
    public AnalysisStatus AnalysisStatus { get; set; } = AnalysisStatus.NotRequested;

    public enum Type : int
    {
        /// <summary>
        /// Root of the Graph
        /// </summary>
        Root = 0,
        Normal = 1,
        /// <summary>
        ///  Source is Broken
        /// </summary>
        LinkFormatError = 2,
    }
}

public class WebRef : Edge<Website2>
{
    public string Uri { get; set; }
    public string Text { get; set; }
    public WebRef(Website2 source, Website2 target, string uri, string text) : base(source, target)
    {
        Uri = uri;
        Text = text;
    }
}

public class AnalysisData
{
    public List<Exception> Exceptions { get; set; } = new();
    public HttpRequestData? HttpRequestData { get; set; }
    public HttpResponseData? HttpResponseData { get; set; }
}

public enum AnalysisStatus
{
    NotRequested,
    Queued,
    
    InProgress,
    Done,
    Failed
}

public class AnalysisResult
{
    public HttpRequestData HttpRequestData { get; set; }
    public HttpResponseData HttpResponseData { get; set; }
}

public class HttpRequestData
{
    public string? UserAgent { get; set; }
    public string? Accept { get; set; }
    public string? ContentType { get; set; }
}


public class ContentAnalyzeResult
{
    public string? Title;
    public string? Description;
}

public class HttpResponseData
{
    public ContentData? ContentData { get; set; }
    public string? ContentType { get; set; }
    public HttpStatusCode? StatusCode { get; set; }
    
    public Exception? Exception { get; set; }
    ResponseStatus Status { get; set; }
    
    public int? RealContentLength { get; set; }
    public int? ContentLengthHeader { get; set; }
    public string? ETag { get; set; }
    

}

public class ContentData
{
    ResponseStatus Status { get; set; }
    
    public string Title { get; set; }
}


enum ResponseStatus
{
    Success,
    Timeout,
    TooLarge,
    NetworkError,
}

