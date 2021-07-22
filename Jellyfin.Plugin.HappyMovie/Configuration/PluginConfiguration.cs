using System.Reflection;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.HappyMovie.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string ApiKey { get; set; } = "4219e299c89411838049ab0dab19ebd5";
        public string ProxyHost { get; set; } = "127.0.0.1";
        public int ProxyPort { get; set; } = 8118;

        public ProxyType Type { get; set; } = ProxyType.NON_PROXY;

        public enum ProxyType
        {
            NON_PROXY = 0,
            HTTP = 1
        }


    }
}