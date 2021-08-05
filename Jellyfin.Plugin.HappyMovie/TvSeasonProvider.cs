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
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.HappyMovie
{
    public class TvSeasonProvider : IRemoteMetadataProvider<Season, SeasonInfo>
    {
        public string Name => Utils.ProviderName;

        private readonly Logger<TvSeasonProvider> _logger;

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return Utils.GetHttpClient().GetAsync(new Uri(url), cancellationToken);
        }

        public async Task<MetadataResult<Season>> GetMetadata(SeasonInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Season>();

            info.SeriesProviderIds.TryGetValue(MetadataProvider.Tmdb.ToString(), out var seriesTmdbId);

            var seasonNumber = info.IndexNumber;


            if (string.IsNullOrWhiteSpace(seriesTmdbId) || !seasonNumber.HasValue)
            {
                return result;
            }

            TMDbLib.Client.TMDbClient client = Utils.GetTmdbClient();
            var seasonResult = await client.GetTvSeasonAsync(Convert.ToInt32(seriesTmdbId), seasonNumber.Value, language: info.MetadataLanguage, cancellationToken: cancellationToken);

            if (seasonResult == null)
            {
                return result;
            }

            result.HasMetadata = true;
            result.Item = new Season
            {
                IndexNumber = seasonNumber,
                Overview = seasonResult.Overview
            };

            if (!string.IsNullOrEmpty(seasonResult.ExternalIds?.TvdbId))
            {
                result.Item.SetProviderId(MetadataProvider.Tvdb, seasonResult.ExternalIds.TvdbId);
            }

            var credits = seasonResult.Credits;
            if (credits?.Cast != null)
            {
                var cast = credits.Cast.OrderBy(c => c.Order).Take(Utils.MaxCastMembers).ToList();
                for (var i = 0; i < cast.Count; i++)
                {
                    result.AddPerson(new PersonInfo
                    {
                        Name = cast[i].Name.Trim(),
                        Role = cast[i].Character,
                        Type = PersonType.Actor,
                        SortOrder = cast[i].Order
                    });
                }
            }

            result.Item.PremiereDate = seasonResult.AirDate;
            result.Item.ProductionYear = seasonResult.AirDate?.Year;

            return result;
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeasonInfo searchInfo, CancellationToken cancellationToken)
        {
            return Task.FromResult(Enumerable.Empty<RemoteSearchResult>());
        }
    }
}
