using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.HappyMovie
{
    public class TvEpisodeImageProvider : IRemoteImageProvider
    {
        public string Name => Utils.ProviderName;

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return Utils.GetHttpClient().GetAsync(new Uri(url), cancellationToken);
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var episode = (Episode)item;
            var series = episode.Series;

            var seriesTmdbId = Convert.ToInt32(series?.GetProviderId(MetadataProvider.Tmdb), CultureInfo.InvariantCulture);

            if (seriesTmdbId <= 0)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var seasonNumber = episode.ParentIndexNumber;
            var episodeNumber = episode.IndexNumber;

            if (!seasonNumber.HasValue || !episodeNumber.HasValue)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var language = item.GetPreferredMetadataLanguage();

            var client = Utils.GetTmdbClient();
            var episodeResult = client.GetTvEpisodeAsync(seriesTmdbId, seasonNumber.Value, episodeNumber.Value, language: item.PreferredMetadataLanguage, cancellationToken: cancellationToken).Result;

            var stills = episodeResult?.Images?.Stills;

            if (stills == null)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }
            var remoteImages = new List<RemoteImageInfo>();

            remoteImages.Add(new RemoteImageInfo()
            {
                Url = $"{Utils.ImageUrlPrefix}{episodeResult.StillPath}",
                ProviderName = Name,
                Type = ImageType.Primary,
            });

            return remoteImages;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            yield return ImageType.Primary;
        }

        public bool Supports(BaseItem item)
        {
            return item is Episode;
        }
    }
}