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
using OsuSharp.Endpoints;

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

        [Command("compare"), Aliases("comp", "cmp"), Priority(4)]
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
                content +=
                    $"**#{counter + 1} [{mapInfo.Title}](https://osu.ppy.sh/b/{map.BeatmapId})** [{mapInfo.Difficulty}] +{map.Mods} [{Math.Round(mapInfo.DifficultyRating, 2)}★]\n" +
                    $"» {DiscordEmoji.FromName(ctx.Client, $":{map.Rank}_Rank:")} » **{Math.Round(map.Pp, 2)}pp** » {Math.Round(map.Accuracy, 2)}%\n" +
                    $"» {map.TotalScore} » {map.MaxCombo}/{mapInfo.MaxCombo} » [{map.Count100}/{map.Count50}/{map.Miss}]\n" +
                    $"» Achieved on {map.Date:dd.MM.yy H:mm:ss}\n\n";

                if (++counter % 5 != 0) continue;
                eb.WithAuthor($"Top osu! Standard Plays for {username}", $"https://osu.ppy.sh/users/{user.Userid}",
                    $"http://s.ppy.sh/a/{user.Userid}");
                eb.WithDescription(content);
                eb.WithColor(new DiscordColor(220, 152, 164));
                pages.Add(new Page(embed: eb));
                eb = new DiscordEmbedBuilder();
                content = "";
            }

            await ctx.Client.GetInteractivity().SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages);
        }

        [Command("leaderboard"), Aliases("lb")]
        public async Task OsuLeaderBoard(CommandContext ctx)
        {
            var eb = new DiscordEmbedBuilder()
                .WithAuthor($"osu! Standard Leaderboard for {ctx.Guild.Name}", iconUrl: ctx.Guild.IconUrl);
            var users = new List<User>();
            foreach (var (_, value) in ctx.Guild.Members)
            {
                if (Users.GetUser(value) == null || Users.GetUser(value).Osu == "#" ||
                    string.IsNullOrWhiteSpace(Users.GetUser(value).Osu)) continue;
                var user = await osuApi.GetUserByNameAsync(Users.GetUser(value).Osu);
                users.Add(user);
            }

            if (users.Count == 0)
                throw new Exception("No users in this guild have linked their osu! account!");

            users = users.OrderBy(x => x.GlobalRank).Take(25).ToList();
            var i = 0;
            foreach (var user in users)
            {
                eb.AddField($"Server Rank: #{++i}",
                    $"[{user.Username}](https://osu.ppy.sh/users/{user.Userid})\nGlobal Rank: #{user.GlobalRank} ({Math.Round(user.Pp, 2)}pp)\n" +
                    $"Local Rank : #{user.RegionalRank} {DiscordEmoji.FromName(Bot.Client, $":flag_{user.Country.ToLower()}:")}",
                    true);
            }

            await ctx.RespondAsync(embed: eb.Build());
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
        
        //Overloads//////////////////////////////////////////////////////////////////////
        [Command("top"), Priority(1)]
        public async Task OsuTop(CommandContext ctx, DiscordUser user = null)
        {
            if (user == null)
            {
                user = ctx.User;
            }

            try
            {
                var name = Utilities.ResolveName("osu", user);
                await OsuTop(ctx, name);
            }
            catch (Exception)
            {
                await OsuTop(ctx, user.Username);
            }
        }

        [GroupCommand, Priority(1)]
        public async Task Osu(CommandContext ctx, DiscordUser user = null)
        {
            if (user == null)
            {
                user = ctx.User;
            }

            try
            {
                var name = Utilities.ResolveName("osu", user);
                await Osu(ctx, name);
            }
            catch (Exception)
            {
                await Osu(ctx, user.Username);
            }
        }

        [Command("compare"),Priority(3)]
        public async Task OsuComp(CommandContext ctx, DiscordUser toComp, DiscordUser self = null)
        {
            if (self == null)
            {
                self = ctx.User;
            }

            string sName;
            string name;

            try
            {
                sName = Utilities.ResolveName("osu", self);
            }
            catch (Exception)
            {
                sName = self.Username;
            }

            try
            {
                name = Utilities.ResolveName("osu", toComp);
            }
            catch (Exception)
            {
                name = toComp.Username;
            }

            await OsuComp(ctx, name, sName);
        }
        
        [Command("compare"),Priority(2)]
        public async Task OsuComp(CommandContext ctx, DiscordUser toComp, string self = "")
        {
            if (self == "")
            {
                self = Utilities.ResolveName("osu", ctx.User);
            }
            string name;

            try
            {
                name = Utilities.ResolveName("osu", toComp);
            }
            catch (Exception)
            {
                name = toComp.Username;
            }

            await OsuComp(ctx, self, name);
        }
        
        [Command("compare"),Priority(1)]
        public async Task OsuComp(CommandContext ctx, string toComp, DiscordUser self = null)
        {
            if (self == null)
            {
                self = ctx.User;
            }

            string sName;

            try
            {
                sName = Utilities.ResolveName("osu", self);
            }
            catch (Exception)
            {
                sName = self.Username;
            }

            await OsuComp(ctx, toComp, sName);
        }
    }
}