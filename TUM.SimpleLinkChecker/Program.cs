// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Net;
using Microsoft.EntityFrameworkCore;
using QuikGraph;
using QuikGraph.Algorithms;
using QuikGraph.Algorithms.ShortestPath;
using TUM.LinkChecker;
using TUM.SimpleLinkChecker;
using TUM.SimpleLinkChecker.Data;
using WebRef = TUM.SimpleLinkChecker.Data.WebRef;

Console.WriteLine("Hello, World!");


var lc = new LinkChecker();
await lc.RunScanner();
lc.Scrapes.ForEach(scrape =>
    scrape.Snapshots = lc.Snapshots.Where(snapshot => snapshot.ScrapeId == scrape.ScrapeId).ToList());
lc.Snapshots.AsParallel().ForAll(snapshot =>
{
    snapshot.OutgoingLinks = lc.WebRefs.Where(webRef => webRef.SourceId == snapshot.SnapshotId).ToList();
    snapshot.IncomingLinks = lc.WebRefs.Where(webRef => webRef.TargetId == snapshot.SnapshotId).ToList();
    snapshot.Scrape = lc.Scrapes.First(scrape => scrape.ScrapeId == snapshot.ScrapeId);
});
lc.WebRefs.AsParallel().ForAll(webRef =>
{
    webRef.Source = lc.Snapshots.First(snapshot => snapshot.SnapshotId == webRef.SourceId);
    webRef.Target = webRef.TargetId == null
        ? null
        : lc.Snapshots.First(snapshot => snapshot.SnapshotId == webRef.TargetId);
});
/*
object brokenRoot = new();
object malformed = new();
var graph = new BidirectionalGraph<object, TaggedEdge<object, WebRef?>>();
graph.AddVertex(brokenRoot);
graph.AddVertex(malformed);
graph.AddEdge(new TaggedEdge<object, WebRef?>(brokenRoot, malformed, null));
graph.AddVertexRange(lc.Snapshots);
graph.AddEdgeRange(lc.WebRefs.Select(webRef =>
    new TaggedEdge<object, WebRef?>(webRef.Target ?? malformed, webRef.Source, webRef)));
*/
var sitesWithErrors = lc.Snapshots
    .Where(snapshot => snapshot.Status != DataStatus.Pending &&
                       (snapshot.HttpStatusCode == null || (int)snapshot.HttpStatusCode < 200 ||
                        (int)snapshot.HttpStatusCode >= 400 ||
                        snapshot.Status == DataStatus.Error)).ToList();

HashSet<WebRef> webRefsInLinks = new();

webRefsInLinks.UnionWith(sitesWithErrors.SelectMany(snapshot =>
    snapshot.IncomingLinks.Where(wr => wr.Source.AnalyzeBecauseUrl)));
webRefsInLinks.UnionWith(webRefsInLinks.Where(wr => wr.Type is "redirect").SelectMany(wr => wr.Source.IncomingLinks)
    .Where(wr => wr.Source.AnalyzeBecauseUrl).ToList());

var malformedLinks = lc.WebRefs.Where(webRef => webRef is { LinkMalformed: true, Source.AnalyzeBecauseUrl: true }).ToList();
webRefsInLinks.UnionWith(malformedLinks);
/*
graph.AddEdgeRange(sitesWithErrors.Select(snapshot =>
    new TaggedEdge<object, WebRef?>(brokenRoot, snapshot, null)));

Func<TaggedEdge<object, WebRef?>, double> edgeWeights = d =>
    d.Source == brokenRoot || (d.Tag?.Type == "redirect") ? 0 : 1;

var dijkstra = new DijkstraShortestPathAlgorithm<object, TaggedEdge<object, WebRef?>>(graph, edgeWeights);


dijkstra.Compute(brokenRoot);
HashSet<WebRef> relevantRefs = new();
HashSet<Snapshot> relevantSnapshots = new();
foreach (var kvp in dijkstra.GetDistances())
{
    if ((kvp.Key as Snapshot)?.HttpStatusCode == HttpStatusCode.NotFound)
        Debugger.Break();
    Console.WriteLine($"Distance to {kvp.Key} is {kvp.Value}");
    if (kvp.Value > 1) break;
    int count = 0;
    foreach (var x in graph.RankedShortestPathHoffmanPavley(edgeWeights, brokenRoot, kvp.Key, 10))
    {
        count++;
        Console.WriteLine($"Path {count}: {string.Join(" -> ", x.Select(o => o.ToString()))}");
        // get path length and check if it is relevant
        var len = x.Select(edgeWeights).Sum();
        if (len == 0)
        {
            Console.WriteLine("Weird length detected: Path: " + string.Join(" -> ", x.Select(o => o.ToString())));
        }

        if (len > 1) break;
        foreach (var edge in x)
        {
            if (edge.Target is Snapshot target) relevantSnapshots.Add(target);
            if (edge.Source is Snapshot source) relevantSnapshots.Add(source);
            if (edge.Tag != null)
                relevantRefs.Add(edge.Tag);
        }
    }
}
*/

