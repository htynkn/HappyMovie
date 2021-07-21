using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities.Libraries;
using Jellyfin.Plugin.HappyMovie.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using Yove.Proxy;

namespace Jellyfin.Plugin.HappyMovie
{
    public class MovieProvider : IRemoteMetadataProvider<MediaBrowser.Controller.Entities.Movies.Movie, MediaBrowser.Controller.Providers.MovieInfo>
    {
        public string Name => "HappyMovie";
        private readonly ILogger<MovieProvider> _logger;


        public MovieProvider(IApplicationPaths appPaths, ILogger<MovieProvider> logger)
        {
            _logger = logger;
        }

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public async Task<MetadataResult<MediaBrowser.Controller.Entities.Movies.Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            return null;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
        {
            var tmdbId = Convert.ToInt32(searchInfo.GetProviderId(MediaBrowser.Model.Entities.MetadataProvider.Tmdb), CultureInfo.InvariantCulture);
            Console.WriteLine($"current tmdbId is {tmdbId} for {searchInfo.Name}");

            PluginConfiguration options = Plugin.Instance.Configuration;

            var results = new List<RemoteSearchResult>();

            if (tmdbId == 0)
            {
                ProxyClient proxyClient = new ProxyClient("172.16.2.22", 1080, ProxyType.Socks5);
                TMDbClient client = new TMDbClient(options.ApiKey, proxy: proxyClient);

                Console.WriteLine($"start search");
                SearchContainer<SearchMovie> movies = client.SearchMovieAsync(searchInfo.Name, language: searchInfo.MetadataLanguage).Result;
                Console.WriteLine($"find movies with size:${movies.TotalPages}");

                foreach (SearchMovie searchMovie in movies.Results)
                {
                    TMDbLib.Objects.Movies.Movie movie = client.GetMovieAsync(searchMovie.Id, language: searchInfo.MetadataLanguage).Result;
                    var remoteSearchResult = new RemoteSearchResult
                    {
                        Name = searchMovie.Title,
                        ImageUrl = $"https://image.tmdb.org/t/p/w500/{movie.PosterPath}",
                        Overview = searchMovie.Overview,
                        SearchProviderName = Name
                    };

                    remoteSearchResult.SetProviderId(MediaBrowser.Model.Entities.MetadataProvider.Tmdb, movie.Id.ToString(CultureInfo.InvariantCulture));

                    results.Add(remoteSearchResult);
                }

            }

            return results;
        }
    }
}