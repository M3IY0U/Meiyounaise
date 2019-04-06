using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;

namespace Meiyounaise.Modules
{
    public class MessageModule : BaseCommandModule
    {
        [Command("say"), RequireOwner, Hidden]
        public async Task Say(CommandContext ctx, [RemainingText] string text)
        {
            var interactivity = ctx.Client.GetInteractivity();
            var guilds = new Dictionary<int, DiscordGuild>();
            var i = 1;
            foreach (var guild in ctx.Client.Guilds)
            {
                guilds.TryAdd(i, guild.Value);
                i++;
            }

            var g = await ctx.RespondAsync($"Found Guilds\n{string.Join("\n", guilds)}\nChoose one via the number!");
            var gResponse = await interactivity.WaitForMessageAsync(x => x.Author == ctx.User);
            if (gResponse == null)
            {
                await ctx.RespondAsync("I didn't get your choice!");
                return;
            }

            await g.DeleteAsync();
            await gResponse.Message.DeleteAsync();
            var target = guilds[Convert.ToInt32(gResponse.Message.Content)];
            var channels = new Dictionary<int, DiscordChannel>();
            var j = 1;
            foreach (var channel in target.Channels)
            {
                channels.TryAdd(j, channel.Value);
                j++;
            }

            var c = await ctx.RespondAsync(
                $"Listing text channels in guild {target.Name}\n{string.Join("\n", channels)}\nChoose one via the number!");
            var cResponse = await interactivity.WaitForMessageAsync(x => x.Author == ctx.User);
            var targetChannel = channels[Convert.ToInt32(cResponse.Message.Content)];
            await targetChannel.SendMessageAsync(text);
            await c.DeleteAsync();
            await cResponse.Message.DeleteAsync();
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
        }

        [Command("purge"), Aliases("prune"), RequireUserPermissions(Permissions.ManageMessages), Description("Bulk delete messages")]
        public async Task Purge(CommandContext ctx, int amount, [Description("If provided will only delete messages by mentioned user")]string user = "")
        {
            if (user != "" && ctx.Message.MentionedUsers.Count==0)
            {
                await ctx.RespondAsync("Please @mention the user whose messages you want to purge.");
                return;
            }
            await ctx.Message.DeleteAsync();
            IReadOnlyList<DiscordMessage> messagesToDelete;
            if (user=="")
            {
                messagesToDelete = await ctx.Channel.GetMessagesAsync(amount);
                await ctx.Channel.DeleteMessagesAsync(messagesToDelete);
                var toDel = await ctx.RespondAsync($"Deleted {messagesToDelete.Count} messages!");
                await Task.Delay(2500);
                await toDel.DeleteAsync();
            }else
            {
                
                var userToDelete = await ctx.Client.GetUserAsync(ctx.Message.MentionedUsers.First().Id);
                messagesToDelete = await ctx.Channel.GetMessagesAsync(amount);
                var filteredMessages = messagesToDelete.ToList();
                filteredMessages.RemoveAll(x => x.Author.Id != userToDelete.Id);
                await ctx.Channel.DeleteMessagesAsync(filteredMessages);
                var toDel = await ctx.RespondAsync($"Deleted {filteredMessages.Count} messages!");
                await Task.Delay(2500);
                await toDel.DeleteAsync();
            }
        }
    }
}