using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Meiyounaise.DB;
using Microsoft.Data.Sqlite;

namespace Meiyounaise.Modules
{
    public class BotOwnerModule : BaseCommandModule
    {
        [Command("icon"), RequireOwner, Hidden]
        public async Task Icon(CommandContext ctx, string url = null)
        {
            var path = $"{Utilities.DataPath}icon.png";
            await Utilities.DownloadAsync(new Uri(url ?? ctx.Message.Attachments.First().Url), path);
            await ctx.Client.UpdateCurrentUserAsync(null, new FileStream(path, FileMode.Open));
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
            File.Delete(path);
        }

        [Command("servers"), RequireOwner, Hidden]
        public async Task Servers(CommandContext ctx)
        {
            var result = new List<string>();
            foreach (var guild in ctx.Client.Guilds)
            {
                var cguild = guild.Value.Name + " - ";
                try
                {
                    var invs = await guild.Value.GetInvitesAsync();
                    if (invs.Count == 0)
                    {
                        foreach (var chn in guild.Value.Channels)
                        {
                            try
                            {
                                cguild += await chn.Value.CreateInviteAsync();
                                break;
                            }
                            catch (Exception)
                            {
                                cguild += "Couldn't create Invite!";
                                break;
                            }
                        }
                    }
                    else
                    {
                        cguild += invs.First();
                    }
                }
                catch (Exception)
                {
                    cguild += "Couldn't create Invite!";
                }

                result.Add(cguild);
            }

            var resultstring = string.Join("\n", result);
            if (resultstring.Length < 2048)
            {
                var embed = new DiscordEmbedBuilder()
                    .WithDescription(resultstring);
                await ctx.RespondAsync(embed: embed.Build());
            }
            else
            {
                var rts = resultstring;
                while (rts.Length >= 2000)
                {
                    await ctx.RespondAsync(rts.Substring(0, 2000));
                    rts = rts.Substring(2000);
                }

                await ctx.RespondAsync(rts);
            }
        }

        [Command("leaveguild"), RequireOwner, Hidden]
        public async Task Leave(CommandContext ctx)
        {
            var interactivity = ctx.Client.GetInteractivity();
            var guilds = new Dictionary<int, DiscordGuild>();
            var i = 1;
            foreach (var guild in ctx.Client.Guilds)
            {
                guilds.TryAdd(i, guild.Value);
                i++;
            }

            var emojis = new List<DiscordEmoji>
                {DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"), DiscordEmoji.FromName(ctx.Client, ":x:")};

            var g = await ctx.RespondAsync($"Found Guilds\n{string.Join("\n", guilds)}\nChoose one via the number!");
            var gResponse = await interactivity.WaitForMessageAsync(x => x.Author == ctx.User);
            if (gResponse.Message.Content == "abort")
            {
                await g.DeleteAsync();
                await gResponse.Message.DeleteAsync();
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":x:"));
                return;
            }

            var guildToLeave = guilds[Convert.ToInt32(gResponse.Message.Content)];
            var rmsg = await ctx.RespondAsync($"This will make the bot leave guild {guildToLeave.Name}. Are you sure?");
            await rmsg.CreateReactionAsync(emojis[0]);
            await rmsg.CreateReactionAsync(emojis[1]);
            var t = await interactivity.WaitForMessageReactionAsync(rmsg, ctx.User);
            if (t.Emoji.GetDiscordName() == ":x:")
            {
                await g.DeleteAsync();
                await gResponse.Message.DeleteAsync();
                await rmsg.DeleteAsync();
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":x:"));
            }
            else if (t.Emoji.GetDiscordName() == ":white_check_mark:")
            {
                await g.DeleteAsync();
                await gResponse.Message.DeleteAsync();
                await rmsg.DeleteAsync();
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));

                Utilities.Con.Open();
                using (var cmd = new SqliteCommand($"DELETE FROM Guilds WHERE Guilds.id='{guildToLeave.Id}'",
                    Utilities.Con))
                {
                    cmd.ExecuteReader();
                }

                Utilities.Con.Close();
                try
                {
                    Guilds.GuildList.Remove(Guilds.GuildList.Find(x => x.Id == guildToLeave.Id));
                }
                catch (Exception)
                {
                    // ignored
                }

                await guildToLeave.LeaveAsync();
            }
        }

        [Command("sql"), RequireOwner, Hidden]
        public async Task ExecuteSql(CommandContext ctx, [RemainingText] string sql)
        {
            Utilities.Con.Open();
            if (sql.ToLower().Contains("select"))
            {
                var names = new List<string>();
                var rows = new List<string>();
                try
                {
                    using (var cmd = new SqliteCommand(sql, Utilities.Con))
                    {
                        using (var rdr = cmd.ExecuteReader())
                        {
                            for (var i = 0; i < rdr.FieldCount; i++)
                            {
                                names.Add($"**{rdr.GetName(i)}**");
                            }

                            while (rdr.Read())
                            {
                                var toAdd = new List<string>();
                                for (var i = 0; i < rdr.FieldCount; i++)
                                {
                                    toAdd.Add(Convert.ToString(rdr.GetValue(i)));
                                }

                                rows.Add(string.Join(" - ", toAdd));
                            }
                        }

                        await ctx.RespondAsync($"{string.Join(" - ", names)}\n{string.Join("\n", rows)}");
                    }
                }
                catch (Exception e)
                {
                    await ctx.RespondAsync(e.Message);
                    Utilities.Con.Close();
                    return;
                }
            }
            else
            {
                try
                {
                    using (var cmd = new SqliteCommand(sql, Utilities.Con))
                    {
                        cmd.ExecuteReader();
                    }
                    await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
                }
                catch (Exception e)
                {
                    await ctx.RespondAsync(e.Message);
                    Utilities.Con.Close();
                    return;
                }
            }
            Utilities.Con.Close();
        }
    }
}