using System.Buffers;
using System.Net.Http.Headers;
using System.Threading.Tasks.Dataflow;

namespace TUM.LinkChecker;

public class WebDlImplementation
{
    internal static async Task<AbstractWebDlResult> Download(WebDlTask task)
    {
        // Cancel at beginning if requested from outside
        if (task.CancellationToken.IsCancellationRequested)
        {
            return new AbstractWebDlCancelledFailResult(task);
        }

        HttpResponseMessage? response = null;
        try
        {
            // Start the request. Returns as soon as headers are received
            response = await task.Client.GetAsync(task.Uri, HttpCompletionOption.ResponseHeadersRead,
                Util.CreateCancellationTokenFromSeconds(task.HeadersTimeoutSeconds, task.CancellationToken));
        }
        catch (Exception e)
        {
            // Handle manual cancellation from outside
            if (task.CancellationToken.IsCancellationRequested)
            {
                // TODO dispose content using try-finally block instead (maybe)
                response?.Content.Dispose();
                return new AbstractWebDlCancelledFailResult(task, e);
            }

            // Handle timeouts
            return new AbstractWebDlFailResult(task, e);
        }

        // If headers are only requested or content is not html, stop download and return headers only
        if (task.HeadersOnly ||
            response.Content.Headers.ContentType?.MediaType is not "text/html" and not "application/xhtml+xml")
        {
            response.Content.Dispose();
            return new WebDlHeadersResult(task, response, response.Content.Headers);
        }

        // Sets the memory stream to the size of the content length or 10MB if not specified
        PooledContent? pooledContent = new PooledContent((int)(response.Content.Headers.ContentLength ?? 10_000_000));
        ContentResult contentResult = ContentResult.Ok;
        Exception? contentException = null;
        try
        {
            // Copies the content to the MemoryStream. The operation is cancelled
            await response.Content.CopyToAsync(pooledContent.ContentStream,
                Util.CreateCancellationTokenFromSeconds(task.ContentTimeoutSeconds, task.CancellationToken));
        }
        catch (Exception e)
        {
            contentException = e;
            // Dispose unused Resources
            pooledContent.Dispose();
            response.Content.Dispose();
            pooledContent = null;

            // Handle manual cancellation from outside
            if (task.CancellationToken.IsCancellationRequested)
            {
                response.Content.Dispose();
                return new AbstractWebDlCancelledFailResult(task, e);
            }

            // Handle different exceptions
            switch (e)
            {
                case TaskCanceledException:
                    contentResult = ContentResult.Timeout;
                    break;
                case NotSupportedException:
                    contentResult = ContentResult.TooLarge;
                    break;
                default:
                    contentResult = ContentResult.OtherError;
                    break;
            }

            response.Content.Dispose();
        }

        // If the content was not successfully downloaded, return headers only
        if (pooledContent == null)
        {
            return new WebDlHeadersResult(task, response, response.Content.Headers, contentResult, contentException);
        }
        
        // Sets the MemoryStreams size to the actual content length. And sets the position to 1.
        // This will not change the underlying buffer array size.
        // TODO: If too much memory is wasted, it could be considered copying it to a new MemoryStream
        pooledContent.ContentStream.SetLength(pooledContent.ContentStream.Position);
        pooledContent.ContentStream.Position = 1;

        return new WebDlHeadersAndContentResult(task, response, response.Content.Headers, pooledContent);
    }
}

record WebDlTask(
    Uri Uri,
    bool HeadersOnly,
    HttpClient Client,
    CancellationToken CancellationToken = default,
    double HeadersTimeoutSeconds = 10,
    double ContentTimeoutSeconds = 10);

abstract record AbstractWebDlResult(WebDlTask Task)
{
    internal abstract bool Success { get; }
    internal abstract bool FullSuccess { get; }
    
}

record AbstractWebDlFailResult(WebDlTask Task, Exception? Exception) : AbstractWebDlResult(Task)
{
    internal override bool Success => false;
    internal override bool FullSuccess => false;
    internal virtual bool ManuallyCancelled => false;
}

record AbstractWebDlCancelledFailResult(
    WebDlTask Task,
    Exception? Exception = null) : AbstractWebDlFailResult(Task, Exception)
{
    internal override bool Success => false;
    internal override bool ManuallyCancelled => false;
}

record WebDlHeadersResult(
    WebDlTask Task,
    HttpResponseMessage Response,
    HttpContentHeaders ContentHeaders,
    ContentResult ContentResult = ContentResult.Ok,
    Exception? Exception = null) : AbstractWebDlResult(Task)
{
    internal override bool Success => true;
    internal override bool FullSuccess => ContentResult == ContentResult.Ok;
    internal virtual bool HasContent => false;
}

record WebDlHeadersAndContentResult(
    WebDlTask Task,
    HttpResponseMessage Response,
    HttpContentHeaders ContentHeaders, PooledContent Content)
    : WebDlHeadersResult(Task, Response, ContentHeaders)
{
    internal override bool Success => true;
    
    internal override bool HasContent => true;
}



/// <summary>
/// Manages an MemoryStream containing the content of a web request.
/// The MemoryStreams buffer is rented from the ArrayPool and should
/// therefore be returned by calling this classes Dispose method.
/// </summary>
internal class PooledContent : IDisposable
{
    internal readonly MemoryStream ContentStream;

    internal PooledContent(int length)
    {
        ContentStream = new MemoryStream(ArrayPool<byte>.Shared.Rent(length), 0, length, true, true);
    }

    private int _disposed = 0;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        ArrayPool<byte>.Shared.Return(ContentStream.GetBuffer());
        ContentStream.Dispose();
    }

    public override string ToString()
    {
        return _disposed == 0 ? $"ContentStream: {ContentStream.Length} bytes" : "ContentStream: Disposed";
    }
}

enum ContentResult
{
    Ok,
    Timeout,
    TooLarge,
    OtherError
}