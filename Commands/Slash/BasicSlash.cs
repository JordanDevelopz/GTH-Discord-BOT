using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using TornWarTracker.Torn_API;

namespace TornWarTracker.Commands.Slash
{
    public class BasicSlash : ApplicationCommandModule
    {
        [SlashCommand("RankedWarReport", "Get most recent ranked war report raw data")]

        public async Task RankedWarCall(InteractionContext ctx)
        {
            //await ctx.Interaction.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DSharpPlus.Entities.DiscordInteractionResponseBuilder().WithContent("I'll get that for you"));
        await ctx.DeferAsync();

            //do api calls here

            await ctx.EditResponseAsync(new DSharpPlus.Entities.DiscordWebhookBuilder().WithContent("Hello"));
        }

        private static readonly string TornApiKey = "MKorHNfemsaPGl5C";


        [SlashCommand("verifyme","Verify yourself on the GTH server")]
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
                await dmChannel.SendMessageAsync("Invalid Torn ID or API Error. Please try again.");
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Invalid Torn ID or API Error."));
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




        [SlashCommand("testdb", "Test the connection to the database")]
        [SlashCommandPermissions(Permissions.All)]
        public async Task TestDatabaseCommand(InteractionContext ctx)
        {
            Console.WriteLine($"Command Started: TestDatabaseCommand by user {ctx.User.Username}");
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            // Create a DM channel with the user
            var dmChannel = await ctx.Member.CreateDmChannelAsync();

            // Create an instance of the DatabaseConnection class
            DatabaseConnection dbConnection = new DatabaseConnection();
            MySqlConnection connection = dbConnection.GetConnection();

            if (connection != null)
            {
                // Connection successful - send a DM to the user
                Console.WriteLine("Database connection successful");
                await dmChannel.SendMessageAsync("Successfully connected to the AWS RDS MySQL database!");
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Database connection successful. Check your DMs."));

                // Close the connection
                dbConnection.CloseConnection(connection);
            }
            else
            {
                // Connection failed - send a DM to the user
                Console.WriteLine("Failed to connect to the database");
                await dmChannel.SendMessageAsync("Failed to connect to the AWS RDS MySQL database.");
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Database connection failed. Check your DMs."));
            }
        }



    }
}
