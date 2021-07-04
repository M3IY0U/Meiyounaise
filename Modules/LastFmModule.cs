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
using Humanizer;
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

        #region Commands

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
                    isPlaying.Contains("Now")
                        ? "https://cdn.discordapp.com/attachments/565956920004050944/752590473247457300/play.png"
                        : "https://cdn.discordapp.com/attachments/565956920004050944/752590397590470757/stop.png")
                .WithColor(new DiscordColor(211, 31, 39))
                .WithDescription(
                    $"**[{response.Content.First().Name}]({response.Content.First().Url.ToString().Replace("(", "\\(").Replace(")", "\\)").Replace("　", "%E3%80%80")})**")
                .WithFooter($"{info.Content.Playcount} total scrobbles on last.fm",
                    "http://icons.iconarchive.com/icons/sicons/basic-round-social/256/last.fm-icon.png")
                .WithThumbnail(response.Content.First().Images.Large != null
                    ? response.Content.First().Images.Large.AbsoluteUri.Replace("/174s/", "/")
                    : "https://lastfm.freetls.fastly.net/i/u/c6f59c1e5e7240a4c0d427abd71f3dbb")
                .AddField("Artist",
                    $"[{response.Content.First().ArtistName}](https://www.last.fm/music/{response.Content.First().ArtistName.Replace(" ", "+").Replace("(", "\\(").Replace(")", "\\)")})",
                    true)
                .AddField("Album",
                    response.Content.First().AlbumName != ""
                        ? $"[{response.Content.First().AlbumName}](https://www.last.fm/music/{response.Content.First().ArtistName.Replace(" ", "+").Replace("(", "\\(").Replace(")", "\\)")}/{response.Content.First().AlbumName.Replace(" ", "+").Replace("(", "\\(").Replace(")", "\\)")})"
                        : "No album linked on last.fm!", true);
            await ctx.RespondAsync(embed: embed.Build());
            Guilds.GetGuild(ctx.Guild).UpdateSongInChannel(ctx.Channel.Id,
                $"{response.Content.First().Name} {response.Content.First().ArtistName}");
        }

        [Command("sp")]
        public async Task Spotify(CommandContext ctx, string username = "")
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

            Guilds.GetGuild(ctx.Guild).UpdateSongInChannel(ctx.Channel.Id,
                $"{response.Content.First().Name} {response.Content.First().ArtistName}");
            await Bot.Client.GetCommandsNext().RegisteredCommands.Values.First(x => x.Name == "spotify")
                .ExecuteAsync(ctx);
        }

        [Command("yt")]
        public async Task Youtube(CommandContext ctx, string username = "")
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

            Guilds.GetGuild(ctx.Guild).UpdateSongInChannel(ctx.Channel.Id,
                $"{response.Content.First().Name} {response.Content.First().ArtistName}");
            await Bot.Client.GetCommandsNext().RegisteredCommands.Values.First(x => x.Name == "youtube")
                .ExecuteAsync(ctx);
        }

        [Command("recent"), Aliases("rs")]
        public async Task FmRecent(CommandContext ctx,
            [Description("How many recent tracks should be shown, maximum is 10, defaults to 5.")]
            int count = 5,
            [Description("The user you want to see the most recent tracks of. Leave empty for own account.")]
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
                    throw new Exception("last.fm's response was not successful, try again later!");

                throw new Exception(
                    $"last.fm's response was not successful! Are you sure `{username}` is a valid account?");
            }

            var eb = new DiscordEmbedBuilder()
                .WithAuthor($"{username}'s most recent scrobbles", $"https://www.last.fm/user/{username}",
                    "http://icons.iconarchive.com/icons/sicons/basic-round-social/256/last.fm-icon.png")
                .WithColor(DiscordColor.Red)
                .WithTimestamp(DateTime.Now)
                .WithThumbnail(response.Content.First().Images.Large != null
                    ? response.Content.First().Images.Large.AbsoluteUri
                    : "https://lastfm.freetls.fastly.net/i/u/174s/c6f59c1e5e7240a4c0d427abd71f3dbb");
            if (count > 10)
                count = 10;
            foreach (var track in response.Content.Take(count))
            {
                var ago = track.TimePlayed.Humanize();
                eb.AddField(ago == "never" ? "Currently scrobbling" : ago, string.Concat(
                    $"[{track.ArtistName}](https://www.last.fm/music/{track.ArtistName.Replace(" ", "+").Replace("(", "\\(").Replace(")", "\\)")})",
                    " - ",
                    $"[{track.Name}]({track.Url.ToString().Replace("(", "\\(").Replace(")", "\\)").Replace("　", "%E3%80%80")})"));
            }

            await ctx.RespondAsync(embed: eb.Build());
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
            var thisChart = GenerateChart();
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

            await using (var fs = new FileStream($"{Utilities.DataPath}{thisChart.Id}.png", FileMode.Open))
            {
                await ctx.RespondAsync(msg =>
                    msg.WithFile(fs));
            }

            DeleteCharts(thisChart.Id);
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
            var thisChart = GenerateChart();

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

            await using (var fs = new FileStream($"{Utilities.DataPath}{thisChart.Id}.png", FileMode.Open))
            {
                await ctx.RespondAsync(msg =>
                    msg.WithFile(fs));
            }

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
                Id = id.ToString()
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

            await using (var fs = new FileStream($"{Utilities.DataPath}{thisChart.Id}.png", FileMode.Open))
            {
                await ctx.RespondAsync(msg =>
                    msg.WithFile(fs));
            }

            DeleteCharts(thisChart.Id);
        }

        [Command("server"), Aliases("guild")]
        [Description(
            "Displays all users in the server which linked their account and are currently scrobbling in one command.")]
        public async Task Server(CommandContext ctx)
        {
            var users = new HashSet<string>();
            foreach (var (_, member) in ctx.Guild.Members)
            {
                if (Users.GetUser(member) == null || Users.GetUser(member).Last == "#" ||
                    string.IsNullOrWhiteSpace(Users.GetUser(member).Last)) continue;
                users.Add(Users.GetUser(member).Last);
            }

            var tasks = users.Select(GetNowPlaying);
            var results = await Task.WhenAll(tasks);
            var texts = new List<string>();
            foreach (var nowPlaying in results)
            {
                if (nowPlaying == null)
                    continue;
                var (user, track) = nowPlaying.Value;
                if (track.IsNowPlaying == null || !track.IsNowPlaying.Value)
                    continue;
                texts.Add(
                    $"<@{Users.UserList.Find(x => x.Last == user)?.Id}> [🔊](https://www.last.fm/user/{user}) [{track.ArtistName}]({track.ArtistUrl}) - [{track.Name}]({track.Url.ToString().Replace("(", "\\(").Replace(")", "\\)").Replace("　", "%E3%80%80")})");
            }

            if (texts.Count == 0)
            {
                await ctx.RespondAsync("No one in this guild is scrobbling something right now.");
                return;
            }

            var embeds = new List<DiscordEmbed>();
            var toAdd = "";

            for (var i = 0; i < texts.Count; i++)
            {
                toAdd += texts[i];
                if (i + 1 != texts.Count)
                    toAdd += "\n⏤⏤⏤⏤⏤⏤⏤⏤⏤⏤⏤⏤⏤⏤⏤⏤⏤⏤\n";
                
                if (toAdd.Length > 1800 || texts.Count <= i + 1)
                {
                    embeds.Add(new DiscordEmbedBuilder()
                        .WithAuthor($"Currently playing in {ctx.Guild.Name}",
                            iconUrl:
                            "http://icons.iconarchive.com/icons/sicons/basic-round-social/256/last.fm-icon.png")
                        .WithColor(DiscordColor.Red)
                        .WithThumbnail(ctx.Guild.IconUrl)
                        .WithDescription(toAdd));
                    toAdd = "";
                }
            }

            foreach (var embed in embeds)
            {
                await ctx.RespondAsync(embed);
            }
        }

        [Command("test")]
        public async Task WeeklyServer(CommandContext ctx)
        {
            var users = new HashSet<string>();
            foreach (var (_, member) in ctx.Guild.Members)
            {
                if (Users.GetUser(member) == null || Users.GetUser(member).Last == "#" ||
                    string.IsNullOrWhiteSpace(Users.GetUser(member).Last)) continue;
                users.Add(Users.GetUser(member).Last);
            }


            var tasks = users.Select(user => GetTopTracks("week", user));
            var results = await Task.WhenAll(tasks);
            var counter = new Dictionary<Track, long>();
            foreach (var result in results)
            {
                if (result.Toptracks == null) continue;
                if (!result.Toptracks.Track.Any()) continue;
                var track = result.Toptracks.Track.First();

                if (!counter.ContainsKey(track))
                    counter.Add(track, track.PlayCount);
                else
                    counter[track] += track.PlayCount;
            }

            var sorted = counter.OrderByDescending(pair => pair.Value).Take(10);
            var eb = new DiscordEmbedBuilder()
                .WithTitle($"Most listened to tracks this week in {ctx.Guild.Name}")
                .WithDescription(string.Join('\n',
                    sorted.Select(x => $"{x.Key.Artist.Name} - {x.Key.Name} | {x.Value} Scrobbles")));
            await ctx.RespondAsync(embed: eb.Build());
        }

        [Command("tagcloud"), Aliases("tc")]
        [Description("Generate a tag cloud out of the top 5 tags of your top 25 artists within a given timespan.")]
        public async Task TagCloud(CommandContext ctx, string timespan = "overall", string username = "")
        {
            var id = Guid.NewGuid();
            var thisChart = new Chart
            {
                Id = id.ToString()
            };
            await ctx.TriggerTypingAsync();

            var user = Users.GetUser(ctx.User);
            if (user == null && username == "")
            {
                throw new Exception(
                    $"I have no Last.fm Username set for you! Set it using `{Guilds.GetGuild(ctx.Guild).Prefix}fm set [Name]`!");
            }

            var name = username == "" ? user?.Last : username;

            var result = await Client.User.GetTopArtists(name, (LastStatsTimeSpan) ConvertTimeSpan(timespan), 1, 25);
            var allTags = new List<string>();
            foreach (var artist in result.Content)
            {
                var tags = await Client.Artist.GetTopTagsAsync(artist.Name);
                allTags.AddRange(tags.Select(x => x.Name.Replace(" ", ""))
                    .Where(tag => !tag.Contains("seen") || !tag.Contains("live")).Take(5));
            }

            var url =
                $"https://quickchart.io/wordcloud?width=800&height=800&maxNumWords=125&rotation=60&minWordLength=2&scale=sqrt&format=png&fontScale=50&text={string.Join(' ', allTags)}";

            await Utilities.DownloadAsync(new Uri(url), $"{Utilities.DataPath}{thisChart.Id}.png");
            await using (var fs = new FileStream($"{Utilities.DataPath}{thisChart.Id}.png", FileMode.Open))
            {
                await ctx.RespondAsync(msg =>
                    msg.WithFile(fs));
            }

            File.Delete($"{Utilities.DataPath}{thisChart.Id}.png");
        }

        #endregion

        #region UtilityFunctions

        private static async Task<KeyValuePair<string, LastTrack>?> GetNowPlaying(string user)
        {
            try
            {
                var response = await Client.User.GetRecentScrobbles(user);
                if (!response.Success)
                    return null;
                var isNowPlaying = response.Content.First().IsNowPlaying;
                if (isNowPlaying != null && (!response.Success || !isNowPlaying.Value))
                    return null;
                return new KeyValuePair<string, LastTrack>(user, response.Content.First());
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static Chart GenerateChart()
        {
            return new Chart
            {
                Id = Guid.NewGuid().ToString()
            };
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
                            : $"<div style=\"position:relative;display:inline-block\"><img src=\"https://lastfm.freetls.fastly.net/i/u/174s/c6f59c1e5e7240a4c0d427abd71f3dbb\"><p style=\"position:absolute;top:-12px;left:4px;\">{album.ArtistName} -<br>{album.Name}</p><p style = \"position: absolute; bottom: -12px;left: 4px;\">{playCount}</p></div>";
                        break;
                    case "names":
                        html += album.Images.Large != null
                            ? $"<div style=\"background-position: center center;background-repeat: no-repeat;background-size: cover;background-image: url('{album.Images.Large.AbsoluteUri}'); width: 174px;  height:174px; position:relative;display:inline-block\"><p style=\"position:absolute;top:-12px;left:4px;\">{album.ArtistName} -<br>{album.Name}</p></div>"
                            : $"<div style=\"position:relative;display:inline-block\"><img src=\"https://lastfm.freetls.fastly.net/i/u/174s/c6f59c1e5e7240a4c0d427abd71f3dbb\"><p style=\"position:absolute;top:-12px;left:4px;\">{album.ArtistName} -<br>{album.Name}</p></div>";
                        break;
                    case "blank":
                        html += album.Images.Large != null
                            ? $"<div style=\"background-position: center center;background-repeat: no-repeat;background-size: cover;background-image: url('{album.Images.Large.AbsoluteUri}'); width: 174px;  height:174px; position:relative;display:inline-block\"></div>"
                            : "<div style=\"position:relative;display:inline-block\"><img src=\"https://lastfm.freetls.fastly.net/i/u/174s/c6f59c1e5e7240a4c0d427abd71f3dbb\"></div>";
                        break;
                    case "plays":
                        if (album.PlayCount.HasValue)
                            playCount = album.PlayCount.Value + " Plays";
                        html += album.Images.Large != null
                            ? $"<div style=\"background-position: center center;background-repeat: no-repeat;background-size: cover;background-image: url('{album.Images.Large.AbsoluteUri}'); width: 174px;  height:174px; position:relative;display:inline-block\"><p style = \"position: absolute; bottom: -12px;left: 4px;\">{playCount}</p></div>"
                            : $"<div style=\"position:relative;display:inline-block\"><img src=\"https://lastfm.freetls.fastly.net/i/u/174s/c6f59c1e5e7240a4c0d427abd71f3dbb\"><p style = \"position: absolute; bottom: -12px;left: 4px;\">{playCount}</p></div>";
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
                    result = "https://lastfm.freetls.fastly.net/i/u/avatar/2a96cbd8b46e442fc41c2b86b821562f";
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
        }

        #endregion

        #region Overloads

        [GroupCommand, Priority(1)]
        public async Task FmUser(CommandContext ctx, DiscordUser user)
        {
            try
            {
                await Fm(ctx, Utilities.ResolveName("last", user));
            }
            catch (Exception e)
            {
                if (e.Message.Contains("No username set for the requested service!") ||
                    e.Message.Contains("Username could not be resolved"))
                    throw new Exception("This user has not set their last.fm account yet!");
                await Fm(ctx, user.Username);
            }
        }

        [Command("recent"), Priority(1)]
        public async Task FmRecentUser(CommandContext ctx, int count = 5, DiscordUser user = null)
        {
            if (user == null)
                user = ctx.User;
            try
            {
                var name = Utilities.ResolveName("last", user);
                await FmRecent(ctx, count, name);
            }
            catch (Exception)
            {
                await FmRecent(ctx, count, user.Username);
            }
        }

        [Command("songchart"), Priority(1)]
        [Description("Returns an image of your top songs scrobbled on last.fm.")]
        public async Task GenerateSongChart(CommandContext ctx,
            [Description("Available Timespans: overall, year, half, quarter, month and week")]
            string timespan = "overall",
            [Description("The username whose songchart you want to generate. Leave blank for own account.")]
            DiscordUser user = null)
        {
            if (user == null)
                user = ctx.User;

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
                user = ctx.User;

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
                user = ctx.User;

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