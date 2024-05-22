﻿using System.Buffers;
using System.Net.Http.Headers;
using System.Threading.Tasks.Dataflow;
using TUM.LinkChecker.Model;

namespace TUM.LinkChecker;

public class WebDlImplementation
{
    internal static async Task<WebDlResult> Download(WebDlTask task)
    {
        // Cancel at beginning if requested from outside
        if (task.CancellationToken.IsCancellationRequested)
        {
            return new WebDlResultFail(task, WebDlFailReason.Cancelled, new TaskCanceledException());
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
                //return new AbstractWebDlCancelledFailResult(task, e);
                return new WebDlResultFail(task, WebDlFailReason.Cancelled, e);
            }

            // Handle timeouts
            if (e is TaskCanceledException)
                return new WebDlResultFail(task, WebDlFailReason.Timeout, e);

            return new WebDlResultFail(task, WebDlFailReason.OtherError, e);
        }

        // If headers are only requested or content is not html, stop download and return headers only
        if (task.HeadersOnly ||
            response.Content.Headers.ContentType?.MediaType is not "text/html" and not "application/xhtml+xml")
        {
            response.Content.Dispose();
            return new WebDlResultSuccess(task, TimeSpan.Zero, response, new ContentNotRequestedResult());
        }

        // Sets the memory stream to the size of the content length or 10MB if not specified
        PooledContent? pooledContent = new PooledContent((int)(response.Content.Headers.ContentLength ?? 10_000_000));
        ContentFailReason contentFailReason = ContentFailReason.Ok;
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
                return new WebDlResultFail(task, WebDlFailReason.Cancelled, e);
            }

            // Handle different exceptions
            switch (e)
            {
                case TaskCanceledException:
                    contentFailReason = ContentFailReason.Timeout;
                    break;
                case NotSupportedException:
                    contentFailReason = ContentFailReason.TooLarge;
                    break;
                default:
                    contentFailReason = ContentFailReason.OtherError;
                    break;
            }

            response.Content.Dispose();
        }

        // If the content was not successfully downloaded, return headers only
        if (pooledContent == null)
        {
            return new WebDlResultSuccess(task, TimeSpan.Zero, response,
                new ContentResultFail(contentFailReason, contentException!));
        }

        // Sets the MemoryStreams size to the actual content length. And sets the position to 1.
        // This will not change the underlying buffer array size.
        // TODO: If too much memory is wasted, it could be considered copying it to a new MemoryStream
        pooledContent.ContentStream.SetLength(pooledContent.ContentStream.Position);
        pooledContent.ContentStream.Position = 1;

        return new WebDlResultSuccess(task, TimeSpan.Zero, response, new ContentResultSuccess(pooledContent));
    }
}

record WebDlTask(
    Uri Uri,
    bool HeadersOnly, // Should not be used
    HttpClient Client,
    CancellationToken CancellationToken = default,
    double HeadersTimeoutSeconds = 10,
    double ContentTimeoutSeconds = 10);

abstract record WebDlResult(WebDlTask Task);

record WebDlResultFail(WebDlTask Task, WebDlFailReason FailReason, Exception Exception) : WebDlResult(Task);

enum WebDlFailReason
{
    Timeout,
    Cancelled,
    OtherError
}

record WebDlResultSuccess(
    WebDlTask Task,
    TimeSpan Duration,
    HttpResponseMessage Response,
    ContentResult ContentResult) : WebDlResult(Task);

abstract record ContentResult;

record ContentResultFail(ContentFailReason FailReason, Exception Exception) : ContentResult;

record ContentNotRequestedResult : ContentResult;

record ContentResultSuccess(PooledContent Content) : ContentResult;

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

enum ContentFailReason
{
    Ok,
    Timeout,
    TooLarge,
    OtherError
}