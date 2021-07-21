using System.Reflection;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.HappyMovie.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string ApiKey { get; set; }


        public PluginConfiguration()
        {
            ApiKey = "4219e299c89411838049ab0dab19ebd5";
        }
    }
}