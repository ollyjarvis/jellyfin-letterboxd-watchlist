using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace LetterboxdWatchlist.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public List<string> Usernames { get; set; } = new List<string>();
    }
}