using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.HappyMovie
{
    public class TvSeasonImageProvider : IRemoteImageProvider
    {
        public string Name => Utils.ProviderName;

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return Utils.GetHttpClient().GetAsync(new Uri(url), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var season = (Season)item;
            var series = season?.Series;

            var seriesTmdbId = Convert.ToInt32(series?.GetProviderId(MetadataProvider.Tmdb), CultureInfo.InvariantCulture);

            if (seriesTmdbId <= 0 || season?.IndexNumber == null)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var language = item.GetPreferredMetadataLanguage();

            TMDbLib.Client.TMDbClient client = Utils.GetTmdbClient();

            var seasonResult = client.GetTvSeasonAsync(seriesTmdbId, season.IndexNumber.Value, language: item.GetPreferredMetadataLanguage(), cancellationToken: cancellationToken).Result;

            var remoteImages = new List<RemoteImageInfo>();

            remoteImages.Add(new RemoteImageInfo()
            {
                Url = $"{Utils.ImageUrlPrefix}{seasonResult.PosterPath}",
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
            return item is Season;
        }
    }
}
