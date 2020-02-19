using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using IF.Lastfm.Core.Api;
using IF.Lastfm.Core.Api.Enums;
using IF.Lastfm.Core.Objects;
using Meiyounaise.DB;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;

namespace Meiyounaise.Modules
{
    [Group("fm"),
     Description(
         "Group containing all last.fm commands. If you just use `fm` you'll get your currently playing song/last played track")]
    public class LastFmModule : BaseCommandModule
    {
        private const string HtmlTemplate =
            "<meta charset=\"UTF-8\"><link href=\"https://fonts.googleapis.com/css?family=Baloo+Thambi\" rel=\"stylesheet\"><style> *{font-size: 15px !important;color: #ffffff !important;line-height: 95%; font-family: 'Baloo Thambi', cursive !important;text-shadow: -1.5px 0 #000, 0 1.5px #000, 1.5px 0 #000, 0 -1.5px #000;} body {margin: 0;}</style>";

        private const string SongChartTemplate =
            "<!doctype html><html><head> <meta charset=\"utf-8\" /><style>body{background: #262626; font-family: sans-serif;}.center{width: 99%; position: absolute;}.skillBox{padding-bottom: 10px;}.skillBox p{letter-spacing: 0.5px; font-size: large; color: #fff; margin: 0 0 3px; padding: 0; font-weight: bold;}.skillBox p:nth-child(2){position: relative; top: -25px; float: right;}.skill{background: #262626; padding: 4px; box-sizing: border-box; border: 1px solid #bb0000;}.skill_level{background: #bb0000; width: 100%; height: 10px;}</style></head><body> <div class=\"center\">";

        private static readonly LastfmClient Client = new LastfmClient(Utilities.GetKey("lastkey"),
            Utilities.GetKey("lastsecret"), new HttpClient());

        [Command("set")]
        [Description("Set your last.fm username.")]
        public async Task FmSet(CommandContext ctx, string username = "")
        {
            if (username == "")
            {
                throw new Exception("I need a name that I can link to your account!");
            }

            if (Users.UserList.Any(x => x.Id == ctx.User.Id))
            {
                Utilities.Con.Open();
                using (var cmd =
                    new SqliteCommand("UPDATE Users SET lastfm = @username WHERE Users.id = @id",
                        Utilities.Con))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@id", ctx.User.Id);
                    cmd.ExecuteReader();
                }

                Users.UpdateUser(ctx.User);
                Utilities.Con.Close();
            }
            else
            {
                Utilities.Con.Open();
                Users.UserList.Add(new Users.User(ctx.User.Id));
                using (var cmd =
                    new SqliteCommand("INSERT INTO Users (id, lastfm, osu) VALUES (@id, @username, '#')",
                        Utilities.Con))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@id", ctx.User.Id);
                    cmd.ExecuteReader();
                }

