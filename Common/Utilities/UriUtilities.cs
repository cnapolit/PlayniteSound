namespace PlayniteSounds.Common.Utilities
{
    public static class UriUtilities
    {
        const string UrlSeparator = "/";

        public static string GetParent(string url) => url.Substring(0, url.LastIndexOf(UrlSeparator));
    }
}