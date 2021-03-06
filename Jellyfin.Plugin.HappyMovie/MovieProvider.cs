using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
using MetadataProvider = MediaBrowser.Model.Entities.MetadataProvider;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.HappyMovie
{
    public class MovieProvider : IRemoteMetadataProvider<MediaBrowser.Controller.Entities.Movies.Movie, MediaBrowser.Controller.Providers.MovieInfo>
    {
        public string Name => Utils.ProviderName;
        private readonly ILogger<MovieProvider> _logger;
        private readonly ILibraryManager _libraryManager;

        public MovieProvider(IApplicationPaths appPaths, ILogger<MovieProvider> logger, ILibraryManager libraryManager)
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

        public async Task<MetadataResult<MediaBrowser.Controller.Entities.Movies.Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
        {
            var tmdbId = info.GetProviderId(MetadataProvider.Tmdb);

            Console.WriteLine($"get meta tmdbId is {tmdbId} for {info.Name}");

            TMDbClient client = Utils.GetTmdbClient();

            if (string.IsNullOrEmpty(tmdbId))
            {
                var parsedName = _libraryManager.ParseName(info.Name);

                SearchContainer<SearchMovie> movies = await client.SearchMovieAsync(parsedName.Name, language: info.MetadataLanguage, includeAdult: Utils.IncludeAdult());

                if (movies.TotalPages > 0)
                {
                    tmdbId = movies.Results[0].Id.ToString();
                    Console.WriteLine($"get metadata with search to become id: {tmdbId} for name: {parsedName}");
                }
            }

            TMDbLib.Objects.Movies.Movie movie = await client.GetMovieAsync(tmdbId, language: info.MetadataLanguage);

            var m = new Movie
            {
                Name = movie.Title ?? movie.OriginalTitle,
                Overview = movie.Overview?.Replace("\n\n", "\n", StringComparison.InvariantCulture),
                Tagline = movie.Tagline
            };

            if (movie.ReleaseDate != null)
            {
                var releaseDate = movie.ReleaseDate.Value.ToUniversalTime();
                m.PremiereDate = releaseDate;
                m.ProductionYear = releaseDate.Year;
            }

            m.SetProviderId(MetadataProvider.Tmdb, movie.Id.ToString());
            if (movie.BelongsToCollection != null)
            {
                m.SetProviderId(MetadataProvider.TmdbCollection, movie.BelongsToCollection.Id.ToString(CultureInfo.InvariantCulture));
                m.CollectionName = movie.BelongsToCollection.Name;
            }

            m.CommunityRating = Convert.ToSingle(movie.VoteAverage);

            foreach (var genre in movie.Genres.Select(g => g.Name))
            {
                m.AddGenre(genre);
            }

            var metadataResult = new MetadataResult<Movie>
            {
                HasMetadata = true,
                ResultLanguage = info.MetadataLanguage,
                Item = m
            };

            return metadataResult;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
        {
            var tmdbId = Convert.ToInt32(searchInfo.GetProviderId(MetadataProvider.Tmdb), CultureInfo.InvariantCulture);
            Console.WriteLine($"current tmdbId is {tmdbId} for {searchInfo.Name}");

            PluginConfiguration options = Plugin.Instance.Configuration;

            var results = new List<RemoteSearchResult>();

            TMDbClient client = Utils.GetTmdbClient();

            if (tmdbId == 0)
            {
                SearchContainer<SearchMovie> movies = await client.SearchMovieAsync(searchInfo.Name, language: searchInfo.MetadataLanguage, includeAdult: Utils.IncludeAdult());

                foreach (SearchMovie searchMovie in movies.Results)
                {
                    TMDbLib.Objects.Movies.Movie movie = await client.GetMovieAsync(searchMovie.Id, language: searchInfo.MetadataLanguage);
                    var remoteSearchResult = new RemoteSearchResult
                    {
                        Name = searchMovie.Title,
                        ImageUrl = $"{Utils.ImageUrlPrefix}{movie.PosterPath}",
                        Overview = searchMovie.Overview,
                        SearchProviderName = Name
                    };

                    remoteSearchResult.SetProviderId(MediaBrowser.Model.Entities.MetadataProvider.Tmdb, movie.Id.ToString());

                    if (!string.IsNullOrWhiteSpace(movie.ImdbId))
                    {
                        remoteSearchResult.SetProviderId(MetadataProvider.Imdb, movie.ImdbId);
                    }

                    results.Add(remoteSearchResult);
                }
            }
            else
            {
                TMDbLib.Objects.Movies.Movie movie = await client.GetMovieAsync(tmdbId, language: searchInfo.MetadataLanguage);

                if (movie == null)
                {
                    return Enumerable.Empty<RemoteSearchResult>();
                }

                var remoteSearchResult = new RemoteSearchResult
                {
                    Name = movie.Title,
                    ImageUrl = $"{Utils.ImageUrlPrefix}{movie.PosterPath}",
                    Overview = movie.Overview,
                    SearchProviderName = Name
                };

                remoteSearchResult.SetProviderId(MediaBrowser.Model.Entities.MetadataProvider.Tmdb, movie.Id.ToString(CultureInfo.InvariantCulture));

                if (!string.IsNullOrWhiteSpace(movie.ImdbId))
                {
                    remoteSearchResult.SetProviderId(MetadataProvider.Imdb, movie.ImdbId);
                }

                results.Add(remoteSearchResult);
            }

            return results;
        }
    }
}