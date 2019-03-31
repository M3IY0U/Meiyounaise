using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace Meiyounaise
{
    internal static class Utilities
    {
        internal static readonly string DataPath = $"DB{Path.DirectorySeparatorChar}";

        public static string GetKey(string name)
        {
            var result ="";
            using (var con =new SqliteConnection($"Data Source = {DataPath}Database.db"))
            {
                con.Open();
                using (var cmd = new SqliteCommand($"SELECT token FROM Tokens WHERE service = '{name}'",con))
                {
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            result = Convert.ToString(rdr["token"]);
                        }
                        con.Close();
                        return result;
                    }
                }
            }
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
                        stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                    {
                        await contentStream.CopyToAsync(stream);
                    }
                }
            }
        }
    }
}