using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Meiyounaise.DB;
using Microsoft.Data.Sqlite;

namespace Meiyounaise.Modules
{
    [Group("GuildConfig"), Aliases("gc"),
     Description("Commands for configuring the Emoji Board and join/leave messages in a guild.")]
    public class GuildConfigModule : BaseCommandModule
    {
        [Command("joinmsg"), Description("Sets the message that the bot will post if someone joins the guild.")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task SetJoinMsg(CommandContext ctx,
            [RemainingText,
             Description(
                 "The new Join message. You can use '[user]' for pinging the joined user. Pass 'disable' if you don't want one.")]
            string jm = "")
        {
            if (jm == "disable")
            {
                jm = "empty";
            }

            if (jm == "")
            {
                await ctx.RespondAsync(
                    $"The current JoinMessage on this guild is: `\"{Guilds.GetGuild(ctx.Guild).JoinMsg}\"`.");
                return;
            }

            Utilities.Con.Open();
            using (var cmd = new SqliteCommand("UPDATE Guilds SET joinMsg = @message WHERE Guilds.id = @id",
                Utilities.Con))
            {
                cmd.Parameters.AddWithValue("@message", jm);
                cmd.Parameters.AddWithValue("@id", ctx.Guild.Id);
                cmd.ExecuteReader();
            }

            Utilities.Con.Close();
            Guilds.UpdateGuild(ctx.Guild);
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
        }

        [Command("msgchannel"), Description("Sets the channel where the bot will post join/leave messages.")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task SetJoinLeaveChannel(CommandContext ctx,
            [Description("A mention of the channel you want the messages to appear in or 'disable' to disable it.")]
            string chn = "")
        {
            switch (chn)
            {
                case "disable":
                    chn = "0";
                    break;
                case "":
                    await ctx.RespondAsync(Guilds.GetGuild(ctx.Guild).JlMessageChannel == 0
                        ? "Currently there is no channel specified for me to post Join/Leave messages in."
                        : $"The current Channel where I will send Join/Leave messages is: {ctx.Guild.GetChannel(Guilds.GetGuild(ctx.Guild).JlMessageChannel).Mention}");
                    return;

                default:
                {
                    if (ctx.Message.MentionedChannels.Count == 0)
                    {
                        await ctx.RespondAsync("I need a mention (#channelname) of the channel you want to use!");
                        return;
                    }

                    chn = ctx.Message.MentionedChannels.First().Id.ToString();
                    break;
                }
            }

            Utilities.Con.Open();
            using (var cmd =
                new SqliteCommand("UPDATE Guilds SET jlMsgChannel = @channel WHERE Guilds.id = @id",
                    Utilities.Con))
            {
                cmd.Parameters.AddWithValue("@channel", chn);
                cmd.Parameters.AddWithValue("@id", ctx.Guild.Id);
                cmd.ExecuteReader();
            }

            Utilities.Con.Close();
            Guilds.UpdateGuild(ctx.Guild);
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
        }

        [Command("ramount"),
         Description("Sets the required amount of reaction a message needs to be posted in the board.")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task Ramount(CommandContext ctx,
            [Description(
                "The amount of reaction (of any kind) a message needs to get posted in the board. Set to 0 to disable.")]
            string amount = "")
        {
            if (amount == "")
            {
                await ctx.RespondAsync(
                    $"A message currently needs {Guilds.GetGuild(ctx.Guild).ReactionNeeded} Reactions to be posted to the board.");
                return;
            }

            Utilities.Con.Open();
            using (var cmd =
                new SqliteCommand("UPDATE Guilds SET reactionNeeded = @amount WHERE Guilds.id = @id",
                    Utilities.Con))
            {
                cmd.Parameters.AddWithValue("@amount", int.Parse(amount));
                cmd.Parameters.AddWithValue("@id", ctx.Guild.Id);
                cmd.ExecuteReader();
            }

            Utilities.Con.Close();
            Guilds.UpdateGuild(ctx.Guild);
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
        }

        [Command("boardchannel"),
         Description("Sets the channel where the bot will post messages that have enough reactions.")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task SetBoardChannel(CommandContext ctx,
            [Description("A mention of the new board channel or 'disable' to disable it.")]
            string chn = "")
        {
            switch (chn)
            {
                case "disable":
                    chn = "0";
                    break;
                case "":
                    await ctx.RespondAsync(Guilds.GetGuild(ctx.Guild).BoardChannel == 0
                        ? "Currently there is no board channel specified."
                        : $"The current board channel is: {ctx.Guild.GetChannel(Guilds.GetGuild(ctx.Guild).BoardChannel).Mention}");
                    return;
                default:
                {
                    if (ctx.Message.MentionedChannels.Count == 0)
                    {
                        await ctx.RespondAsync("I need a mention (#channelname) of the channel you want to use!");
                        return;
                    }

                    chn = ctx.Message.MentionedChannels.First().Id.ToString();
                    break;
                }
            }

            Utilities.Con.Open();
            using (var cmd =
                new SqliteCommand("UPDATE Guilds SET boardChannel = @channel WHERE Guilds.id = @id",
                    Utilities.Con))
            {
                cmd.Parameters.AddWithValue("@channel", chn);
                cmd.Parameters.AddWithValue("@id", ctx.Guild.Id);
                cmd.ExecuteReader();
            }

            Utilities.Con.Close();
            Guilds.UpdateGuild(ctx.Guild);
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
        }

        [Command("bridgechannel"),
         Description("Sets the channel where the bot will post messages that have enough reactions.")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task SetBridgeChannel(CommandContext ctx,
            [Description("A mention of the new bridge channel or 'disable' to disable it.")]
            string chn = "")
        {
            switch (chn)
            {
                case "disable":
                    chn = "0";

                    break;
                case "":
                    await ctx.RespondAsync(Guilds.GetGuild(ctx.Guild).BridgeChannel == 0
                        ? "Currently there is no board channel specified."
                        : $"The current board channel is: {ctx.Guild.GetChannel(Guilds.GetGuild(ctx.Guild).BridgeChannel).Mention}");
                    return;
                default:
                {
                    if (ctx.Message.MentionedChannels.Count == 0)
                    {
                        await ctx.RespondAsync("I need a mention (#channelname) of the channel you want to use!");
                        return;
                    }

                    chn = ctx.Message.MentionedChannels.First().Id.ToString();
                    break;
                }
            }

            Utilities.Con.Open();
            using (var cmd =
                new SqliteCommand("UPDATE Guilds SET bridgeChannel = @channel WHERE Guilds.id = @id",
                    Utilities.Con))
            {
                cmd.Parameters.AddWithValue("@channel", chn);
                cmd.Parameters.AddWithValue("@id", ctx.Guild.Id);
                cmd.ExecuteReader();
            }

            Utilities.Con.Close();
            Guilds.UpdateGuild(ctx.Guild);
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
        }
        
        [Command("leavemsg"), Description("Sets the message that the bot will post if someone leaves the guild.")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task SetLeaveMsg(CommandContext ctx,
            [RemainingText,
             Description(
                 "The new Leave message. You can use '[user]' for \"pinging\" the user. Pass 'disable' if you don't want one.")]
            string lm = "")
        {
            if (lm == "disable")
            {
                lm = "empty";
            }

            if (lm == "")
            {
                await ctx.RespondAsync(
                    $"The current LeaveMessage on this guild is: \"{Guilds.GetGuild(ctx.Guild).LeaveMsg}\"");
                return;
            }

            Utilities.Con.Open();
            using (var cmd = new SqliteCommand("UPDATE Guilds SET leaveMsg = @message WHERE Guilds.id = @id",
                Utilities.Con))
            {
                cmd.Parameters.AddWithValue("@message", lm);
                cmd.Parameters.AddWithValue("@id", ctx.Guild.Id);
                cmd.ExecuteReader();
            }

            Utilities.Con.Close();
            Guilds.UpdateGuild(ctx.Guild);
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
        }

        [Command("repeatmsg"),
         Description("Sets the required amount messages needed to repeat it.")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task RepeatMsg(CommandContext ctx,
            [Description("The amount of messages that need to have the same content. Set to 0 to disable.")]
            string amount = "")
        {
            if (amount == "")
            {
                await ctx.RespondAsync(
                    $"If {Guilds.GetGuild(ctx.Guild).PrevMessageAmount} messages in a row have the same content, the bot will repeat it");
                return;
            }

            Utilities.Con.Open();
            using (var cmd =
                new SqliteCommand("UPDATE Guilds SET prevMessageAmount = @amount WHERE Guilds.id = @id",
                    Utilities.Con))
            {
                cmd.Parameters.AddWithValue("@amount", int.Parse(amount));
                cmd.Parameters.AddWithValue("@id", ctx.Guild.Id);
                cmd.ExecuteReader();
            }

            Utilities.Con.Close();
            Guilds.UpdateGuild(ctx.Guild);
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
        }
    }
}