using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Dataflow;
using QuikGraph;
using QuikGraph.Algorithms.ShortestPath;
using QuikGraph.Predicates;
using TUM.LinkChecker.Model;

namespace TUM.LinkChecker;

public class LinkChecker
{
    internal BidirectionalGraph<Website2, WebRef> Graph { get; } = new();
    private HttpClient _httpClient = new();
    private TaskCompletionSource _tcs = new();
    private BufferBlock<Website2> _analysisQueueBlock { get; }
    private HashSet<Website2> _websites;

    private SemaphoreSlim _lock = new(1, 1);
    private ActionBlock<Website2> _analysisPreprocessor;
    private ActionBlock<WebDlTask> _downloadBlock;
    private ActionBlock<WebDlResult> _analysisBlock;
    private ActionBlock<WebContentAnalyzeResult> _analyzePostProcessor;

    public LinkChecker()
    {
        _analysisPreprocessor = new ActionBlock<Website2>(async website =>
        {
            await _lock.WaitAsync();
            try
            {
                website.AnalysisStatus = AnalysisStatus.Queued;
                //await _downloadBlock!.SendAsync(new WebDlTask(website, false, _httpClient));
            }
            catch
            {
                website.AnalysisStatus = AnalysisStatus.Failed;
            }
            finally
            {
                _lock.Release();
            }
        });

        _downloadBlock = new ActionBlock<WebDlTask>(async task =>
        {
            try
            {
                var result = await WebDlImplementation.Download(task);
                await _analysisBlock!.SendAsync(result);
            }
            catch (Exception e)
            {
                await _analysisBlock!.SendAsync(new WebDlResultFail(task, WebDlFailReason.OtherError, e));
            }
        });

        _analysisBlock = new ActionBlock<WebDlResult>(async result =>
        {
            if (result is WebDlResultSuccess success)
            {
                var analyzeTask = WebContentAnalyzeImplementation.Analyze(new WebContentAnalyzeTask(success));
                var analyzeResult = await analyzeTask;
                await _analyzePostProcessor!.SendAsync(analyzeResult);
            }
        });
        

    }

    private void _queueProcess()
    {
        while (true)
        {
        }
    }

    private void onWebsiteUpdated(Website2 website2)
    {
    }

    public void AddWebsite(string url)
    {
        var uri = new Uri(url);
    }
}

public class ScraperConfiguration
{
}

public class WebsitePlan
{
}

public interface IScraperBehavior
{
    Uri? SimplifyUrl(string url) => null;
    bool? ShouldDownloadWebsite(Website2 website2) => null;

    bool? OnAddReferenceShouldDownload(Website2 source, Website2 target) => null;
}