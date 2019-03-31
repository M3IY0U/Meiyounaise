using System;
using System.Collections.Generic;
using System.Linq;
using DSharpPlus.Entities;
using Microsoft.Data.Sqlite;

namespace Meiyounaise.DB
{
    public class Users
    {
        private static List<User> _users;

        static Users()
        {
            _users = new List<User>();
            Utilities.Con.Open();
            using (var cmd = new SqliteCommand("SELECT * FROM Users", Utilities.Con))
            {
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        _users.Add(new User
                        {
                            Id = Convert.ToUInt64(rdr.GetValue(rdr.GetOrdinal("id"))),
                            Last = rdr.GetString(rdr.GetOrdinal("lastfm"))
                        });
                    }
                }
            }
            Utilities.Con.Close();
        }

        public static User GetGuild(DiscordUser user)
        {
            var result = from a in _users
                where a.Id == user.Id
                select a;
            return result.FirstOrDefault();
        }
        public class User
        {
            public string Last { get; set; }
            public ulong Id { get; set; }
        }
    }
    
    
}