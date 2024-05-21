namespace TUM.LinkChecker.Model;

public class Webressource
{
    public string Url { get; set; }
    public List<Snapshot> Snapshots { get; set; }
}

public class Snapshot
{
    public DateTime Timestamp { get; set; }
    
    public bool ContentRequested { get; set; }
    public bool ContentRecieved { get; set; }
    public Exception Exception { get; set; }
    public HttpRequestData? RequestData { get; set; }
    public HttpResponseData? ResponseData { get; set; }
    public ContentAnalyzeResult? ContentAnalysisResult { get; set; }
    
    internal AbstractWebDlResult WebDlResult { get; set; }
}

public class HttpRequestData
{
    double? HeaderTimeout { get; set; }
    double? ContentTimeout { get; set; }
    bool? OnlyHeader { get; set; }
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
    HeaderData? Header { get; set; }
    public int? RealContentSize { get; set; }
    
    public Exception? Exception { get; set; }
    public bool? ContentRequested { get; set; }
}

public class HeaderData
{
    public string? ContentType { get; set; }
    public int? ContentLength { get; set; }
    public string? ETag { get; set; }
    public string? Location;
    public HttpStatusCode StatusCode { get; set; }
}

public class OptionalData<TData, TError>
{
    public TData? Data { get; set; }
    public TError? Error { get; set; }
}