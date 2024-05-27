using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using TUM.LinkChecker;
using TUM.LinkChecker.Model;


var baseUrl = new Uri("https://tum.de");

var testUrl1 = "/studium/studienangebot/studiengaenge/studiengang.html";
var testUrl2 = "https://www.it.tum.de/it/aktuelles/";

var uri1 = TryCreateUri(baseUrl, testUrl1);
var uri2 = TryCreateUri(baseUrl, testUrl2);
Uri.TryCreate(baseUrl, testUrl2, out var uri3);
Console.WriteLine($"{uri1}\n{uri2}\n{uri3}");

Uri? TryCreateUri(Uri? baseUri, string relativeOrAbsoluteUrl)
{
    if (!Uri.TryCreate(relativeOrAbsoluteUrl, UriKind.Absolute, out var result))
    {
        if (baseUri == null) return null;
        Uri.TryCreate(baseUri, relativeOrAbsoluteUrl, out result);
    }
    return result;
}