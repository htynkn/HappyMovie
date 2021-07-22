using TMDbLib.Client;
using Yove.Proxy;
using Jellyfin.Plugin.HappyMovie.Configuration;

namespace Jellyfin.Plugin.HappyMovie
{
    public static class Utils
    {
        public static string ProviderName => "HappyMovie";

        public static string ImageUrlPrefix = "https://image.tmdb.org/t/p/w500/";

        public static TMDbClient GetTmdbClient()
        {
            PluginConfiguration options = Plugin.Instance.Configuration;
            ProxyClient proxyClient = new ProxyClient(options.ProxyHost, options.ProxyPort, ProxyType.Http);
            TMDbClient client = new TMDbClient(options.ApiKey, proxy: proxyClient);
            return client;
        }
    }
}