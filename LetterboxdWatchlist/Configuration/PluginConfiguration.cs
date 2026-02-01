using System;
using MediaBrowser.Model.Plugins;

namespace LetterboxdWatchlist.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public Username[] Usernames { get; set; } = Array.Empty<Username>();
    }
}