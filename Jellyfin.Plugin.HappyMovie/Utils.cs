using TMDbLib.Client;
using Yove.Proxy;
using Jellyfin.Plugin.HappyMovie.Configuration;
using System;
using System.Net.Http;

namespace Jellyfin.Plugin.HappyMovie
{
    public static class Utils
    {
        public static string ProviderName => "HappyMovie";

        public static string ImageUrlPrefix = "https://image.tmdb.org/t/p/original";

        public static string BaseTmdbUrl = "https://www.themoviedb.org/";

        public static int MaxCastMembers = 10;

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
            else if (options.Type == HappyMovieProxyType.SOCKS5)
            {
                proxyClient = new ProxyClient(options.ProxyHost, options.ProxyPort, ProxyType.Socks5);
            }
            else
            {
                Console.WriteLine("Unknown proxy type, will use directly connection");
            }

            TMDbClient client = new TMDbClient(options.ApiKey, proxy: proxyClient);
            return client;
        }

        public static bool IncludeAdult()
        {
            PluginConfiguration options = Plugin.Instance.Configuration;

            return options.IncludeAdult;
        }

        public static HttpClient GetHttpClient()
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
            else if (options.Type == HappyMovieProxyType.SOCKS5)
            {
                proxyClient = new ProxyClient(options.ProxyHost, options.ProxyPort, ProxyType.Socks5);
            }
            else
            {
                Console.WriteLine("Unknown proxy type, will use directly connection");
            }

            HttpClientHandler handler = new HttpClientHandler { Proxy = proxyClient };
            HttpClient client = new HttpClient(handler);
            return client;
        }
    }
}