using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.HappyMovie.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
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

                TMDbLib.Objects.General.SearchContainer<SearchTv> tvs = await client.SearchTvShowAsync(parsedName.Name, language: info.MetadataLanguage, includeAdult: Utils.IncludeAdult());

                if (tvs.TotalPages > 0)
                {
                    tmdbId = tvs.Results[0].Id.ToString();
                    Console.WriteLine($"get metadata with search to become id: {tmdbId} for name: {parsedName}");
                }
            }

            var tvShow = await client.GetTvShowAsync(Convert.ToInt32(tmdbId), language: info.MetadataLanguage);
            var tvCredits = await client.GetTvShowCreditsAsync(Convert.ToInt32(tmdbId), language: info.MetadataLanguage);

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

            if (tvCredits?.Cast != null)
            {
                var cast = tvCredits.Cast.OrderBy(c => c.Order).Take(Utils.MaxCastMembers).ToList();
                for (var i = 0; i < cast.Count; i++)
                {
                    var p = new PersonInfo
                    {
                        Name = cast[i].Name.Trim(),
                        Role = cast[i].Character,
                        Type = PersonKind.Actor,
                        SortOrder = cast[i].Order
                    };

                    if (!string.IsNullOrEmpty(cast[i].ProfilePath))
                    {
                        p.ImageUrl = $"{Utils.ImageUrlPrefix}{cast[i].ProfilePath}";
                    }
                    result.AddPerson(p);
                }
            }

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
                TMDbLib.Objects.General.SearchContainer<TMDbLib.Objects.Search.SearchTv> result = await client.SearchTvShowAsync(searchInfo.Name, language: searchInfo.MetadataLanguage, includeAdult: Utils.IncludeAdult());

                foreach (SearchTv searchTv in result.Results)
                {
                    TMDbLib.Objects.TvShows.TvShow tvShow = await client.GetTvShowAsync(searchTv.Id, language: searchInfo.MetadataLanguage);
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
                TMDbLib.Objects.TvShows.TvShow tvShow = await client.GetTvShowAsync(tmdbId, language: searchInfo.MetadataLanguage);

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