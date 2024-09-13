using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TornWarTracker.Torn_API;

namespace TornWarTracker.Commands.Slash
{
    [SlashCommandGroup("Initiation", "Perform initiation methods")]
    public class InitiationSC : ApplicationCommandModule
    {
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
                    // Get the Discord ID of the user who invoked the command
                    ulong discordId = ctx.User.Id;

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

                    // Step 3: Insert member data into the `members` table including the `faction_id` and `Discord_ID`
                    string query = "INSERT INTO members (Torn_ID, Torn_API, Torn_UserName, faction_id, Discord_ID) " +
                                   "VALUES (@TornId, @TornAPI, @TornUserName, @FactionId, @DiscordID) " +
                                   "ON DUPLICATE KEY UPDATE Torn_API = @TornAPI, Torn_UserName = @TornUserName, faction_id = @FactionId, Discord_ID = @DiscordID";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@TornId", TornID);
                        cmd.Parameters.AddWithValue("@TornAPI", APIKey);
                        cmd.Parameters.AddWithValue("@TornUserName", TornUsername);
                        cmd.Parameters.AddWithValue("@FactionId", factionId);
                        cmd.Parameters.AddWithValue("@DiscordID", discordId);  // Store Discord ID

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


    }
}
