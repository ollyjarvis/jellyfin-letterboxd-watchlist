using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using LetterboxdWatchlist.Configuration;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

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

    public string Name => "Sync Watchlists";

    public string Key => "Letterboxd Watchlist";

    public string Description => "Sync Letterboxd watchlists to Jellyfin Collections";

    public string Category => "Letterboxd Watchlist";


    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var usernames = Configuration.Usernames;

        foreach (var username in usernames)
        {
            var api = new LetterboxdApi(_loggerFactory);

            var letterboxdWatchlist = await api.GetFilmsFromWatchlist(username, 1).ConfigureAwait(false);
            var watchlistFilmIds = letterboxdWatchlist.Select(w => w.filmId).ToList();

            var watchlistItems = _libraryManager.GetItemList(new InternalItemsQuery
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

            var watchlistItemIds = watchlistItems.Select(m => m.Id).ToList();

            var boxSets = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = [BaseItemKind.BoxSet],
                CollapseBoxSetItems = false,
                Recursive = true,
            }).Select(b => b as BoxSet).ToList();

            string boxSetTitle = $"{username}'s Watchlist";

            var watchlistBoxSet = boxSets.FirstOrDefault(b => string.Equals(b.Name, boxSetTitle, StringComparison.OrdinalIgnoreCase));

            List<Guid> itemsToAdd = watchlistItemIds;
            List<Guid> watchlistBoxSetItems = new List<Guid>();
            List<Guid> itemsToRemove = new List<Guid>();

            if (watchlistBoxSet != null)
            {
                watchlistBoxSetItems = watchlistBoxSet.LinkedChildren.Where(item => item.ItemId.HasValue).Select(item => item.ItemId.Value).ToList();

                itemsToAdd = watchlistItemIds.Except(watchlistBoxSetItems).ToList();
                itemsToRemove = watchlistBoxSetItems.Except(watchlistItemIds).ToList();
            }

            if (itemsToAdd.Count == 0 && itemsToRemove.Count == 0)
            {
                _logger.LogInformation(@"{Username}'s Watchlist is already in sync", username);
                return;
            }

            _logger.LogInformation(@"{Add}, {Remove}", itemsToAdd, itemsToRemove);

            if (watchlistBoxSet == null)
            {
                watchlistBoxSet = await _collectionManager.CreateCollectionAsync(new CollectionCreationOptions
                {
                    Name = boxSetTitle,
                }).ConfigureAwait(false);
            }

            await _collectionManager.RemoveFromCollectionAsync(watchlistBoxSet.Id, itemsToRemove).ConfigureAwait(false);
            await _collectionManager.AddToCollectionAsync(watchlistBoxSet.Id, itemsToAdd).ConfigureAwait(false);

            boxSets = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = [BaseItemKind.BoxSet],
                CollapseBoxSetItems = false,
                Recursive = true,
            }).Select(b => b as BoxSet).ToList();

            watchlistBoxSet = boxSets.FirstOrDefault(b => string.Equals(b.Name, boxSetTitle, StringComparison.OrdinalIgnoreCase));

            /* REMOVING NOT WORKING (pls help)
            if (watchlistBoxSet != null)
            {
                if (watchlistBoxSet.LinkedChildren.Length == 0)
                {
                    _logger.LogInformation("Removing box set {BoxSetName} as it is now empty", watchlistBoxSet.Name);

                    _libraryManager.DeleteItem(watchlistBoxSet, new DeleteOptions
                    {
                        DeleteFileLocation = false
                    });
                }
            }
            */
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
