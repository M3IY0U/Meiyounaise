using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;

namespace Meiyounaise
{
    class Bot : IDisposable
    {
        private DiscordClient _client;
        private CommandsNextModule _cnext;
        private InteractivityModule _interactivity;
        private CancellationTokenSource _cts;

        public Bot()
        {
            _client = new DiscordClient(new DiscordConfiguration()
            {
                AutoReconnect = true,
                EnableCompression = true,
                LogLevel = LogLevel.Debug,
                Token = Utilities.GetKey("bottoken"),
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true
            });
            
            _interactivity = _client.UseInteractivity(new InteractivityConfiguration()
            {
                PaginationBehaviour = TimeoutBehaviour.Delete,
                PaginationTimeout = TimeSpan.FromSeconds(30),
                Timeout = TimeSpan.FromSeconds(30)
            });
            
            _cts = new CancellationTokenSource();
            
            _cnext = _client.UseCommandsNext(new CommandsNextConfiguration()
            {
                CaseSensitive = false,
                EnableDefaultHelp = true,
                EnableDms = false,
                EnableMentionPrefix = true,
                CustomPrefixPredicate = CustomPrefixPredicate
            });
           
            
            _cnext.RegisterCommands(Assembly.GetEntryAssembly());
    
            _client.Ready += Utilities.Ready;
            
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
            await _client.ConnectAsync();
            await WaitForCancellationAsync();
        }
        
        private async Task WaitForCancellationAsync()
        {
            while(!_cts.IsCancellationRequested)
                await Task.Delay(500);
        }
        
        public void Dispose()
        {
            _client.Dispose();
            _interactivity = null;
            _cnext = null;
        }
    }

    /*
    class Bot
    {
        private static DiscordClient _discord;
        private static CommandsNextModule _commands;
        
        private static void Main()
        {
            MainAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static async Task MainAsync()
        {
            _discord = new DiscordClient(new DiscordConfiguration
            {
                Token = Utilities.GetKey("bottoken"),
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Debug,
                TokenType = TokenType.Bot
            });

            _commands = _discord.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefix = "$",
                CaseSensitive = false,
                EnableMentionPrefix = true,
                EnableDms = false
            });

            _commands.RegisterCommands(Assembly.GetEntryAssembly());
            
            
            
            _discord.MessageCreated += async e =>
            {
                if (e.Message.Content.ToLower().StartsWith("ping"))
                    await e.Message.RespondAsync("pong!");
            };

            await _discord.ConnectAsync();
            await Task.Delay(-1);
        }
    }*/
}