using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CoolAsciiFaces;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Markov;
using Meiyounaise.DB;

namespace Meiyounaise.Modules
{
    public class MiscModule : BaseCommandModule
    {
        [Command("sleep")]
        [Description(
            "Ask when you should go to bed at a specified time or when you should wake up if you don't specify one.")]
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
            await temp.ModifyAsync(embed: embed.Build());
        }

        [Command("markov"), Aliases("sentence"), Description(("Generates a \"sentence\" from the last 50 messages"))]
        public async Task Markov(CommandContext ctx, int amount = 50)
        {
            if (amount >= 1000)
                amount = 1000;
            
            var messages = await ctx.Channel.GetMessagesAsync(amount);
            //messages = messages.Where(m => !m.Author.IsBot).ToList(); //Filter Bot messages
            var lines = messages.Select(msg => msg.Content).ToList(); //Select content
            lines = lines.Where(s => !string.IsNullOrEmpty(s)).ToList(); //Remove empty messages
            lines = lines.Where(s => !s.Contains($"{Guilds.GetGuild(ctx.Guild).Prefix}markov")).ToList();
            var chain = new MarkovChain<string>(0);
            foreach (var line in lines)
            {
                chain.Add(line.Split());
            }

            var rand = new Random();
            await ctx.RespondAsync(string.Join(" ", chain.Chain()));
        }

        [Command("mock"), Aliases("spott")]
        public async Task Mock(CommandContext ctx, string content = "")
        {
            if (content == "")
            {
                var message = await ctx.Channel.GetMessagesAsync(2);
                content = message.Last().Content;
            }

            var result = "";

            for (var i = 0; i < content.Length; i++)
            {
                if (i % 2 == 0)
                {
                    result += char.ToLower(content[i]);
                }
                else
                {
                    result += char.ToUpper(content[i]);
                }
            }

            await ctx.RespondAsync(result);
        }
    }
}