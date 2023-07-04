using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Data.Sqlite;

namespace Meiyounaise.DB
{
    public static class EventHandlers
    {
        public static async Task Ready(DiscordClient sender, ReadyEventArgs readyEventArgs)
        {
            //Get all guilds the Bot is connected to
            var guilds = sender.Guilds.Select(guild => guild.Value).ToList();

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
                        "INSERT INTO Guilds (id,prefix,boardChannel,joinMsg,leaveMsg,jlMsgChannel,reactionNeeded, bridgeChannel) VALUES (@id,'&','0','empty','empty', '0','0', '0')",
                        Utilities.Con))
                {
                    cmd.Parameters.AddWithValue("@id", guild.Id);
                    cmd.ExecuteReader();
                }
            }

            Utilities.Con.Close();

            try
            {
                var file = new StreamReader("update.txt");
                var chn = await Bot.Client.GetChannelAsync(Convert.ToUInt64(file.ReadLine()));
                await chn.SendMessageAsync(
                    $"Back online.\nRestart took {Math.Round(DateTime.Now.Subtract(DateTime.Parse(file.ReadLine())).TotalSeconds), 2} seconds.");
                file.Close();
                File.Delete("update.txt");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Tried to announce time it took to restart but couldn't because of: {ex.Message}");
            }
            //Update the "Playing" status to something random
            await Status(sender);
        }

        public static Task GuildCreated(DiscordClient sender, GuildCreateEventArgs args)
        {
            if (Guilds.GuildList.Exists(x => x.Id == args.Guild.Id)) return Task.CompletedTask;
            //Once the bot joins a guild (while he's online), add it to the Database
            Utilities.Con.Open();
            using (var cmd =
                new SqliteCommand(
                    "INSERT INTO Guilds (id,prefix,boardChannel,joinMsg,leaveMsg, jlMsgChannel, reactionNeeded, bridgeChannel) VALUES (@id,'&','0','empty', 'empty', '0', '0', '0')",
                    Utilities.Con))
            {
                cmd.Parameters.AddWithValue("@id", args.Guild.Id);
                cmd.ExecuteReader();
            }

            Utilities.Con.Close();
            Guilds.GuildList.Add(new Guilds.Guild(args.Guild.Id));
            return Task.CompletedTask;
        }

        private static async Task Status(DiscordClient client)
        {
            //Set the status to a random one from the database
            Utilities.Con.Open();
            using (var cmd =
                new SqliteCommand(
                    "SELECT text FROM Status ORDER BY RANDOM() LIMIT 1", Utilities.Con))
            {
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        client?.UpdateStatusAsync(new DiscordActivity(rdr.GetString(0), ActivityType.ListeningTo));
                    }
                }
            }

            Utilities.Con.Close();
            await Task.CompletedTask;
        }

        public static async Task UserJoined(DiscordClient sender, GuildMemberAddEventArgs e)
        {
            var guild = Guilds.GetGuild(e.Guild);
            //Abort if the guild doesn't have join/leave messages set up
            if (!ShouldSendMessage(guild.JoinMsg, guild.JlMessageChannel)) return;

            await e.Guild.GetChannel(guild.JlMessageChannel)
                .SendMessageAsync(guild.JoinMsg.Replace("[user]", e.Member.Mention));
        }

        public static async Task UserRemoved(DiscordClient sender, GuildMemberRemoveEventArgs e)
        {
            var guild = Guilds.GetGuild(e.Guild);
            if (guild == null)
            {
                return;
            }

            //Abort if the guild doesn't have join/leave messages set up
            if (!ShouldSendMessage(guild.LeaveMsg, guild.JlMessageChannel)) return;

            await e.Guild.GetChannel(guild.JlMessageChannel)
                .SendMessageAsync(guild.LeaveMsg.Replace("[user]", $"@{e.Member.Username}"));
        }

        private static bool ShouldSendMessage(string m, ulong c)
        {
            //Check if the leave/join message isn't disabled, that it's not empty and that the channel is set 
            return m != "empty" && m != "" && c != 0;
        }

        public static async Task ReactionAdded(DiscordClient sender, MessageReactionAddEventArgs e)
        {
            //Bot reactions don't count
            if (e.User.IsBot) return;
            //Put into variable for convenience
            var guild = Guilds.GetGuild(e.Message.Channel.Guild);

            //Abort if the guild has no board set up
            if (guild.BoardChannel == 0 || guild.ReactionNeeded == 0) return;

            //This means that it was the first reaction to the message, and that we have to add it to the Database
            //if (!Messages.BoardMessages.ContainsKey(e.Message.Id))
            if (!Messages.BoardMessages.Exists(x => x.SourceId == e.Message.Id))
            {
                Messages.BoardMessages.Add(new Messages.Message {SourceId = e.Message.Id, BoardId = 0, Sent = false});
                Utilities.Con.Open();
                using (var cmd =
                    new SqliteCommand("INSERT INTO Messages (id,sent,bId) VALUES (@id,@sent,'0')",
                        Utilities.Con))
                {
                    cmd.Parameters.AddWithValue("@sent", false);
                    cmd.Parameters.AddWithValue("@id", e.Message.Id);
                    cmd.ExecuteReader();
                }

                Utilities.Con.Close();
            }

            //Put into variable for convenience
            var msg = await e.Channel.GetMessageAsync(e.Message.Id);

            //Abort if the message has already been posted to the board

            if (Messages.BoardMessages.Exists(x => x.SourceId == msg.Id && x.Sent))
            {
                var bmsg = await e.Channel.Guild.GetChannel(guild.BoardChannel)
                    .GetMessageAsync(Messages.BoardMessages.Find(x => x.SourceId == msg.Id).BoardId);

                //Loop through all reactions on the message
                var reactions = new List<string>();
                foreach (var reaction in msg.Reactions)
                {
                    if (reaction.Count < guild.ReactionNeeded) continue;
                    try
                    {
                        reactions.Add(reaction.Emoji.IsAnimated
                            ? $"{DiscordEmoji.FromGuildEmote(Bot.Client, reaction.Emoji.Id)} x {reaction.Count}"
                            : $"{DiscordEmoji.FromName(Bot.Client, reaction.Emoji.GetDiscordName())} x {reaction.Count}");
                    }
                    catch (Exception)
                    {
                        reactions.Add(reaction.Emoji.GetDiscordName());
                    }
                }

                await bmsg.ModifyAsync(string.Join(" • ", reactions));
            }
            else
            {
                var sendIt = false;
                var reactions = new List<string>();
                //Loop through all reactions on the message
                foreach (var reaction in msg.Reactions)
                {
                    //If the current reaction is less than what we need, we skip this one
                    if (reaction.Count < guild.ReactionNeeded) continue;
                    try
                    {
                        reactions.Add(reaction.Emoji.IsAnimated
                            ? $"{DiscordEmoji.FromGuildEmote(Bot.Client, reaction.Emoji.Id)} x {reaction.Count}"
                            : $"{DiscordEmoji.FromName(Bot.Client, reaction.Emoji.GetDiscordName())} x {reaction.Count}");
                    }
                    catch (Exception)
                    {
                        reactions.Add($"{reaction.Emoji.GetDiscordName()} x {reaction.Count}");
                    }

                    sendIt = true;
                }

                if (sendIt)
                {
                    //Post the message to the board
                    var bmsg = await e.Message.Channel.Guild.GetChannel(guild.BoardChannel)
                        .SendMessageAsync(messageBuilder => messageBuilder.WithContent(string.Join(" • ", reactions)).WithEmbed(BuildEmbed(msg)));

                    //Update it in our local collection and in the database so it doesn't get sent twice
                    Messages.BoardMessages.Remove(Messages.BoardMessages.Find(x => x.SourceId == e.Message.Id));
                    Messages.BoardMessages.Add(new Messages.Message
                    {
                        SourceId = msg.Id,
                        BoardId = bmsg.Id,
                        Sent = true
                    });
                    Utilities.Con.Open();
                    using (var cmd =
                        new SqliteCommand(
                            "UPDATE Messages SET sent= 'true', bId = @bid WHERE Messages.id = @id",
                            Utilities.Con))
                    {
                        cmd.Parameters.AddWithValue("@bid", bmsg.Id);
                        cmd.Parameters.AddWithValue("@id", e.Message.Id);
                        cmd.ExecuteReader();
                    }

                    Utilities.Con.Close();
                }
            }
        }

        public static async Task ReactionRemoved(DiscordClient sender, MessageReactionRemoveEventArgs e)
        {
            var guild = Guilds.GetGuild(e.Message.Channel.Guild);
            if (guild.BoardChannel == 0 || guild.ReactionNeeded == 0) return;
            if (e.User.IsBot) return;

            var msg = await e.Channel.GetMessageAsync(e.Message.Id);

            //Abort if the message has already been posted to the board

            if (Messages.BoardMessages.Exists(x => x.SourceId == msg.Id && x.Sent))
            {
                var bmsg = await e.Channel.Guild.GetChannel(guild.BoardChannel)
                    .GetMessageAsync(Messages.BoardMessages.Find(x => x.SourceId == msg.Id).BoardId);

                //Loop through all reactions on the message
                var reactions = new List<string>();
                foreach (var reaction in msg.Reactions)
                {
                    if (reaction.Count < guild.ReactionNeeded) continue;
                    try
                    {
                        reactions.Add(reaction.Emoji.IsAnimated
                            ? $"{DiscordEmoji.FromGuildEmote(Bot.Client, reaction.Emoji.Id)} x {reaction.Count}"
                            : $"{DiscordEmoji.FromName(Bot.Client, reaction.Emoji.GetDiscordName())} x {reaction.Count}");
                    }
                    catch (Exception)
                    {
                        reactions.Add($"{reaction.Emoji.GetDiscordName()} x {reaction.Count}");
                    }
                }

                await bmsg.ModifyAsync(string.Join(" • ", reactions));
            }
        }

        private static DiscordEmbed BuildEmbed(DiscordMessage msg)
        {
            var builder = new DiscordEmbedBuilder().AddField("Author", msg.Author.Mention, true)
                .AddField("Channel", msg.Channel.Mention, true)
                .WithThumbnail(msg.Author.AvatarUrl)
                .WithTimestamp(msg.Timestamp)
                .WithColor(new DiscordColor("420DAB"));
            //This is needed so that the bot doesn't shit itself if the message was just a picture
            if (msg.Content != "")
            {
                builder.AddField("Message", msg.Content);
            }

            builder.AddField("Link",
                $"[Jump to](https://discordapp.com/channels/{msg.Channel.Guild.Id}/{msg.ChannelId}/{msg.Id})");
            //If there were any attachments, put it into the image field of the embed (apparently it doesn't throw if it's an mp3/4 and I'm okay with that)
            if (msg.Attachments.Any())
            {
                builder.WithImageUrl(msg.Attachments.First().Url);
            }

            return builder.Build();
        }

        public static async Task CommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            if (e.Exception.Message.Contains("command was not found"))
            {
                if (e.Context.Message.Content.StartsWith("$wie"))
                    return;
                await e.Context.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("❓"));
                return;
            }

            var eb = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Red)
                .WithAuthor("Command Execution failed!",
                    iconUrl: "https://www.shareicon.net/data/128x128/2016/08/18/810028_close_512x512.png")
                .WithDescription(e.Exception.Message);

            if (e.Exception.Message.Contains("pre-execution checks failed"))
            {
                var checks = new List<string>();
                foreach (var check in e.Command.ExecutionChecks)
                {
                    if (!await check.ExecuteCheckAsync(e.Context, false))
                    {
                        checks.Add(check.ToString()
                            .Substring(check.ToString().LastIndexOf(".", StringComparison.Ordinal) + 1)
                            .Replace("Attribute", string.Empty));
                    }
                }

                eb.AddField("Failed Pre-Execution checks:", string.Join(", ", checks));
            }

            if (e.Exception.InnerException != null)
            {
                eb.AddField("Inner Exception:", e.Exception.InnerException.Message);
            }

            await e.Context.RespondAsync(embed: eb.Build());
        }

        public static async Task MessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            //Bots don't count
            if (e.Message.Author.IsBot) return;
            //Check if guild has the "feature" enabled
            if (Guilds.GetGuild(e.Guild).PrevMessageAmount == 0) return;
            //Put current guild the message was received in into variable
            var guild = Guilds.GetGuild(e.Guild);

            //Add channel if it isn't already in the dictionary
            if (!guild.PrevMessages.ContainsKey(e.Channel.Id))
            {
                guild.PrevMessages.TryAdd(e.Channel.Id, new KeyValuePair<DiscordMessage, int>(e.Message, 0));
            }

            var (_, value) = guild.PrevMessages[e.Channel.Id];

            if (guild.PrevMessages[e.Channel.Id].Key == null)
            {
                guild.PrevMessages.TryUpdate(e.Channel.Id, new KeyValuePair<DiscordMessage, int>(e.Message, 1),
                    guild.PrevMessages[e.Channel.Id]);
                return;
            }

            //Check for the same content
            if (guild.PrevMessages[e.Channel.Id].Key.Content == e.Message.Content &&
                guild.PrevMessages[e.Channel.Id].Key.Author != e.Message.Author)
            {
                //Update the Count
                guild.PrevMessages.TryUpdate(e.Channel.Id, new KeyValuePair<DiscordMessage, int>(e.Message, value + 1),
                    guild.PrevMessages[e.Channel.Id]);
            }
            else
            {
                //Set count to 0 and update last message
                guild.PrevMessages.TryUpdate(e.Channel.Id, new KeyValuePair<DiscordMessage, int>(e.Message, 1),
                    guild.PrevMessages[e.Channel.Id]);
            }

            if (guild.PrevMessages[e.Channel.Id].Value >= guild.PrevMessageAmount)
            {
                //Send message and reset count/last message
                await e.Channel.SendMessageAsync(e.Message.Content);
                guild.PrevMessages.TryUpdate(e.Channel.Id, new KeyValuePair<DiscordMessage, int>(null, 1),
                    guild.PrevMessages[e.Channel.Id]);
            }
        }
    }
}