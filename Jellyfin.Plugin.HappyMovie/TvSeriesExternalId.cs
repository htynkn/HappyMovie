using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.HappyMovie
{
    public class TvSeriesExternalId : IExternalId
    {
        public string ProviderName => Utils.ProviderName;

        public string Key => MetadataProvider.Tmdb.ToString();

        public ExternalIdMediaType? Type => ExternalIdMediaType.Series;
        public string UrlFormatString => Utils.BaseTmdbUrl + "tv/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item)
        {
            return item is Series;
        }
    }
}
