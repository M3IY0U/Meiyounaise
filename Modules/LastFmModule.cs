using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using IF.Lastfm.Core.Api;
using Meiyounaise.DB;
using Microsoft.Data.Sqlite;

namespace Meiyounaise.Modules
{
    [Group("fm",CanInvokeWithoutSubcommand = true), Description("Group containing all last.fm commands. If you just use `fm` you'll get your currently playing song/last played track")]
    public class LastFmModule
    {
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
        public async Task ExecuteGroupAsync(CommandContext ctx, [Description("The user you want to see the last track of. Leave empty for own account.")]string username = "")
        {
            if (username=="")
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
                await ctx.RespondAsync("I couldn't find a user with that name!");
                return;
            }

            var info = await Client.User.GetInfoAsync(username);
            
            var isPlaying = response.Content.First().IsNowPlaying != null ? "Now Playing" : "Last Track";
            
            var embed = new DiscordEmbedBuilder()
                .WithAuthor($"{username} - {isPlaying}", $"https://www.last.fm/user/{username}","http://icons.iconarchive.com/icons/sicons/basic-round-social/256/last.fm-icon.png")
                .WithColor(DiscordColor.Red)
                .WithDescription(string.Concat(
                    $"[{response.Content.First().ArtistName}](https://www.last.fm/music/{response.Content.First().ArtistName.Replace(" ", "+").Replace("(","\\(").Replace(")","\\)")})",
                    " - ", $"[{response.Content.First().Name}]({response.Content.First().Url.ToString().Replace("(","\\(").Replace(")","\\)")})"))               
                .WithFooter($"{info.Content.Playcount} total scrobbles on last.fm")
                .WithThumbnailUrl(response.Content.First().Images.Large != null
                    ? response.Content.First().Images.Large.AbsoluteUri
                    : "https://lastfm-img2.akamaized.net/i/u/174s/4128a6eb29f94943c9d206c08e625904")
                .AddField("Album",
                    response.Content.First().AlbumName != ""
                        ? $"[{response.Content.First().AlbumName}](https://www.last.fm/music/{response.Content.First().ArtistName.Replace(" ", "+").Replace("(","\\(").Replace(")","\\)")}/{response.Content.First().AlbumName.Replace(" ", "+").Replace("(","\\(").Replace(")","\\)")})"
                        : "No Album linked on last.fm!");
            await ctx.RespondAsync("", false, embed.Build());
        }
    }
}