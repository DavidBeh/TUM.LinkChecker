// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TUM.LinkChecker;
using TUM.LinkChecker.Model;
using TUM.SimpleLinkChecker;
using TUM.SimpleLinkChecker.Data;
using WebRef = TUM.SimpleLinkChecker.Data.WebRef;

Console.WriteLine("Hello, World!");


var lc = new LinkChecker();
await lc.RunScanner();

DateTime dt = DateTime.Now;

public class LinkChecker
{
    public List<WebSnapshot> Websites = new();
    public List<Uri> InitialWebsites = new();
    private HttpClient _httpClient = new();
    private AppDbContext _dbContext = AppDbContext.CreateDefaultDbContext();

    public LinkChecker()
    {
        InitialWebsites.Add(new Uri("https://www.it.tum.de/"));
    }

    public bool ShouldAnalyze(Uri uri)
    {
        return uri.Host == "www.it.tum.de";
    }

    public async Task RunScanner()
    {
        if (InitialWebsites.Count == 0)
            throw new InvalidOperationException("No websites to scan. Add websites to InitialWebsites first");

        var scrape = new Scrape()
        {
            TimeStamp = DateTime.Now,
        };

        _dbContext.Scrapes.Add(scrape);
        await _dbContext.SaveChangesAsync();

        foreach (var initialWebsite in InitialWebsites)
        {
            var website = GetOrCreateWebsite(initialWebsite, scrape);
            website.AnalyzeBecauseUrl = true;
            await _dbContext.SaveChangesAsync();
        }

        int count = 0;
        while (true)
        {
            count++;
            var website = _dbContext.Snapshots.FirstOrDefault(snapshot => snapshot.ScrapeId == scrape.ScrapeId &&
                (snapshot.AnalyzeBecauseUrl || snapshot.AnalyzeBecauseReference ) &&
                snapshot.Status == DataStatus.Pending);
            if (website == null) break;

            //if (Websites.Count > 4000) break;
            /*var website = Websites.FirstOrDefault(
                website1 => website1.State == WebsiteState.JustAdded &&
                            (website1.Uri.Host.Equals("www.it.tum.de") ||
                             website1.SourceSites.Any(tuple =>
                                 tuple.WebSnapshot.Uri.Host
                                     .Equals("www.it.tum.de"))));*/
            var sourceSite = true||website.AnalyzeBecauseUrl
                ? null
                : _dbContext.WebRefs
                    .Include(webRef => webRef.Source)
                    .FirstOrDefault(webRef => webRef.TargetId == website.SnapshotId)?.Source;
            if (false && sourceSite == null && !website.AnalyzeBecauseUrl)
                Console.WriteLine(
                    "Unexpected Behavior: Website should be analyzed because of reference, but has no source site.");

            website.Status = DataStatus.Running;
            //await _dbContext.SaveChangesAsync();
            /*
            var doneCount = _dbContext.Snapshots.Count(snapshot => snapshot.ScrapeId == scrape.ScrapeId && snapshot.Status == DataStatus.Finished);
            var pendingCount = _dbContext.Snapshots.Count(snapshot => snapshot.ScrapeId == scrape.ScrapeId &&
                snapshot.AnalyzeBecauseUrl || snapshot.AnalyzeBecauseReference);
 
            double percentageDone = doneCount / (double)(doneCount + pendingCount) * 100;
           
            Console.Write(
                $"Total: {doneCount + pendingCount} {percentageDone:0.##}% Done | Scanning {website.Uri} from {sourceSite?.Uri}...");
                */
            Console.Write($"Total: {count} | Scanning {website.Uri} from {sourceSite?.Uri}...");
            try
            {
                new NpgsqlCommand().CreateParameter().
                var dlResult =
                    await WebDlImplementation.Download(new WebDlTask(website.Uri, false, _httpClient));

                var redirect = (dlResult as WebDlResultSuccess)?.Response.RequestMessage?.RequestUri;

                if (redirect != null && redirect != website.Uri)
                {
                    website.HttpStatusCode = HttpStatusCode.Redirect;
                    website.ContentStatus = DataStatus.NotRequested;
                    website.Status = DataStatus.Finished;

                    var redirectedWebsite = GetOrCreateWebsite(redirect, scrape);
                    redirectedWebsite.Status = DataStatus.Running;
                    redirectedWebsite.AnalyzeBecauseReference = true;
                    redirectedWebsite.AnalyzeBecauseUrl = ShouldAnalyze(redirect);
                    _dbContext.WebRefs.Add(new TUM.SimpleLinkChecker.Data.WebRef()
                    {
                        Source = website,
                        Target = redirectedWebsite,
                        Type = "redirect",
                    });
                    await _dbContext.SaveChangesAsync();
                    website = redirectedWebsite;
                }

                var analyzeResult = await WebContentAnalyzeImplementation.Analyze(new WebContentAnalyzeTask(dlResult));

                if (analyzeResult.HeaderData != null)
                {
                    website.HttpStatusCode = analyzeResult.HeaderData.StatusCode;
                }

                if (analyzeResult.HtmlContentData != null)
                {
                    website.ContentStatus = DataStatus.Finished;
                    website.C_Title = analyzeResult.HtmlContentData.Title;
                    website.C_Description = analyzeResult.HtmlContentData.Description;
                    website.C_Typo3PageId = analyzeResult.HtmlContentData.Typo3PageId;
                    website.ContentExceptionType = analyzeResult.WebContentHtmlException?.GetType().ToString();
                    website.ContentExceptionMessage = analyzeResult.WebContentHtmlException?.Message;

                    if (analyzeResult.HtmlContentData?.Links != null)
                    {
                        var webRefs =
                            analyzeResult.HtmlContentData.Links.Select(data =>
                            {
                                var uri = Helpers.TryCreateUri(website.Uri, data.Url);
                                return new WebRef()
                                {
                                    Source = website,
                                    Target = uri != null ? new Snapshot() { Uri = uri } : null,
                                    Type = data.Type,
                                    RawLink = data.Url,
                                    LinkText = data.Text,
                                    LinkMalformed = uri == null,
                                };
                            }).Where(wr =>
                                wr.LinkMalformed || wr.Target!.Uri.Scheme == "http" ||
                                wr.Target.Uri.Scheme == "https").ToList();

                        var webrefsWithTarget = webRefs.Where(@ref => @ref.Target != null).ToList();
                        var targetWebsites =
                            GetOrCreateWebsites(webrefsWithTarget.Select(@ref => @ref.Target!.Uri), scrape);
                        webrefsWithTarget.ForEach(webRef =>
                        {
                            webRef.Target = targetWebsites.First(snapshot => snapshot.Uri == webRef.Target!.Uri);
                            webRef.Target.AnalyzeBecauseReference = webRef.Target.AnalyzeBecauseReference || website.AnalyzeBecauseUrl;
                        });
                        
                        _dbContext.WebRefs.AddRange(webRefs);
                        
                        /*
                        foreach (var link in analyzeResult.HtmlContentData?.Links!)
                        {
                            var uri = Helpers.TryCreateUri(website.Uri, link.Url);
                            if (uri != null && (uri.Scheme == "http" || uri.Scheme == "https"))
                            {
                                var targetWebsite = GetOrCreateWebsite(SimplifyUri(uri), scrape);

                                var webRef = _dbContext.WebRefs.Add(new TUM.SimpleLinkChecker.Data.WebRef()
                                {
                                    Source = website,
                                    Target = targetWebsite,
                                    Type = link.Type,
                                });
                                if (targetWebsite.AnalyzeBecauseReference == false)
                                    targetWebsite.AnalyzeBecauseReference = website.AnalyzeBecauseUrl;
                            }
                            else
                            {
                                _dbContext.WebRefs.Add(new TUM.SimpleLinkChecker.Data.WebRef()
                                {
                                    Source = website,
                                    Target = null,
                                    Type = link.Type,
                                    LinkMalformed = true,
                                });
                            }
                        }
                        */
                    }


                }
                website.Status = DataStatus.Finished;
                await _dbContext.SaveChangesAsync();
                Console.WriteLine($" || Done! {website.HttpStatusCode}");
            }
            catch (Exception e)
            {
                website.ExceptionMessage = e.Message;
                website.ExceptionType = e.GetType().ToString();
                website.Status = DataStatus.Error;
                Console.WriteLine($" || Error! {e.Message}");
                await _dbContext.SaveChangesAsync();
            }
        }
    }


