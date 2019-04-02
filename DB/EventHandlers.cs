using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Data.Sqlite;

namespace Meiyounaise.DB
{
    public static class EventHandlers
    {
        public static async Task Ready(ReadyEventArgs e)
        {
            var guilds = e.Client.Guilds.Select(guild => guild.Value).ToList();
            var dbGuilds = new List<ulong>();
            Utilities.Con.Open();
            using (var cmd = new SqliteCommand($"SELECT id FROM Guilds", Utilities.Con))
            {
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        dbGuilds.Add(Convert.ToUInt64(rdr.GetString(0)));
                    }
                }
            }

            foreach (var guild in guilds)
            {
                if (dbGuilds.Contains(guild.Id)) continue;
                using (var cmd =
                    new SqliteCommand(
                        $"INSERT INTO Guilds (id,prefix,boardChannel,joinMsg,leaveMsg,jlMsgChannel,reactionNeeded) VALUES ('{guild.Id}','&','0','empty','empty', '0','0')",
                        Utilities.Con))
                {
                    cmd.ExecuteReader();
                }
            }

            Utilities.Con.Close();
            await Status(e);
        }

        public static Task GuildCreated(GuildCreateEventArgs args)
        {
            Utilities.Con.Open();
            using (var cmd =
                new SqliteCommand(
                    $"INSERT INTO Guilds (id,prefix,boardChannel,joinMsg,leaveMsg, jlMsgChannel, reactionNeeded) VALUES ('{args.Guild.Id}','&','0','empty', 'empty', '0', '0')",
                    Utilities.Con))
            {
                cmd.ExecuteReader();
            }

            Utilities.Con.Close();
            return Task.CompletedTask;
        }

        private static async Task Status(DiscordEventArgs e)
        {
            var c = e.Client as DiscordClient;
            Utilities.Con.Open();
            using (var cmd =
                new SqliteCommand(
                    "SELECT text FROM Status ORDER BY RANDOM() LIMIT 1", Utilities.Con))
            {
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        c?.UpdateStatusAsync(new DiscordGame(rdr.GetString(0)));
                    }
                }
            }

            Utilities.Con.Close();
            await Task.CompletedTask;
        }

        public static async Task UserJoined(GuildMemberAddEventArgs e)
        {
            var guild = Guilds.GetGuild(e.Guild);

            if (!ShouldSendMessage(guild.JoinMsg, guild.JlMessageChannel)) return;

            await e.Guild.GetChannel(guild.JlMessageChannel)
                .SendMessageAsync(guild.JoinMsg.Replace("[user]", e.Member.Mention));
        }

        public static async Task UserRemoved(GuildMemberRemoveEventArgs e)
        {
            var guild = Guilds.GetGuild(e.Guild);

            if (!ShouldSendMessage(guild.LeaveMsg, guild.JlMessageChannel)) return;

            await e.Guild.GetChannel(guild.JlMessageChannel)
                .SendMessageAsync(guild.LeaveMsg.Replace("[user]", $"{e.Member.Username}#{e.Member.Discriminator}"));
        }

        private static bool ShouldSendMessage(string m, ulong c)
        {
            return m != "empty" && m != "" && c != 0;
        }

        public static async Task ReactionAdded(MessageReactionAddEventArgs e)
        {
            if (e.User.IsBot) return;
            
            var guild = Guilds.GetGuild(e.Message.Channel.Guild);
            
            //ABORT IF BOARD IS NOT ENABLED/SET UP
            if (guild.BoardChannel == 0 || guild.ReactionNeeded == 0) return;
            
            
            
            if (!Messages.BoardMessages.ContainsKey(e.Message.Id))
            {
                Messages.BoardMessages.Add(e.Message.Id, false);
                Utilities.Con.Open();
                using (var cmd = new SqliteCommand($"INSERT INTO Messages (id,sent) VALUES ('{e.Message.Id}','{false}')",Utilities.Con))
                {
                    cmd.ExecuteReader();
                }
                Utilities.Con.Close();
            }
            //ABORT IF MESSAGE IS ALREADY SENT
            if (Messages.BoardMessages[e.Message.Id]) return;

            var msg = await e.Channel.GetMessageAsync(e.Message.Id);
            
            if (e.Message.Reactions.Any(reaction => reaction.Count >= guild.ReactionNeeded))
            {
                var builder = new DiscordEmbedBuilder()
                    .AddField("Author", msg.Author.Mention, true)
                    .AddField("Channel", msg.Channel.Mention, true)
                    .WithThumbnailUrl(msg.Author.AvatarUrl)
                    .AddField("Link",
                        $"[Jump to](https://discordapp.com/channels/{e.Message.Channel.Guild.Id}/{e.Message.ChannelId}/{e.Message.Id})")
                    .WithTimestamp(msg.Timestamp)
                    .WithFooter($"{e.Emoji.GetDiscordName()}");
                if (msg.Content!="")
                {
                    builder.AddField("Message", msg.Content);
                }
                if (msg.Attachments.Any())
                {
                    builder.WithImageUrl(msg.Attachments.First().Url);
                }
                await e.Message.Channel.Guild.GetChannel(guild.BoardChannel).SendMessageAsync(null,false,builder.Build());
                Messages.BoardMessages[e.Message.Id] = true;
                Utilities.Con.Open();
                using (var cmd = new SqliteCommand($"UPDATE Messages SET sent= 'true' WHERE Messages.id = '{e.Message.Id}'",Utilities.Con))
                {
                    cmd.ExecuteReader();
                }
                Utilities.Con.Close();
            }
        }
    }
}