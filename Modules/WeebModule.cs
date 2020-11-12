using System;
using System.Collections.Generic;
using JikanDotNet;
using System.Linq;
using DSharpPlus.Entities;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using YouTubeSearch;

namespace Meiyounaise.Modules
{
    [Group("anime"), Aliases("a")]
    public class WeebModule : BaseCommandModule
    {
        private readonly IJikan _jikan = new Jikan();

        [GroupCommand]
        public async Task Anime(CommandContext ctx, [RemainingText] string query)
        {
            var search = await _jikan.SearchAnime(query);
            var anime = await _jikan.GetAnime(search.Results.First().MalId);

            var aired = anime.Aired.To.HasValue
                ? $"From {anime.Aired.From?.ToShortDateString()} to {anime.Aired.To.Value.ToShortDateString()}"
                : $"{(anime.Aired.From.HasValue ? anime.Aired.From.Value.ToShortDateString() : "?")}";
            try
            {
                var eb = new DiscordEmbedBuilder()
                    .WithColor(anime.Airing ? DiscordColor.SapGreen : DiscordColor.Azure)
                    .WithAuthor(anime.Title, anime.LinkCanonical)
                    .WithThumbnail(anime.ImageURL)
                    .WithDescription(anime.Synopsis.Length > 2000
                        ? anime.Synopsis.Remove(2000)
                        : anime.Synopsis)
                    .AddField("Genre", $"{string.Join(", ", anime.Genres)}", true)
                    .AddField("Episodes", anime.Episodes.HasValue ? anime.Episodes.Value.ToString() : "Unknown", true)
                    .AddField("Aired", aired, true)
                    .AddField("Rating", anime.Score.HasValue ? anime.Score.ToString() : "No Rating", true)
                    .AddField("Studio(s)",
                        anime.Studios.Count == 0
                            ? "No known studio."
                            : string.Join(", ", anime.Studios.Select(x => x.Name)), true)
                    .AddField("Length", anime.Duration, true);
                await ctx.RespondAsync(embed: eb.Build());
            }
            catch (Exception)
            {
                throw new Exception($"Something went wrong trying to get anime `{query}`");
            }
        }

        [Command("manga")]
        public async Task Manga(CommandContext ctx, [RemainingText] string query)
        {
            var search = await _jikan.SearchManga(query);
            var manga = await _jikan.GetManga(search.Results.First().MalId);

            var published = manga.Published.To.HasValue
                ? $"From {manga.Published.From?.ToShortDateString()} to {manga.Published.To.Value.ToShortDateString()}"
                : $"{(manga.Published.From.HasValue ? manga.Published.From.Value.ToShortDateString() : "?")}";

            try
            {
                var eb = new DiscordEmbedBuilder()
                    .WithColor(manga.Publishing ? DiscordColor.SapGreen : DiscordColor.Azure)
                    .WithAuthor(manga.Title, manga.LinkCanonical)
                    .WithThumbnail(manga.ImageURL)
                    .WithDescription(manga.Synopsis)
                    .AddField("Author(s)", string.Join(", ", manga.Authors.Select(x => $"[{x.Name}]({x.Url})")), true)
                    .AddField("Published", published, true)
                    .AddField("Genre", string.Join(", ", manga.Genres), true)
                    .AddField("Chapters", manga.Chapters.HasValue ? manga.Chapters.Value.ToString() : "Unknown", true)
                    .AddField("Rating", manga.Score.HasValue ? manga.Score.ToString() : "No Rating", true)
                    .AddField("Volumes", manga.Volumes.HasValue ? manga.Volumes.Value.ToString() : "Unknown", true);
                await ctx.RespondAsync(embed: eb.Build());
            }
            catch (Exception)
            {
                throw new Exception($"Something went wrong trying to get manga `{query}`");
            }
        }

        [Command("opening"), Aliases("op")]
        public async Task Opening(CommandContext ctx, [RemainingText] string query)
        {
            var search = await _jikan.SearchAnime(query);
            var anime = await SkimForTheme(search.Results, a => a.OpeningTheme.Count > 0);
            if (anime == null) throw new Exception("Anime either has no opening or was not found!");
            var interactivity = ctx.Client.GetInteractivity();

            var yt = new VideoSearch();

            var links = new List<Page>();
            var counter = 0;
            foreach (var opening in anime.OpeningTheme.Select(x => x.Substring(x.IndexOf('\"') + 1)))
            {
                var video = await yt.GetVideos($"{opening.Remove(opening.IndexOf('\"'))} {anime.Title}", 1);
                links.Add(new Page(
                    $"Opening {++counter}/{anime.OpeningTheme.Count}: {anime.OpeningTheme.ElementAt(counter - 1)}\n{video.First().getUrl()}"));
            }

            if (links.Count > 1)
                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.Member, links);
            else
                await ctx.RespondAsync(links.First().Content);
        }

