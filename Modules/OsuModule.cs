using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Meiyounaise.DB;
using Microsoft.Data.Sqlite;
using OsuSharp;

namespace Meiyounaise.Modules
{
    [Group("osu")]
    public class OsuModule : BaseCommandModule
    {
        private OsuApi osuApi = new OsuApi(new OsuSharpConfiguration
        {
            ApiKey = Utilities.GetKey("osu"),
            ModsSeparator = " • "
        });

        [GroupCommand]
        public async Task Osu(CommandContext ctx, string username = "")
        {
            if (username == "")
            {
                if (Users.UserList.All(x => x.Id != ctx.User.Id))
                {
                    throw new Exception(
                        "I don't have an osu username linked to your discord account. Set it using `osu set [Name]`.");
                }

                username = Utilities.ResolveName("osu", ctx.User);
            }

            var user = osuApi.GetUserByName(username);
            if (user == null)
            {
                throw new Exception($"No user by the name `{username}` was found!");
            }

            var eb = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor(220, 152, 164))
                .WithAuthor($"{user.Username}'s osu! profile:", $"https://osu.ppy.sh/users/{user.Userid}")
                .WithThumbnailUrl($"http://s.ppy.sh/a/{user.Userid}")
                .WithDescription($"**Rank** » #{user.GlobalRank} ({user.Country}: #{user.RegionalRank})\n" +
                                 $"**PP** » {Math.Round(user.Pp)}\n" +
                                 $"**Accuracy** » {Math.Round(user.Accuracy, 2)}%\n" +
                                 $"**Playcount** » {user.PlayCount}\n");
            await ctx.RespondAsync(embed: eb.Build());
        }

        [Command("compare"), Aliases("comp", "cmp")]
        public async Task OsuComp(CommandContext ctx, string toComp, string ownAcc = "")
        {
            if (ownAcc == "")
            {
                ownAcc = Utilities.ResolveName("osu", ctx.User);
            }

            var ou1 = osuApi.GetUserByName(toComp);
            var ou2 = osuApi.GetUserByName(ownAcc);
            if (ou1 == null || ou2 == null)
            {
                throw new Exception("One or more users not found!");
            }

            var user1String = string.Join("\n", $"» Rank: #{ou1.GlobalRank}",
                $"» Level: {Math.Round(ou1.Level, 2)}", $"» PP: {ou1.Pp}", $"» Accuracy: {Math.Round(ou1.Accuracy, 2)}",
                $"» PlayCount: {ou1.PlayCount}");

            var user2String = string.Join("\n", $"» Rank: #{ou2.GlobalRank}",
                $"» Level: {Math.Round(ou2.Level, 2)}", $"» PP: {ou2.Pp}", $"» Accuracy: {Math.Round(ou2.Accuracy, 2)}",
                $"» PlayCount: {ou2.PlayCount}");

            var eb = new DiscordEmbedBuilder()
                .WithAuthor($"Comparing {ou1.Username} with {ou2.Username}",
                    iconUrl: "https://upload.wikimedia.org/wikipedia/commons/d/d3/Osu%21Logo_%282015%29.png")
                .WithColor(new DiscordColor(220, 152, 164))
                .AddField(ou1.Username, user1String, true)
                .AddField(ou2.Username, user2String, true)
                .WithDescription(ou1.Pp > ou2.Pp
                    ? $"[{ou1.Username}](https://osu.ppy.sh/users/{ou1.Userid}) is {Math.Abs(ou1.Pp - ou2.Pp)}pp ({Math.Abs(ou1.GlobalRank - ou2.GlobalRank)} Ranks) ahead of [{ou2.Username}](https://osu.ppy.sh/users/{ou2.Userid})"
                    : $"[{ou1.Username}](https://osu.ppy.sh/users/{ou1.Userid}) is {Math.Round(Math.Abs(ou1.Pp - ou2.Pp))}pp ({Math.Abs(ou1.GlobalRank - ou2.GlobalRank)} Ranks) behind [{ou2.Username}](https://osu.ppy.sh/users/{ou2.Userid})");
            await ctx.RespondAsync(embed: eb.Build());
        }

        [Command("top")]
        public async Task OsuTop(CommandContext ctx, string username = "")
        {
            await ctx.TriggerTypingAsync();
            if (username == "")
            {
                username = Utilities.ResolveName("osu", ctx.User);
            }

            var user = await osuApi.GetUserByNameAsync(username);
            var result = await osuApi.GetUserBestByUsernameAsync(username, limit: 50);
            var pages = new List<Page>();
            var counter = 0;

            var eb = new DiscordEmbedBuilder();
            var content = "";
            foreach (var map in result)
            {
                var mapInfo = await osuApi.GetBeatmapAsync(map.BeatmapId);
                content += $"**#{counter+1} [{mapInfo.Title}](https://osu.ppy.sh/b/{map.BeatmapId})** [{mapInfo.Difficulty}] +{map.Mods} [{Math.Round(mapInfo.DifficultyRating, 2)}★]\n" +
                           $"» {DiscordEmoji.FromName(ctx.Client, $":{map.Rank}_Rank:")} » **{Math.Round(map.Pp, 2)}pp** » {Math.Round(map.Accuracy, 2)}%\n" +
                           $"» {map.TotalScore} » {map.MaxCombo}/{mapInfo.MaxCombo} » [{map.Count300}/{map.Count100}/{map.Count50}]\n" +
                           $"» Achieved on {map.InitializedAt:dd.MM.yy H:mm:ss}\n\n";
                
                if (++counter % 5 != 0) continue;
                eb.WithAuthor($"Top osu! Standard Plays for {username}", $"https://osu.ppy.sh/users/{user.Userid}",
                    $"http://s.ppy.sh/a/{user.Userid}");
                eb.WithDescription(content);
                eb.WithColor(new DiscordColor(220, 152, 164));
                pages.Add(new Page(embed:eb));
                eb = new DiscordEmbedBuilder();
                content = "";
            }
            await ctx.Client.GetInteractivity().SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages);
        }

        [Command("set")]
        [Description("Set your osu username.")]
        public async Task OsuSet(CommandContext ctx, string username = "")
        {
            if (username == "")
            {
                throw new Exception("I need a name that I can link to your account!");
            }

            if (Users.UserList.Any(x => x.Id == ctx.User.Id))
            {
                Utilities.Con.Open();
                using (var cmd =
                    new SqliteCommand("UPDATE Users SET osu = @username WHERE Users.id = @id",
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
                    new SqliteCommand("INSERT INTO Users (id, osu) VALUES (@id, @username)",
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
    }
}