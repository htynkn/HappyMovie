using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.HappyMovie
{

    public class Plugin : BasePlugin<PluginConfiguration>
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer) { }
        public override string Name => "Happy Movie";
        public override Guid Id => Guid.Parse("a3a07da4-ae5a-4d4a-a843-5aa7e3ba0a62");
    }

}