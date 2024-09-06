using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using System;
using System.Threading;
using System.Threading.Tasks;
using TornWarTracker.Commands;
using TornWarTracker.Commands.Slash;
using TornWarTracker.config;
using TornWarTracker.Events;

using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;



namespace TornWarTracker
{
    internal class Program
    {
        //private CancellationTokenSource _cts{  get; set; }
        //private IConfigurationRoot _config;
        private static DiscordClient _discord { get; set; }
        private static CommandsNextExtension _commands {  get; set; }
        private static EventHandlingBuilder.MessageHandler _messageHandler;
        private static EventHandlingBuilder.CoolDownHandler _cooldownHandler;
        private  InteractivityExtension _interactivity { get; set; }

        static async Task Main(string[] args)
        {
            //Read in json config to get token and prefix
            var jsonReader = new JSONReader();
            await jsonReader.ReadJson();

            //set up bot configuration
            var discordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = jsonReader.token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
            };

            //apply configuration to the bot
            _discord = new DiscordClient(discordConfig);

            //set up message handler events
            _messageHandler = new EventHandlingBuilder.MessageHandler();
            _cooldownHandler = new EventHandlingBuilder.CoolDownHandler();


            // set up task handler ready event
            _discord.Ready += Client_Ready;
            _discord.MessageCreated += _messageHandler._discord_MessageCreated;

            // Enable the Interactivity module
            _discord.UseInteractivity(new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromMinutes(2)
            });


            //set-up commands config
            var commandsConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new string[] {jsonReader.prefix},
                EnableMentionPrefix = true,
                EnableDms = true,
                //EnableDefaultHelp = false,
            };

            _commands = _discord.UseCommandsNext(commandsConfig);            
            _commands.CommandErrored += _cooldownHandler._commands_CommandErrored;
            //register the commands
            _commands.RegisterCommands<TornCommands>();

            var slashcommandsConfig = _discord.UseSlashCommands();
            slashcommandsConfig.RegisterCommands<BasicSlash>(guildId: null);
            //slashcommandsConfig.RegisterCommands<BasicSlash>();

            //connect the bot online
            await _discord.ConnectAsync();
            await Task.Delay(Timeout.InfiniteTimeSpan);
        }

        

        private static Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            return Task.CompletedTask;
        }
    }
}