    /// <summary>
    /// Removes the fragment part of the uri
    /// </summary>
    internal Uri SimplifyUri(Uri uri)
    {
        return new Uri(new UriBuilder(uri) { Fragment = "" }.Uri.ToString());
    }

    internal List<Snapshot> GetOrCreateWebsites(IEnumerable<Uri> uris, Scrape scrape)
    {
        var websites = _dbContext.Snapshots
            .Where(snapshot => uris.Contains(snapshot.Uri) && snapshot.ScrapeId == scrape.ScrapeId).ToList();
        var otherUris = uris.Except(websites.Select(website => website.Uri)).Select(SimplifyUri).ToList();
        foreach (var otherUri in otherUris)
        {
            var snapshot = new Snapshot()
            {
                Uri = otherUri,
                Scrape = scrape,
            };
            snapshot.AnalyzeBecauseUrl = ShouldAnalyze(otherUri);
            _dbContext.Snapshots.Add(snapshot);
            websites.Add(snapshot);
        }

        if (otherUris.Count > 0)
            _dbContext.SaveChanges();
        return websites;
    }

    internal Snapshot GetOrCreateWebsite(Uri simplifiedUri, Scrape scrape)
    {
        // canonicalize uri (remove default ports etc)
        simplifiedUri = new Uri(simplifiedUri.ToString());
        var snapshot = _dbContext.Snapshots.FirstOrDefault(snapshot =>
            snapshot.Uri == simplifiedUri && snapshot.ScrapeId == scrape.ScrapeId);


        if (snapshot == null)
        {
            snapshot = new Snapshot()
            {
                Uri = simplifiedUri,
                Scrape = scrape,
            };
            if (ShouldAnalyze(simplifiedUri))
                snapshot.AnalyzeBecauseUrl = true;
            _dbContext.Snapshots.Add(snapshot);
            //_dbContext.SaveChanges();
        }

        return snapshot;
    }
}

