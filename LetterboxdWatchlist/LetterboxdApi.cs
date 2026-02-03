using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace LetterboxdWatchlist;

public class LetterboxdApi
{
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;

    public LetterboxdApi(
            ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<LetterboxdApi>();
    }

    public async Task<List <FilmResult>> GetFilmsFromWatchlist(string username, int pageNum)
    {
        _logger.LogInformation(@"Grabbing Watchlist: {Url}", "https://letterboxd.com/" + username + "/watchlist/page/" + pageNum + "/");

        var handler = new HttpClientHandler()
        {
            AllowAutoRedirect = true
        };

        using (var client = new HttpClient(handler))
        {
            var res = await client.GetAsync("https://letterboxd.com/" + username + "/watchlist/page/" + pageNum + "/").ConfigureAwait(false);
            res.EnsureSuccessStatusCode();

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
                    if (string.IsNullOrEmpty(filmSlug))
                    {
                        continue;
                    }

                    films.Add(await GetFilmFromURL("https://letterboxd.com/film/" + filmSlug + "/").ConfigureAwait(false));

                    await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                }
            }
            else
            {
                throw new Exception("Watchlist does not exist or is empty.");
            }

            bool isNextPage = htmlDoc.DocumentNode.SelectNodes($"//li[a/text() = '{pageNum + 1}']") is not null;

            if (isNextPage)
            {
                List<FilmResult> nextPageFilms = await GetFilmsFromWatchlist(username, pageNum + 1).ConfigureAwait(false);

                foreach (FilmResult film in nextPageFilms)
                {
                    films.Add(film);
                }
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
            res.EnsureSuccessStatusCode();

            _logger.LogInformation(@"Grabbing Film: {StatusCode}, {Content}, {Url}", res.StatusCode, res.Content, Url);

            string letterboxdUrl = res?.RequestMessage?.RequestUri?.ToString();
            var filmSlugRegex = Regex.Match(letterboxdUrl, @"https:\/\/letterboxd\.com\/film\/([^\/]+)\/");

            string filmSlug = filmSlugRegex.Groups[1].Value;
            if (string.IsNullOrEmpty(filmSlug))
            {
                throw new Exception($"No Slug {Url}");
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(await res.Content.ReadAsStringAsync().ConfigureAwait(false));

            var span = htmlDoc.DocumentNode.SelectSingleNode("//body");
            if (span == null)
            {
                throw new Exception($"No Body {Url}");
            }

            string filmId = span.GetAttributeValue("data-tmdb-id", string.Empty);
            if (string.IsNullOrEmpty(filmId))
            {
                throw new Exception($"No TMDB ID {Url}");
            }

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
