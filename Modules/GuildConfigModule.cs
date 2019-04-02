using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Meiyounaise.DB;
using Microsoft.Data.Sqlite;

namespace Meiyounaise.Modules
{
    [Group("GuildConfig"),Aliases("gc")]
    public class GuildConfigModule
    {
        [Command("joinmsg")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task SetJoinMsg(CommandContext ctx, [RemainingText,Description("The new Join message. You can use '[user]' for pinging the joined user. Pass 'disable' if you don't want one.")] string jm = "")
        {
            if (jm == "disable")
            {
                jm = "empty";
            }

            if (jm == "")
            {
                await ctx.RespondAsync($"The current JoinMessage on this guild is: \"{Guilds.GetGuild(ctx.Guild).JoinMsg}\"");
                return;
            }
            Utilities.Con.Open();
            using (var cmd = new SqliteCommand($"UPDATE Guilds SET joinMsg='{jm}' WHERE Guilds.id = '{ctx.Guild.Id}'", Utilities.Con))
            {
                cmd.ExecuteReader();
            }
            Utilities.Con.Close();
            Guilds.UpdateGuild(ctx.Guild);
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
        }

        [Command("msgchannel")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task SetJoinLeaveChannel(CommandContext ctx, [Description("A mention of the channel you want the messages to appear in or 'disable' to disable it.")]string chn = "")
        {
            if (chn == "disable")
            {
                chn = "0";
            }

            if (chn == "")
            {
                if (Guilds.GetGuild(ctx.Guild).JlMessageChannel==0)
                {
                    await ctx.RespondAsync("Currently there is no channel specified for me to post Join/Leave messages in.");
                    return;
                }
                await ctx.RespondAsync($"The current Channel where I will send Join/Leave messages is: {ctx.Guild.GetChannel(Guilds.GetGuild(ctx.Guild).JlMessageChannel).Mention}");
                return;
            }
            Utilities.Con.Open();
            using (var cmd = new SqliteCommand($"UPDATE Guilds SET jlMsgChannel = '{ctx.Message.MentionedChannels[0].Id}' WHERE Guilds.id = '{ctx.Guild.Id}'", Utilities.Con))
            {
                cmd.ExecuteReader();
            }
            Utilities.Con.Close();
            Guilds.UpdateGuild(ctx.Guild);
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
        }

        [Command("ramount")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task Ramount(CommandContext ctx, [Description("The amount of reaction (of any kind) a message needs to get posted in the board. Set to 0 to disable")]int amount)
        {
            Utilities.Con.Open();
            using (var cmd = new SqliteCommand($"UPDATE Guilds SET reactionNeeded = '{amount}' WHERE Guilds.id = '{ctx.Guild.Id}'", Utilities.Con))
            {
                cmd.ExecuteReader();
            }
            Utilities.Con.Close();
            Guilds.UpdateGuild(ctx.Guild);
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
        }

        [Command("boardchannel")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task SetBoardChannel(CommandContext ctx, [Description("A mention of the new board channel or 'disable' to disable it.")]string chn = "")
        {
            if (chn == "disable")
            {
                chn = "0";
            }

            if (chn == "")
            {
                if (Guilds.GetGuild(ctx.Guild).BoardChannel==0)
                {
                    await ctx.RespondAsync("Currently there is no board channel specified.");
                    return;
                }
                await ctx.RespondAsync($"The current board channel is: {ctx.Guild.GetChannel(Guilds.GetGuild(ctx.Guild).BoardChannel).Mention}");
                return;
            }
            Utilities.Con.Open();
            using (var cmd = new SqliteCommand($"UPDATE Guilds SET boardChannel = '{ctx.Message.MentionedChannels[0].Id}' WHERE Guilds.id = '{ctx.Guild.Id}'", Utilities.Con))
            {
                cmd.ExecuteReader();
            }
            Utilities.Con.Close();
            Guilds.UpdateGuild(ctx.Guild);
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
        }
        
        
        
        [Command("leavemsg")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task SetLeaveMsg(CommandContext ctx, [RemainingText,Description("The new Leave message. You can use '[user]' for \"pinging\" the user. Pass 'disable' if you don't want one.")] string lm = "")
        {
            if (lm == "disable")
            {
                lm = "empty";
            }

            if (lm == "")
            {
                await ctx.RespondAsync($"The current LeaveMessage on this guild is: \"{Guilds.GetGuild(ctx.Guild).LeaveMsg}\"");
                return;
            }
            Utilities.Con.Open();
            using (var cmd = new SqliteCommand($"UPDATE Guilds SET leaveMsg='{lm}' WHERE Guilds.id = '{ctx.Guild.Id}'", Utilities.Con))
            {
                cmd.ExecuteReader();
            }
            Utilities.Con.Close();
            Guilds.UpdateGuild(ctx.Guild);
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
        }
    }
}