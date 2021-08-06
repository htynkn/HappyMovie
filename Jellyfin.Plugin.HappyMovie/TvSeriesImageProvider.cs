
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.HappyMovie.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using TMDbLib.Client;
using Yove.Proxy;

namespace Jellyfin.Plugin.HappyMovie
{
    public class TvSeriesImageProvider : IRemoteImageProvider
    {
        public string Name => Utils.ProviderName;
        public bool Supports(BaseItem item)
        {
            return item is Series;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            yield return ImageType.Primary;
            yield return ImageType.Backdrop;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var tmdbId = item.GetProviderId(MetadataProvider.Tmdb);

            var remoteImages = new List<RemoteImageInfo>();

            if (string.IsNullOrEmpty(tmdbId))
            {
                return remoteImages;
            }

            TMDbClient client = Utils.GetTmdbClient();

            TMDbLib.Objects.TvShows.TvShow tvShow = await client.GetTvShowAsync(Convert.ToInt32(tmdbId, CultureInfo.InvariantCulture), language: item.PreferredMetadataLanguage, cancellationToken: cancellationToken);

            if (tvShow == null)
            {
                return remoteImages;
            }

            remoteImages.Add(new RemoteImageInfo
            {
                Url = $"{Utils.ImageUrlPrefix}{tvShow.PosterPath}",
                ProviderName = Name,
                Type = ImageType.Primary,
            });

            remoteImages.Add(new RemoteImageInfo
            {
                Url = $"{Utils.ImageUrlPrefix}{tvShow.BackdropPath}",
                ProviderName = Name,
                Type = ImageType.Backdrop,
            });

            return remoteImages;
        }

        public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            Console.WriteLine($"get image for {url}");

            HttpClient client = Utils.GetHttpClient();

            return await client.GetAsync(new Uri(url), cancellationToken);
        }
    }
}