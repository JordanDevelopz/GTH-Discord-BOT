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
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Sheets.v4;
using System.Diagnostics;
using TornWarTracker.DB;

namespace TornWarTracker.Commands.Slash
{
    [SlashCommandGroup("Initiation", "Perform initiation methods")]
    public class InitiationSC : ApplicationCommandModule
    {
        [SlashCommand("register", "Register your Torn information.")]
        public async Task RegisterCommand(InteractionContext ctx,
      [Option("TornID", "Type in your torn ID")] long TornID,
      [Option("TornUsername", "Type in your torn username")] string TornUsername,
      [Option("APIKey", "Type in your API key (Must use Torn Stats API Key)")] string APIKey)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var googleSheetsService = new GoogleSheetsService();
            var service = googleSheetsService.GetService();

            if (service == null)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Could not connect to database service."));
                return;
            }

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

                // Step 3.1: Retrieve all values in column A
                string range = $"{GS_DBUtils.memberSheet}!A:A";
                SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get(GS_DBUtils.masterSheetID, range);
                ValueRange response = await request.ExecuteAsync();
                IList<IList<object>> values = response.Values;

                int rowToUpdate = -1;

                // Step 3.2: Check if discordId exists in the retrieved values
                if (values != null)
                {
                    for (int i = 0; i < values.Count; i++)
                    {
                        if (values[i].Count > 0 && values[i][0].ToString() == discordId.ToString())
                        {
                            rowToUpdate = i + 1; // Row numbers are 1-based in Google Sheets
                            break;
                        }
                    }
                }

                // Step 3: Determine the row to insert/update
                int nextRow = rowToUpdate != -1 ? rowToUpdate : (values != null ? values.Count + 1 : 1);

                // Define the range to insert data into the next empty row.
                string insertRange = $"{GS_DBUtils.memberSheet}!A{nextRow}:F{nextRow}";

                // Create the data to be written.
                var valueRange = new ValueRange();
                var oblist = new List<IList<object>> { new List<object> { discordId.ToString(), factionName, factionId.ToString(), TornID.ToString(), TornUsername, APIKey } };
                valueRange.Values = oblist;

                // Create the update request.
                var updateRequest = service.Spreadsheets.Values.Update(valueRange, GS_DBUtils.masterSheetID, insertRange);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;

                // Execute the update request.
                var updateResponse = await updateRequest.ExecuteAsync();
                Console.WriteLine("Update response: {0}", updateResponse.UpdatedRange);

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

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embedSuccess).AsEphemeral(true));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inserting into the database: {ex.Message}");
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("An error occurred while saving your details. Please try again."));
            }
            finally
            {
                service.Dispose();
            }

        }
        //  public async Task RegisterCommand(InteractionContext ctx,
        //[Option("TornID", "Type in your torn ID")] long TornID,
        //[Option("TornUsername", "Type in your torn username")] string TornUsername,
        //[Option("APIKey", "Type in your API key (Must use Torn Stats API Key)")] string APIKey)
        //  {
        //      await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        //      DatabaseConnection dbConnection = new DatabaseConnection();
        //      MySqlConnection connection = dbConnection.GetConnection();

        //      if (connection != null)
        //      {
        //          try
        //          {
        //              // Get the Discord ID of the user who invoked the command
        //              ulong discordId = ctx.User.Id;

        //              // Step 1: Call Torn API to retrieve faction data using TornID and APIKey
        //              string tornApiUrl = $"https://api.torn.com/user/{TornID}?key={APIKey}";
        //              string jsonResponse = await requestAPI.GetFrom(tornApiUrl);

        //              if (jsonResponse == null)
        //              {
        //                  await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Error connecting to the Torn API. Please try again later."));
        //                  return;
        //              }

        //              var jsonData = JObject.Parse(jsonResponse);
        //              if (jsonData["error"] != null)
        //              {
        //                  await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Torn API Error: {jsonData["error"]["error"]}"));
        //                  return;
        //              }

        //              // Step 2: Get the faction ID from the Torn API response
        //              int factionId = (int)jsonData["faction"]["faction_id"];
        //              string factionName = (string)jsonData["faction"]["faction_name"];

        //              if (factionId == 0)
        //              {
        //                  await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("You are not part of a faction. Please join a faction before registering."));
        //                  return;
        //              }

        //              // Step 3: Insert member data into the `members` table including the `faction_id` and `Discord_ID`
        //              string query = "INSERT INTO members (Torn_ID, Torn_API, Torn_UserName, faction_id, Discord_ID) " +
        //                             "VALUES (@TornId, @TornAPI, @TornUserName, @FactionId, @DiscordID) " +
        //                             "ON DUPLICATE KEY UPDATE Torn_API = @TornAPI, Torn_UserName = @TornUserName, faction_id = @FactionId, Discord_ID = @DiscordID";

        //              using (var cmd = new MySqlCommand(query, connection))
        //              {
        //                  cmd.Parameters.AddWithValue("@TornId", TornID);
        //                  cmd.Parameters.AddWithValue("@TornAPI", APIKey);
        //                  cmd.Parameters.AddWithValue("@TornUserName", TornUsername);
        //                  cmd.Parameters.AddWithValue("@FactionId", factionId);
        //                  cmd.Parameters.AddWithValue("@DiscordID", discordId);  // Store Discord ID

        //                  await cmd.ExecuteNonQueryAsync();
        //              }

        //              // Step 4: Send a success message with details about the registered user and faction
        //              var embedSuccess = new DiscordEmbedBuilder
        //              {
        //                  Title = "Torn DataSpartan Registration",
        //                  Description = "Registration successful! Your details have been saved.",
        //                  Color = DiscordColor.Green
        //              };
        //              embedSuccess.AddField("Torn ID", TornID.ToString());
        //              embedSuccess.AddField("Torn Username", TornUsername);
        //              embedSuccess.AddField("Faction Name", factionName);  // Display faction name

        //              await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embedSuccess).AsEphemeral(true));
        //          }
        //          catch (Exception ex)
        //          {
        //              Console.WriteLine($"Error inserting into the database: {ex.Message}");
        //              await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("An error occurred while saving your details. Please try again."));
        //          }
        //          finally
        //          {
        //              dbConnection.CloseConnection(connection);
        //          }
        //      }
        //      else
        //      {
        //          await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Database connection failed. Please try again later."));
        //      }
        //  }


    }
}
