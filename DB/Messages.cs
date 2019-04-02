using System;
using System.Collections.Generic;
using DSharpPlus.Entities;
using Microsoft.Data.Sqlite;

namespace Meiyounaise.DB
{
    public class Messages
    {
        public static Dictionary<ulong, bool> BoardMessages;

        static Messages()
        {
            BoardMessages = new Dictionary<ulong, bool>();
            Utilities.Con.Open();
            using (var cmd = new SqliteCommand("SELECT * FROM Messages", Utilities.Con))
            {
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        BoardMessages.Add(Convert.ToUInt64(rdr.GetValue(rdr.GetOrdinal("id"))),Convert.ToBoolean(rdr.GetValue(rdr.GetOrdinal("sent"))));
                    }
                }
            }
            Utilities.Con.Close();
        }
    }
}