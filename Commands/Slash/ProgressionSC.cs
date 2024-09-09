using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using TornWarTracker.Torn_API;

namespace TornWarTracker.Commands.Slash
{
    [SlashCommandGroup("Progression", "Progression-related tasks")]
    public class ProgressionSC : ApplicationCommandModule
    {
        private static HttpClient httpClient = new HttpClient();

        // Xanax consumption tracker
        [SlashCommand("xanax_progress", "Tracks faction members' Xanax consumption from a given date.")]
        public async Task XanaxProgressCommand(InteractionContext ctx,
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

            string discordID = ctx.User.Id.ToString();
            string factionName = null;
            List<(long TornID, string TornUsername, string TornAPI)> membersList = new List<(long, string, string)>();

            // Step 1: Fetch faction ID and members from the database based on Discord ID
            DatabaseConnection dbConnection = new DatabaseConnection();
            MySqlConnection connection = dbConnection.GetConnection();

            if (connection != null)
            {
                try
                {
                    // Fetch the faction ID and faction name using Discord ID
                    string query = @"SELECT factions.faction_id, factions.faction_name 
                                     FROM factions 
                                     JOIN members ON factions.faction_id = members.faction_id 
                                     WHERE members.discord_id = @DiscordID";
                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@DiscordID", discordID);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (reader.Read())
                            {
                                factionName = reader.GetString(reader.GetOrdinal("faction_name"));
                            }
                            else
                            {
                                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("You are not registered in a faction."));
                                return;
                            }
                        }
                    }

                    // Fetch faction members
                    string memberQuery = "SELECT Torn_ID, Torn_UserName, Torn_API FROM members WHERE faction_id = (SELECT faction_id FROM members WHERE discord_id = @DiscordID)";
                    using (var cmd = new MySqlCommand(memberQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@DiscordID", discordID);
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
                }
                catch (Exception ex)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Database Error: {ex.Message}"));
                    return;
                }
                finally
                {
                    dbConnection.CloseConnection(connection);
                }
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Unable to connect to the database. Please try again later."));
                return;
            }

            // Step 2: Iterate over faction members and fetch Xanax usage
            var embedBuilder = new DiscordEmbedBuilder
            {
                Title = $"Xanax Consumption for {factionName} Since {startDate.ToShortDateString()}",
                Color = DiscordColor.Blurple
            };

            foreach (var member in membersList)
            {
                long tornId = member.TornID;
                string tornUsername = member.TornUsername;
                string tornAPI = member.TornAPI;

                // Get Xanax data at the specified start date
                int xanaxAtStart = await GetXanaxAtTimestamp(tornId, tornUsername, tornAPI, startDate);
                if (xanaxAtStart == -1)
                {
                    embedBuilder.AddField(tornUsername, "Failed to retrieve start date Xanax data", true);
                    continue;
                }

                // Get current Xanax data
                int xanaxCurrent = await GetXanaxAtTimestamp(tornId, tornUsername, tornAPI, DateTime.UtcNow);
                if (xanaxCurrent == -1)
                {
                    embedBuilder.AddField(tornUsername, "Failed to retrieve current Xanax data", true);
                    continue;
                }

                // Calculate total Xanax taken since start date
                int xanaxTaken = xanaxCurrent - xanaxAtStart;
                embedBuilder.AddField(tornUsername, $"Xanax taken: {xanaxTaken}", true);
            }