int savedBrokenLinks = webRefsInLinks.Count(wr => wr.Target != null && sitesWithErrors.Contains(wr.Target));
int savedMalformedLinks = malformedLinks.Count;
Console.WriteLine("Saving " + savedBrokenLinks + " broken links and " + savedMalformedLinks + " malformed links");

lc.Scrapes.ForEach(scrape => scrape.Snapshots = []);
var saveSites = webRefsInLinks.Select(wr => wr.Source)
    .Concat(webRefsInLinks.Select(wr => wr.Target).Where(snapshot => snapshot != null)).Concat(sitesWithErrors)
    .Distinct().ToList();

saveSites.ForEach(snapshot =>
{
    snapshot.IncomingLinks = [];
    snapshot.OutgoingLinks = [];
    snapshot.Scrape = null;
});

var saveWebRefs = webRefsInLinks.Select(webRef => webRef).ToList();
saveWebRefs.ForEach(webRef =>
{
    webRef.Source = null;
    webRef.Target = null;
});

var db = AppDbContext.CreateDefaultDbContext();
await db.Database.MigrateAsync();
db.Scrapes.AddRange(lc.Scrapes);
//await db.SaveChangesAsync();
db.Snapshots.AddRange(saveSites!);
//await db.SaveChangesAsync();
db.WebRefs.AddRange(saveWebRefs);
await db.SaveChangesAsync();


Console.WriteLine("");


public class LinkChecker
{
    public List<Snapshot> Snapshots = new();
    public List<WebRef> WebRefs = new();
    public List<Scrape> Scrapes = new();

    public List<Uri> InitialUris = new();
    private HttpClient _httpClient = new();
    //private AppDbContext _dbContext = AppDbContext.CreateDefaultDbContext();

    public LinkChecker()
    {
        InitialUris.Add(new Uri("https://www.it.tum.de/"));
    }

    public bool ShouldAnalyze(Uri uri)
    {
        return uri.Host == "www.it.tum.de";
    }

