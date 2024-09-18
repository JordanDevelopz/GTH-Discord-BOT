using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Script.v1;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
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

    public class GoogleSheetsService : IDisposable
    {
        private SheetsService _service;
        private UserCredential _credential;

        public GoogleSheetsService()
        {
            InitializeService();
        }

        private void InitializeService()
        {
            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                _credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    new[] { SheetsService.Scope.Spreadsheets },
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            _service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = _credential,
                ApplicationName = "Google Sheets API .NET Quickstart",
            });
        }

        public SheetsService GetService()
        {
            return _service;
        }

        public void Dispose()
        {
            _service?.Dispose();
            _credential = null;
        }
    }

    public class GoogleAppsScriptService : IDisposable
    {
        private ScriptService _service;
        private UserCredential _credential;

        public GoogleAppsScriptService()
        {
            InitializeService();
        }

        private void InitializeService()
        {
            using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                _credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    new[] { "https://www.googleapis.com/auth/script.external_request", "https://www.googleapis.com/auth/spreadsheets" },
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            _service = new ScriptService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = _credential,
                ApplicationName = "Google Apps Script API .NET Quickstart",
            });
        }

        public ScriptService GetService()
        {
            RefreshTokenIfNeeded();
            return _service;
        }

        private void RefreshTokenIfNeeded()
        {
            if (_credential.Token.IsStale)
            {
                _credential.RefreshTokenAsync(CancellationToken.None).Wait();
            }
        }

        public void Dispose()
        {
            _service?.Dispose();
            _credential = null;
        }
    }

    public class UserData
    {
        public string ApiKey { get; set; }
        public long TornID { get; set; }
        public string TornUserName { get; set; }
        public int FactionID { get; set; }
        public string factionName { get; set; }
        public string factionSheetID { get; set; }
        public string factionDeploymentID { get; set; }
        public bool paid { get; set; }
    }

    public class UserService
    {
        private readonly GoogleSheetsService _dbConnection;

        public UserService()
        {
            _dbConnection = new GoogleSheetsService();
        }

        public async Task<(bool Success, UserData Data, string ErrorMessage)> GetUserDetailsAsync(string discordID)
        {
            var service = _dbConnection.GetService();

            if (service == null)
            {
                return (false, null, "Unable to connect to the database. Please try again later.");
            }

            try
            {
                var userData = await GetUserDataFromSheetAsync(discordID, service);
                if (userData == null)
                {
                    return (false, null, "User not found or not registered properly.");
                }

                Console.WriteLine($"ApiKey: {userData.ApiKey}");
                Console.WriteLine($"TornID: {userData.TornID}");
                Console.WriteLine($"TornUserName: {userData.TornUserName}");
                Console.WriteLine($"FactionID: {userData.FactionID}");
                Console.WriteLine($"factionName: {userData.factionName}");

                Console.WriteLine("1");

                var factionData = await GetFactionDataFromSheetAsync(userData.FactionID, service);
                if (factionData == null)
                {
                    return (false, null, "Faction details not found.");
                }

                Console.WriteLine($"factionName : {userData.factionName}");
                Console.WriteLine($"factionSheetID : {userData.factionSheetID}");
                Console.WriteLine($"factionDeploymentID : {userData.factionDeploymentID}");
                Console.WriteLine($"paid : {userData.paid}");

                userData.factionName = factionData.factionName;
                userData.factionSheetID = factionData.factionSheetID;
                userData.factionDeploymentID = factionData.factionDeploymentID;
                userData.paid = factionData.paid;


                if (!userData.paid)
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
                _dbConnection.Dispose();
            }
        }

        private async Task<UserData> GetUserDataFromSheetAsync(string discordID, SheetsService service)
        {
            // Define the range to read the entire column A.
            string searchRange = $"{GS_DBUtils.memberSheet}!A:A";
            SpreadsheetsResource.ValuesResource.GetRequest searchRequest =
                service.Spreadsheets.Values.Get(GS_DBUtils.masterSheetID, searchRange);

            // Execute the request asynchronously.
            ValueRange searchResponse = await searchRequest.ExecuteAsync();
            IList<IList<object>> searchValues = searchResponse.Values;

            if (searchValues != null && searchValues.Count > 0)
            {
                for (int i = 0; i < searchValues.Count; i++)
                {
                    if (searchValues[i].Count > 0 && searchValues[i][0].ToString() == discordID)
                    {
                        // Define the range to read the entire row.
                        string rowRange = $"{GS_DBUtils.memberSheet}!A{i + 1}:F{i + 1}";
                        SpreadsheetsResource.ValuesResource.GetRequest rowRequest =
                            service.Spreadsheets.Values.Get(GS_DBUtils.masterSheetID, rowRange);

                        // Execute the request asynchronously.
                        ValueRange rowResponse = await rowRequest.ExecuteAsync();
                        IList<IList<object>> rowValues = rowResponse.Values;

                        if (rowValues != null && rowValues.Count > 0)
                        {
                            var row = rowValues[0];
                            return new UserData
                            {
                                factionName = row.Count > 1 ? row[1].ToString() : null,
                                FactionID = row.Count > 2 ? int.Parse(row[2].ToString()) : 0,
                                TornID = row.Count > 3 ? long.Parse(row[3].ToString()) : 0,
                                TornUserName = row.Count > 4 ? row[4].ToString() : null,
                                ApiKey = row.Count > 5 ? row[5].ToString() : null                                

                                //ApiKey = row[1].ToString(),
                                //TornID = long.Parse(row[2].ToString()),
                                //FactionID = int.Parse(row[3].ToString()),
                                //factionName = row[4].ToString(),
                                //TornUserName = row[5].ToString()
                            };
                        }
                    }
                }
            }

            // Return null if the user is not found.
            return null;
        }

        private async Task<UserData> GetFactionDataFromSheetAsync(int factionID, SheetsService service)
        {
            // Define the range to read the entire column A.
            string searchRange = $"{GS_DBUtils.factionDataSheet}!A:A";
            SpreadsheetsResource.ValuesResource.GetRequest searchRequest =
                service.Spreadsheets.Values.Get(GS_DBUtils.masterSheetID, searchRange);

            // Execute the request asynchronously.
            ValueRange searchResponse = await searchRequest.ExecuteAsync();
            IList<IList<object>> searchValues = searchResponse.Values;

            string FactID = factionID.ToString();

            if (searchValues != null && searchValues.Count > 0)
            {
                for (int i = 0; i < searchValues.Count; i++)
                {
                    if (searchValues[i].Count > 0 && searchValues[i][0].ToString() == FactID)
                        {
                        // Define the range to read the entire row.
                        string rowRange = $"{GS_DBUtils.factionDataSheet}!A{i + 1}:E{i + 1}";
                        SpreadsheetsResource.ValuesResource.GetRequest rowRequest =
                            service.Spreadsheets.Values.Get(GS_DBUtils.masterSheetID, rowRange);

                        // Execute the request asynchronously.
                        ValueRange rowResponse = await rowRequest.ExecuteAsync();
                        IList<IList<object>> rowValues = rowResponse.Values;
                        if (rowValues != null && rowValues.Count > 0)
                        {
                            var row = rowValues[0];
                            bool paid = false;
                            if (row.Count > 4)
                            {
                                bool.TryParse(row[4].ToString(), out paid);
                            }
                            return new UserData
                            {
                                factionName = row[1].ToString(),
                                factionSheetID = row[2].ToString(),
                                factionDeploymentID = row[3].ToString(),
                                paid = paid
                            };
                        }
                    }
                }
            }

            // Return null if the faction is not found.
            return null;
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



        //public class UserData
        //{
        //    public string ApiKey { get; set; }
        //    public long TornID { get; set; }
        //    public int FactionID { get; set; }
        //    public string factionName { get; set; }
        //}

        //public class UserService
        //{
        //    private readonly DatabaseConnection _dbConnection;

        //    public UserService()
        //    {
        //        _dbConnection = new DatabaseConnection();
        //    }

        //    public async Task<(bool Success, UserData Data, string ErrorMessage)> GetUserDetailsAsync(string discordID)
        //    {
        //        MySqlConnection connection = _dbConnection.GetConnection();

        //        if (connection == null)
        //        {
        //            return (false, null, "Unable to connect to the database. Please try again later.");
        //        }

        //        try
        //        {
        //            var userData = new UserData();

        //            userData.ApiKey = await DBUtils.GetAPIKey(discordID, connection);
        //            if (userData.ApiKey == null)
        //            {
        //                return (false, null, "Cannot get your API Key. Please register first.");
        //            }

        //            userData.TornID = await DBUtils.GetTornID(discordID, connection);
        //            if (userData.TornID == 0)
        //            {
        //                return (false, null, "Cannot get your TornID. Please register first.");
        //            }

        //            userData.FactionID = await DBUtils.GetfactionID(discordID, connection);
        //            if (userData.FactionID == 0)
        //            {
        //                return (false, null, "You are not registered in the faction. Please register first.");
        //            }

        //            bool paid = await DBUtils.VerifyPayment(userData.FactionID, connection);
        //            if (!paid)
        //            {
        //                return (false, null, "Your faction has not paid for the DataSpartan services. Please ensure the payment is completed before using DataSpartan.");
        //            }

        //            if (string.IsNullOrEmpty(userData.ApiKey) || userData.FactionID == 0)
        //            {
        //                return (false, null, "Could not find your API key or faction ID.");
        //            }

        //            return (true, userData, null);
        //        }
        //        catch (Exception ex)
        //        {
        //            return (false, null, $"Database Error: {ex.Message}");
        //        }
        //        finally
        //        {
        //            _dbConnection.CloseConnection(connection);
        //        }
        //    }
        //}


    }


}
