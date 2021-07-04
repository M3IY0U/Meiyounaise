using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DSharpPlus;
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
        [Description("Changes the bot's status.")]
        public async Task Status(CommandContext ctx,
            [Description("Available options are:\n\"p\"-> playing\n\"l\"-> listening to\n\"w\"-> watching")]
            string opt, [RemainingText, Description("The new status.")]
            string status)
        {
            switch (opt.ToLower())
            {
                case "l":
                    await ctx.Client.UpdateStatusAsync(new DiscordActivity(status, ActivityType.ListeningTo));
                    break;
                case "p":
                    await ctx.Client.UpdateStatusAsync(new DiscordActivity(status, ActivityType.Playing));
                    break;
                case "w":
                    await ctx.Client.UpdateStatusAsync(new DiscordActivity(status, ActivityType.Watching));
                    break;
                default:
                    throw new Exception(
                        $"Available options are:\n\"p\"-> playing\n\"l\"-> listening to\n\"w\"-> watching\nExample: `{Guilds.GetGuild(ctx.Guild).Prefix}status l boomer music`");
            }

            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
        }

        [Command("prefix"), RequireUserPermissions(Permissions.ManageGuild)]
        [Description("Set the Bot's Prefix on this guild.")]
        public async Task Prefix(CommandContext ctx, [RemainingText, Description("The new Prefix")]
            string newPrefix)
        {
            Utilities.Con.Open();
            using (var cmd =
                new SqliteCommand("UPDATE Guilds SET prefix = @prefix WHERE Guilds.id = @id", Utilities.Con))
            {
                cmd.Parameters.AddWithValue("@prefix", newPrefix);
                cmd.Parameters.AddWithValue("@id", ctx.Guild.Id);
                cmd.ExecuteReader();
            }

            Utilities.Con.Close();
            Guilds.UpdateGuild(ctx.Guild);
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
        }

        [Command("prefix"), RequireUserPermissions(Permissions.ManageGuild), Priority(5)]
        [Description("Set the Bot's Prefix on this guild.")]
        public async Task Prefix(CommandContext ctx)
        {
            await ctx.RespondAsync($"The prefix on this guild is `{Guilds.GetGuild(ctx.Guild).Prefix}`");
        }

        [Command("nick"), Description("Changes the Bot's Nickname.")]
        public async Task Nick(CommandContext ctx, [RemainingText, Description("The new Nickname.")]
            string newNick)
        {
            await ctx.Guild.CurrentMember.ModifyAsync(x => x.Nickname = newNick);
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
            await ctx.RespondAsync(msg => msg.WithEmbed(embed));
        }
    }
}