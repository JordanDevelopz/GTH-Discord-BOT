using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TornWarTracker.Commands;
using TornWarTracker.config;
using TornWarTracker.Events;

using Microsoft.Extensions.Configuration;
using DSharpPlus.Interactivity;


namespace TornWarTracker
{
    internal class Program
    {
        private CancellationTokenSource _cts{  get; set; }
        private IConfigurationRoot _config;

        private static DiscordClient _discord { get; set; }
        private static CommandsNextExtension _commands {  get; set; }
        private static EventHandlingBuilder.MessageHandler _messageHandler;
        private  InteractivityExtension _interactivity { get; set; }

        static async Task Main(string[] args)
        {
            var jsonReader = new JSONReader();
            await jsonReader.ReadJson();

            var discordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = jsonReader.token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
            };

            _discord = new DiscordClient(discordConfig);
            _messageHandler = new EventHandlingBuilder.MessageHandler();

            _discord.Ready += Client_Ready;
            _discord.MessageCreated += _messageHandler._discord_MessageCreated;

            var commandsConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new string[] {jsonReader.prefix},
                EnableMentionPrefix = true,
                EnableDms = true,
                //EnableDefaultHelp = false,
            };

            _commands = _discord.UseCommandsNext(commandsConfig);

            _commands.RegisterCommands<TornCommands>();

            await _discord.ConnectAsync();

            await Task.Delay(Timeout.InfiniteTimeSpan);
            await Task.Delay(-1);
        }


        private static Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            return Task.CompletedTask;
        }
    }
}
