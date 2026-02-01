using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace LetterboxdWatchlist;

public class LetterboxdApi
{
    public async Task<List <FilmResult>> GetFilmsFromWatchlist(string username)
    {
        var handler = new HttpClientHandler()
        {
            AllowAutoRedirect = true
        };

        using (var client = new HttpClient(handler))
        {
            var res = await client.GetAsync("https://letterboxd.com/" + username + "/watchlist/").ConfigureAwait(false);

            List<FilmResult> films = new List<FilmResult>();

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(await res.Content.ReadAsStringAsync().ConfigureAwait(false));

            var posters = htmlDoc.DocumentNode.SelectNodes("//div[@data-component-class='LazyPoster']");
            string filmSlug;

            if (posters != null)
            {
                foreach (var poster in posters)
                {
                    filmSlug = poster.GetAttributeValue("data-item-slug", string.Empty);
                    films.Add(await GetFilmFromURL("https://letterboxd.com/film/" + filmSlug + "/").ConfigureAwait(false));
                }
            }
            else
            {
                throw new Exception("Watchlist does not exist or is empty.");
            }

            return films;
        }
    }

    public async Task<FilmResult> GetFilmFromURL(string Url)
    {
        var handler = new HttpClientHandler()
        {
            AllowAutoRedirect = true
        };

        using (var client = new HttpClient(handler))
        {
            var res = await client.GetAsync(Url).ConfigureAwait(false);

            string letterboxdUrl = res?.RequestMessage?.RequestUri?.ToString();
            var filmSlugRegex = Regex.Match(letterboxdUrl, @"https:\/\/letterboxd\.com\/film\/([^\/]+)\/");

            string filmSlug = filmSlugRegex.Groups[1].Value;
            if (string.IsNullOrEmpty(filmSlug))
                throw new Exception("The search returned no results");

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(await res.Content.ReadAsStringAsync().ConfigureAwait(false));

            var span = htmlDoc.DocumentNode.SelectSingleNode("//body");
            if (span == null)
                throw new Exception("The search returned no results");

            string filmId = span.GetAttributeValue("data-tmdb-id", string.Empty);
            if (string.IsNullOrEmpty(filmId))
                throw new Exception("The search returned no results");

            return new FilmResult(filmSlug, filmId);
        }
    }
}

public class FilmResult
{
    public string filmSlug = string.Empty;
    public string filmId = string.Empty;

    public FilmResult(string filmSlug, string filmId)
    {
        this.filmSlug = filmSlug;
        this.filmId = filmId;
    }
}