            // Send the embed as a response
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embedBuilder));
        }

        // Method to get Xanax consumption at a specific timestamp
        public async Task<int> GetXanaxAtTimestamp(long tornId, string tornUsername, string tornAPI, DateTime date)
        {
            long timestamp = ((DateTimeOffset)date).ToUnixTimeSeconds();
            string tornApiUrl = $"https://api.torn.com/user/{tornId}?key={tornAPI}&timestamp={timestamp}&stat=xantaken&selections=personalstats";

            string jsonResponse = await requestAPI.GetFrom(tornApiUrl);
            if (jsonResponse == null) return -1;

            JObject jsonData = JObject.Parse(jsonResponse);
            if (jsonData["error"] != null) return -1;

            return jsonData["personalstats"]?["xantaken"]?.ToObject<int>() ?? -1;
        }

        // Battle stats graph command

        [SlashCommand("battle_stats_graph", "Displays a graph of battle stats")]
        public async Task BattleStatsGraph(InteractionContext ctx)
        {
            // get interaction
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            string discordID = ctx.User.Id.ToString();
            string apiKey = null;

            // Step 1: Fetch API Key from the database based on Discord ID
            DatabaseConnection dbConnection = new DatabaseConnection();
            MySqlConnection connection = dbConnection.GetConnection();

            if (connection != null)
            {
                try
                {
                    // Query the database to get the API key linked with the user's Discord ID
                    string query = "SELECT Torn_API FROM members WHERE discord_id = @DiscordID";
                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@DiscordID", discordID);

                        var apiKeyResult = await cmd.ExecuteScalarAsync();
                        if (apiKeyResult == null)
                        {
                            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("You are not registered in the faction. Please register first."));
                            return;
                        }

                        apiKey = apiKeyResult.ToString();
                    }
                }
                catch (Exception ex)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Database Error: {ex.Message}"));
                    return;
                }
                finally
                {
                    dbConnection.CloseConnection(connection);
                }
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Unable to connect to the database. Please try again later."));
                return;
            }

            // Step 2: Fetch battle stats data from TornStats API using the API key has to match torn stats 
            string tornStatsUrl = $"https://www.tornstats.com/api/v2/{apiKey}/battlestats/graph";
            string responseString = null;

            try
            {
                var response = await httpClient.GetAsync(tornStatsUrl);
                responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Failed to fetch data from TornStats API: {responseString}"));
                    return;
                }
            }
            catch (Exception ex)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Error: {ex.Message}"));
                return;
            }

            // Step 3: Parse the data from TornStats API
            JObject jsonData = JObject.Parse(responseString);
            if (jsonData["error"] != null)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"API Error: {jsonData["error"]["error"]}"));
                return;
            }

            // Example: Get battle stats over time 
            JArray statsData = (JArray)jsonData["data"];

            // Prepare data for plotting
            var plotModel = new PlotModel
            {
                Title = "Battle Stats Over Time",
                Background = OxyColors.White,  // Set the background color to white
                TextColor = OxyColors.Black    // Set text color to black for visibility
            };
            plotModel.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "yyyy-MM-dd",
                Title = "Date",
                AxislineColor = OxyColors.Black,
                TextColor = OxyColors.Black
            });
            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Battle Stats",
                AxislineColor = OxyColors.Black,
                TextColor = OxyColors.Black,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dash,
                StringFormat = "#,##0" // Format the axis to avoid scientific notation how fucking weird
            });

            // Define the series for each stat with thinner lines and more visible colors
            var strengthSeries = new LineSeries { Title = "Strength", LineStyle = LineStyle.Solid, StrokeThickness = 2, Color = OxyColors.Red };
            var defenseSeries = new LineSeries { Title = "Defense", LineStyle = LineStyle.Solid, StrokeThickness = 2, Color = OxyColors.Blue };
            var speedSeries = new LineSeries { Title = "Speed", LineStyle = LineStyle.Solid, StrokeThickness = 2, Color = OxyColors.Green };
            var dexteritySeries = new LineSeries { Title = "Dexterity", LineStyle = LineStyle.Solid, StrokeThickness = 2, Color = OxyColors.Orange };
            var totalSeries = new LineSeries { Title = "Total", LineStyle = LineStyle.Solid, StrokeThickness = 2, Color = OxyColors.Purple };

            // Populate the series with data
            foreach (var statEntry in statsData)
            {
                double strength = statEntry["strength"].ToObject<double>();
                double defense = statEntry["defense"].ToObject<double>();
                double speed = statEntry["speed"].ToObject<double>();
                double dexterity = statEntry["dexterity"].ToObject<double>();
                double total = statEntry["total"].ToObject<double>();

                long timestamp = statEntry["timestamp"].ToObject<long>();
                DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;

                // Add data points to each series
                strengthSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(dateTime), strength));
                defenseSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(dateTime), defense));
                speedSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(dateTime), speed));
                dexteritySeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(dateTime), dexterity));
                totalSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(dateTime), total));
            }

            // Add series to plot model
            plotModel.Series.Add(strengthSeries);
            plotModel.Series.Add(defenseSeries);
            plotModel.Series.Add(speedSeries);
            plotModel.Series.Add(dexteritySeries);
            plotModel.Series.Add(totalSeries);

            // Step 4: Save the graph as an image (PNG)
            string filePath = Path.Combine(Path.GetTempPath(), "battle_stats_graph.png");
            using (var stream = File.Create(filePath))
            {
                var pngExporter = new PngExporter { Width = 800, Height = 600 };
                pngExporter.Export(plotModel, stream);
            }

            // Step 5: Send the graph back to Discord
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var messageBuilder = new DiscordMessageBuilder()
                    .AddFile("battle_stats_graph.png", fs)
                    .WithContent("Here is your battle stats graph!");

                await ctx.Channel.SendMessageAsync(messageBuilder);
            }

            // Final interaction complete response
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Graph generation complete."));
        }
    }
}