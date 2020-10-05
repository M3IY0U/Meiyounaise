using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using DSharpPlus.Entities;
using Microsoft.Data.Sqlite;

namespace Meiyounaise.DB
{
    public class Guilds
    {
        internal static List<Guild> GuildList;

        static Guilds()
        {
            GuildList = new List<Guild>();
            Utilities.Con.Open();
            using (var cmd = new SqliteCommand("SELECT * FROM Guilds", Utilities.Con))
            {
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        GuildList.Add(new Guild
                        {
                            Id = Convert.ToUInt64(rdr.GetValue(rdr.GetOrdinal("id"))),
                            BoardChannel = Convert.ToUInt64(rdr.GetValue(rdr.GetOrdinal("boardChannel"))),
                            Prefix = rdr.GetString(rdr.GetOrdinal("prefix")),
                            JoinMsg = rdr.GetString(rdr.GetOrdinal("joinMsg")),
                            LeaveMsg = rdr.GetString(rdr.GetOrdinal("leaveMsg")),
                            JlMessageChannel = Convert.ToUInt64(rdr.GetValue(rdr.GetOrdinal("jlMsgChannel"))),
                            ReactionNeeded = rdr.GetInt32(rdr.GetOrdinal("reactionNeeded")),
                            PrevMessageAmount = Convert.ToInt32(rdr.GetValue(rdr.GetOrdinal("prevMessageAmount"))),
                            PrevMessages = new ConcurrentDictionary<ulong, KeyValuePair<DiscordMessage, int>>(),
                            FmLog = new ConcurrentDictionary<ulong, string>(),
                            BridgeChannel = Convert.ToUInt64(rdr.GetValue(rdr.GetOrdinal("bridgeChannel")))
                        });
                    }
                }
            }

            Utilities.Con.Close();
        }

        public static void UpdateGuild(DiscordGuild guild)
        {
            Utilities.Con.Open();
            using (var cmd = new SqliteCommand("SELECT * FROM Guilds WHERE Guilds.id = @id", Utilities.Con))
            {
                cmd.Parameters.AddWithValue("@id", guild.Id);
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        var newGuild = new Guild
                        {
                            Id = Convert.ToUInt64(rdr.GetValue(rdr.GetOrdinal("id"))),
                            BoardChannel = Convert.ToUInt64(rdr.GetValue(rdr.GetOrdinal("boardChannel"))),
                            Prefix = rdr.GetString(rdr.GetOrdinal("prefix")),
                            JoinMsg = rdr.GetString(rdr.GetOrdinal("joinMsg")),
                            LeaveMsg = rdr.GetString(rdr.GetOrdinal("leaveMsg")),
                            JlMessageChannel = Convert.ToUInt64(rdr.GetValue(rdr.GetOrdinal("jlMsgChannel"))),
                            ReactionNeeded = rdr.GetInt32(rdr.GetOrdinal("reactionNeeded")),
                            PrevMessageAmount = Convert.ToInt32(rdr.GetValue(rdr.GetOrdinal("prevMessageAmount"))),
                            PrevMessages = new ConcurrentDictionary<ulong, KeyValuePair<DiscordMessage, int>>(),
                            FmLog = new ConcurrentDictionary<ulong, string>(),
                            BridgeChannel = Convert.ToUInt64(rdr.GetValue(rdr.GetOrdinal("bridgeChannel")))
                        };
                        GuildList.Remove(GetGuild(guild));
                        GuildList.Add(newGuild);
                    }
                }
            }

            Utilities.Con.Close();
        }

        public static Guild GetGuild(DiscordGuild guild)
        {
            var result = from a in GuildList
                where a.Id == guild.Id
                select a;
            var enumerable = result.ToList();
            return enumerable.ToList().Any() ? enumerable.ToList().First() : null;
        }
        
        public class Guild
        {
            public Guild()
            {
            }

            public Guild(ulong id)
            {
                Id = id;
                Prefix = "&";
                BoardChannel = 0;
                JoinMsg = "empty";
                LeaveMsg = "empty";
                JlMessageChannel = 0;
                ReactionNeeded = 0;
                PrevMessageAmount = 0;
                PrevMessages = new ConcurrentDictionary<ulong, KeyValuePair<DiscordMessage, int>>();
                FmLog = new ConcurrentDictionary<ulong, string>();
                BridgeChannel = 0;
            }
            
            public void UpdateSongInChannel(ulong channel, string name)
            {
                if (!FmLog.ContainsKey(channel))
                    FmLog.TryAdd(channel, name);
                else
                    FmLog.TryUpdate(channel, name, FmLog[channel]);
            }

            public int PrevMessageAmount { get; set; }
            public ConcurrentDictionary<ulong, KeyValuePair<DiscordMessage, int>> PrevMessages { get; set; }
            public ConcurrentDictionary<ulong, string> FmLog { get; set; }
            public string Prefix { get; set; }
            public ulong Id { get; set; }
            public ulong BoardChannel { get; set; }
            public string JoinMsg { get; set; }
            public string LeaveMsg { get; set; }
            public ulong JlMessageChannel { get; set; }
            public int ReactionNeeded { get; set; }
            public ulong BridgeChannel { get; set; }
        }
    }
}