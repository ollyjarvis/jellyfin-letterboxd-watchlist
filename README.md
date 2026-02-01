# Letterboxd Watchlist
    
<p align="center">
    Generate Collections containing items on your Letterboxd watchlist that are found in your Jellyfin Library.
</p>

## About

This plugin updates your collections daily by grabbing the TMDB IDs from Letterboxd watchlists and collates local TMDB ID matches into collections. Also removes items once they leave your watchlist. Since the Letterboxd API is not publicly available, this project uses the HtmlAgilityPack package to interact directly with the website's interface.

## Installation

1. Open the dashboard in Jellyfin, then select `Plugins` and open `Manage Repositories` at the top.

2. Click the `+` button and add the repository URL below, naming it whatever you like and save.

```
https://raw.githubusercontent.com/ollyjarvis/jellyfin-letterboxd-watchlist/master/manifest.json
```

3. Go back to `Plugins`, click on 'Letterboxd Watchlist' in the 'Films and Programmes' group and install the most recent version.

4. Restart Jellyfin and go back to the plugin settings. Go to `My Plugins` and click on 'Letterboxd Watchlist' to add Letterboxds users to track.
   
## Configure

 - Add as many Letterboxd accounts as you like and `Save` the config.

 - The synchronization task runs every 24 hours.

 - Go to Scheduled Tasks to run or change update cadence.

 - For large or many watchlists this will take a while so as to not spam the Letterboxd site too much. I recommend reducing the frequency to every few days/ once a week when searching over 100 films.

## Known Issues


 - Errors on TV shows in watchlist due to TMDB tagging.
