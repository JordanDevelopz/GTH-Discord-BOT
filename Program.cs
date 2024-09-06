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
using DSharpPlus.Entities;

using MySql.Data.MySqlClient;

namespace TornWarTracker
{

    public class DatabaseConnection
    {
        // Connection string to your AWS RDS MySQL database
        private string connectionString = "server=database-1.cbq06eeyev9b.eu-west-2.rds.amazonaws.com;port=3306;database=GTHDiscordBot;uid=admin;pwd=masterpass;";

        // Method to get a MySQL connection
        public MySqlConnection GetConnection()
        {
            try
            {
                // Initialize the connection object and open the connection
                var connection = new MySqlConnection(connectionString);
                connection.Open();

                Console.WriteLine("Connection to AWS RDS MySQL established.");
                return connection;
            }
            catch (MySqlException ex)
            {
                // Handle connection errors
                Console.WriteLine("Error connecting to the database: " + ex.Message);
                return null;
            }
        }

        // Method to close the connection
        public void CloseConnection(MySqlConnection connection)
        {
            if (connection != null && connection.State == System.Data.ConnectionState.Open)
            {
                try
                {
                    connection.Close();
                    Console.WriteLine("Connection to AWS RDS MySQL closed.");
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine("Error closing the connection: " + ex.Message);
                }
            }
        }
    }
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
                MinimumLogLevel = LogLevel.Debug,
                LogTimestampFormat = "MMM dd yyyy - hh:mm:ss tt"
            };

            //apply configuration to the bot
            _discord = new DiscordClient(discordConfig);
            // set up task handler ready event
            _discord.Ready += Client_Ready;

            //set up message handler events
            _messageHandler = new EventHandlingBuilder.MessageHandler();
            _discord.MessageCreated += _messageHandler._discord_MessageCreated;


            _cooldownHandler = new EventHandlingBuilder.CoolDownHandler();
            
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

            // Error handling for slash commands
            slashcommandsConfig.SlashCommandErrored += async (s, e) =>
            {
                await e.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .WithContent($"An error occurred: {e.Exception.Message}"));
            };

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
