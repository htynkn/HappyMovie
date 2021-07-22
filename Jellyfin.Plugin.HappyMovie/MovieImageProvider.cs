using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.HappyMovie.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using TMDbLib.Client;
using Yove.Proxy;

namespace Jellyfin.Plugin.HappyMovie
{
    public class MovieImageProvider : IRemoteImageProvider
    {
        public string Name => Utils.ProviderName;

        public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            Console.WriteLine($"get image for {url}");

            PluginConfiguration options = Plugin.Instance.Configuration;

            ProxyClient proxyClient = new ProxyClient(options.ProxyHost, options.ProxyPort, ProxyType.Http);

            HttpClientHandler handler = new HttpClientHandler { Proxy = proxyClient };
            HttpClient client = new HttpClient(handler);

            return await client.GetAsync(new Uri(url), cancellationToken);
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var tmdbId = Convert.ToInt32(item.GetProviderId(MetadataProvider.Tmdb), CultureInfo.InvariantCulture);
            Console.WriteLine($"get images for tmdbId: {tmdbId}");
            if (tmdbId <= 0)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }
            else
            {
                TMDbClient client = Utils.GetTmdbClient();

                TMDbLib.Objects.General.ImagesWithId result = client.GetMovieImagesAsync(tmdbId, language: item.GetPreferredMetadataLanguage()).Result;

                if (result == null)
                {
                    return Enumerable.Empty<RemoteImageInfo>();
                }

                var remoteImages = new List<RemoteImageInfo>();


                TMDbLib.Objects.Movies.Movie movie = client.GetMovieAsync(tmdbId).Result;

                remoteImages.Add(new RemoteImageInfo
                {
                    Url = $"${Utils.ImageUrlPrefix}{movie.PosterPath}",
                    ProviderName = Name,
                    Type = ImageType.Primary,
                });

                remoteImages.Add(new RemoteImageInfo
                {
                    Url = $"{Utils.ImageUrlPrefix}{movie.PosterPath}",
                    ProviderName = Name,
                    Type = ImageType.Backdrop,
                });
                Console.WriteLine($"Find image with size: {remoteImages.Count}");
                return remoteImages;
            }
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            yield return ImageType.Primary;
            yield return ImageType.Backdrop;
        }

        public bool Supports(BaseItem item)
        {
            return item is Movie;
        }
    }
}