    public async Task RunScanner()
    {
        if (InitialUris.Count == 0)
            throw new InvalidOperationException("No websites to scan. Add websites to InitialWebsites first");

        var scrape = new Scrape()
        {
            ScrapeId = Guid.NewGuid(),
            TimeStamp = DateTime.Now,
        };

        Scrapes.Add(scrape);

        foreach (var initialWebsite in InitialUris)
        {
            var website = GetOrCreateWebsite(initialWebsite, scrape);
            website.AnalyzeBecauseUrl = true;
        }

        int count = 0;
        while (true)
        {

            count++;
            var snapshot = Snapshots.FirstOrDefault(sn =>
                sn.Status == DataStatus.Pending && (sn.AnalyzeBecauseUrl || sn.AnalyzeBecauseReference));
            if (snapshot == null) break;

            snapshot.Status = DataStatus.Running;

            Console.Write($"Total: {count} | Scanning {snapshot.Uri}");
            try
            {
                var dlResult =
                    await WebDlImplementation.Download(new WebDlTask(snapshot.Uri, false, _httpClient));

                var redirect = (dlResult as WebDlResultSuccess)?.Response.RequestMessage?.RequestUri;

                if (redirect != null && redirect != snapshot.Uri)
                {
                    snapshot.HttpStatusCode = HttpStatusCode.Redirect;
                    snapshot.ContentStatus = DataStatus.NotRequested;
                    snapshot.Status = DataStatus.Finished;

                    var redirectedWebsite = GetOrCreateWebsite(redirect, scrape);
                    redirectedWebsite.Status = DataStatus.Running;
                    redirectedWebsite.AnalyzeBecauseReference = true;
                    redirectedWebsite.AnalyzeBecauseUrl = ShouldAnalyze(redirect);
                    WebRefs.Add(new WebRef()
                    {
                        WebRefId = Guid.NewGuid(),
                        SourceId = snapshot.SnapshotId,
                        TargetId = redirectedWebsite.SnapshotId,
                        Type = "redirect",
                    });

                    snapshot = redirectedWebsite;
                }

                var analyzeResult = await WebContentAnalyzeImplementation.Analyze(new WebContentAnalyzeTask(dlResult));

                if (analyzeResult.HeaderData != null)
                {
                    snapshot.HttpStatusCode = analyzeResult.HeaderData.StatusCode;
                }
                else
                {
                    snapshot.HttpStatusCode = (HttpStatusCode?)(-1);
                }

                if (analyzeResult.HtmlContentData != null)
                {
                    snapshot.ContentStatus = DataStatus.Finished;
                    snapshot.C_Title = analyzeResult.HtmlContentData.Title;
                    snapshot.C_Description = analyzeResult.HtmlContentData.Description;
                    snapshot.C_Typo3PageId = analyzeResult.HtmlContentData.Typo3PageId;
                    snapshot.ContentExceptionType = analyzeResult.WebContentHtmlException?.GetType().ToString();
                    snapshot.ContentExceptionMessage = analyzeResult.WebContentHtmlException?.Message;

                    if (analyzeResult.HtmlContentData?.Links != null)
                    {
                        foreach (var link in analyzeResult.HtmlContentData?.Links!)
                        {
                            var uri = Helpers.TryCreateUri(snapshot.Uri, link.Url);
                            if (uri != null && (uri.Scheme == "http" || uri.Scheme == "https"))
                            {
                                var targetWebsite = GetOrCreateWebsite(SimplifyUri(uri), scrape);

                                var webRef = new WebRef()
                                {
                                    WebRefId = Guid.NewGuid(),
                                    SourceId = snapshot.SnapshotId,
                                    TargetId = targetWebsite.SnapshotId,
                                    LinkText = link.Text,
                                    RawLink = link.Url,
                                    Type = link.Type,
                                };
                                if (targetWebsite.AnalyzeBecauseReference == false)
                                    targetWebsite.AnalyzeBecauseReference = snapshot.AnalyzeBecauseUrl;
                                WebRefs.Add(webRef);
                            }
                            else if (uri == null)// malformed link
                            {
                                var webRef = new WebRef()
                                {
                                    WebRefId = Guid.NewGuid(),
                                    SourceId = snapshot.SnapshotId,
                                    LinkText = link.Text,
                                    RawLink = link.Url,
                                    Type = link.Type,
                                    LinkMalformed = true,
                                };
                                WebRefs.Add(webRef);
                            }
                        }
                    }
                }

                snapshot.Status = DataStatus.Finished;
                Console.WriteLine($" || Done! {snapshot.HttpStatusCode}");
            }
            catch (Exception e)
            {
                snapshot.ExceptionMessage = e.Message;
                snapshot.ExceptionType = e.GetType().ToString();
                snapshot.Status = DataStatus.Error;
                Console.WriteLine($" || Error! {e.Message}");
            }
        }

        scrape.Finished = true;
    }


    /// <summary>
    /// Removes the fragment part of the uri
    /// </summary>
    internal Uri SimplifyUri(Uri uri)
    {
        return new Uri(new UriBuilder(uri) { Fragment = "" }.Uri.ToString());
    }

    internal Snapshot GetOrCreateWebsite(Uri uri, Scrape scrape)
    {
        // canonicalize uri (remove default ports etc)
        uri = new Uri(SimplifyUri(uri).ToString());
        var snapshot = Snapshots.FirstOrDefault(snapshot =>
            snapshot.Uri == uri && snapshot.ScrapeId == scrape.ScrapeId);


        if (snapshot == null)
        {
            snapshot = new Snapshot()
            {
                SnapshotId = Guid.NewGuid(),
                Uri = uri,
                ScrapeId = scrape.ScrapeId,
                AnalyzeBecauseUrl = ShouldAnalyze(uri)
            };
            Snapshots.Add(snapshot);
        }

        return snapshot;
    }
}

public class CustomDistanceRelaxer : IDistanceRelaxer
{
    public double InitialDistance => double.PositiveInfinity;

    public double Combine(double distance, double edgeWeight) => distance + edgeWeight;

    public int Compare(double a, double b) => a.CompareTo(b);

    public double Add(double a, double b) => a + b;

    public double Zero => 0;
}