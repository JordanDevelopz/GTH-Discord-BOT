using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using TornWarTracker.Torn_API;
using TornWarTracker.War;

namespace TornWarTracker.Commands.Slash
{
    public class SlashCommands : ApplicationCommandModule
    {
        //private static readonly string TornApiKey = "MKorHNfemsaPGl5C"; //Get this from leadership member in db
        private static readonly string TornApiKey = "";
        public class Initiation : ApplicationCommandModule
        {
            [SlashCommand("verifyme", "Verify yourself on the GTH server")]
            [SlashCommandPermissions(Permissions.All)] // Ensure the command is available to everyone
            public async Task VerifyMeCommand(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                var dmChannel = await ctx.Member.CreateDmChannelAsync();
                await dmChannel.SendMessageAsync("Please provide your Torn ID.");

                var interactivity = ctx.Client.GetInteractivity();
                var response = await interactivity.WaitForMessageAsync(x => x.Channel == dmChannel && x.Author == ctx.User, TimeSpan.FromMinutes(1));

                if (response.TimedOut)
                {
                    await dmChannel.SendMessageAsync("You fell asleep bro?");
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Verification timed out."));
                    return;
                }

                string tornId = response.Result.Content;
                string apiUrl = $"https://api.torn.com/faction/16057?selections=basic&key={TornApiKey}";
                string jsonResponse = await requestAPI.GetFrom(apiUrl);

                if (jsonResponse == null)
                {
                    await dmChannel.SendMessageAsync("Error occurred while connecting to the Torn API. Please try again later.");
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Error occurred while connecting to the Torn API."));
                    return;
                }

                var jsonData = JObject.Parse(jsonResponse);
                if (jsonData["error"] != null)
                {
                    await tornAPIUtils.APIErrorReporting(jsonData, ctx);
                    return;
                }

                var members = jsonData["members"];
                var member = members[tornId];

                if (member == null)
                {
                    await dmChannel.SendMessageAsync("You are not in the correct faction. Verification failed.");
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Verification failed. You are not in the correct faction."));
                    return;
                }

                var factionRole = (string)member["position"];
                await dmChannel.SendMessageAsync($"Your role in the faction is: {factionRole}");

                var guild = ctx.Guild;
                DiscordRole discordRole = null;
                switch (factionRole.ToLower())
                {
                    case "leader":
                        discordRole = guild.Roles.Values.FirstOrDefault(r => r.Name == "Leader");
                        break;
                    case "co-leader":
                        discordRole = guild.Roles.Values.FirstOrDefault(r => r.Name == "Co-Leader");
                        break;
                    case "leadership":
                        discordRole = guild.Roles.Values.FirstOrDefault(r => r.Name == "Leadership");
                        break;
                    case "master chief":
                        discordRole = guild.Roles.Values.FirstOrDefault(r => r.Name == "Master Chief");
                        break;
                    case "prometheus":
                        discordRole = guild.Roles.Values.FirstOrDefault(r => r.Name == "Prometheus");
                        break;
                    default:
                        discordRole = guild.Roles.Values.FirstOrDefault(r => r.Name == "Spartan");
                        break;
                }

                if (discordRole != null)
                {
                    await ctx.Member.GrantRoleAsync(discordRole);
                    await dmChannel.SendMessageAsync($"You have been verified and assigned the role: {discordRole.Name}");
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"You have been verified and assigned the role: {discordRole.Name}"));
                }
                else
                {
                    await dmChannel.SendMessageAsync("Unable to assign a role. Please contact Leez or Alaska.");
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Unable to assign a role. Please contact Leez or Alaska."));
                }
            }


            [SlashCommand("register", "Register your Torn information.")]
            public async Task RegisterCommand(InteractionContext ctx,
                [Option("TornID", "Type in your torn ID")] long TornID,
                [Option("TornUsername", "Type in your torn username")] string TornUsername,
                [Option("APIKey", "Type in your API key")] string APIKey)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                DatabaseConnection dbConnection = new DatabaseConnection();
                MySqlConnection connection = dbConnection.GetConnection();

