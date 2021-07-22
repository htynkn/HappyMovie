using TMDbLib.Client;
using Yove.Proxy;
using Jellyfin.Plugin.HappyMovie.Configuration;
using System;

namespace Jellyfin.Plugin.HappyMovie
{
    public static class Utils
    {
        public static string ProviderName => "HappyMovie";

        public static string ImageUrlPrefix = "https://image.tmdb.org/t/p/w500/";

        public static TMDbClient GetTmdbClient()
        {
            PluginConfiguration options = Plugin.Instance.Configuration;
            ProxyClient proxyClient = null;

            if (options.Type == HappyMovieProxyType.NON_PROXY)
            {
                proxyClient = null;
            }
            else if (options.Type == HappyMovieProxyType.HTTP)
            {
                proxyClient = new ProxyClient(options.ProxyHost, options.ProxyPort, ProxyType.Http);
            }
            else
            {
                Console.WriteLine("Unknown proxy type, will use directly connection");
            }

            TMDbClient client = new TMDbClient(options.ApiKey, proxy: proxyClient);
            return client;
        }
    }
}