                Utilities.Con.Close();
                Users.UpdateUser(ctx.User);
            }

            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
        }

        [GroupCommand]
        public async Task Fm(CommandContext ctx,
            [Description("The user you want to see the last track of. Leave empty for own account.")]
            string username = "")
        {
            if (username == "")
            {
                if (username == "#" || Users.UserList.All(x => x.Id != ctx.User.Id))
                {
                    throw new Exception(
                        "I don't have a last.fm name linked to your discord account. Set it using `fm set [Name]`.");
                }

                username = Users.GetUser(ctx.User).Last;
            }

            var response = await Client.User.GetRecentScrobbles(username);
            if (!response.Success)
            {
                if (username == "")
                {
                    throw new Exception("last.fm's response was not successful, try again later!");
                }

                throw new Exception(
                    $"last.fm's response was not successful! Are you sure `{username}` is a valid account?");
            }

            var info = await Client.User.GetInfoAsync(username);

            var isPlaying = response.Content.First().IsNowPlaying != null ? "Now Playing" : "Last Track";

            var embed = new DiscordEmbedBuilder()
                .WithAuthor($"{username} - {isPlaying}", $"https://www.last.fm/user/{username}",
                    "http://icons.iconarchive.com/icons/sicons/basic-round-social/256/last.fm-icon.png")
                .WithColor(DiscordColor.Red)
                .WithDescription(string.Concat(
                    $"[{response.Content.First().ArtistName}](https://www.last.fm/music/{response.Content.First().ArtistName.Replace(" ", "+").Replace("(", "\\(").Replace(")", "\\)")})",
                    " - ",
                    $"[{response.Content.First().Name}]({response.Content.First().Url.ToString().Replace("(", "\\(").Replace(")", "\\)")})"))
                .WithFooter($"{info.Content.Playcount} total scrobbles on last.fm")
                .WithThumbnailUrl(response.Content.First().Images.Large != null
                    ? response.Content.First().Images.Large.AbsoluteUri
                    : "https://lastfm-img2.akamaized.net/i/u/174s/4128a6eb29f94943c9d206c08e625904")
                .AddField("Album",
                    response.Content.First().AlbumName != ""
                        ? $"[{response.Content.First().AlbumName}](https://www.last.fm/music/{response.Content.First().ArtistName.Replace(" ", "+").Replace("(", "\\(").Replace(")", "\\)")}/{response.Content.First().AlbumName.Replace(" ", "+").Replace("(", "\\(").Replace(")", "\\)")})"
                        : "No album linked on last.fm!");
            await ctx.RespondAsync(embed: embed.Build());
        }

        [GroupCommand, Priority(1)]
        public async Task FmUser(CommandContext ctx, DiscordUser user)
        {
            try
            {
                await Fm(ctx, Utilities.ResolveName("last", user));
            }
            catch (Exception e)
            {
                if (!e.Message.Contains("No username set for the requested service!") &&
                    !e.Message.Contains("Username could not be resolved"))
                {
                    await Fm(ctx, user.Username);
                }

                throw new Exception("This user has not set their last.fm account yet!");
            }
        }

        private static string GenerateHtml(IEnumerable<LastAlbum> albums, string html, string option)
        {
            var counter = 0;
            var playCount = "";
            foreach (var album in albums)
            {
                switch (option.ToLower())
                {
                    case "all":
                        if (album.PlayCount.HasValue)
                            playCount = album.PlayCount.Value + " Plays";
                        html += album.Images.Large != null
                            ? $"<div style=\"background-position: center center;background-repeat: no-repeat;background-size: cover;background-image: url('{album.Images.Large.AbsoluteUri}'); width: 174px;  height:174px; position:relative;display:inline-block\"><p style=\"position:absolute;top:-12px;left:4px;\">{album.ArtistName} -<br>{album.Name}</p><p style = \"position: absolute; bottom: -12px;left: 4px;\">{playCount}</p></div>"
                            : $"<div style=\"position:relative;display:inline-block\"><img src=\"https://lastfm-img2.akamaized.net/i/u/174s/4128a6eb29f94943c9d206c08e625904\"><p style=\"position:absolute;top:-12px;left:4px;\">{album.ArtistName} -<br>{album.Name}</p><p style = \"position: absolute; bottom: -12px;left: 4px;\">{playCount}</p></div>";
                        break;
                    case "names":
                        html += album.Images.Large != null
                            ? $"<div style=\"background-position: center center;background-repeat: no-repeat;background-size: cover;background-image: url('{album.Images.Large.AbsoluteUri}'); width: 174px;  height:174px; position:relative;display:inline-block\"><p style=\"position:absolute;top:-12px;left:4px;\">{album.ArtistName} -<br>{album.Name}</p></div>"
                            : $"<div style=\"position:relative;display:inline-block\"><img src=\"https://lastfm-img2.akamaized.net/i/u/174s/4128a6eb29f94943c9d206c08e625904\"><p style=\"position:absolute;top:-12px;left:4px;\">{album.ArtistName} -<br>{album.Name}</p></div>";
                        break;
                    case "blank":
                        html += album.Images.Large != null
                            ? $"<div style=\"background-position: center center;background-repeat: no-repeat;background-size: cover;background-image: url('{album.Images.Large.AbsoluteUri}'); width: 174px;  height:174px; position:relative;display:inline-block\"></div>"
                            : "<div style=\"position:relative;display:inline-block\"><img src=\"https://lastfm-img2.akamaized.net/i/u/174s/4128a6eb29f94943c9d206c08e625904\"></div>";
                        break;
                    case "plays":
                        if (album.PlayCount.HasValue)
                            playCount = album.PlayCount.Value + " Plays";
                        html += album.Images.Large != null
                            ? $"<div style=\"background-position: center center;background-repeat: no-repeat;background-size: cover;background-image: url('{album.Images.Large.AbsoluteUri}'); width: 174px;  height:174px; position:relative;display:inline-block\"><p style = \"position: absolute; bottom: -12px;left: 4px;\">{playCount}</p></div>"
                            : $"<div style=\"position:relative;display:inline-block\"><img src=\"https://lastfm-img2.akamaized.net/i/u/174s/4128a6eb29f94943c9d206c08e625904\"><p style = \"position: absolute; bottom: -12px;left: 4px;\">{playCount}</p></div>";
                        break;
                    default:
                        throw new Exception($"`{option}` is not a valid option");
                }

                if (++counter % 5 != 0) continue;
                html += "<br>";
                counter = 0;
            }

            return html;
        }

        private static async Task<string> GenerateHtml(IEnumerable<LastArtist> artists, string html, string option)
        {
            var counter = 0;
            var playCount = "";
            var lastArtists = await CollectArtists(artists);
            foreach (var (artist, imageUrl) in lastArtists)
            {
                switch (option.ToLower())
                {
                    case "all":
                        if (artist.PlayCount.HasValue)
                            playCount = artist.PlayCount.Value + " Plays";
                        html +=
                            $"<div style=\"background-position: center center;background-repeat: no-repeat;background-size: cover;background-image: url('{imageUrl}'); width: 174px;  height:174px; position:relative;display:inline-block\"><p style=\"position:absolute;top:-12px;left:4px;\">{artist.Name}</p><p style = \"position: absolute; bottom: -12px;left: 4px;\">{playCount}</p></div>";
                        break;
                    case "names":
                        html +=
                            $"<div style=\"background-position: center center;background-repeat: no-repeat;background-size: cover;background-image: url('{imageUrl}'); width: 174px;  height:174px; position:relative;display:inline-block\"><p style=\"position:absolute;top:-12px;left:4px;\">{artist.Name}</p></div>";
                        break;
                    case "blank":
                        html +=
                            $"<div style=\"background-position: center center;background-repeat: no-repeat;background-size: cover;background-image: url('{imageUrl}'); width: 174px;  height:174px; position:relative;display:inline-block\"></div>";
                        break;
                    case "plays":
                        if (artist.PlayCount.HasValue)
                            playCount = artist.PlayCount.Value + " Plays";
                        html +=
                            $"<div style=\"background-position: center center;background-repeat: no-repeat;background-size: cover;background-image: url('{imageUrl}'); width: 174px;  height:174px; position:relative;display:inline-block\"><p style = \"position: absolute; bottom: -12px;left: 4px;\">{playCount}</p></div>";
                        break;
                    default:
                        throw new Exception($"`{option}` is not a valid option");
                }

                if (++counter % 5 != 0) continue;
                html += "<br>";
                counter = 0;
            }

            return html;
        }

        private static string GenerateHtml(IEnumerable<Track> tracks, string html)
        {
            var playCount = "";
            var enumerable = tracks.ToList();
            var maxPlayCount = enumerable.First().PlayCount;
            foreach (var track in enumerable)
            {
                if (track.PlayCount != 0)
                    playCount = track.PlayCount + " Plays";
                html +=
                    $"<div class=\"skillBox\"> <p>{track.Name} <i>by {track.Artist.Name}</i></p><p>{playCount}</p><div class=\"skill\"> <div class=\"skill_level\" style=\"width: {99 * track.PlayCount / maxPlayCount + 1}%\"></div></div></div>";
            }

            return html + "</div></body></html>";
        }

        private static int ConvertTimeSpan(string timespan)
        {
            switch (timespan.ToLower())
            {
                case "":
                case "overall":
                    return 0;
                case "week":
                    return 1;
                case "month":
                    return 2;
                case "quarter":
                    return 3;
                case "half":
                    return 4;
                case "year":
                    return 5;
                default:
                    throw new Exception(
                        "Couldn't convert timespan! Try using `help fm [artist/album]chart` to get more info.");
            }
        }

        private static Task GenerateImage(string width, string height, Chart chart)
        {
            using (var exeProcess = Process.Start(new ProcessStartInfo
            {
                FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "wkhtmltoimage.exe" : "wkhtmltoimage",
                Arguments =
                    $"{width} {height} {Utilities.DataPath}{chart.Id}.html {Utilities.DataPath}{chart.Id}.png",
                UseShellExecute = false,
                RedirectStandardOutput = true
            }))
            {
                exeProcess?.WaitForExit();
            }

            return Task.CompletedTask;
        }

        private static void DeleteCharts(string guid)
        {
            File.Delete(Utilities.DataPath + $"{guid}.png");
            File.Delete(Utilities.DataPath + $"{guid}.html");
        }

        [Command("albumchart")]
        [Description("Returns an image of your top albums scrobbled on last.fm.")]
        public async Task GenerateAlbumChart(CommandContext ctx,
            [Description("Available timespans: overall, year, half, quarter, month and week")]
            string timespan = "", [Description("Available options: all, names, plays, blank")]
            string option = "all",
            [Description("The username whose albumchart you want to generate. Leave blank for own account.")]
            string username = "")
        {
            var thisChart = GenerateChart(ctx);
            await ctx.TriggerTypingAsync();
            var user = Users.GetUser(ctx.User);
            if (user == null && username == "")
            {
                throw new Exception(
                    $"I have no Last.fm Username set for you! Set it using `{Guilds.GetGuild(ctx.Guild).Prefix}fm set [Name]`!");
            }

            //If a name was provided, generate a chart for that user
            var name = username == "" ? user?.Last : username;

            //Get the top 25 albums on last.fm
            var albums = await Client.User.GetTopAlbums(name, (LastStatsTimeSpan) ConvertTimeSpan(timespan), 1, 25);
            if (!albums.Success)
            {
                if (username == "")
                {
                    throw new Exception("last.fm's response was not successful, try again later!");
                }

                throw new Exception(
                    $"last.fm's response was not successful! Are you sure `{username}` is a valid account?");
            }

            if (!albums.Content.Any())
                throw new Exception($"User `{username}` didn't listen to any albums yet!");
            
            try
            {
                File.WriteAllText($"{Utilities.DataPath}{thisChart.Id}.html",
                    GenerateHtml(albums, HtmlTemplate, option));
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

            await GenerateImage(albums.Content.Count >= 5 ? "--width 870" : $"--width {albums.Content.Count * 174}",
                $"--height {CalcHeight(albums.Content.Count)}", thisChart);

            await ctx.RespondWithFileAsync($"{Utilities.DataPath}{thisChart.Id}.png",
                $"Requested by: {thisChart.User}");
            DeleteCharts(thisChart.Id);
        }

        private static Chart GenerateChart(CommandContext ctx)
        {
            return new Chart
            {
                Id = Guid.NewGuid().ToString(),
                User = $"{ctx.User.Username}#{ctx.User.Discriminator}"
            };
        }

        [Command("artistchart")]
        [Description("Returns an image of your top artists scrobbled on last.fm.")]
        public async Task GenerateArtistChart(CommandContext ctx,
            [Description("Available Timespans: overall, year, half, quarter, month and week")]
            string timespan = "", [Description("Available Options: all, names, plays, blank")]
            string option = "all",
            [Description("The username whose artistchart you want to generate. Leave blank for own account.")]
            string username = "")
        {
            var thisChart = GenerateChart(ctx);

            await ctx.TriggerTypingAsync();

            var user = Users.GetUser(ctx.User);
            if (user == null && username == "")
            {
                throw new Exception(
                    $"I have no Last.fm Username set for you! Set it using `{Guilds.GetGuild(ctx.Guild).Prefix}fm set [Name]`!");
            }

            //If a name was provided, generate a chart for that user
            var name = username == "" ? user?.Last : username;

            //Get the top 25 artists on last.fm
            var artists = await Client.User.GetTopArtists(name, (LastStatsTimeSpan) ConvertTimeSpan(timespan), 1, 25);
            if (!artists.Success)
            {
                if (username == "")
                {
                    throw new Exception("last.fm's response was not successful, try again later!");
                }

                throw new Exception(
                    $"last.fm's response was not successful! Are you sure `{username}` is a valid account?");
            }

            if (!artists.Content.Any())
            {
                throw new Exception($"User `{username}` didn't listen to any artists yet!");
            }

            try
            {
                var html = await GenerateHtml(artists, HtmlTemplate, option);
                File.WriteAllText($"{Utilities.DataPath}{thisChart.Id}.html",
                    html);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

            await GenerateImage(artists.Content.Count >= 5 ? "--width 870" : $"--width {artists.Content.Count * 174}",
                $"--height {CalcHeight(artists.Content.Count)}", thisChart);

            await ctx.Channel.SendFileAsync($"{Utilities.DataPath}{thisChart.Id}.png",
                $"Requested by: {thisChart.User}");
            DeleteCharts(thisChart.Id);
        }

        [Command("songchart")]
        [Description("Returns an image of your top songs scrobbled on last.fm.")]
        public async Task GenerateSongChart(CommandContext ctx,
            [Description("Available Timespans: overall, year, half, quarter, month and week")]
            string timespan = "overall",
            [Description("The username whose songchart you want to generate. Leave blank for own account.")]
            string username = "")
        {
            var id = Guid.NewGuid();
            var thisChart = new Chart
            {
                Id = id.ToString(),
                User = $"{ctx.User.Username}#{ctx.User.Discriminator}"
            };
            await ctx.TriggerTypingAsync();

            var user = Users.GetUser(ctx.User);
            if (user == null && username == "")
            {
                throw new Exception(
                    $"I have no Last.fm Username set for you! Set it using `{Guilds.GetGuild(ctx.Guild).Prefix}fm set [Name]`!");
            }

            var name = username == "" ? user?.Last : username;

            SongResponse songs;
            try
            {
                songs = await GetTopTracks(timespan, name);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

            if (songs == null)
            {
                if (username == "")
                {
                    throw new Exception("last.fm's response was not successful, try again later!");
                }

                throw new Exception(
                    $"last.fm's response was not successful! Are you sure `{username}` is a valid account?");
            }

            if (songs.Toptracks.Track.Count == 0)
            {
                throw new Exception($"User `{username}` didn't listen to any songs yet!");
            }

            try
            {
                File.WriteAllText($"{Utilities.DataPath}{thisChart.Id}.html",
                    GenerateHtml(songs.Toptracks.Track, SongChartTemplate));
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

            await GenerateImage(
                "",
                "", thisChart);

            await ctx.Channel.SendFileAsync($"{Utilities.DataPath}{thisChart.Id}.png",
                $"Requested by: {thisChart.User}");
            DeleteCharts(thisChart.Id);
        }

        private static async Task<SongResponse> GetTopTracks(string timespan, string user)
        {
            switch (timespan.ToLower())
            {
                case "":
                case "overall":
                    break;
                case "week":
                    timespan = "7day";
                    break;
                case "month":
                    timespan = "1month";
                    break;
                case "quarter":
                    timespan = "3month";
                    break;
                case "half":
                    timespan = "6month";
                    break;
                case "year":
                    timespan = "12month";
                    break;
                default:
                    throw new Exception(
                        "Couldn't convert timespan! Try using `help fm [artist/album/song]chart` to get more info.");
            }

            var rParams =
                $"/2.0/?method=user.gettoptracks&user={user}&api_key={Utilities.GetKey("lastkey")}&format=json&limit=25&period={timespan}";
            var httpClient = new HttpClient {BaseAddress = new Uri("http://ws.audioscrobbler.com")};
            var response = await httpClient.GetAsync(rParams);
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<SongResponse>(json);
        }

        private static async Task<KeyValuePair<LastArtist, string>[]> CollectArtists(IEnumerable<LastArtist> artists)
        {
            var tasks = artists.Select(ScrapeImageAsync);
            return await Task.WhenAll(tasks);
        }

        private static async Task<KeyValuePair<LastArtist, string>> ScrapeImageAsync(LastArtist artist)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(artist.Url).ConfigureAwait(false);
                var result = await response.Content.ReadAsStringAsync();
                try
                {
                    result = result.Substring(
                        result.IndexOf("<meta property=\"og:image\"           content=\"", StringComparison.Ordinal) +
                        45,
                        150);
                    result = result.Remove(result.IndexOf("\"", StringComparison.Ordinal));
                }
                catch (Exception)
                {
                    result = "https://lastfm-img2.akamaized.net/i/u/174s/4128a6eb29f94943c9d206c08e625904";
                }

                return new KeyValuePair<LastArtist, string>(artist, result);
            }
        }

        private static string CalcHeight(int amount)
        {
            return Convert.ToString((((amount - 1) / 5) + 1) * 174);
        }

        private class Chart
        {
            public string Id { get; set; }
            public string User { get; set; }
        }

        #region Overloads

        [Command("songchart"), Priority(1)]
        [Description("Returns an image of your top songs scrobbled on last.fm.")]
        public async Task GenerateSongChart(CommandContext ctx,
            [Description("Available Timespans: overall, year, half, quarter, month and week")]
            string timespan = "overall",
            [Description("The username whose songchart you want to generate. Leave blank for own account.")]
            DiscordUser user = null)
        {
            if (user == null)
            {
                user = ctx.User;
            }

            try
            {
                var name = Utilities.ResolveName("last", user);
                await GenerateSongChart(ctx, timespan, name);
            }
            catch (Exception)
            {
                await GenerateSongChart(ctx, timespan, user.Username);
            }
        }

        [Command("albumchart"), Priority(1)]
        [Description("Returns an image of your top albums scrobbled on last.fm.")]
        public async Task GenerateAlbumChart(CommandContext ctx,
            [Description("Available timespans: overall, year, half, quarter, month and week")]
            string timespan = "", [Description("Available options: all, names, plays, blank")]
            string option = "all",
            [Description("The username whose albumchart you want to generate. Leave blank for own account.")]
            DiscordUser user = null)
        {
            if (user == null)
            {
                user = ctx.User;
            }

            try
            {
                var name = Utilities.ResolveName("last", user);
                await GenerateAlbumChart(ctx, timespan, option, name);
            }
            catch (Exception)
            {
                await GenerateAlbumChart(ctx, timespan, option, user.Username);
            }
        }

        [Command("artistchart"), Priority(1)]
        public async Task GenerateArtistChart(CommandContext ctx,
            [Description("Available Timespans: overall, year, half, quarter, month and week")]
            string timespan = "", [Description("Available Options: all, names, plays, blank")]
            string option = "all",
            [Description("The username whose artistchart you want to generate. Leave blank for own account.")]
            DiscordUser user = null)
        {
            if (user == null)
            {
                user = ctx.User;
            }

            try
            {
                var name = Utilities.ResolveName("last", user);
                await GenerateArtistChart(ctx, timespan, option, name);
            }
            catch (Exception)
            {
                await GenerateArtistChart(ctx, timespan, option, user.Username);
            }
        }

        #endregion
    }
}