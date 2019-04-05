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

namespace Meiyounaise.Modules
{
    [Group("fm"),Description("Group containing all last.fm commands. If you just use `fm` you'll get your currently playing song/last played track")]
    public class LastFmModule : BaseCommandModule
    {
        private List<Chart> _charts = new List<Chart>();

        private const string HtmlTemplate =
            "<meta charset=\"UTF-8\"><link href=\"https://fonts.googleapis.com/css?family=Baloo+Thambi\" rel=\"stylesheet\"><style> *{font-size: 15px !important;color: #ffffff !important;line-height: 95%; font-family: 'Baloo Thambi', cursive !important;text-shadow: -1.5px 0 #000, 0 1.5px #000, 1.5px 0 #000, 0 -1.5px #000;} body {margin: 0;}</style>";


        private static readonly LastfmClient Client = new LastfmClient(Utilities.GetKey("lastkey"),
            Utilities.GetKey("lastsecret"), new HttpClient());

        [Command("set")]
        [Description("Set your last.fm Username.")]
        public async Task FmSet(CommandContext ctx, string username = "")
        {
            if (username == "")
            {
                await ctx.RespondAsync("I need a Name that I can link to your account!");
                return;
            }

            if (Users.UserList.Any(x => x.Id == ctx.User.Id))
            {
                Utilities.Con.Open();
                using (var cmd =
                    new SqliteCommand($"UPDATE Users SET lastfm = '{username}' WHERE Users.id = '{ctx.User.Id}'",
                        Utilities.Con))
                {
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
                    new SqliteCommand($"INSERT INTO Users (id, lastfm) VALUES ('{ctx.User.Id}', '{username}')",
                        Utilities.Con))
                {
                    cmd.ExecuteReader();
                }

                Utilities.Con.Close();
                Users.UpdateUser(ctx.User);
            }

            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
        }

        //Normal fm
        [GroupCommand]
        public async Task Fm(CommandContext ctx,
            [Description("The user you want to see the last track of. Leave empty for own account.")]
            string username = "")
        {
            if (username == "")
            {
                if (username == "#" || !Users.UserList.Any(x => x.Id == ctx.User.Id))
                {
                    await ctx.RespondAsync(
                        "I don't have a last.fm name linked to your Discord account. Set it using `fm set [Name]`.");
                    return;
                }

                username = Users.GetUser(ctx.User).Last;
            }

            var response = await Client.User.GetRecentScrobbles(username);
            if (!response.Success)
            {
                if (username == "")
                {
                    await ctx.RespondAsync("last.fm's response was not successful, try again later!");
                }
                else
                {
                    await ctx.RespondAsync(
                        $"last.fm's response was not successful! Are you sure `{username}` is a valid account?");
                }

                return;
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
                        : "No Album linked on last.fm!");
            await ctx.RespondAsync("", false, embed.Build());
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
                            ? $"<div style=\"position:relative;display:inline-block\"><img src=\"{album.Images.Large.AbsoluteUri}\"><p style=\"position:absolute;top:-12px;left:4px;\">{album.ArtistName} -<br>{album.Name}</p><p style = \"position: absolute; bottom: -12px;left: 4px;\">{playCount}</p></div>"
                            : $"<div style=\"position:relative;display:inline-block\"><img src=\"https://lastfm-img2.akamaized.net/i/u/174s/4128a6eb29f94943c9d206c08e625904\"><p style=\"position:absolute;top:-12px;left:4px;\">{album.ArtistName} -<br>{album.Name}</p><p style = \"position: absolute; bottom: -12px;left: 4px;\">{playCount}</p></div>";
                        break;
                    case "names":
                        html += album.Images.Large != null
                            ? $"<div style=\"position:relative;display:inline-block\"><img src=\"{album.Images.Large.AbsoluteUri}\"><p style=\"position:absolute;top:-12px;left:4px;\">{album.ArtistName} -<br>{album.Name}</p></div>"
                            : $"<div style=\"position:relative;display:inline-block\"><img src=\"https://lastfm-img2.akamaized.net/i/u/174s/4128a6eb29f94943c9d206c08e625904\"><p style=\"position:absolute;top:-12px;left:4px;\">{album.ArtistName} -<br>{album.Name}</p></div>";
                        break;
                    case "blank":
                        html += album.Images.Large != null
                            ? $"<div style=\"position:relative;display:inline-block\"><img src=\"{album.Images.Large.AbsoluteUri}\"></div>"
                            : "<div style=\"position:relative;display:inline-block\"><img src=\"https://lastfm-img2.akamaized.net/i/u/174s/4128a6eb29f94943c9d206c08e625904\"></div>";
                        break;
                    case "plays":
                        if (album.PlayCount.HasValue)
                            playCount = album.PlayCount.Value + " Plays";
                        html += album.Images.Large != null
                            ? $"<div style=\"position:relative;display:inline-block\"><img src=\"{album.Images.Large.AbsoluteUri}\"><p style = \"position: absolute; bottom: -12px;left: 4px;\">{playCount}</p></div>"
                            : $"<div style=\"position:relative;display:inline-block\"><img src=\"https://lastfm-img2.akamaized.net/i/u/174s/4128a6eb29f94943c9d206c08e625904\"><p style = \"position: absolute; bottom: -12px;left: 4px;\">{playCount}</p></div>";
                        break;
                    default:
                        throw new Exception($"`{option}` is not a valid Option");
                }

