using System;
using MediaBrowser.Model.Plugins;

namespace LetterboxdWatchlist.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public User[] Users { get; set; } = Array.Empty<User>();
    }
}