public class WebSnapshot(Uri originalUri)
{
    public WebSnapshot() : this(null!)
    {
    }

    public Guid Id = Guid.NewGuid();
    public Uri OriginalUri = originalUri;
    public Uri? RedirectUri;
    [JsonIgnore] public Uri Uri => RedirectUri ?? OriginalUri;
    public List<WebRefOld> SourceSites = new();
    public List<LinkData> OutgoingNonHttpHttpsLinks = new();
    public WebsiteState State = WebsiteState.JustAdded;
    public string? ExceptionMessage;
    public string? ExceptionType;
    public WebsiteContentData? ContentData;
    public HttpStatusCode? HttpStatusCode;
}

public class WebRefOld
{
    public WebRefOld()
    {
        WebSnapshot = new WebSnapshot();
    }

    public WebRefOld(LinkData linkData, WebSnapshot webSnapshot)
    {
        this.linkData = linkData;
        this.WebSnapshot = webSnapshot;
    }

    public LinkData linkData;
    [JsonIgnore] public WebSnapshot WebSnapshot;

    public Guid WebsiteId
    {
        get => WebSnapshot.Id;
        set => WebSnapshot.Id = value;
    }
}

public class WebsiteContentData
{
    public string? ExceptionMessage;
    public string? ExceptionType;
    public string? Title;
    public string? Description;
    public string? Typo3PageId;
}

public enum WebsiteState
{
    JustAdded,
    NotForScan,
    Scanned,
}

public class ContentData
{
    public string Title;
    public string Description;
}