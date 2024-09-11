using DSharpPlus.Entities;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TornWarTracker.Torn_API;
using TornWarTracker.War;
using Google.Protobuf.WellKnownTypes;
using DSharpPlus.Interactivity.Extensions;
using TornWarTracker.DB;
using static TornWarTracker.Torn_API.tornAPIUtils;
using System.IO;
using TornWarTracker.Data_Creation;
using TornWarTracker.Discord_Utilities;

namespace TornWarTracker.Commands.Slash
{
    [SlashCommandGroup("War", "Perform war related tasks")]
    public class WarSC : ApplicationCommandModule
    {
        private static ConcurrentDictionary<ulong, bool> warTrackerRunning = new ConcurrentDictionary<ulong, bool>();
        [SlashCommand("RankedWarTracker", "Use this command to interact with ranked war tracker")]
        public async Task InitiateWarTracker(InteractionContext ctx)
        {

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            Console.WriteLine($"WarTracker Command Started: Registered by user {ctx.User.Username}");
            Console.WriteLine($"WarTracker Command Started: Registered from guild {ctx.Guild.Id}");

            string discordID = ctx.User.Id.ToString();
            string apiKey = null;
            long tornID = 0;
            int factionID = 16057;

            DatabaseConnection dbConnection = new DatabaseConnection();
            MySqlConnection connection = dbConnection.GetConnection();

            if (connection != null)
            {
                try
                {
                    Console.WriteLine(discordID);
                    apiKey = await DBUtils.GetAPIKey(discordID,connection);
                    if (apiKey == null)
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Cannot get you API Key. Please register first."));
                        return;
                    }
                    Console.WriteLine("api done");

                    tornID = await DBUtils.GetTornID(discordID,connection);
                    if (tornID == 0)
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Cannot get your TornID. Please register first."));
                        return;
                    }
                    Console.WriteLine("tornid done");

                    //factionID = await DBUtils.GetfactionID(discordID,connection);
                    //if (factionID == 0)
                    //{
                    //    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("You are not registered in the faction. Please register first."));
                    //    return ;
                    //}
                    Console.WriteLine("factionid done");

                    //check payment
                    bool paid = await DBUtils.VerifyPayment(factionID, connection);
                    if (!paid)
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Your faction has not paid for the DataSpartan services. Please ensure the payment is completed before using DataSpartan."));
                        return;
                    }
                    Console.WriteLine("paid done");
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

