using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace Meiyounaise.DB
{
    public class Messages
    {
        public static List<Message> BoardMessages;

        static Messages()
        {
            BoardMessages = new List<Message>();
            Utilities.Con.Open();
            using (var cmd = new SqliteCommand("SELECT * FROM Messages", Utilities.Con))
            {
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        BoardMessages.Add(new Message
                        {
                            SourceId = Convert.ToUInt64(rdr.GetValue(rdr.GetOrdinal("id"))),
                            BoardId = Convert.ToUInt64(rdr.GetValue(rdr.GetOrdinal("bId"))),
                            Sent = Convert.ToBoolean(rdr.GetValue(rdr.GetOrdinal("sent")))
                        });
                    }
                }
            }
            Utilities.Con.Close();
        }

        public class Message
        {
            public ulong SourceId { get; set; }
            public ulong BoardId { get; set; }
            public bool Sent { get; set; }
        }
    }
}