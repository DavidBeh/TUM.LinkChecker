using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.EntityFrameworkCore;

namespace TUM.SimpleLinkChecker.Data;

public class Scrape
{
    public Guid ScrapeId { get; set; }
    public DateTime TimeStamp { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? ExceptionType { get; set; }
    public bool Finished { get; set; }
    public ICollection<Snapshot> Snapshots { get; set; } = [];
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class Snapshot
{
    public Guid SnapshotId { get; set; }
    public Scrape Scrape { get; set; }
    public Guid ScrapeId { get; set; }

    public Uri Uri { get; set; }

    public HttpStatusCode? HttpStatusCode { get; set; }

    public DataStatus Status { get; set; } = DataStatus.Pending;
    
    public bool AnalyzeBecauseUrl { get; set; }
    public bool AnalyzeBecauseReference { get; set; }
    public bool Downloaded { get; set; }
    public string? ContentType { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? ExceptionType { get; set; }
    public bool? ContentScraped { get; set; }
    public DataStatus ContentStatus { get; set; } = DataStatus.Pending;
    public string? ContentExceptionMessage { get; set; }
    public string? ContentExceptionType { get; set; }
    public string? C_Title { get; set; }
    public string? C_Description { get; set; }
    public string? C_Typo3PageId { get; set; }

    public ICollection<WebRef> OutgoingLinks { get; set; } = [];
    public ICollection<WebRef> IncomingLinks { get; set; } = [];
}



public enum DataStatus
{
    Pending,
    Running,
    NotRequested,
    Finished,
    Error,
    Stripped
}

public class WebRef
{
    public Guid WebRefId { get; set; }
    public Snapshot Source { get; set; } = null!;
    public Guid SourceId { get; set; }
    public Snapshot? Target { get; set; }
    public Guid? TargetId { get; set; }

    public string? LinkText { get; set; }
    public string? RawLink { get; set; }
    public string? XPath { get; set; }
    public string? Type { get; set; }
    public bool LinkMalformed { get; set; }
}