            //perform check to see if tornID is in faction
            int testFactionID = await tornAPIUtils.User.GetFactionID(ctx, tornID, apiKey);
            if (testFactionID != 0)
            {
                if (testFactionID != factionID)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("You are no longer in your registerd faction!"));
                    return;
                }
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("You are not in a faction!"));
                return;
            }

            //perfom war tasks
            var interactivity = Program._discord.GetInteractivity();


            //this is used to test user has suitable api access to faction data
            JObject attacksdata = await tornAPIUtils.Faction.GetAttacks(ctx, apiKey, factionID);
            if (attacksdata == null)
            {
                //no need to respond here as response is generated through get attacks > APIErrorReporting
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Error"));
                return;
            }

            //Get faction basic data
            JObject factionBasic = await tornAPIUtils.Faction.BasicData(apiKey, factionID);
            if (factionBasic == null)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Error: Could not retrieve faction data."));
                return;
            }


            try
            {

                var rankedWar = (JObject)factionBasic["ranked_wars"].First.First;

                if (rankedWar != null)
                {
                    var factions = (JObject)rankedWar["factions"];
                    // Accessing the war data
                    long startTime = (long)rankedWar["war"]["start"];
                    long endTime = (long)rankedWar["war"]["end"];
                    int target = (int)rankedWar["war"]["target"];
                    int winner = (int)rankedWar["war"]["winner"];

                    Console.WriteLine($"War Start Time: {startTime}");
                    Console.WriteLine($"War End Time: {endTime}");
                    Console.WriteLine($"War Target: {target}");
                    Console.WriteLine($"War Winner: {winner}");

                    // Accessing the first faction
                    var firstFaction = factions.Properties().ElementAt(0);
                    string firstFactionName = (string)firstFaction.Value["name"];
                    int firstFactionScore = (int)firstFaction.Value["score"];
                    int firstFactionChain = (int)firstFaction.Value["chain"];

                    Console.WriteLine($"First Faction ID: {firstFaction.Name}, Name: {firstFactionName}, Score: {firstFactionScore}, Chain: {firstFactionChain}");

                    // Accessing the second faction
                    var secondFaction = factions.Properties().ElementAt(1);
                    string secondFactionName = (string)secondFaction.Value["name"];
                    int secondFactionScore = (int)secondFaction.Value["score"];
                    int secondFactionChain = (int)secondFaction.Value["chain"];

                    Console.WriteLine($"Second Faction ID: {secondFaction.Name}, Name: {secondFactionName}, Score: {secondFactionScore}, Chain: {secondFactionChain}");

                    //compile in to embed
                    DateTime dateTimeStart = DateTimeOffset.FromUnixTimeSeconds(startTime).DateTime;
                    long currenttime = await tornAPIUtils.Torn.GetCurrentTimeStamp(apiKey);
                    DateTime dateTimeCurrent = DateTimeOffset.FromUnixTimeSeconds(currenttime).DateTime;

                    // Calculate the difference
                    TimeSpan timeDifference = dateTimeCurrent - dateTimeStart;

                    // Extract days, hours, minutes, and seconds
                    int days = timeDifference.Days;
                    int hours = timeDifference.Hours;
                    int minutes = timeDifference.Minutes;
                    int seconds = timeDifference.Seconds;

                    // Format the time difference into a string
                    string timeToStart = $"{days} days, {hours} hours, {minutes} minutes, {seconds} seconds";

                    var embedSuccess = new DiscordEmbedBuilder
                    {
                        Title = "WAR INFO",
                        Description = $"{firstFactionName} vs {secondFactionName}",
                        Color = DiscordColor.DarkRed
                    };
                    embedSuccess.AddField("Start Date:", DateTimeOffset.FromUnixTimeSeconds(startTime).DateTime.ToString());
                    embedSuccess.AddField("Time until start", timeToStart);
                    embedSuccess.AddField("Target to win", target.ToString());

                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embedSuccess));

                    //proceed to war tracker
                    if (endTime == 0)
                    {

                    }
                }
                else
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Your faction is not currently matched for war."));
                    return;
                }
            }
            catch
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Your faction is not currently matched for war."));
                return;
            }


            warTrackerRunning[ctx.Guild.Id] = true;

            var warTracking = new WarTracking(warTrackerRunning);
            await warTracking.Tracker(ctx);

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"War Tracker has finished, <@{ctx.User.Id}>."));

        }

        [SlashCommand("RW_Tracker_Status", "Use this command to check the status of the enemy")]
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

        [SlashCommand("EnemyStats", "Use this command to check the status of the war tracker")]
        [SlashCommandPermissions(Permissions.All)]
        public async Task GetEnemyStats(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            Console.WriteLine($"WarTracker Command Started: Registered by user {ctx.User.Username}");
            Console.WriteLine($"WarTracker Command Started: Registered from guild {ctx.Guild.Id}");

            string discordID = ctx.User.Id.ToString();
            string apiKey = null;
            long tornID = 0;
            int factionID = 16057;

            DatabaseConnection dbConnection = new DatabaseConnection();
            MySqlConnection connection = dbConnection.GetConnection();

            if (connection != null)
            {
                try
                {
                    Console.WriteLine(discordID);
                    apiKey = await DBUtils.GetAPIKey(discordID, connection);
                    if (apiKey == null)
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Cannot get you API Key. Please register first."));
                        return;
                    }
                    Console.WriteLine("api done");

                    tornID = await DBUtils.GetTornID(discordID, connection);
                    if (tornID == 0)
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Cannot get your TornID. Please register first."));
                        return;
                    }
                    Console.WriteLine("tornid done");

                    //factionID = await DBUtils.GetfactionID(discordID,connection);
                    //if (factionID == 0)
                    //{
                    //    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("You are not registered in the faction. Please register first."));
                    //    return ;
                    //}
                    Console.WriteLine("factionid done");

                    //check payment
                    bool paid = await DBUtils.VerifyPayment(factionID, connection);
                    if (!paid)
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Your faction has not paid for the DataSpartan services. Please ensure the payment is completed before using DataSpartan."));
                        return;
                    }
                    Console.WriteLine("paid done");
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

            //perform check to see if tornID is in faction
            int testFactionID = await tornAPIUtils.User.GetFactionID(ctx, tornID, apiKey);
            if (testFactionID != 0)
            {
                if (testFactionID != factionID)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("You are no longer in your registerd faction!"));
                    return;
                }
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("You are not in a faction!"));
                return;
            }

            //Get faction basic data
            JObject factionBasic = await tornAPIUtils.Faction.BasicData(apiKey, factionID);
            if (factionBasic == null)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Error: Could not retrieve faction data."));
                return;
            }

            //get ranked war enemy faction ID
            try
            {

                var rankedWar = (JObject)factionBasic["ranked_wars"].First.First;

                if (rankedWar != null)
                {
                    long currenttime = await tornAPIUtils.Torn.GetCurrentTimeStamp(apiKey);
                    DateTime dateTimeCurrent = DateTimeOffset.FromUnixTimeSeconds(currenttime).DateTime;

                    var factions = (JObject)rankedWar["factions"];
                    // Accessing the war data                   

                    // Accessing the second faction
                    var secondFaction = rankedWar["factions"].Last as JProperty;
                    string secondFactionId = secondFaction.Name;

                    //Get enemy faction basic data
                    JObject enemyfactionBasic = await tornAPIUtils.Faction.BasicData(apiKey, Convert.ToInt32(secondFactionId));
                    if (enemyfactionBasic == null)
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Error: Could not retrieve faction data."));
                        return;
                    }

                    // Assuming enemyfactionBasic is your JObject and ctx is your command context
                    using (MemoryStream memoryStream = await WarData.WriteMembersToMemoryStream(enemyfactionBasic))
                    {
                        // Upload the MemoryStream to the Discord channel
                        await discordUtils.UploadMemoryStreamToDiscord(ctx.Client, ctx.Channel, memoryStream, "members.txt");
                    }


                    

                    //Console.WriteLine($"secondFactionId {secondFactionId}");

                    //var embedSuccess = new DiscordEmbedBuilder
                    //{
                    //    Title = "ENEMY INFO",
                    //    Description = "List of enemy data",
                    //    Color = DiscordColor.DarkRed
                    //};

                    //// Access the members object
                    //var members = (JObject)enemyfactionBasic["members"];
                    //foreach (var member in members.Properties())
                    //{
                    //    string memberId = member.Name;
                    //    var memberDetails = member.Value;
                    //    string memberName = (string)memberDetails["name"];
                    //    int memberLevel = (int)memberDetails["level"];
                    //    long lastActionTimestamp = (long)memberDetails["last_action"]["timestamp"];

                    //    Console.WriteLine($"memberName {memberName}");
                    //    //compile in to embed
                    //    DateTime dateTimeStart = DateTimeOffset.FromUnixTimeSeconds(lastActionTimestamp).DateTime;

                    //    // Calculate the difference
                    //    TimeSpan timeDifference = dateTimeCurrent - dateTimeStart;

                    //    // Extract days, hours, minutes, and seconds
                    //    int days = timeDifference.Days;
                    //    int hours = timeDifference.Hours;
                    //    int minutes = timeDifference.Minutes;
                    //    int seconds = timeDifference.Seconds;

                    //    // Format the time difference into a string
                    //    string lastactionTime = $"{days} days, {hours} hours, {minutes} minutes, {seconds} seconds";

                    //    embedSuccess.AddField("Name:", memberName);
                    //    embedSuccess.AddField("Level:", memberLevel.ToString());
                    //    embedSuccess.AddField("Last Action Time:", lastactionTime);

                    //    //await Task.Delay(1050);
                    //}

                    //await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embedSuccess));
                }
                else
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Your faction is not currently matched for war."));
                    return;
                }
            }
            catch
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Your faction is not currently matched for war."));
                return;
            }

        }
    }
}
