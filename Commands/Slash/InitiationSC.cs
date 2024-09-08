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
        //[SlashCommand("verifyme", "Verify yourself on the GTH server")]
        //[SlashCommandPermissions(Permissions.All)] // Ensure the command is available to everyone
        //public async Task VerifyMeCommand(InteractionContext ctx)
        //{
        //    await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        //    var dmChannel = await ctx.Member.CreateDmChannelAsync();
        //    await dmChannel.SendMessageAsync("Please provide your Torn ID.");

        //    var interactivity = ctx.Client.GetInteractivity();
        //    var response = await interactivity.WaitForMessageAsync(x => x.Channel == dmChannel && x.Author == ctx.User, TimeSpan.FromMinutes(1));

        //    if (response.TimedOut)
        //    {
        //        await dmChannel.SendMessageAsync("You fell asleep bro?");
        //        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Verification timed out."));
        //        return;
        //    }

        //    string tornId = response.Result.Content;
        //    string apiUrl = $"https://api.torn.com/faction/16057?selections=basic&key={TornApiKey}";
        //    string jsonResponse = await requestAPI.GetFrom(apiUrl);

        //    if (jsonResponse == null)
        //    {
        //        await dmChannel.SendMessageAsync("Error occurred while connecting to the Torn API. Please try again later.");
        //        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Error occurred while connecting to the Torn API."));
        //        return;
        //    }

        //    var jsonData = JObject.Parse(jsonResponse);
        //    if (jsonData["error"] != null)
        //    {
        //        await tornAPIUtils.APIErrorReporting(jsonData, ctx);
        //        return;
        //    }

        //    var members = jsonData["members"];
        //    var member = members[tornId];

        //    if (member == null)
        //    {
        //        await dmChannel.SendMessageAsync("You are not in the correct faction. Verification failed.");
        //        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Verification failed. You are not in the correct faction."));
        //        return;
        //    }

        //    var factionRole = (string)member["position"];
        //    await dmChannel.SendMessageAsync($"Your role in the faction is: {factionRole}");

        //    var guild = ctx.Guild;
        //    DiscordRole discordRole = null;
        //    switch (factionRole.ToLower())
        //    {
        //        case "leader":
        //            discordRole = guild.Roles.Values.FirstOrDefault(r => r.Name == "Leader");
        //            break;
        //        case "co-leader":
        //            discordRole = guild.Roles.Values.FirstOrDefault(r => r.Name == "Co-Leader");
        //            break;
        //        case "leadership":
        //            discordRole = guild.Roles.Values.FirstOrDefault(r => r.Name == "Leadership");
        //            break;
        //        case "master chief":
        //            discordRole = guild.Roles.Values.FirstOrDefault(r => r.Name == "Master Chief");
        //            break;
        //        case "prometheus":
        //            discordRole = guild.Roles.Values.FirstOrDefault(r => r.Name == "Prometheus");
        //            break;
        //        default:
        //            discordRole = guild.Roles.Values.FirstOrDefault(r => r.Name == "Spartan");
        //            break;
        //    }

        //    if (discordRole != null)
        //    {
        //        await ctx.Member.GrantRoleAsync(discordRole);
        //        await dmChannel.SendMessageAsync($"You have been verified and assigned the role: {discordRole.Name}");
        //        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"You have been verified and assigned the role: {discordRole.Name}"));
        //    }
        //    else
        //    {
        //        await dmChannel.SendMessageAsync("Unable to assign a role. Please contact Leez or Alaska.");
        //        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Unable to assign a role. Please contact Leez or Alaska."));
        //    }
        //}


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



    }
}
