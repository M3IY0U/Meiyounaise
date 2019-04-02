using System;
using System.Collections.Generic;
using System.Linq;
using DSharpPlus.Entities;
using Microsoft.Data.Sqlite;

namespace Meiyounaise.DB
{
    public class Guilds
    {
        private static List<Guild> _guilds;

        static Guilds()
        {
            _guilds = new List<Guild>();
            Utilities.Con.Open();
            using (var cmd = new SqliteCommand("SELECT * FROM Guilds", Utilities.Con))
            {
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        _guilds.Add(new Guild
                        {
                            Id = Convert.ToUInt64(rdr.GetValue(rdr.GetOrdinal("id"))),
                            BoardChannel = Convert.ToUInt64(rdr.GetValue(rdr.GetOrdinal("boardChannel"))),
                            Prefix = rdr.GetString(rdr.GetOrdinal("prefix")),
                            JoinMsg = rdr.GetString(rdr.GetOrdinal("joinMsg")),
                            LeaveMsg = rdr.GetString(rdr.GetOrdinal("leaveMsg")),
                            JlMessageChannel = Convert.ToUInt64(rdr.GetValue(rdr.GetOrdinal("jlMsgChannel"))),
                            ReactionNeeded = rdr.GetInt32(rdr.GetOrdinal("reactionNeeded"))
                        });
                        
                    }
                }
            }
            Utilities.Con.Close();
        }

        public static void UpdateGuild(DiscordGuild guild)
        {
            Utilities.Con.Open();
            using (var cmd = new SqliteCommand($"SELECT * FROM Guilds WHERE Guilds.id = {guild.Id}", Utilities.Con))
            {
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
                            ReactionNeeded = rdr.GetInt32(rdr.GetOrdinal("reactionNeeded"))
                        };
                        _guilds.Remove(GetGuild(guild));
                        _guilds.Add(newGuild);
                    }
                }
            }
            Utilities.Con.Close();
        }

        public static Guild GetGuild(DiscordGuild guild)
        {
            var result = from a in _guilds
                where a.Id == guild.Id
                select a;
            return result.FirstOrDefault();
        }
        public class Guild
        {
            public string Prefix { get; set; }
            public ulong Id { get; set; }
            public ulong BoardChannel { get; set; }
            public string JoinMsg { get; set; }
            public string LeaveMsg { get; set; }
            public ulong JlMessageChannel { get; set; }
            public int ReactionNeeded { get; set; }
        }
    }
    
    
}