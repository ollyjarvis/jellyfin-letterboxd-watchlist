using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities.Libraries;
using Jellyfin.Database.Implementations.Enums;
using LetterboxdWatchlist.Configuration;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LetterboxdWatchlist;

public class LetterboxdWatchlistTask : IScheduledTask
{
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILibraryManager _libraryManager;
    private readonly ICollectionManager _collectionManager;
    private readonly IUserManager _userManager;
    private readonly IUserDataManager _userDataManager;
    private readonly IActivityManager _activityManager;

    public LetterboxdWatchlistTask(
            IUserManager userManager,
            ILoggerFactory loggerFactory,
            ILibraryManager libraryManager,
            ICollectionManager collectionManager,
            IActivityManager activityManager,
            IUserDataManager userDataManager)
    {
        _logger = loggerFactory.CreateLogger<LetterboxdWatchlistTask>();
        _loggerFactory = loggerFactory;
        _userManager = userManager;
        _libraryManager = libraryManager;
        _collectionManager = collectionManager;
        _activityManager = activityManager;
        _userDataManager = userDataManager;
    }

    private static PluginConfiguration Configuration =>
            Plugin.Instance!.Configuration;

    public string Name => "View your Letterboxd Watchlist in Jellyfin";

    public string Key => "Letterboxd Watchlist";

    public string Description => "View your Letterboxd Watchlist in Jellyfin";

    public string Category => "Letterboxd Watchlist";


    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var usernames = Configuration.Usernames;

        foreach (var username in usernames)
        {
            var api = new LetterboxdApi();

            var watchlistItems = await api.GetFilmsFromWatchlist(username.username).ConfigureAwait(false);
            var watchlistFilmIds = watchlistItems.Select(w => w.filmId).ToList();

            var localItems = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = [BaseItemKind.Movie],
                IsVirtualItem = false,
                OrderBy = new List<(ItemSortBy, SortOrder)>
            {
                new(ItemSortBy.SortName, SortOrder.Ascending)
            },
                Recursive = true,
                HasTmdbId = true
            }).Where(m => m.ProviderIds.Values.Any(p => watchlistFilmIds.Contains(p))).ToList();

            var boxSets = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = [BaseItemKind.BoxSet],
                CollapseBoxSetItems = false,
                Recursive = true,
            }).Select(b => b as BoxSet).ToList();

            string boxSetTitle = $"{username.username}'s Watchlist";

            var watchlistBoxSet = boxSets.FirstOrDefault(b => string.Equals(b.Name, boxSetTitle, StringComparison.OrdinalIgnoreCase));

            if (watchlistBoxSet == null)
            {
                var newBoxSet = await _collectionManager.CreateCollectionAsync(new CollectionCreationOptions
                {
                    Name = boxSetTitle,
                }).ConfigureAwait(false);
            }

            boxSets = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = [BaseItemKind.BoxSet],
                CollapseBoxSetItems = false,
                Recursive = true,
            }).Select(b => b as BoxSet).ToList();

            watchlistBoxSet = boxSets.FirstOrDefault(b => string.Equals(b.Name, boxSetTitle, StringComparison.OrdinalIgnoreCase));

            if (watchlistBoxSet == null)
            {
                _logger.LogError("BoxSet does not exist after creation.");
                return;
            }

            var itemsToAdd = localItems.Where(m => !watchlistBoxSet.ContainsLinkedChildByItemId(m.Id)).Select(m => m.Id).ToList();

            if (itemsToAdd.Count == 0)
            {
                _logger.LogInformation(@"{Username}'s Watchlist is already complete", username.username);
                return;
            }

            await _collectionManager.AddToCollectionAsync(watchlistBoxSet.Id, itemsToAdd).ConfigureAwait(false);
        }

        progress.Report(100);
        return;
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfoType.DailyTrigger,
                    TimeOfDayTicks = TimeSpan.FromDays(1).Ticks
                }
            };
}
