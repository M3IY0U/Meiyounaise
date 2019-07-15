using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.EventArgs;

namespace Meiyounaise.DB
{
    public static class Bridge
    {
        public static async Task Message(MessageCreateEventArgs e)
        {
            var g = Guilds.GetGuild(e.Guild);
            if (g.BridgeChannel==0 || g.BridgeChannel != e.Channel.Id || e.Message.Author.IsCurrent)
                return;
            foreach (var guild in Guilds.GuildList)
            {
                if (guild.BridgeChannel == 0 || e.Guild.Id == guild.Id)
                    continue;
                var c = await e.Client.GetChannelAsync(guild.BridgeChannel);
                var header = $"**{e.Author.Username}** in `{e.Guild.Name}`: ";
                if (e.Message.Attachments.Count == 0)
                {
                    await c.SendMessageAsync(header + e.Message.Content);
                }
                else
                {
                    var content = e.Message.Content == ""
                        ? $"No Content.\n Attached file: {e.Message.Attachments.First().Url}"
                        : $"{e.Message.Content}\nAttached file: {e.Message.Attachments.First().Url}";
                    await c.SendMessageAsync(header + content);
                }
            }
        }
    }
}