                if (connection != null)
                {
                    try
                    {
                        // Step 1: Call Torn API to retrieve faction data using TornID and APIKey
                        string tornApiUrl = $"https://api.torn.com/user/{TornID}?key={APIKey}";

                        string jsonResponse = await requestAPI.GetFrom(tornApiUrl);

                        if (jsonResponse == null)
                        {
                            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Error connecting to the Torn API. Please try again later."));
                            return;
                        }

                        var jsonData = JObject.Parse(jsonResponse);
                        if (jsonData["error"] != null)
                        {
                            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Torn API Error: {jsonData["error"]["error"]}"));
                            return;
                        }

                        // Step 2: Get the faction ID from the Torn API response
                        int factionId = (int)jsonData["faction"]["faction_id"];
                        string factionName = (string)jsonData["faction"]["faction_name"];

                        if (factionId == 0)
                        {
                            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("You are not part of a faction. Please join a faction before registering."));
                            return;
                        }

                        // Step 3: Insert member data into the `members` table including the `faction_id`
                        string query = "INSERT INTO members (Torn_ID, Torn_API, Torn_UserName, faction_id) VALUES (@TornId, @TornAPI, @TornUserName, @FactionId)";
                        using (var cmd = new MySqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@TornId", TornID);
                            cmd.Parameters.AddWithValue("@TornAPI", APIKey);
                            cmd.Parameters.AddWithValue("@TornUserName", TornUsername);
                            cmd.Parameters.AddWithValue("@FactionId", factionId);  // Store faction ID

                            await cmd.ExecuteNonQueryAsync();
                        }

                        // Step 4: Send a success message with details about the registered user and faction
                        var embedSuccess = new DiscordEmbedBuilder
                        {
                            Title = "Torn DataSpartan Registration",
                            Description = "Registration successful! Your details have been saved.",
                            Color = DiscordColor.Green
                        };
                        embedSuccess.AddField("Torn ID", TornID.ToString());
                        embedSuccess.AddField("Torn Username", TornUsername);
                        embedSuccess.AddField("Faction Name", factionName);  // Display faction name
                        //embedSuccess.AddField("API Key", APIKey);

                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(embedSuccess.Description).AsEphemeral(true));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error inserting into the database: {ex.Message}");
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("An error occurred while saving your details. Please try again."));
                    }
                    finally
                    {
                        dbConnection.CloseConnection(connection);
                    }
                }
                else
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Database connection failed. Please try again later."));
                }
            }


            [SlashCommand("authenticate_faction", "Authenticate a faction and mark them as paid.")]
            [SlashCommandPermissions(Permissions.ManageGuild)]  // Ensures only admins can use this command
            public async Task AuthenticateFactionCommand(InteractionContext ctx,
                [Option("FactionID", "Enter the Faction ID")] long FactionID,
                [Option("FactionName", "Enter the Faction Name")] string FactionName,
                [Option("PaymentReceived", "Payment received status (true/false)")] bool PaymentReceived)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                DatabaseConnection dbConnection = new DatabaseConnection();
                MySqlConnection connection = dbConnection.GetConnection();

                if (connection != null)
                {
                    try
                    {
                        // Step 1: Insert or update faction information
                        string query = @"
                        INSERT INTO factions (faction_id, faction_name, payment_received)
                        VALUES (@FactionID, @FactionName, @PaymentReceived)
                        ON DUPLICATE KEY UPDATE
                        faction_name = @FactionName, payment_received = @PaymentReceived;";

                        using (var cmd = new MySqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@FactionID", FactionID);
                            cmd.Parameters.AddWithValue("@FactionName", FactionName);
                            cmd.Parameters.AddWithValue("@PaymentReceived", PaymentReceived);

                            await cmd.ExecuteNonQueryAsync();
                        }

                        // Step 2: Send a success message with details
                        var embedSuccess = new DiscordEmbedBuilder
                        {
                            Title = "Faction Authentication",
                            Description = $"Faction {FactionName} has been authenticated.",
                            Color = DiscordColor.Green
                        };
                        embedSuccess.AddField("Faction ID", FactionID.ToString());
                        embedSuccess.AddField("Faction Name", FactionName);
                        embedSuccess.AddField("Payment Received", PaymentReceived ? "Yes" : "No");

                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(embedSuccess.Description).AsEphemeral(true));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error inserting/updating faction payment: {ex.Message}");
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("An error occurred while authenticating the faction. Please try again."));
                    }
                    finally
                    {
                        dbConnection.CloseConnection(connection);
                    }
                }
                else
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Database connection failed. Please try again later."));
                }
            }
        }



        public class General : ApplicationCommandModule
        {
            
        }

        public class War : ApplicationCommandModule
        {

            private static readonly string FactionRoleTornApiKeyCustom = "WGvqLLbvP63PgoLH";
            private static readonly string FactionRoleTornApiKey = "4vuNVQ1KoGWV7PIt";

            private static readonly string TempTornApiKey = FactionRoleTornApiKey;
            

            private static ConcurrentDictionary<ulong, bool> warTrackerRunning = new ConcurrentDictionary<ulong, bool>();
            [SlashCommand("WarTracker", "Use this command to begin the war tracker")]
            public async Task InitiateWarTracker(InteractionContext ctx,
               [Option("TornID", "Enter your Torn ID")] long TornID)
            {
                Console.WriteLine($"WarTracker Command Started: Registered by user {ctx.User.Username}");
                Console.WriteLine($"WarTracker Command Started: Registered from guild {ctx.Guild.Id}");

                DatabaseConnection dbConnection = new DatabaseConnection();
                MySqlConnection connection = dbConnection.GetConnection();

                if (connection != null)
                {
                    try
                    {
                        // Log request URL
                        Console.WriteLine($"Requesting Torn API with TornID: {TornID}");

                        string apiKey = null;
                        string getApiKeyQuery = "SELECT Torn_API FROM members WHERE Torn_ID = @TornID LIMIT 1";
                        using (var cmd = new MySqlCommand(getApiKeyQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@TornID", TornID);

                            var apiKeyResult = await cmd.ExecuteScalarAsync();
                            if (apiKeyResult == null)
                            {
                                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                                    .WithContent("You are not registered. Please use /register to register before starting the War Tracker."));
                                return;
                            }

                            apiKey = apiKeyResult.ToString();
                        }

                        // Create request URL
                        // Updated Torn API URL without 'selections=faction'
                        string tornApiUrl = $"https://api.torn.com/user/{TornID}?key={apiKey}";
                        ;

                        // Log the request URL
                        Console.WriteLine($"Torn API URL: {tornApiUrl}");

                        string jsonResponse = await requestAPI.GetFrom(tornApiUrl);

                        // Log the response from Torn API
                        Console.WriteLine($"Torn API Response: {jsonResponse}");

                        if (jsonResponse == null)
                        {
                            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                                .WithContent("Error occurred while connecting to the Torn API. Please try again later."));
                            return;
                        }

                        var jsonData = JObject.Parse(jsonResponse);
                        if (jsonData["error"] != null)
                        {
                            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                                .WithContent($"Torn API Error: {jsonData["error"]["error"]}"));
                            return;
                        }

                        int factionId = (int)jsonData["faction"]["faction_id"];
                        if (factionId == 0)
                        {
                            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                                .WithContent("You are not part of a faction. War Tracker requires faction membership."));
                            return;
                        }

                        //check payment
                        bool paid = await tornAPIUtils.PaymentVerification.VerifyPayment(factionId, connection);
                        if (!paid)
                        {
                            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                                                                .WithContent("Your faction has not paid for the War Tracker service. Please ensure the payment is completed before using this feature."));
                            return;
                        }

                        if (warTrackerRunning.TryGetValue(ctx.Guild.Id, out bool isRunning) && isRunning)
                        {
                            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                                .WithContent("War Tracker is already running."));
                            return;
                        }

                        JObject attacksdata = await tornAPIUtils.Faction.GetAttacks(ctx, apiKey, factionId);
                        if (attacksdata == null)
                        {
                            //no need to respond here as response is generated through get attacks > APIErrorReporting
                            return;
                        }

                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                            .WithContent($"War Tracker has been started by {ctx.Member.DisplayName}."));

                        warTrackerRunning[ctx.Guild.Id] = true;

                        var warTracking = new WarTracking(warTrackerRunning);
                        await warTracking.Tracker(ctx);

                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"War Tracker has finished, <@{ctx.User.Id}>."));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                            .WithContent("An error occurred while processing the War Tracker. Please try again later."));
                    }
                    finally
                    {
                        dbConnection.CloseConnection(connection);
                    }
                }
                else
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                        .WithContent("Unable to connect to the database. Please try again later."));
                }
            }

            [SlashCommand("WarTrackerStatus", "Use this command to check the status of the war tracker")]
            [SlashCommandPermissions(Permissions.All)] // Ensure the command is available to everyone
            public async Task StatusWarTracker(InteractionContext ctx)
            {
                if (warTrackerRunning.TryGetValue(ctx.Guild.Id, out bool isRunning) && isRunning)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                        .WithContent("War Tracker is already running."));
                    return;
                }
                else
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                        .WithContent("War Tracker is not currently running."));
                    return;
                }
            }
        }        

        public class Progression : ApplicationCommandModule
        {

        }

    }
}
