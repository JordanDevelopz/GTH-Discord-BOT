using DSharpPlus.Entities;
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
    [SlashCommandGroup("Progression", "progression related tasks")]
    public class ProgressionSC : ApplicationCommandModule
    {
        // Slash command to track Xanax consumption from a given start date
        [SlashCommand("xanax_progress", "Tracks faction members' Xanax consumption from a given date.")]
        public async Task ProgressionCommand(InteractionContext ctx,
            [Option("FactionID", "Enter the Faction ID")] long factionID,
            [Option("StartDate", "Enter the start date in YYYY-MM-DD format")] string startDateStr)
        {
            // Parse the start date from the user's input
            DateTime startDate;
            if (!DateTime.TryParse(startDateStr, out startDate))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .WithContent("Invalid date format. Please use YYYY-MM-DD format."));
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource); // Acknowledge the command

            DatabaseConnection dbConnection = new DatabaseConnection();
            MySqlConnection connection = dbConnection.GetConnection();

            if (connection != null)
            {
                try
                {
                    // Step 1: Retrieve faction name
                    string factionName = null;
                    string factionQuery = "SELECT faction_name FROM factions WHERE faction_id = @FactionID";
                    using (var factionCmd = new MySqlCommand(factionQuery, connection))
                    {
                        factionCmd.Parameters.AddWithValue("@FactionID", factionID);
                        var factionResult = await factionCmd.ExecuteScalarAsync();
                        if (factionResult != null)
                        {
                            factionName = factionResult.ToString();
                        }
                        else
                        {
                            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                                .WithContent("Faction not found in the database."));
                            return;
                        }
                    }

                    // Step 2: Retrieve all members of the faction from the database along with their API keys
                    string query = "SELECT Torn_ID, Torn_UserName, Torn_API FROM members WHERE faction_id = @FactionID";
                    var membersList = new List<(long TornID, string TornUsername, string TornAPI)>();

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@FactionID", factionID);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                long tornId = reader.GetInt64(reader.GetOrdinal("Torn_ID"));
                                string tornUsername = reader.GetString(reader.GetOrdinal("Torn_UserName"));
                                string tornAPI = reader.GetString(reader.GetOrdinal("Torn_API"));
                                membersList.Add((tornId, tornUsername, tornAPI));
                            }
                        }
                    }

                    // Step 3: Iterate over each faction member and fetch their Xanax usage from Torn API using their stored API key
                    var embedBuilder = new DiscordEmbedBuilder
                    {
                        Title = $"Xanax Consumption for {factionName} Since {startDate.ToShortDateString()}", // Use faction name here
                        Color = DiscordColor.Blurple
                    };

                    foreach (var member in membersList)
                    {
                        long tornId = member.TornID;
                        string tornUsername = member.TornUsername;
                        string tornAPI = member.TornAPI;

                        // Get the Xanax value at the specified start date
                        int xanaxAtStart = await GetXanaxAtTimestamp(tornId, tornUsername, tornAPI, startDate);
                        if (xanaxAtStart == -1)
                        {
                            embedBuilder.AddField(tornUsername, "Failed to retrieve start date Xanax data", true);
                            continue;
                        }

                        // Get the current Xanax value
                        int xanaxCurrent = await GetXanaxAtTimestamp(tornId, tornUsername, tornAPI, DateTime.UtcNow);
                        if (xanaxCurrent == -1)
                        {
                            embedBuilder.AddField(tornUsername, "Failed to retrieve current Xanax data", true);
                            continue;
                        }

                        // Calculate the Xanax taken since the start date
                        int xanaxTaken = xanaxCurrent - xanaxAtStart;

                        // Add Xanax history in an embedded message
                        embedBuilder.AddField(tornUsername, $"Xanax taken: {xanaxTaken}", true);
                    }

                    // Step 4: Send the embed as a response
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                        .AddEmbed(embedBuilder));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("An error occurred while tracking Xanax consumption. Please try again."));
                }
                finally
                {
                    dbConnection.CloseConnection(connection);
                }
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Unable to connect to the database. Please try again later."));
            }
        }

        // Method to get the Xanax consumption at a specific timestamp (in UNIX time)
        public async Task<int> GetXanaxAtTimestamp(long tornId, string tornUsername, string tornAPI, DateTime date)
        {
            long timestamp = ((DateTimeOffset)date).ToUnixTimeSeconds();
            string tornApiUrl = $"https://api.torn.com/user/{tornId}?key={tornAPI}&timestamp={timestamp}&stat=xantaken&comment=TornAPI&selections=personalstats";
            Console.WriteLine($"Requesting Xanax data for {tornUsername} (ID: {tornId}) at timestamp {timestamp}: {tornApiUrl}");

            string jsonResponse = await requestAPI.GetFrom(tornApiUrl);
            Console.WriteLine($"Torn API Full Response for {tornUsername}: {jsonResponse}");

            if (jsonResponse == null)
            {
                Console.WriteLine($"Failed to get a response for {tornUsername}. Skipping...");
                return -1;
            }

            JObject jsonData;
            try
            {
                jsonData = JObject.Parse(jsonResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse JSON response for {tornUsername}: {ex.Message}");
                return -1;
            }

            if (jsonData["error"] != null)
            {
                Console.WriteLine($"API Error for {tornUsername}: {jsonData["error"]["error"]}");
                return -1;
            }

            var xanaxHistory = jsonData["personalstats"]?["xantaken"];
            if (xanaxHistory != null && xanaxHistory.Type == JTokenType.Integer)
            {
                int xanaxTaken = (int)xanaxHistory;
                Console.WriteLine($"{tornUsername} had taken {xanaxTaken} Xanax at {date.ToShortDateString()}");
                return xanaxTaken;
            }
            else
            {
                Console.WriteLine($"No valid Xanax data available for {tornUsername}");
                return -1;
            }
        }
    }
}
