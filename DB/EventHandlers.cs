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
            if (e.Client is DiscordClient c)
                await c.UpdateStatusAsync(new DiscordGame("Booting Up🌚🌚🌚🌚🌚🌚🌚🌚🌚🌚🌚🌚🌚🌚🌚🌚🌚🌚🌚🌚🌚🌚🌚"));
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
                        $"INSERT INTO Guilds (id,prefix,boardChannel,joinMsg,leaveMsg,jlMsgChannel) VALUES ('{guild.Id}','&','0','empty','empty', '0')",
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
                    $"INSERT INTO Guilds (id,prefix,boardChannel,joinMsg,leaveMsg, jlMsgChannel) VALUES ('{args.Guild.Id}','&','0','empty', 'empty', '0')",
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
                    $"SELECT text FROM Status ORDER BY RANDOM() LIMIT 1", Utilities.Con))
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
    }
}