using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.HappyMovie.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using TMDbLib.Client;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;
using Yove.Proxy;

namespace Jellyfin.Plugin.HappyMovie
{
    public class TvSeriesProvider : IRemoteMetadataProvider<Series, SeriesInfo>
    {
        public string Name => Utils.ProviderName;

        private readonly ILogger<TvSeriesProvider> _logger;
        private readonly ILibraryManager _libraryManager;

        public TvSeriesProvider(IApplicationPaths appPaths, ILogger<TvSeriesProvider> logger, ILibraryManager libraryManager)
        {
            _logger = logger;
            _libraryManager = libraryManager;
        }

        public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            Console.WriteLine($"get image for {url}");

            PluginConfiguration options = Plugin.Instance.Configuration;

            ProxyClient proxyClient = new ProxyClient(options.ProxyHost, options.ProxyPort, ProxyType.Http);

            HttpClientHandler handler = new HttpClientHandler { Proxy = proxyClient };
            HttpClient client = new HttpClient(handler);

            return await client.GetAsync(new Uri(url), cancellationToken);
        }

        public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
        {
            Console.WriteLine($"getMetadata for {info.Name}");

            var tmdbId = info.GetProviderId(MetadataProvider.Tmdb);

            Console.WriteLine($"get meta tmdbId is {tmdbId} for {info.Name}");

            TMDbClient client = Utils.GetTmdbClient();

            if (string.IsNullOrEmpty(tmdbId))
            {
                var parsedName = _libraryManager.ParseName(info.Name);

                TMDbLib.Objects.General.SearchContainer<SearchTv> tvs = client.SearchTvShowAsync(parsedName.Name, language: info.MetadataLanguage).Result;

                if (tvs.TotalPages > 0)
                {
                    tmdbId = tvs.Results[0].Id.ToString();
                    Console.WriteLine($"get metadata with search to become id: {tmdbId} for name: {parsedName}");
                }
            }

            var tvShow = client.GetTvShowAsync(Convert.ToInt32(tmdbId), language: info.MetadataLanguage).Result;

            if (tvShow == null)
            {
                Console.WriteLine($"can't find tvShow for {tmdbId}");
                return null;
            }

            var series = new Series
            {
                Name = tvShow.Name,
                OriginalTitle = tvShow.OriginalName
            };

            series.SetProviderId(MetadataProvider.Tmdb, tvShow.Id.ToString());
            series.Overview = tvShow.Overview;

            if (tvShow.Networks != null)
            {
                series.Studios = tvShow.Networks.Select(i => i.Name).ToArray();
            }

            if (tvShow.Genres != null)
            {
                series.Genres = tvShow.Genres.Select(i => i.Name).ToArray();
            }

            series.HomePageUrl = tvShow.Homepage;

            if (string.Equals(tvShow.Status, "Ended", StringComparison.OrdinalIgnoreCase))
            {
                series.Status = SeriesStatus.Ended;
                series.EndDate = tvShow.LastAirDate;
            }
            else
            {
                series.Status = SeriesStatus.Continuing;
            }

            series.PremiereDate = tvShow.FirstAirDate;

            var result = new MetadataResult<Series>
            {
                Item = series,
                ResultLanguage = info.MetadataLanguage ?? tvShow.OriginalLanguage
            };

            result.HasMetadata = result.Item != null;

            return result;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo searchInfo, CancellationToken cancellationToken)
        {
            var tmdbId = Convert.ToInt32(searchInfo.GetProviderId(MetadataProvider.Tmdb), CultureInfo.InvariantCulture);
            Console.WriteLine($"current tmdbId is {tmdbId} for {searchInfo.Name}");

            PluginConfiguration options = Plugin.Instance.Configuration;

            var results = new List<RemoteSearchResult>();

            TMDbClient client = Utils.GetTmdbClient();

            if (tmdbId == 0)
            {
                TMDbLib.Objects.General.SearchContainer<TMDbLib.Objects.Search.SearchTv> result = client.SearchTvShowAsync(searchInfo.Name, language: searchInfo.MetadataLanguage).Result;

                foreach (SearchTv searchTv in result.Results)
                {
                    TMDbLib.Objects.TvShows.TvShow tvShow = client.GetTvShowAsync(searchTv.Id, language: searchInfo.MetadataLanguage).Result;
                    var remoteSearchResult = new RemoteSearchResult
                    {
                        Name = tvShow.Name,
                        ImageUrl = $"{Utils.ImageUrlPrefix}{tvShow.PosterPath}",
                        Overview = tvShow.Overview,
                        SearchProviderName = Name
                    };

                    remoteSearchResult.SetProviderId(MetadataProvider.Tmdb, searchTv.Id.ToString(CultureInfo.InvariantCulture));
                    if (tvShow.ExternalIds != null)
                    {
                        if (!string.IsNullOrEmpty(tvShow.ExternalIds.ImdbId))
                        {
                            remoteSearchResult.SetProviderId(MetadataProvider.Imdb, tvShow.ExternalIds.ImdbId);
                        }

                        if (!string.IsNullOrEmpty(tvShow.ExternalIds.TvdbId))
                        {
                            remoteSearchResult.SetProviderId(MetadataProvider.Tvdb, tvShow.ExternalIds.TvdbId);
                        }
                    }

                    remoteSearchResult.PremiereDate = tvShow.FirstAirDate?.ToUniversalTime();

                    results.Add(remoteSearchResult);
                }
            }
            else
            {
                TMDbLib.Objects.TvShows.TvShow tvShow = client.GetTvShowAsync(tmdbId, language: searchInfo.MetadataLanguage).Result;

                if (tvShow == null)
                {
                    return Enumerable.Empty<RemoteSearchResult>();
                }

                var remoteSearchResult = new RemoteSearchResult
                {
                    Name = tvShow.Name,
                    ImageUrl = $"{Utils.ImageUrlPrefix}{tvShow.PosterPath}",
                    Overview = tvShow.Overview,
                    SearchProviderName = Name
                };

                remoteSearchResult.SetProviderId(MetadataProvider.Tmdb, tvShow.Id.ToString(CultureInfo.InvariantCulture));
                if (tvShow.ExternalIds != null)
                {
                    if (!string.IsNullOrEmpty(tvShow.ExternalIds.ImdbId))
                    {
                        remoteSearchResult.SetProviderId(MetadataProvider.Imdb, tvShow.ExternalIds.ImdbId);
                    }

                    if (!string.IsNullOrEmpty(tvShow.ExternalIds.TvdbId))
                    {
                        remoteSearchResult.SetProviderId(MetadataProvider.Tvdb, tvShow.ExternalIds.TvdbId);
                    }
                }

                remoteSearchResult.PremiereDate = tvShow.FirstAirDate?.ToUniversalTime();
            }

            return results;
        }
    }

}