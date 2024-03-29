using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;

namespace Meiyounaise
{
    internal class Bot : IDisposable
    {
        public static DiscordClient Client;
        public static CommandsNextExtension Cnext;
        private CancellationTokenSource _cts;

        public Bot()
        {
            Client = new DiscordClient(new DiscordConfiguration()
            {
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Information,
                Token = Utilities.GetKey("bottoken"),
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All
            });

            Client.UseInteractivity(new InteractivityConfiguration());
            _cts = new CancellationTokenSource();

            Cnext = Client.UseCommandsNext(new CommandsNextConfiguration
            {
                CaseSensitive = false,
                EnableDefaultHelp = true,
                EnableDms = false,
                EnableMentionPrefix = true,
                PrefixResolver = CustomPrefixPredicate
            });

            Cnext.RegisterCommands(Assembly.GetEntryAssembly());
            Cnext.CommandErrored += DB.EventHandlers.CommandErrored;

            Client.Ready += DB.EventHandlers.Ready;
            Client.GuildCreated += DB.EventHandlers.GuildCreated;
            Client.GuildMemberAdded += DB.EventHandlers.UserJoined;
            Client.MessageCreated += DB.EventHandlers.MessageCreated;
            Client.GuildMemberRemoved += DB.EventHandlers.UserRemoved;
            Client.MessageReactionAdded += DB.EventHandlers.ReactionAdded;
            Client.MessageReactionRemoved += DB.EventHandlers.ReactionRemoved;
        }

        private static Task<int> CustomPrefixPredicate(DiscordMessage msg)
        {
            var guild = DB.Guilds.GetGuild(msg.Channel.Guild);
            return msg.Content.StartsWith(guild.Prefix) ? Task.FromResult(guild.Prefix.Length) : Task.FromResult(-1);
        }

        public async Task RunAsync()
        {
            await Client.ConnectAsync();
            await WaitForCancellationAsync();
        }

        private async Task WaitForCancellationAsync()
        {
            while (!_cts.IsCancellationRequested)
                await Task.Delay(500);
        }

        public void Dispose()
        {
            Client.Dispose();
            Cnext = null;
        }
    }
}