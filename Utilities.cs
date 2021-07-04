using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Meiyounaise.DB;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using YouTubeSearch;

namespace Meiyounaise
{
    internal static class Utilities
    {
        internal static readonly string DataPath = $"DB{Path.DirectorySeparatorChar}";
        internal static SqliteConnection Con = new SqliteConnection($"Data Source = {DataPath}Database.db");

        public static string GetKey(string name)
        {
            if (!File.Exists($"{DataPath}Database.db"))
            {
                Console.WriteLine("oh gott oh fick die datenbank ist weg");
                Environment.Exit(0);
            }
            
            var result = "";
            Con.Open();
            using (var cmd = new SqliteCommand($"SELECT token FROM Tokens WHERE service = '{name}'", Con))
            {
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        result = Convert.ToString(rdr["token"]);
                    }
                    Con.Close();
                    return result;
                }
            }
        }

        public static string ResolveName(string service, DiscordUser user)
        {
            var rUser = Users.GetUser(user);
            if (rUser == null) throw new Exception("Username could not be resolved");
            string result;
            switch (service)
            {
                case "osu":
                    result = rUser.Osu;
                    break;
                case "last":
                    result= rUser.Last;
                    break;
                default:
                    throw new Exception("Unknown service provided!");
            }

            if (result == "#" || string.IsNullOrWhiteSpace(result))
            {
                throw new Exception("No username set for the requested service!");
            }
            return result;
        }

        public static async Task<string> SearchYoutube(string query)
        {
            var yt = new VideoSearch();
            var videos = await yt.GetVideos(query, 1);
            return videos.First().getUrl();
        } 
        
        public static async Task<string> ToHastebin(string content)
        {
            using (var client = new HttpClient())
            {
                var response = await client.PostAsync("https://haste.timostestdoma.in/documents",
                    new StringContent(content, Encoding.UTF8));
                var rs = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<dynamic>(rs);
                return $"https://haste.timostestdoma.in/{data?.key}";
            }
        }
        
        public static async Task<string> CheckInput(string input, CommandContext ctx)
        {
            if (!string.IsNullOrEmpty(input)) return input;
            var messages = await ctx.Channel.GetMessagesAsync(2);
            return messages.Last().Content;
        }
        
        public static async Task DownloadAsync(Uri requestUri, string filename)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var handler = new HttpClientHandler();
            using (var httpClient = new HttpClient(handler, false))
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, requestUri))
                {
                    using (
                        Stream contentStream = await (await httpClient.SendAsync(request)).Content.ReadAsStreamAsync(),
                        stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None, 4096,
                            true))
                    {
                        await contentStream.CopyToAsync(stream);
                    }
                }
            }
        }
    }
}