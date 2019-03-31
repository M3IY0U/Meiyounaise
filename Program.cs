using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;

namespace Meiyounaise
{
    class Program
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
                EnableMentionPrefix = true
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
    }
}