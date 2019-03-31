using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace Meiyounaise.Modules
{
    public class SettingsModule
    {
        [Command("status")]
        [Description("Changes the Bot's \"Playing\" Status.")]
        public async Task Status(CommandContext ctx, [RemainingText, Description("The new status.")]
            string status)
        {
            await ctx.Client.UpdateStatusAsync(new DiscordGame(status));
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
        }

        [Command("nick"), Description("Changes the Bot's Nickname.")]
        public async Task Nick(CommandContext ctx, [RemainingText, Description("The new Nickname.")]
            string newnick)
        {
            await ctx.Guild.CurrentMember.ModifyAsync(newnick);
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
        }

        [Command("icon"), Description("Change the Botx Avatar"), RequireOwner]
        public async Task Icon(CommandContext ctx, string url = null)
        {
            var path = $"{Utilities.DataPath}icon.png";
            await Utilities.DownloadAsync(new Uri(url ?? ctx.Message.Attachments.First().Url), path);
            await ctx.Client.EditCurrentUserAsync(null, new FileStream(path, FileMode.Open));
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
            File.Delete(path);
        }

        [Command("info")]
        public async Task Info(CommandContext ctx)
        {
            var rand = new Random();
            var process = Process.GetCurrentProcess();
            var embed = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor((float) rand.NextDouble(), (float) rand.NextDouble(),
                    (float) rand.NextDouble()))
                .WithAuthor("Meiyounaise", null, ctx.Client.CurrentUser.AvatarUrl)
                .WithTitle("Info about this Bot")
                .WithDescription("Meiyounaise is being maintained by Meiyou#0001 on Discord. \n" +
                                 "If something breaks or kills the bot, please tell me :)\n" +
                                 "[Here's an invite if you want this bot on your server](https://discordapp.com/oauth2/authorize?client_id=488112585640509442&permissions=16384&scope=bot)")
                .AddField("Uptime", (DateTime.Now - process.StartTime).ToString("d'd 'h'h 'm'm 's's'"), true)
                .AddField("Current Threads", process.Threads.Count.ToString(), true)
                .AddField("Memory Usage", $"{process.WorkingSet64 / 1000000} MB", true)
                .AddField("Guilds", ctx.Client.Guilds.Count.ToString(), true);
            await ctx.RespondAsync(null, false, embed.Build());
        }
    }
}