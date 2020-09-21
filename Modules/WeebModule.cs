using System;
using JikanDotNet;
using System.Linq;
using DSharpPlus.Entities;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace Meiyounaise.Modules
{
    [Group("anime"), Aliases("a")]
    public class WeebModule : BaseCommandModule
    {
        private IJikan _jikan = new Jikan(true);

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
                        : anime.Synopsis.Remove(anime.Synopsis.LastIndexOf('[')))
                    .AddField("Genre", $"{string.Join(", ", anime.Genres)}", true)
                    .AddField("Episodes", anime.Episodes.HasValue ? anime.Episodes.Value.ToString() : "Unknown", true)
                    .AddField("Aired", aired, true)
                    .AddField("Rating", anime.Score.HasValue ? anime.Score.ToString() : "No Rating", true)
                    .AddField("Studio(s)", string.Join(", ", anime.Studios.Select(x => x.Name)), true)
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