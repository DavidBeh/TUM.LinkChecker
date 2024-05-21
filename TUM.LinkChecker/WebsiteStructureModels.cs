using System.Net;
using HtmlAgilityPack;
using QuikGraph;
using QuikGraph.Algorithms;
using QuikGraph.Algorithms.Search;
using QuikGraph.Algorithms.Services;
using QuikGraph.Predicates;

namespace TUM.LinkChecker;

public abstract class LCVertex;

public class Website : LCVertex
{
    public string Url { get; set; }
}

public class SnapshotInfo : LCVertex
{
    public DateTime Timestamp { get; init; }
    public virtual TimeSpan? Duration { get; init; }

    public virtual object HeaderPersistedData { get; init; }
    public virtual HttpRequestData? RequestData { get; init; }
    public virtual HttpResponseData? ResponseData { get; init; }
    public virtual ContentAnalyzeResult? ContentAnalysisResult { get; init; }
}

public abstract class LCEdge(LCVertex source, LCVertex target) : IEdge<LCVertex>
{
    public LCVertex Source { get; } = source;
    public LCVertex Target { get; } = target;
}

public abstract class LCEdge<TSource, TTarget>(TSource source, TTarget target) : LCEdge(source, target)
    where TSource : LCVertex
    where TTarget : LCVertex
{
    public new TSource Source => (TSource)base.Source;
    public new TTarget Target => (TTarget)base.Target;
}

public class WebsiteToSnapshotEdge(Website source, SnapshotInfo target, string href)
    : LCEdge<Website, SnapshotInfo>(source, target)
{
    public string Href { get; } = href;
}

public class SnapshotToWebsiteEdge(SnapshotInfo source, Website target) : LCEdge<SnapshotInfo, Website>(source, target);

enum SnapshotTaskStatus
{
    Success,
    Error,
    Timeout
}


public abstract class TaskResult<T>;

public class TaskSuccessResult<T> : TaskResult<T>
{
}

public class TaskFailResult<T> : TaskResult<T>
{
}

public abstract class TaskNotStartedResult<T> : TaskResult<T>
{
}

public class TaskWaitingResult<T> : TaskNotStartedResult<T>
{
}

public class TaskCancelledResult<T> : TaskNotStartedResult<T>
{
    enum CancelReason
    {
        User,
        PreviousError
    }
}



public enum HttpHeaderResult
{
    Ok,
    Timeout,
    TooLarge,
    OtherError
}