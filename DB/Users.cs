using System;
using System.Collections.Generic;
using System.Linq;
using DSharpPlus.Entities;
using Microsoft.Data.Sqlite;

namespace Meiyounaise.DB
{
    public class Users
    {
        public static List<User> UserList;

        static Users()
        {
            UserList = new List<User>();
            Utilities.Con.Open();
            using (var cmd = new SqliteCommand("SELECT * FROM Users", Utilities.Con))
            {
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        UserList.Add(new User
                        {
                            Id = Convert.ToUInt64(rdr.GetValue(rdr.GetOrdinal("id"))),
                            Last = rdr.GetString(rdr.GetOrdinal("lastfm"))
                        });
                    }
                }
            }
            Utilities.Con.Close();
        }

        
        public static void UpdateUser(DiscordUser user)
        {
            Utilities.Con.Open();
            using (var cmd = new SqliteCommand($"SELECT * FROM Users WHERE Users.id = {user.Id}", Utilities.Con))
            {
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        var newGuild = new User
                        {
                            Id = Convert.ToUInt64(rdr.GetValue(rdr.GetOrdinal("id"))),
                            Last = rdr.GetString(rdr.GetOrdinal("lastfm"))
                        };
                        UserList.Remove(GetUser(user));
                        UserList.Add(newGuild);
                    }
                }
            }
            Utilities.Con.Close();
        }
        
        
        public static User GetUser(DiscordUser user)
        {
            var result = from a in UserList
                where a.Id == user.Id
                select a;
            return result.FirstOrDefault();
        }
        public class User
        {
            public User(){}
            public User(ulong id)
            {
                Id = id;
                Last = null;
            }
            public string Last { get; set; }
            public ulong Id { get; set; }
        }
    }
    
    
}