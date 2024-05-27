// See https://aka.ms/new-console-template for more information

using System.ComponentModel;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using TUM.LinkChecker;
using TUM.SimpleLinkChecker;

var options = new JsonSerializerOptions()
{
    IncludeFields = true,
};
var list = JsonSerializer.Deserialize<List<Website>>(File.ReadAllText("filtered.json"), options);

LinkWebRef(list!);

Create1NfEntries();

void Create1NfEntries()
{
    var output = list!.SelectMany(website => website.SourceSites.Select(webRef => new OutputEntry()
    {
        SourceSite = webRef.website.Uri,
        TargetSite = website.Uri,
        HttpStatusCode = website.HttpStatusCode,
        LinkText = webRef.linkData.Text,
        RawLink = webRef.linkData.Url,
        LinkMalformed = false,
        SourceSiteTitle = webRef.website.ContentData?.Title,
        TargetSiteTitle = website.ContentData?.Title,
    })).Concat(list!.SelectMany(website => website.OutgoingNonHttpHttpsLinks.Select(data => new OutputEntry()
    {
        SourceSite = website.Uri,
        SourceSiteTitle = website.ContentData?.Title,
        RawLink = data.Url,
        LinkMalformed = true,
        LinkText = data.Text,
        TargetSite = null,
        TargetSiteTitle = null,
        HttpStatusCode = null,
    }))).Where(entry => entry.SourceSite == null || entry.SourceSite?.Host == "www.it.tum.de").ToList();
    var serialize = JsonSerializer.Serialize(output
        , options);

    File.WriteAllText("brokenlinks.json", serialize);
}


/*


var filtered = list!.Select(website =>
{
    website.OutgoingNonHttpHttpsLinks = !website.Uri.Host.Equals("www.it.tum.de") ? new List<LinkData>() : website.OutgoingNonHttpHttpsLinks
        .Where(data => !Uri.TryCreate(data.Url, UriKind.RelativeOrAbsolute, out var _)).ToList();
    return website;
}).Where(website => website.State == WebsiteState.Scanned &&
                    (website.OutgoingNonHttpHttpsLinks.Count > 0 || website.HttpStatusCode == null ||
                     (int)website.HttpStatusCode < 200 ||
                     (int)website.HttpStatusCode >= 300)).ToList();
list!.ExceptBy(filtered.Select(website => website.Id), website => website.Id).ToList()
    .ForEach(website => website.SourceSites = new List<WebRef>());
filtered = filtered.Concat(filtered.SelectMany(website => website.SourceSites).Select(webRef => webRef.website)).DistinctBy(website => website.Id).ToList();
Console.WriteLine();

var serialize = JsonSerializer.Serialize(filtered, options);

File.WriteAllText("filtered.json", serialize);

var deserialize = JsonSerializer.Deserialize<List<Website>>(File.ReadAllText("filtered.json"), options);

LinkWebRef(deserialize!);
*/

void LinkWebRef(List<Website> websites)
{
    foreach (var website in websites)
    {
        website.SourceSites.ForEach(webRef => webRef.website = websites.First(w => w.Id == webRef.WebsiteId));
    }
}

public class OutputEntry
{
    public bool LinkMalformed;
    public Uri? SourceSite { get; set; }
    public string? SourceSiteTitle { get; set; }
    public Uri? TargetSite { get; set; }
    public string? TargetSiteTitle { get; set; }
    public HttpStatusCode? HttpStatusCode { get; set; }
    public string? LinkText { get; set; }
    public string? RawLink { get; set; }
}