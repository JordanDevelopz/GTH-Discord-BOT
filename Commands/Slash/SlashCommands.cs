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
using static TornWarTracker.Data_Structures.tornDataStructures.warDataStructures.FactionRankedWars;

namespace TornWarTracker.Commands.Slash
{
    public class SlashCommands : ApplicationCommandModule
    {
        private static readonly string TornApiKey = "MKorHNfemsaPGl5C"; //Get this from leadership member in db

        public class Initiation : ApplicationCommandModule
        {
            [SlashCommand("verifyme", "Verify yourself on the GTH server")]
            [SlashCommandPermissions(Permissions.All)] // Ensure the command is available to everyone
            public async Task VerifyMeCommand(InteractionContext ctx)
            {
                // Acknowledge the interaction immediately
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                // Send a DM asking for the Torn ID
                var dmChannel = await ctx.Member.CreateDmChannelAsync();
                await dmChannel.SendMessageAsync("Please provide your Torn ID.");

                // Wait for a response from the user
                var interactivity = ctx.Client.GetInteractivity();
                var response = await interactivity.WaitForMessageAsync(x => x.Channel == dmChannel && x.Author == ctx.User, TimeSpan.FromMinutes(1));

                if (response.TimedOut)
                {
                    await dmChannel.SendMessageAsync("You fell asleep bro?");
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Verification timed out."));
                    return;
                }

                string tornId = response.Result.Content;
                // Fetch data from the Torn API using requestAPI class
                string apiUrl = $"https://api.torn.com/faction/16057?selections=basic&key={TornApiKey}";
                string jsonResponse = await requestAPI.GetFrom(apiUrl);

                if (jsonResponse == null)
                {
                    await dmChannel.SendMessageAsync("Error occurred while connecting to the Torn API. Please try again later.");
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Error occurred while connecting to the Torn API."));
                    return;
                }

                // Parse the JSON response
                var jsonData = JObject.Parse(jsonResponse);
                if (jsonData["error"] != null)
                {
                    await tornAPIUtils.APIErrorReporting(ctx, jsonData);
                    return;
                }

                // Check if the user is in the faction members list
                var members = jsonData["members"];
                var member = members[tornId];

                if (member == null)
                {
                    await dmChannel.SendMessageAsync("You are not in the correct faction. Verification failed.");
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Verification failed. You are not in the correct faction."));
                    return;
                }

                // Get the user's position within the faction
                var factionRole = (string)member["position"];

                // Debug: Check what faction role is being retrieved
                await dmChannel.SendMessageAsync($"Your role in the faction is: {factionRole}");

                // Assign a role based on the faction position
                var guild = ctx.Guild;
                DiscordRole discordRole = null;

                switch (factionRole.ToLower()) //always lowercase dont forget this alaska you bozo
                {
                    case "leader":
                        discordRole = guild.Roles.Values.FirstOrDefault(r => r.Name == "Leader"); // Adjust the role name
                        break;
                    case "co-leader":
                        discordRole = guild.Roles.Values.FirstOrDefault(r => r.Name == "Co-Leader"); // Adjust the role name
                        break;
                    case "leadership": // Make sure the comparison is lowercase
                        discordRole = guild.Roles.Values.FirstOrDefault(r => r.Name == "Leadership"); // Adjust the role name
                        break;
                    case "master chief": // Make sure the comparison is lowercase
                        discordRole = guild.Roles.Values.FirstOrDefault(r => r.Name == "Master Chief"); // Adjust the role name
                        break;
                    case "prometheus": // Make sure the comparison is lowercase
                        discordRole = guild.Roles.Values.FirstOrDefault(r => r.Name == "Prometheus"); // Adjust the role name
                        break;
                    // Add more cases for each role im abit to lazy for this right now 
                    default:
                        discordRole = guild.Roles.Values.FirstOrDefault(r => r.Name == "Spartan"); // Default role
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
                [Option("TornID","Type in your torn ID")] long TornID, 
                [Option("TornUsername", "Type in your torn username")] string TornUsername,
                [Option("APIKey", "Type in your API key")] string APIKey)
            {
                Console.WriteLine($"Command Started: Register by user {ctx.User.Username}");
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                // Create a DM channel with the spartan
                var dmChannel = await ctx.Member.CreateDmChannelAsync();

                // Create an embedded message , testing this not sure how it will look
                //var embed = new DiscordEmbedBuilder
                //{
                //    Title = "Torn Registration",
                //    Description = "Please fill in the details below to register:",
                //    Color = DiscordColor.Azure
                //};
                //embed.AddField("Torn ID", "Please enter your Torn ID.");
                //embed.AddField("Torn Username", "Please enter your Torn Username.");
                //embed.AddField("API Key", "Please enter your Torn API Key.");

                //await dmChannel.SendMessageAsync(embed: embed);

                // Get user responses
                //var interactivity = ctx.Client.GetInteractivity();

                //// Torn ID input
                //await dmChannel.SendMessageAsync("Torn ID:");
                //var tornIdResponse = await interactivity.WaitForMessageAsync(
                //    x => x.Channel == dmChannel && x.Author == ctx.User, TimeSpan.FromMinutes(2));

                //if (tornIdResponse.TimedOut)
                //{
                //    await dmChannel.SendMessageAsync("You took too long to respond. Registration cancelled.");
                //    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Registration timed out."));
                //    return;
                //}

                //// Torn Username input
                //await dmChannel.SendMessageAsync("Torn Username:");
                //var tornUsernameResponse = await interactivity.WaitForMessageAsync(
                //    x => x.Channel == dmChannel && x.Author == ctx.User, TimeSpan.FromMinutes(2));

                //if (tornUsernameResponse.TimedOut)
                //{
                //    await dmChannel.SendMessageAsync("You took too long to respond. Registration cancelled.");
                //    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Registration timed out."));
                //    return;
                //}

                //// API Key input
                //await dmChannel.SendMessageAsync("API Key:");
                //var apiKeyResponse = await interactivity.WaitForMessageAsync(
                //    x => x.Channel == dmChannel && x.Author == ctx.User, TimeSpan.FromMinutes(2));

                //if (apiKeyResponse.TimedOut)
                //{
                //    await dmChannel.SendMessageAsync("You took too long to respond. Registration cancelled.");
                //    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Registration timed out."));
                //    return;
                //}

                // Parse the responses
                //string tornId = tornIdResponse.Result.Content;
                //string tornUsername = tornUsernameResponse.Result.Content;
                //string apiKey = apiKeyResponse.Result.Content;

                // Save to the database
                DatabaseConnection dbConnection = new DatabaseConnection();
                MySqlConnection connection = dbConnection.GetConnection();

                if (connection != null)
                {
                    try
                    {
                        //// Insert into the members table
                        //string query = "INSERT INTO members (Torn_ID, Torn_API, Torn_UserName) VALUES (@TornId, @TornAPI, @TornUserName)";
                        //using (var cmd = new MySqlCommand(query, connection))
                        //{
                        //    cmd.Parameters.AddWithValue("@TornId", TornID);
                        //    cmd.Parameters.AddWithValue("@TornAPI", APIKey);
                        //    cmd.Parameters.AddWithValue("@TornUserName", TornUsername);

                        //    await cmd.ExecuteNonQueryAsync();
                        //}

                        var embedsuccess = new DiscordEmbedBuilder
                        {
                            Title = "Torn DataSpartan Registration",
                            Description = "Registration successful! Your details have been saved.",
                            Color = DiscordColor.Green
                        };
                        embedsuccess.AddField("Torn ID", TornID.ToString());
                        embedsuccess.AddField("Torn Username", TornUsername);
                        embedsuccess.AddField("API Key", APIKey);
                        await dmChannel.SendMessageAsync(embed: embedsuccess);

                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Registration completed."));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error inserting into the database: {ex.Message}");
                        await dmChannel.SendMessageAsync("An error occurred while saving your details. Please try again.");
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Database error occurred."));
                    }
                    finally
                    {
                        dbConnection.CloseConnection(connection);
                    }
                }
                else
                {
                    await dmChannel.SendMessageAsync("Failed to connect to the database. Please try again later.");
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Database connection failed."));
                }
            }


