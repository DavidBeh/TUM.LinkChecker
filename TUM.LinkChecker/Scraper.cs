using QuikGraph;
using QuikGraph.Algorithms.ShortestPath;
using QuikGraph.Predicates;

namespace TUM.LinkChecker;

public class Scraper
{
    internal BidirectionalGraph<LCVertex, LCEdge> Graph { get; } = new();
    public List<Website> Websites { get; } = new();
    public Scraper()
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