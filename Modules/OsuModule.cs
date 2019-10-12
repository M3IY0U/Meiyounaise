using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
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
        public static void UpdateTopPlays()
        {
            foreach (var user in Users.UserList)
            {
                if (user.Osu == "#" || string.IsNullOrWhiteSpace(user.Osu))
                    continue;
                var ub = osuApi.GetUserBestByUsername(user.Osu, limit: 50);
                if (Bot.OsuTops.ContainsKey(user.Osu))
                    Bot.OsuTops[user.Osu] = ub;
                else
                    Bot.OsuTops.Add(user.Osu, ub);
            }
        }

        public static async void OsuTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                foreach (var (key, oldTop) in Bot.OsuTops)
                {
                    var newTop = osuApi.GetUserBestByUsername(key, limit: 50);

                    if (newTop.SequenceEqual(oldTop, comp))
                        continue;

                    for (var i = 0; i < newTop.Count; i++)
                    {
                        var play = newTop[i];
                        if (play.BeatmapId == oldTop[i].BeatmapId)
                            continue;

                        var map = osuApi.GetBeatmap(play.BeatmapId);
                        var user = osuApi.GetUserByName(key);
                        var ts = DateTime.Now.Subtract(play.Date);
                        var dtFlag = play.Mods.ToString().ToLower().Contains("doubletime") ||
                                     play.Mods.ToString().ToLower().Contains("nightcore");

                        var eb = new DiscordEmbedBuilder()
                            .WithAuthor($"New #{i + 1} for {key}!", $"https://osu.ppy.sh/users/{play.Userid}",
                                $"http://s.ppy.sh/a/{play.Userid}")
                            .WithThumbnailUrl(map.ThumbnailUrl)
                            .WithColor(DiscordColor.Gold)
                            .WithDescription(
                                $"» **[{map.Title} [{map.Difficulty}]](https://osu.ppy.sh/b/{map.BeatmapId})**\n" +
                                $"» **{Math.Round(!dtFlag ? map.DifficultyRating : map.DifficultyRating * 1.4, 2)}★** » {TimeSpan.FromSeconds(!dtFlag ? map.TotalLength : map.TotalLength / 1.5):mm\\:ss} » {(!dtFlag ? map.Bpm : map.Bpm * 1.5)}bpm » +{play.Mods}\n" +
                                $"» {DiscordEmoji.FromName(Bot.Client, $":{play.Rank}_Rank:")} » **{Math.Round(play.Accuracy, 2)}%** » **{Math.Round(play.Pp, 2)}pp**\n" +
                                $"» {play.TotalScore} » x{play.MaxCombo}/{map.MaxCombo} » [{play.Count300}/{play.Count100}/{play.Count50}/{play.Miss}]\n" +
                                $"New Global Rank: #{user.GlobalRank} (:flag_{user.Country.ToLower()}: #{user.RegionalRank})")
                            .WithFooter(ts.Minutes == 0 ? "" : $"{ts.Minutes} minutes " + $"{ts.Seconds} seconds ago");
                        var osuChannel = await Bot.Client.GetChannelAsync(560880902570246174);
                        await osuChannel.SendMessageAsync(embed: eb.Build());
                        break;
                    }
                }

                UpdateTopPlays();
            }
            catch (Exception exception)
            {
                Console.WriteLine("Timer callback failed: " + exception.Message);
            }
        }

        private class TopPlayComparator : IEqualityComparer<UserBest>
        {
            public bool Equals(UserBest x, UserBest y)
            {
                if (x == null || y == null)
                    return false;
                return x.BeatmapId == y.BeatmapId;
            }

            public int GetHashCode(UserBest obj)
            {
                return obj.BeatmapId.GetHashCode();
            }
        }

        private static TopPlayComparator comp = new TopPlayComparator();

        private static OsuApi osuApi = new OsuApi(new OsuSharpConfiguration
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

        [Command("stats")]
        public async Task OsuStats(CommandContext ctx, string username = "", int limit = 50)
        {
            if (username == "")
            {
                username = Utilities.ResolveName("osu", ctx.User);
            }

            await ctx.TriggerTypingAsync();
            var result = await osuApi.GetUserBestAndBeatmapByUsernameAsync(username, limit: limit);
            if (result == null || result.Count == 0)
                throw new Exception($"No user by the name `{username}` found!");

            var avgStars = 0f;
            var fcCount = 0;
            var avgLength = 0;
            var avgBpm = 0f;
            var avgPp = 0f;
            var avgAr = 0f;
            var avgDate = new List<DateTime>();
            var avgRankedDate = new List<DateTime>();
            var mapperCount = new Dictionary<string, int>();
            var modCount = new Dictionary<string, int>();
            foreach (var entry in result)
            {
                if (entry.UserBest.MaxCombo == entry.Beatmap.MaxCombo)
                    fcCount++;
                avgStars += entry.Beatmap.DifficultyRating;
                avgAr += entry.Beatmap.ApproachRate;
                avgPp += entry.UserBest.Pp;
                avgLength += entry.Beatmap.TotalLength;
                avgBpm += entry.Beatmap.Bpm;
                avgDate.Add(entry.UserBest.Date);
                avgRankedDate.Add(entry.Beatmap.ApprovedDate);
                if (mapperCount.ContainsKey(entry.Beatmap.Creator))
                    mapperCount[entry.Beatmap.Creator]++;
                else
                    mapperCount.Add(entry.Beatmap.Creator, 1);

                if (modCount.ContainsKey(entry.UserBest.Mods.ToString()))
                    modCount[entry.UserBest.Mods.ToString()]++;
                else
                    modCount.Add(entry.UserBest.Mods.ToString(), 1);
            }

            avgStars /= result.Count;
            avgAr /= result.Count;
            avgLength /= result.Count;
            avgBpm /= result.Count;
            avgPp /= result.Count;

            var avg = avgDate.Average(x => x.TimeOfDay.TotalMilliseconds);
            var avgRanked = (int) avgRankedDate.Average(x => x.Year);

            var mostPlayedMod = modCount.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
            var mostPlayedMapper = mapperCount.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;

            var eb = new DiscordEmbedBuilder()
                .WithAuthor($"Stats for {username}", $"https://osu.ppy.sh/users/{result.First().UserBest.Userid}",
                    $"http://s.ppy.sh/a/{result.First().UserBest.Userid}")
                .WithColor(new DiscordColor(220, 152, 164))
                .WithFooter("All values are averages")
                .WithDescription($"**Star Rating** » {Math.Round(avgStars, 2)}\n" +
                                 $"**Length** » {avgLength}s\n" +
                                 $"**BPM** » {Math.Round(avgBpm)}\n" +
                                 $"**AR** » {Math.Round(avgAr, 2)}\n" +
                                 $"**PP** » {Math.Round(avgPp, 2)}\n" +
                                 $"**Mapper** » {mostPlayedMapper}\n" +
                                 $"**Year** » {avgRanked}\n" +
                                 $"**Mods** » {mostPlayedMod}\n" +
                                 $"**Perfect Combos** » {fcCount}/{result.Count}\n" +
                                 $"**Time when submitting** » {TimeSpan.FromMilliseconds(avg):g}\n");
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
                    $"Local Rank : #{user.RegionalRank} {DiscordEmoji.FromName(Bot.Client, $":flag_{user.Country.ToLower()}:")}");
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
                    new SqliteCommand("INSERT INTO Users (id, lastfm, osu) VALUES (@id, '#', @username)",
                        Utilities.Con))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@id", ctx.User.Id);
                    cmd.ExecuteReader();
                }

                Utilities.Con.Close();
                Users.UpdateUser(ctx.User);
            }

            var top = await osuApi.GetUserBestByUsernameAsync(username, limit: 50);
            if (Bot.OsuTops.ContainsKey(username))
            {
                Bot.OsuTops[username] = top;
            }
            else
            {
                Bot.OsuTops.Add(username, top);
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

        [Command("compare"), Priority(3)]
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

        [Command("compare"), Priority(2)]
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

        [Command("compare"), Priority(1)]
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