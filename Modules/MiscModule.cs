using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CoolAsciiFaces;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace Meiyounaise.Modules
{
    public class MiscModule : BaseCommandModule
    {
        [Command("sleep")]
        [Description("Ask when you should go to bed at a specified time or when you should wake up if you don't specify one.")]
        public async Task Sleep(CommandContext ctx, string time = "")
        {
            var dtime = DateTime.Now;
            if (time != "")
            {
                if (!DateTime.TryParse(time, out dtime))
                {
                    await ctx.RespondAsync("Couldn't parse time!");
                    return;
                }
            }
            var eb = new DiscordEmbedBuilder()
                .WithAuthor("Sleep Calculator", null, "https://png.pngtree.com/svg/20170223/sleep_412926.png")
                .WithColor(new DiscordColor(14, 46, 96));
            eb.WithDescription(
                time == ""
                    ? $"{dtime.AddHours(2).AddMinutes(44).ToShortTimeString()} or {dtime.AddHours(4).AddMinutes(14).ToShortTimeString()} or {dtime.AddHours(5).AddMinutes(44).ToShortTimeString()} or {dtime.AddHours(7).AddMinutes(14).ToShortTimeString()} or {dtime.AddHours(8).AddMinutes(44).ToShortTimeString()} or {dtime.AddHours(10).AddMinutes(14).ToShortTimeString()}"
                    : $"{dtime.AddHours(-9).AddMinutes(-14).ToShortTimeString()} or {dtime.AddHours(-7).AddMinutes(-44).ToShortTimeString()} or {dtime.AddHours(-6).AddMinutes(-14).ToShortTimeString()} or {dtime.AddHours(-4).AddMinutes(-44).ToShortTimeString()}");

            eb.WithTitle(time == ""
                ? "If you head to bed right now, you should try to wake up at one of the following times:"
                : $"If you want to wake up at {dtime.ToShortTimeString()} you should try to go to bed at one of the following times:");
            await ctx.RespondAsync("", false, eb.Build());
        }
        
        [Command("twitchlotto"), Aliases("tr", "tl")]
        [Description("Returns a link from the infamous Twitch Lotto.")]
        public async Task TwitchLotto(CommandContext ctx)
        {
            if (!ctx.Channel.IsNSFW)
            {
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("🔞"));
                return;
            }
            var lines = File.ReadAllLines(Utilities.DataPath + "urls.txt");
            var r = new Random();
            var randomLineNumber = r.Next(0, lines.Length - 1);
            await ctx.RespondAsync(lines[randomLineNumber]);
        }
        
        [Command("someone"), Description("Returns a random online user, kinda like Discord's april fools prank.")]
        public async Task Someone(CommandContext ctx, [RemainingText] string input = "")
        {
            var rand = new Random();
            var users = ctx.Guild.Members.Values;
            var result = users.Where(x => !x.IsBot && x.Presence != null).ToList();
            await ctx.RespondAsync($"{Cool.Face} {result[rand.Next(result.Count)].Username} {input}");
        }
        
        [Command("ping"), Description("Returns the Bot's Latencies.")]
        public async Task Ping(CommandContext ctx)
        {
            var stopwatch = new Stopwatch();
            var embed = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Black)
                .AddField("WS Latency", $"{ctx.Client.Ping}ms", true);
            var temp = await ctx.Channel.SendMessageAsync("Ping...");
            stopwatch.Start();
            await temp.ModifyAsync("Pong!");
            stopwatch.Stop();
            embed.AddField("\"Local\" latency", $"{stopwatch.Elapsed.Milliseconds}ms", true);
            await temp.ModifyAsync(embed:embed.Build());
        }
    }
}