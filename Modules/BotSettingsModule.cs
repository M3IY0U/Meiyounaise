using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Meiyounaise.DB;
using Microsoft.Data.Sqlite;

namespace Meiyounaise.Modules
{
    public class SettingsModule : BaseCommandModule
    {
        [Command("status")]
        [Description("Changes the Bot's \"Playing\" Status.")]
        public async Task Status(CommandContext ctx, [RemainingText, Description("The new status.")]
            string status)
        {
            await ctx.Client.UpdateStatusAsync(new DiscordActivity(status,ActivityType.ListeningTo));
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
            Utilities.Con.Open();
            using (var cmd = new SqliteCommand($"INSERT INTO Status VALUES ('{status}');",Utilities.Con))
            {
                cmd.ExecuteReader();
            }
            Utilities.Con.Close();
        }

        [Command("prefix")]
        [Description("Set the Bot's Prefix on this guild.")]
        public async Task Prefix(CommandContext ctx, [RemainingText, Description("The new Prefix")] string newPrefix = "")
        {
            if (newPrefix=="")
            {
                await ctx.RespondAsync($"The prefix on this guild is `{Guilds.GetGuild(ctx.Guild).Prefix}`");
                return;
            }
            Utilities.Con.Open();
            using (var cmd = new SqliteCommand($"UPDATE Guilds SET prefix = '{newPrefix}' WHERE Guilds.id = {ctx.Guild.Id}", Utilities.Con))
            {
                cmd.ExecuteReader();
            }
            Utilities.Con.Close();
            Guilds.UpdateGuild(ctx.Guild);
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
        }

        [Command("nick"), Description("Changes the Bot's Nickname.")]
        public async Task Nick(CommandContext ctx, [RemainingText, Description("The new Nickname.")]
            string newNick)
        {
            await ctx.Guild.CurrentMember.ModifyAsync(x=>x.Nickname= newNick);
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
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
                .WithDescription("This rewrite of Meiyounaise is being maintained by Meiyou#0001 on Discord. \n" +
                                 "If something breaks or kills the bot, please tell me :)\n" +
                                 "[Here's an invite if you want this bot on your server](https://discordapp.com/oauth2/authorize?client_id=488112585640509442&permissions=16384&scope=bot)")
                .AddField("Process UpTime", (DateTime.Now - process.StartTime).ToString("d'd 'h'h 'm'm 's's'"), true)
                .AddField("Current Threads", process.Threads.Count.ToString(), true)
                .AddField("Memory Usage", $"{process.WorkingSet64 / 1000000} MB", true)
                .AddField("Guilds", ctx.Client.Guilds.Count.ToString(), true)
                .AddField("Library Version", ctx.Client.VersionString, true);
            await ctx.RespondAsync(null, false, embed.Build());
        }
    }
}