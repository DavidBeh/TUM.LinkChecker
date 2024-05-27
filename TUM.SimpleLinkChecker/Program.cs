// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using TUM.LinkChecker;
using TUM.LinkChecker.Model;
using TUM.SimpleLinkChecker;

Console.WriteLine("Hello, World!");


var lc = new LinkChecker();
lc.AddWebsite("https://www.it.tum.de/");
await lc.RunScanner();

// Write the data of lc.Websites to a json file using system.text.json
// and open the directory in windows explorer
// Serialize lc.Websites to a JSON string
string jsonString = JsonSerializer.Serialize(lc.Websites, new JsonSerializerOptions()
{
    IncludeFields = true,
});

// Define the path of the JSON file
string filePath = @"results.json";

// Write the JSON string to the file
await File.WriteAllTextAsync(filePath, jsonString);

Process.Start("explorer.exe", ".");

public class LinkChecker
{
    public List<Website> Websites = new();
    private HttpClient _httpClient = new();

    public LinkChecker()
    {
    }

    public async Task RunScanner()
    {
        while (true)
        {
            //if (Websites.Count > 4000) break;
            var website = Websites.FirstOrDefault(
                website1 => website1.State == WebsiteState.JustAdded &&
                            (website1.Uri.Host.Equals("www.it.tum.de") ||
                             website1.SourceSites.Any(tuple =>
                                 tuple.website.Uri.Host
                                     .Equals("www.it.tum.de"))));
            if (website == null) break;
            var sourceSite = website.Uri.Host.Equals("www.it.tum.de")
                ? null
                : website.SourceSites.FirstOrDefault(tuple =>
                    tuple.website.Uri.Host.Equals("www.it.tum.de")).website;
            var doneCount = Websites.Count(website1 => website1.State == WebsiteState.Scanned);
            double percentageDone = doneCount / (double)Websites.Count;
            Console.Write(
                $"Total: {Websites.Count} {percentageDone:0.##}% Done | Scanning {website.Uri} from {sourceSite?.Uri}...");
            try
            {
                var dlResult =
                    await WebDlImplementation.Download(new WebDlTask(website.OriginalUri, false, _httpClient));

                var redirect = (dlResult as WebDlResultSuccess)?.Response.RequestMessage?.RequestUri;

                if (!website.OriginalUri.Equals(redirect))
                    website.RedirectUri = redirect;

                var analyzeResult = await WebContentAnalyzeImplementation.Analyze(new WebContentAnalyzeTask(dlResult));
                if (analyzeResult.HeaderData != null)
                {
                    website.HttpStatusCode = analyzeResult.HeaderData.StatusCode;
                }

                if (analyzeResult.HtmlContentData != null)
                {
                    website.ContentData = new()
                    {
                        ExceptionMessage = analyzeResult.WebContentHtmlException?.Message,
                        ExceptionType = analyzeResult.WebContentHtmlException?.GetType().ToString(),
                        Title = analyzeResult.HtmlContentData.Title,
                        Description = analyzeResult.HtmlContentData.Description,
                        Typo3PageId = analyzeResult.HtmlContentData.Typo3PageId
                    };

                    if (analyzeResult.HtmlContentData?.Links != null)
                        foreach (var link in analyzeResult.HtmlContentData?.Links!)
                        {
                            var uri = Helpers.TryCreateUri(website.Uri, link.Url);
                            if (uri != null && (uri.Scheme == "http" || uri.Scheme == "https"))
                            {
                                var targetWebsite = GetOrCreateWebsite(SimplifyUri(uri));
                                targetWebsite.SourceSites.Add(new(link, website));
                            }
                            else
                            {
                                website.OutgoingNonHttpHttpsLinks.Add(link);
                            }
                        }
                }

                Console.WriteLine($" || Done! {website.HttpStatusCode}");
            }
            catch (Exception e)
            {
                website.ExceptionMessage = e.Message;
                website.ExceptionType = e.GetType().ToString();
                Console.WriteLine($"|| Error: {e}");
            }

            website.State = WebsiteState.Scanned;
        }
    }

    /// <summary>
    /// Removes the fragment part of the uri
    /// </summary>
    internal Uri SimplifyUri(Uri uri)
    {
        return new UriBuilder(uri) { Fragment = "" }.Uri;
    }

    public Website? AddWebsite(string url)
    {
        var uri = SimplifyUri(new Uri(url));
        if (uri.Scheme != "http" && uri.Scheme != "https") return null;
        return GetOrCreateWebsite(uri);
    }

    internal Website GetOrCreateWebsite(Uri simplifiedUri)
    {
        var website = Websites.FirstOrDefault(website1 => website1.Uri == simplifiedUri);
        if (website == null)
        {
            website = new Website(simplifiedUri);
            Websites.Add(website);
        }

        return website;
    }
}

public class Website(Uri originalUri)
{
    public Website() : this(null!)
    {
    }

    public Guid Id = Guid.NewGuid();
    public Uri OriginalUri = originalUri;
    public Uri? RedirectUri;
    [JsonIgnore] public Uri Uri => RedirectUri ?? OriginalUri;
    public List<WebRef> SourceSites = new();
    public List<LinkData> OutgoingNonHttpHttpsLinks = new();
    public WebsiteState State = WebsiteState.JustAdded;
    public string? ExceptionMessage;
    public string? ExceptionType;
    public WebsiteContentData? ContentData;
    public HttpStatusCode? HttpStatusCode;
}

public class WebRef
{
    public WebRef()
    {
        website = new Website();
    }

    public WebRef(LinkData linkData, Website website)
    {
        this.linkData = linkData;
        this.website = website;
    }

    public LinkData linkData;
    [JsonIgnore] public Website website;

    public Guid WebsiteId
    {
        get => website.Id;
        set => website.Id = value;
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