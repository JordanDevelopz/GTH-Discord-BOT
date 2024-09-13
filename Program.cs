using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TornWarTracker.Commands;
using TornWarTracker.Commands.Slash;
using TornWarTracker.config;
using TornWarTracker.DB;
using TornWarTracker.Events;

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
        public static DiscordClient _discord { get; set; }
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

            _discord.ComponentInteractionCreated += _discord_ComponentInteractionCreated;

            _discord.ModalSubmitted += ModalEventHandler;

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

            //register the commands
            _commands.RegisterCommands<TornCommands>();

            SetUpSlashes();


            //connect the bot online
            await _discord.ConnectAsync();
            await Task.Delay(Timeout.InfiniteTimeSpan);
        }        

        static void SetUpSlashes()
        {
            Console.WriteLine("Registering SlashCommands...");
            var slashCommandConfig = _discord.UseSlashCommands();
            slashCommandConfig.RegisterCommands<DevSC>(guildId: null);
            //slashCommandConfig.RegisterCommands<AdminSC>(guildId: null);
            slashCommandConfig.RegisterCommands<InitiationSC>(guildId: null);
            //slashCommandConfig.RegisterCommands<GeneralSC>(guildId: null);
            slashCommandConfig.RegisterCommands<WarSC>(guildId: null);
            //slashCommandConfig.RegisterCommands<ChainSC>(guildId: null);
            slashCommandConfig.RegisterCommands<ProgressionSC>(guildId: null);
            slashCommandConfig.RegisterCommands<buttonTests>(guildId: null);
            slashCommandConfig.RegisterCommands<modalTest>(guildId: null);
            slashCommandConfig.RegisterCommands<dropdownTests>(guildId: null);

            // Error handling for slash commands
            slashCommandConfig.SlashCommandErrored += async (s, e) =>
            {
                await e.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .WithContent($"An error occurred: {e.Exception.Message}"));
            };
        }


        private static Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
          return Task.CompletedTask;
        }

        private static async Task _discord_ComponentInteractionCreated(DiscordClient sender, DSharpPlus.EventArgs.ComponentInteractionCreateEventArgs args)
        {
            //Dropdown events
            if (args.Id == "dropdownlist" && args.Interaction.Data.ComponentType == ComponentType.StringSelect)
            {
                var options = args.Values;
                foreach (var option in options)
                {
                    switch (option)
                    {
                        case "option1":
                            await args.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent($"{args.User.Username} has selected option 1"));
                            break;
                        case "option2":
                            await args.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent($"{args.User.Username} has selected option 2"));
                            break;
                        case "option3":
                            await args.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent($"{args.User.Username} has selected option 3"));
                            break;
                    }
                }

            }

            //Button Events
            if (args.Interaction.Data.ComponentType == ComponentType.Button)
            {
                switch (args.Interaction.Data.CustomId)
                {
                    default:
                        await args.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("Thanks!"));
                        break;
                }
            }              
            
        }

        private static async Task ModalEventHandler(DiscordClient sender, DSharpPlus.EventArgs.ModalSubmitEventArgs args)
        {
            if (args.Interaction.Type == InteractionType.ModalSubmit)
            {
                var values = args.Values;

                await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .WithContent($"{args.Interaction.User.Username} submitted the input {values.Values.First()}"));
            }
        }



        public class UserData
        {
            public string ApiKey { get; set; }
            public long TornID { get; set; }
            public int FactionID { get; set; }
            public string factionName { get; set; }
        }

        public class UserService
        {
            private readonly DatabaseConnection _dbConnection;

            public UserService()
            {
                _dbConnection = new DatabaseConnection();
            }

            public async Task<(bool Success, UserData Data, string ErrorMessage)> GetUserDetailsAsync(string discordID)
            {
                MySqlConnection connection = _dbConnection.GetConnection();

                if (connection == null)
                {
                    return (false, null, "Unable to connect to the database. Please try again later.");
                }

                try
                {
                    var userData = new UserData();

                    userData.ApiKey = await DBUtils.GetAPIKey(discordID, connection);
                    if (userData.ApiKey == null)
                    {
                        return (false, null, "Cannot get your API Key. Please register first.");
                    }

                    userData.TornID = await DBUtils.GetTornID(discordID, connection);
                    if (userData.TornID == 0)
                    {
                        return (false, null, "Cannot get your TornID. Please register first.");
                    }

                    userData.FactionID = await DBUtils.GetfactionID(discordID, connection);
                    if (userData.FactionID == 0)
                    {
                        return (false, null, "You are not registered in the faction. Please register first.");
                    }

                    bool paid = await DBUtils.VerifyPayment(userData.FactionID, connection);
                    if (!paid)
                    {
                        return (false, null, "Your faction has not paid for the DataSpartan services. Please ensure the payment is completed before using DataSpartan.");
                    }

                    if (string.IsNullOrEmpty(userData.ApiKey) || userData.FactionID == 0)
                    {
                        return (false, null, "Could not find your API key or faction ID.");
                    }

                    return (true, userData, null);
                }
                catch (Exception ex)
                {
                    return (false, null, $"Database Error: {ex.Message}");
                }
                finally
                {
                    _dbConnection.CloseConnection(connection);
                }
            }
        }


    }


}
