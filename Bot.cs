using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;

namespace Meiyounaise
{
    internal class Bot : IDisposable
    {
        public static DiscordClient Client;
        private CommandsNextExtension _cnext;
        public  static InteractivityExtension Interactivity;
        private CancellationTokenSource _cts;

        public Bot()
        {
            Client = new DiscordClient(new DiscordConfiguration()
            {
                AutoReconnect = true,
                LogLevel = LogLevel.Info,
                Token = Utilities.GetKey("bottoken"),
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true
            });

            Interactivity= Client.UseInteractivity(new InteractivityConfiguration());
            
            _cts = new CancellationTokenSource();
            
            _cnext = Client.UseCommandsNext(new CommandsNextConfiguration
            {
                CaseSensitive = false,
                EnableDefaultHelp = true,
                EnableDms = false,
                EnableMentionPrefix = true,
                PrefixResolver= CustomPrefixPredicate
            });
           
            
            _cnext.RegisterCommands(Assembly.GetEntryAssembly());
            _cnext.CommandErrored += DB.EventHandlers.CommandErrored;
            
            Client.Ready += DB.EventHandlers.Ready;
            Client.GuildCreated += DB.EventHandlers.GuildCreated;
            Client.GuildMemberAdded += DB.EventHandlers.UserJoined;
            Client.GuildMemberRemoved += DB.EventHandlers.UserRemoved;
            Client.MessageReactionAdded += DB.EventHandlers.ReactionAdded;
            Client.MessageReactionRemoved += DB.EventHandlers.ReactionRemoved;
        }

        private Task<int> CustomPrefixPredicate(DiscordMessage msg)
        {
            var guild = DB.Guilds.GetGuild(msg.Channel.Guild);
            if (msg.Content.StartsWith(guild.Prefix))
            {
                return Task.FromResult(guild.Prefix.Length);
            }
            return Task.FromResult(-1);
        }

        public async Task RunAsync()
        {
            await Client.ConnectAsync();
            await WaitForCancellationAsync();
        }
        
        private async Task WaitForCancellationAsync()
        {
            while(!_cts.IsCancellationRequested)
                await Task.Delay(500);
        }
        
        public void Dispose()
        {
            Client.Dispose();
            _cnext = null;
        }
    }
}