        [Command("ending"), Aliases("ed")]
        public async Task Ending(CommandContext ctx, [RemainingText] string query)
        {
            var search = await _jikan.SearchAnime(query);
            var anime = await SkimForTheme(search.Results, a => a.EndingTheme.Count > 0);
            if (anime == null) throw new Exception("Anime either has no ending or was not found!");
            var interactivity = ctx.Client.GetInteractivity();

            var yt = new VideoSearch();

            var links = new List<Page>();
            var counter = 0;
            foreach (var ending in anime.EndingTheme.Select(x => x.Substring(x.IndexOf('\"') + 1)))
            {
                var q = $"{ending.Remove(ending.IndexOf('\"'))} {anime.Title}";
                var video = await yt.GetVideos(q, 1);
                links.Add(new Page(
                    $"Ending {++counter}/{anime.EndingTheme.Count}: {anime.EndingTheme.ElementAt(counter - 1)}\n{video.First().getUrl()}"));
            }

            if (links.Count > 1)
                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.Member, links);
            else
                await ctx.RespondAsync(links.First().Content);
        }

        private async Task<Anime> SkimForTheme(IEnumerable<AnimeSearchEntry> results, Predicate<Anime> predicate)
        {
            var threshold = 5;
            foreach (var entry in results)
            {
                var anime = await _jikan.GetAnime(entry.MalId);

                if (predicate.Invoke(anime))
                    return anime;

                if (--threshold == 0)
                    return null;
            }

            return null;
        }

        [Command("recommend"), Aliases("r", "rec")]
        public async Task Recommend(CommandContext ctx, [RemainingText] string query)
        {
            var search = await _jikan.SearchAnime(query);
            var recommendations = await _jikan.GetAnimeRecommendations(search.Results.First().MalId);
            var interactivity = ctx.Client.GetInteractivity();

            var pages = recommendations.RecommendationCollection.Select(rec
                    => new DiscordEmbedBuilder()
                        .WithAuthor(rec.Title, rec.RecommendationUrl)
                        .WithColor(DiscordColor.Azure)
                        .WithImageUrl(rec.ImageURL))
                .Select(eb => new Page(embed: eb));

            await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.Member, pages);
        }

        [Command("mangarecommend"), Aliases("mrecommend", "mr", "mrec")]
        public async Task MangaRecommend(CommandContext ctx, [RemainingText] string query)
        {
            var search = await _jikan.SearchAnime(query);
            var recommendations = await _jikan.GetMangaRecommendations(search.Results.First().MalId);
            var interactivity = ctx.Client.GetInteractivity();

            var pages = recommendations.RecommendationCollection.Select(rec
                    => new DiscordEmbedBuilder()
                        .WithAuthor(rec.Title, rec.RecommendationUrl)
                        .WithColor(DiscordColor.Azure)
                        .WithImageUrl(rec.ImageURL))
                .Select(eb => new Page(embed: eb));

            await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.Member, pages);
        }

        [Command("character"), Aliases("char")]
        public async Task Character(CommandContext ctx, [RemainingText] string query)
        {
            var search = await _jikan.SearchCharacter(query);
            var character = await _jikan.GetCharacter(search.Results.First().MalId);

            try
            {
                var eb = new DiscordEmbedBuilder()
                    .WithAuthor(
                        $"{character.Name}" +
                        $"{(string.IsNullOrEmpty(character.NameKanji) ? "" : $"({character.NameKanji})")}",
                        character.LinkCanonical)
                    .WithThumbnail(character.ImageURL)
                    .WithColor(DiscordColor.Azure)
                    .WithDescription(character.About ?? "No Description provided.")
                    .AddField("Voice Actor(s)",
                        character.VoiceActors.Count == 0
                            ? "No known voice actors."
                            : string.Join("; ",
                                character.VoiceActors.Select(x => $"[{x.Name} ({x.Language})]({x.Url})")),
                        true)
                    .AddField("Anime Appereances",
                        character.Animeography.Count == 0
                            ? "None"
                            : string.Join(", ", character.Animeography.Select(x => $"[{x.Name} ({x.Role})]({x.Url})")),
                        true)
                    .AddField("Manga Appereances",
                        character.Mangaography.Count == 0
                            ? "None"
                            : string.Join(", ", character.Mangaography.Select(x => $"[{x.Name} ({x.Role})]({x.Url})")),
                        true);
                await ctx.RespondAsync(embed: eb.Build());
            }
            catch (Exception)
            {
                throw new Exception($"Something went wrong trying to get character `{query}`");
            }
        }
    }
}