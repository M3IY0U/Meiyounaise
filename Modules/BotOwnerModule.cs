using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Meiyounaise.DB;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
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
            await ctx.RespondAsync("Hang on, this could take a while");
            var sw = new Stopwatch();
            sw.Start();
            var result = new List<string>();
            foreach (var guild in ctx.Client.Guilds)
            {
                var cguild = guild.Value.Name + " - ";
                try
                {
                    var invs = await guild.Value.GetInvitesAsync();
                    if (invs.Count != 0)
                    {
                        cguild += invs.First();
                    }
                }
                catch (Exception)
                {
                    var createdInvite = false;
                    foreach (var chn in guild.Value.Channels)
                    {
                        try
                        {
                            cguild += await chn.Value.CreateInviteAsync();
                            createdInvite = true;
                            break;
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                    }

                    if (!createdInvite)
                    {
                        cguild += "Couldn't create Invite!";
                    }
                }

                result.Add(cguild);
            }

            sw.Stop();

            var resultstring = string.Join("\n", result);
            if (resultstring.Length < 2048)
            {
                var embed = new DiscordEmbedBuilder()
                    .WithDescription(resultstring);
                await ctx.RespondAsync($"{ctx.User.Mention}: Took {Math.Round(sw.Elapsed.TotalSeconds)}s",
                    embed: embed.Build());
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
            if (gResponse.Result.Content == "abort")
            {
                await g.DeleteAsync();
                await gResponse.Result.DeleteAsync();
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":x:"));
                return;
            }

            var guildToLeave = guilds[Convert.ToInt32(gResponse.Result.Content)];
            var rmsg = await ctx.RespondAsync($"This will make the bot leave guild {guildToLeave.Name}. Are you sure?");
            await rmsg.CreateReactionAsync(emojis[0]);
            await rmsg.CreateReactionAsync(emojis[1]);

            //var t = await interactivity.WaitForMessageReactionAsync(rmsg, ctx.User);
            var t = await interactivity.WaitForReactionAsync(rmsg, ctx.User);
            if (t.Result.Emoji.GetDiscordName() == ":x:")
            {
                await g.DeleteAsync();
                await gResponse.Result.DeleteAsync();
                await rmsg.DeleteAsync();
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":x:"));
            }
            else if (t.Result.Emoji.GetDiscordName() == ":white_check_mark:")
            {
                await g.DeleteAsync();
                await gResponse.Result.DeleteAsync();
                await rmsg.DeleteAsync();
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));

                Utilities.Con.Open();
                using (var cmd = new SqliteCommand("DELETE FROM Guilds WHERE Guilds.id=@id",
                    Utilities.Con))
                {
                    cmd.Parameters.AddWithValue("@id", guildToLeave.Id);
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

        [Command("updateuser"), Aliases("uu"), RequireOwner, Hidden]
        public async Task UpdateUser(CommandContext ctx, DiscordUser user = null)
        {
            if (user == null)
            {
                await Users.UpdateAllUsers();
            }
            else
            {
                Users.UpdateUser(user);
            }

            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
        }

        [Command("messages"), RequireOwner, Hidden]
        public async Task FetchMessages(CommandContext ctx, DiscordChannel channel, int amount = 50)
        {
            var messages = await channel.GetMessagesAsync(amount);
            var sb = new StringBuilder();
            foreach (var message in messages)
            {
                sb.AppendLine($"[{message.Timestamp:g}] {message.Author.Username}: {message.Content}");
                if (message.Attachments.Count <= 0) continue;
                sb.AppendLine("Attachments:");
                foreach (var attachment in message.Attachments)
                {
                    sb.AppendLine("\t" + attachment.Url);
                }
            }

            if (sb.Length > 2000)
            {
                File.WriteAllText("messages.txt", sb.ToString());
                await ctx.RespondWithFileAsync("messages.txt");
                File.Delete("messages.txt");
                return;
            }

            await ctx.RespondAsync(sb.ToString());
        }

        [Command("execute"), Aliases("exec", "run"), RequireOwner, Hidden]
        public async Task Eval(CommandContext ctx, [RemainingText] string input)
        {
            var globals = new Globals
            {
                Client = Bot.Client, Ctx = ctx
            };
            try
            {
                await CSharpScript.EvaluateAsync(input.Trim('`'),
                    ScriptOptions.Default.WithReferences(typeof(System.Net.Dns).Assembly), globals);
            }
            catch (Exception)
            {
                GC.WaitForPendingFinalizers();
                GC.Collect();
                throw;
            }

            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        [Command("update"), RequireOwner, Hidden]
        public async Task Update(CommandContext ctx)
        {
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("👋"));
            var output = "Output:\n```";
            using (var exeProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "git",
                Arguments ="pull",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }))
            {
                exeProcess?.WaitForExit();
                output += exeProcess?.StandardOutput.ReadToEnd();
                output += exeProcess?.StandardError.ReadToEnd().Length != 0 ? $"\n```\nError:\n``` {exeProcess?.StandardError.ReadToEnd()}" : "";
            }

            await ctx.RespondAsync(output + "\n```");
            await Task.Delay(5000);
            Process.Start("dotnet", "run MeiyounaiseRewrite.sln");
            Environment.Exit(0);
        }

        public class Globals
        {
            public CommandContext Ctx;
            public DiscordClient Client;
        }
    }
}