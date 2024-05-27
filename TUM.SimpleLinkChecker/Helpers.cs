namespace TUM.SimpleLinkChecker;

public static class Helpers
{
    internal static Uri? TryCreateUri(Uri? baseUri, string relativeOrAbsoluteUrl)
    {
        if (!Uri.TryCreate(relativeOrAbsoluteUrl, UriKind.Absolute, out var result))
        {
            if (baseUri == null) return null;
            Uri.TryCreate(baseUri, relativeOrAbsoluteUrl, out result);
        }
        return result;
    }
}