                if (++counter % 5 != 0) continue;
                html += "<br>";
                counter = 0;
            }

            return html;
        }

        private static string GenerateHtml(IEnumerable<LastArtist> artists, string html, string option)
        {
            var counter = 0;
            var playCount = "";
            foreach (var artist in artists)
            {
                switch (option.ToLower())
                {
                    case "all":
                        if (artist.PlayCount.HasValue)
                            playCount = artist.PlayCount.Value + " Plays";
                        html += artist.MainImage.Large != null
                            ? $"<div style=\"position:relative;display:inline-block\"><img src=\"{artist.MainImage.Large.AbsoluteUri}\"><p style=\"position:absolute;top:-12px;left:4px;\">{artist.Name}</p><p style = \"position: absolute; bottom: -12px;left: 4px;\">{playCount}</p></div>"
                            : $"<div style=\"position:relative;display:inline-block\"><img src=\"https://lastfm-img2.akamaized.net/i/u/174s/4128a6eb29f94943c9d206c08e625904\"><p style=\"position:absolute;top:-12px;left:4px;\">{artist.Name}</p><p style = \"position: absolute; bottom: -12px;left: 4px;\">{playCount}</p></div>";
                        break;
                    case "names":
                        html += artist.MainImage.Large != null
                            ? $"<div style=\"position:relative;display:inline-block\"><img src=\"{artist.MainImage.Large.AbsoluteUri}\"><p style=\"position:absolute;top:-12px;left:4px;\">{artist.Name}</p></div>"
                            : $"<div style=\"position:relative;display:inline-block\"><img src=\"https://lastfm-img2.akamaized.net/i/u/174s/4128a6eb29f94943c9d206c08e625904\"><p style=\"position:absolute;top:-12px;left:4px;\">{artist.Name}</p></div>";
                        break;
                    case "blank":
                        html += artist.MainImage.Large != null
                            ? $"<div style=\"position:relative;display:inline-block\"><img src=\"{artist.MainImage.Large.AbsoluteUri}\"></div>"
                            : "<div style=\"position:relative;display:inline-block\"><img src=\"https://lastfm-img2.akamaized.net/i/u/174s/4128a6eb29f94943c9d206c08e625904\"></div>";
                        break;
                    case "plays":
                        if (artist.PlayCount.HasValue)
                            playCount = artist.PlayCount.Value + " Plays";
                        html += artist.MainImage.Large != null
                            ? $"<div style=\"position:relative;display:inline-block\"><img src=\"{artist.MainImage.Large.AbsoluteUri}\"><p style = \"position: absolute; bottom: -12px;left: 4px;\">{playCount}</p></div>"
                            : $"<div style=\"position:relative;display:inline-block\"><img src=\"https://lastfm-img2.akamaized.net/i/u/174s/4128a6eb29f94943c9d206c08e625904\"><p style = \"position: absolute; bottom: -12px;left: 4px;\">{playCount}</p></div>";
                        break;
                    default:
                        throw new Exception($"`{option}` is not a valid Option");
                }

                if (++counter % 5 != 0) continue;
                html += "<br>";
                counter = 0;
            }

            return html;
        }

        private int ConvertTimeSpan(string timespan)
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

        private Task GenerateImage(string width, string height, Chart chrt)
        {
            using (var exeProcess = Process.Start(new ProcessStartInfo
            {
                FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "wkhtmltoimage.exe" : "wkhtmltoimage",
                Arguments =
                    $"{width} {height} {Utilities.DataPath}chart{chrt.Id}.html {Utilities.DataPath}chart{chrt.Id}.png",
                UseShellExecute = false,
                RedirectStandardOutput = true
            }))
            {
                exeProcess?.WaitForExit();
            }

            return Task.CompletedTask;
        }

        private void DeleteCharts(int count)
        {
            File.Delete(Utilities.DataPath + $"chart{count}.png");
            File.Delete(Utilities.DataPath + $"chart{count}.html");
        }


        [Command("albumchart"), Cooldown(1,10,CooldownBucketType.User)]
        [Description("Returns an Image of your Top Albums scrobbled on last.fm.")]
        public async Task GenerateAlbumChart(CommandContext ctx, [Description("The username whose artistchart you want to generate. Leave blank for own account.")]string uname = "", [Description("Available Timespans: Overall, Year, Half, Quarter, Month and Week")]string timespan = "", [Description("Available Options: all, names, plays, blank")]string option = "all")
        {
            var thisChart = new Chart
            {
                Id = _charts.Count,
                User = ctx.User.Mention
            };
            _charts.Add(thisChart);
            //Trigger typing to let the user know we're generating his chart
            await ctx.TriggerTypingAsync();
            //If a name was provided, generate a chart for that user
            var name = uname == "" ? Users.GetUser(ctx.User).Last : uname;
            if (name == null)
            {
                await ctx.RespondAsync(
                    $"I have no Last.fm Username set for you! Set it using `{Guilds.GetGuild(ctx.Guild).Prefix}fm set [Name]`!");
                return;
            }

            //Last.fm timespans are weird so we have to convert it
            int ts;
            try
            {
                ts = ConvertTimeSpan(timespan);
            }
            catch (Exception e)
            {
                await ctx.RespondAsync(e.Message);
                return;
            }

            //Get the top 25 albums on last.fm
            var albums = await Client.User.GetTopAlbums(name, (LastStatsTimeSpan) ts, 1, 25);
            if (!albums.Success)
            {
                if (uname == "")
                {
                    await ctx.RespondAsync("last.fm's response was not successful, try again later!");
                }
                else
                {
                    await ctx.RespondAsync(
                        $"last.fm's response was not successful! Are you sure `{uname}` is a valid account?");
                }

                return;
            }

            if (!albums.Content.Any())
            {
                await ctx.RespondAsync("You didn't listen to any albums yet!");
                return;
            }

            try
            {
                File.WriteAllText($"{Utilities.DataPath}chart{thisChart.Id}.html",
                    GenerateHtml(albums, HtmlTemplate, option));
            }
            catch (Exception e)
            {
                await ctx.RespondAsync(e.Message);
                return;
            }

            await GenerateImage(albums.Content.Count >= 5 ? "--width 870" : $"--width {albums.Content.Count * 174}",
                $"--height {CalcHeight(albums.Content.Count)}", thisChart);

            await ctx.Channel.SendFileAsync($"{Utilities.DataPath}chart{thisChart.Id}.png",
                $"Requested by: {thisChart.User}");
            _charts.Remove(thisChart);
            DeleteCharts(thisChart.Id);
        }

        [Command("artistchart")]
        [Description("Returns an Image of your Top Albums scrobbled on last.fm.")]
        public async Task GenerateArtistChart(CommandContext ctx, [Description("The username whose albumchart you want to generate. Leave blank for own account.")]string uname = "", [Description("Available Timespans: Overall, Year, Half, Quarter, Month and Week")]string timespan = "", [Description("Available Options: all, names, plays, blank")]string option = "all")
        {
            var thisChart = new Chart
            {
                Id = _charts.Count,
                User = ctx.User.Mention
            };
            _charts.Add(thisChart);
            //Trigger typing to let the user know we're generating his chart
            await ctx.TriggerTypingAsync();
            //If a name was provided, generate a chart for that user
            var name = uname == "" ? Users.GetUser(ctx.User).Last : uname;

            if (name == null)
            {
                await ctx.RespondAsync(
                    $"I have no Last.fm Username set for you! Set it using `{Guilds.GetGuild(ctx.Guild).Prefix}fm set [Name]`!");
                return;
            }
            //Last.fm timespans are weird so we have to convert it
            int ts;
            try
            {
                ts = ConvertTimeSpan(timespan);
            }
            catch (Exception e)
            {
                await ctx.RespondAsync(e.Message);
                return;
            }

            

            //Get the top 25 albums on last.fm
            var artists = await Client.User.GetTopArtists(name, (LastStatsTimeSpan) ts, 1, 25);
            if (!artists.Success)
            {
                if (uname == "")
                {
                    await ctx.RespondAsync("last.fm's response was not successful, try again later!");
                }
                else
                {
                    await ctx.RespondAsync(
                        $"last.fm's response was not successful! Are you sure `{uname}` is a valid account?");
                }

                return;
            }

            if (!artists.Content.Any())
            {
                await ctx.RespondAsync("You didn't listen to any artists yet!");
                return;
            }

            try
            {
                File.WriteAllText($"{Utilities.DataPath}chart{thisChart.Id}.html",
                    GenerateHtml(artists, HtmlTemplate, option));
                Console.WriteLine($"Wrote chart{thisChart.Id}");
            }
            catch (Exception e)
            {
                await ctx.RespondAsync(e.Message);
                return;
            }


            await GenerateImage(artists.Content.Count >= 5 ? "--width 870" : $"--width {artists.Content.Count * 174}",
                $"--height {CalcHeight(artists.Content.Count)}", thisChart);

            await ctx.Channel.SendFileAsync($"{Utilities.DataPath}chart{thisChart.Id}.png",
                $"Requested by: {thisChart.User}");
            _charts.Remove(thisChart);
            DeleteCharts(thisChart.Id);
        }

        private static string CalcHeight(int height)
        {
            return Convert.ToString(((height-1)/5 +1) * 174); //Danke Twarq <3
        }

        private class Chart
        {
            public int Id { get; set; }
            public string User { get; set; }
        }
    }
}