            [SlashCommand("register_faction", "Register your faction for DataSpartan services.")]

            public async Task VerifyFaction(InteractionContext ctx)
            {
                //check leez or alaska logs for payments: payments to have defined message.

                //If payment found, log faction ID in db, with timestamp for expiry; based on payment amount.

                //If payment not found, deny services.
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

            public async Task InitiateWarTracker(InteractionContext ctx)
            {
                Console.WriteLine($"WarTracker Command Started: Registered by user {ctx.User.Username}");
                Console.WriteLine($"WarTracker Command Started: Registered from guild {ctx.Guild.Id}");

                //perform payment checks for service

                //perform second check on expiry, to see if this method will exceed the service end time.


                //check if this command has already been called
                if (warTrackerRunning.TryGetValue(ctx.Guild.Id, out bool isRunning) && isRunning)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                        .WithContent("War Tracker is already running."));
                    return;
                }

                //get faction ID from user profile
                int factionId = await tornAPIUtils.User.GetFactionID(ctx, TempTornApiKey);

                if (factionId == 0)
                {
                    return;
                }

                //Check user has suitable API key by pinging faction attacks api endpoint
                JObject attacksdata = await tornAPIUtils.Faction.GetAttacks(ctx, TempTornApiKey, factionId);
                if (attacksdata == null)
                {
                    return;
                }

                //Check war status of faction.


                //check war start time


                //Inform user the war tracker has started
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
        .WithContent($"War Tracker has been started by {ctx.Member.DisplayName}."));

                warTrackerRunning[ctx.Guild.Id] = true;                

                //spin up war tracking routines
                var warTracking = new WarTracking(warTrackerRunning);
                await warTracking.Tracker(ctx);

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"War Tracker has finished, <@{ctx.User.Id}>."));
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
