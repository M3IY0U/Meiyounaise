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
            //Get all guilds the Bot is connected to
            var guilds = e.Client.Guilds.Select(guild => guild.Value).ToList();
           
            //Once the Client is ready, get all guilds in the database and put them into dbGuilds
            var dbGuilds = new List<ulong>();
            Utilities.Con.Open();
            using (var cmd = new SqliteCommand("SELECT id FROM Guilds", Utilities.Con))
            {
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        dbGuilds.Add(Convert.ToUInt64(rdr.GetString(0)));
                    }
                }
            }

            //Loop through all connected guilds and add the ones that aren't in the Database yet (in case the bot gets invited into a server while it's down)
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
            
            //Update the "Playing" status to something random
            await Status(e);
        }

        public static Task GuildCreated(GuildCreateEventArgs args)
        {
            if (Guilds.GuildList.Exists(x => x.Id == args.Guild.Id)) return Task.CompletedTask;
            //Once the bot joins a guild (while he's online), add it to the Database
            Utilities.Con.Open();
            using (var cmd =
                new SqliteCommand(
                    $"INSERT INTO Guilds (id,prefix,boardChannel,joinMsg,leaveMsg, jlMsgChannel, reactionNeeded) VALUES ('{args.Guild.Id}','&','0','empty', 'empty', '0', '0')",
                    Utilities.Con))
            {
                cmd.ExecuteReader();
            }
            Utilities.Con.Close();
            Guilds.GuildList.Add(new Guilds.Guild(args.Guild.Id));
            return Task.CompletedTask;
        }

        private static async Task Status(DiscordEventArgs e)
        {
            //Set the status to a random one from the database
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
            //Abort if the guild doesn't have join/leave messages set up
            if (!ShouldSendMessage(guild.JoinMsg, guild.JlMessageChannel)) return;

            await e.Guild.GetChannel(guild.JlMessageChannel)
                .SendMessageAsync(guild.JoinMsg.Replace("[user]", e.Member.Mention));
        }

        public static async Task UserRemoved(GuildMemberRemoveEventArgs e)
        {
            var guild = Guilds.GetGuild(e.Guild);
            //Abort if the guild doesn't have join/leave messages set up
            if (!ShouldSendMessage(guild.LeaveMsg, guild.JlMessageChannel)) return;
            
            await e.Guild.GetChannel(guild.JlMessageChannel)
                .SendMessageAsync(guild.LeaveMsg.Replace("[user]", $"{e.Member.Username}#{e.Member.Discriminator}"));
        }

        private static bool ShouldSendMessage(string m, ulong c)
        {
            //Check if the leave/join message isn't disabled, that it's not empty and that the channel is set 
            return m != "empty" && m != "" && c != 0;
        }

        public static async Task ReactionAdded(MessageReactionAddEventArgs e)
        {
            //Bot reactions don't count
            if (e.User.IsBot) return;
            //Put into variable for convenience
            var guild = Guilds.GetGuild(e.Message.Channel.Guild);
            
            //Abort if the guild has no board set up
            if (guild.BoardChannel == 0 || guild.ReactionNeeded == 0) return;
            
            //This means that it was the first reaction to the message, and that we have to add it to the Database
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
            //Put into variable for convenience
            var msg = await e.Channel.GetMessageAsync(e.Message.Id);
            
            //Abort if the message has already been posted to the board
            if (Messages.BoardMessages[msg.Id]) return;

            //Loop through all reactions on the message
            foreach (var reaction in msg.Reactions)
            {
                //If the current reaction is less than what we need, we skip this one
                if (reaction.Count < guild.ReactionNeeded) continue;
                //Build a fancy little embed
                var rand = new Random();
                var builder = new DiscordEmbedBuilder().AddField("Author", msg.Author.Mention, true)
                    .AddField("Channel", msg.Channel.Mention, true)
                    .WithThumbnailUrl(msg.Author.AvatarUrl)
                    .WithTimestamp(msg.Timestamp)
                    .WithColor(new DiscordColor((float) rand.NextDouble(), (float) rand.NextDouble(),
                        (float) rand.NextDouble()));
                //This is needed so that the bot doesn't shit itself if the message was just a picture
                if (msg.Content != "")
                {
                    builder.AddField("Message", msg.Content);
                }
                builder.AddField("Link",
                    $"[Jump to](https://discordapp.com/channels/{e.Message.Channel.Guild.Id}/{e.Message.ChannelId}/{e.Message.Id})");
                //If there were any attachments, put it into the image field of the embed (apparently it doesn't throw if it's an mp3/4 and I'm okay with that)
                if (msg.Attachments.Any())
                {
                    builder.WithImageUrl(msg.Attachments.First().Url);
                }
                //Post the message to the board
                await e.Message.Channel.Guild.GetChannel(guild.BoardChannel).SendMessageAsync($"{reaction.Emoji}", false, builder.Build());
                //Update it in our local collection and in the database so it doesn't get sent twice
                Messages.BoardMessages[e.Message.Id] = true;
                Utilities.Con.Open();
                using (var cmd = new SqliteCommand($"UPDATE Messages SET sent= 'true' WHERE Messages.id = '{e.Message.Id}'", Utilities.Con))
                {
                    cmd.ExecuteReader();
                }
                Utilities.Con.Close();
                //Break so it only gets posted once
                break;
            }
        }
    }
}