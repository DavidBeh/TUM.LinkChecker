using QuikGraph;
using QuikGraph.Algorithms.ShortestPath;
using QuikGraph.Predicates;
using TUM.LinkChecker.Model;

namespace TUM.LinkChecker;

public class LinkChecker
{
    internal BidirectionalGraph<Website, WebRef> Graph { get; } = new();
    public List<Website> Websites { get; } = new();
    
    public LinkChecker()
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
    string? ResolveUri(string uri) => null;
    bool? OnAddWebsiteShouldDownload(Website website) => null;
    
    bool? OnAddReferenceShouldDownload(Website source, Website target) => null;
    
}