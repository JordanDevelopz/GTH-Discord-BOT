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
using System.Globalization;
using System.Net.Http;
using static TornWarTracker.War.WarTracking;
using static TornWarTracker.Program;

namespace TornWarTracker.Commands.Slash
{
    [SlashCommandGroup("War", "Perform war related tasks")]
    public class WarSC : ApplicationCommandModule
    {
        private static HttpClient httpClient = new HttpClient();

        private readonly WarTracking warTracking = new WarTracking();

        [SlashCommand("RankedWarTracker", "Use this command to interact with ranked war tracker")]
        public async Task InitiateWarTracker(InteractionContext ctx)
        {

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            Console.WriteLine($"WarTracker Command Started: Registered by user {ctx.User.Username}");
            Console.WriteLine($"WarTracker Command Started: Registered from guild {ctx.Guild.Id}");


            //Step 1: Check user service
            string discordID = ctx.User.Id.ToString();
            string apiKey = null;
            long tornID = 0;
            int factionID = 0;

            UserService userService = new UserService();
            var result = await userService.GetUserDetailsAsync(discordID);
            if (result.Success)
            {
                var userData = result.Data;
                Console.WriteLine($"API Key: {userData.ApiKey}");
                Console.WriteLine($"TornID: {userData.TornID}");
                Console.WriteLine($"FactionID: {userData.FactionID}");

                apiKey = userData.ApiKey;
                tornID = userData.TornID;
                factionID = userData.FactionID;
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Error: {result.ErrorMessage}"));
                return;
            }

            //Step 2: perform check to see if this is already running for this faction:
            if (WarTrackerState.WarTrackerRunning.TryGetValue(factionID, out bool isRunning) && isRunning)
            {
                Console.WriteLine("Task already running for this faction.");
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("War Tracker already running for this faction."));
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
                //followup handled in getfaction call
                return;
            }

            //this is used to test user has suitable api access to faction data
            JObject attacksdata = await tornAPIUtils.Faction.GetAttacks(ctx, apiKey, factionID);
            if (attacksdata == null)
            {
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

            //craete long to hold start time of war
            long startTime = 0;
            int enemyFactionID = 0;
            int rankedWarID = 0;
            //create initial war message to channel
            try
            {
                var rankedWar = (JObject)factionBasic["ranked_wars"].First.First;
                

                if (rankedWar != null)
                {
                    var rankedWarQuery = factionBasic["ranked_wars"].First.Path;
                    var rankedWarKey = rankedWarQuery.Split('.').Last();
                    Console.WriteLine($"rankedWarQuery {rankedWarKey}");
                    rankedWarID = Convert.ToInt32(rankedWarKey);

                    var factions = (JObject)rankedWar["factions"];
                    // Accessing the war data
                    startTime = (long)rankedWar["war"]["start"];
                    long endTime = (long)rankedWar["war"]["end"];
                    int target = (int)rankedWar["war"]["target"];
                    int winner = (int)rankedWar["war"]["winner"];

                    if (endTime != 0)
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("The war is over, did you miss it?."));
                    }

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

                    if (Convert.ToInt32(firstFaction.Name) == factionID)
                    {
                        enemyFactionID = Convert.ToInt32(secondFaction.Name);
                    }
                    else
                    {
                        enemyFactionID = Convert.ToInt32(firstFaction.Name);
                    }

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
                    
                }
                else
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Your faction is not currently matched for war."));
                    return;
                }
            }
            catch
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Error Occured with war tracker!"));
                return;
            }

            //Check start time is not zero
            if (startTime != 0)
            {
                //get emebers of the faction
                var members = factionBasic["members"] as JObject;
                if (members != null)
                {
                    //mark war tracker as running
                    WarTrackerState.WarTrackerRunning[factionID] = true;

                    long teststarttime = 1726334915;
                    bool cannotStartYet = false;
                    //perform war tracking tasks
                    try
                    {
                        cannotStartYet = await warTracking.Tracker(ctx, apiKey, rankedWarID, factionID, enemyFactionID, members, startTime);
                        //await warTracking.Tracker(ctx, apiKey, rankedWarID, factionID, enemyFactionID, members, teststarttime);
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine("Task was cancelled.");
                    }
                    finally
                    {
                        if (!cannotStartYet)
                        {
                            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"War Tracker has finished, <@{ctx.User.Id}>."));
                        }                       
                        
                    }            

                    //finished
                }
                else
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Not data recieved about members of your faction. Could not start war tracker!"));
                    return;
                }                
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Could not start war tracker!"));
                return;
            }        
        }

        [SlashCommand("RW_Tracker_Status", "Use this command to check the status of the enemy")]
        [SlashCommandPermissions(Permissions.All)] // Ensure the command is available to everyone
        public async Task StatusWarTracker(InteractionContext ctx)
        {

            string discordID = ctx.User.Id.ToString();
            string apiKey = null;
            long tornID = 0;
            int factionID = 0;

            UserService userService = new UserService();
            var result = await userService.GetUserDetailsAsync(discordID);
            if (result.Success)
            {
                var userData = result.Data;
                Console.WriteLine($"API Key: {userData.ApiKey}");
                Console.WriteLine($"TornID: {userData.TornID}");
                Console.WriteLine($"FactionID: {userData.FactionID}");

                apiKey = userData.ApiKey;
                tornID = userData.TornID;
                factionID = userData.FactionID;
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Error: {result.ErrorMessage}"));
                return;
            }

            if (WarTrackerState.WarTrackerRunning.TryGetValue(factionID, out bool isRunning) && isRunning)
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


        [SlashCommand("get_enemy_stats", "Retrieve enemy faction stats in the current ranked war.")]
        public async Task GetEnemyStatsCommand(InteractionContext ctx)
        {
            // Step 1: Acknowledge the command to prevent timeout
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            //Step 2: Check user service
            string discordID = ctx.User.Id.ToString();
            string apiKey = null;
            long tornID = 0;
            int userFactionId = 0;

            UserService userService = new UserService();
            var result = await userService.GetUserDetailsAsync(discordID);
            if (result.Success)
            {
                var userData = result.Data;
                Console.WriteLine($"API Key: {userData.ApiKey}");
                Console.WriteLine($"TornID: {userData.TornID}");
                Console.WriteLine($"FactionID: {userData.FactionID}");

                apiKey = userData.ApiKey;
                tornID = userData.TornID;
                userFactionId = userData.FactionID;
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Error: {result.ErrorMessage}"));
                return;
            }



            // Step 3: Use user's faction ID to query the ranked wars match-up
            string rankedWarApiUrl = $"https://api.torn.com/faction/{userFactionId}?key={apiKey}&selections=rankedwars";
            Console.WriteLine($"Making request to Torn API: {rankedWarApiUrl}");

            string rankedWarResponse = await httpClient.GetStringAsync(rankedWarApiUrl);
            Console.WriteLine($"Torn API Response: {rankedWarResponse}");

            JObject rankedWarData = JObject.Parse(rankedWarResponse);
            if (rankedWarData["error"] != null)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Error from Torn API: {rankedWarData["error"]["error"]}"));
                return;
            }

            // Step 4: Get the current ranked war ID and the enemy faction ID
            var rankedWars = rankedWarData["rankedwars"];
            if (rankedWars.Count() == 0)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("No active ranked wars found."));
                return;
            }

            // Assume we are interested in the first ranked war listed
            var firstWar = rankedWars.First();
            string warID = firstWar.Path.Split('.').Last(); // Extract the actual war ID from the path
            JObject factions = (JObject)firstWar.First["factions"];
            string enemyFactionId = factions.Properties().First(f => f.Name != userFactionId.ToString()).Name;
            string enemyFactionName = factions[enemyFactionId]["name"].ToString();

            Console.WriteLine($"Enemy Faction ID: {enemyFactionId}, Enemy Faction Name: {enemyFactionName}");

            // Step 5: Use TornStats API to retrieve the enemy faction's member list
            string tornStatsApiUrl = $"https://www.tornstats.com/api/v2/{apiKey}/wars/{warID}";
            Console.WriteLine($"Making request to TornStats API: {tornStatsApiUrl}");

            string tornStatsResponse = await httpClient.GetStringAsync(tornStatsApiUrl);
            Console.WriteLine($"TornStats API Response: {tornStatsResponse}");

            JObject tornStatsData = JObject.Parse(tornStatsResponse);
            if (tornStatsData["error"] != null)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Error from TornStats API: {tornStatsData["error"]["error"]}"));
                return;
            }

            // Step 6: Extract enemy faction members
            string factionKey = tornStatsData["war"]["faction_a_id"].ToString() == enemyFactionId ? "faction_a" : "faction_b";
            var enemyMembers = tornStatsData[factionKey]["members"].Cast<JProperty>().Select(m => m.Name).ToList();
            Console.WriteLine($"Enemy members found: {string.Join(", ", enemyMembers)}");

            // Step 7: Query YATA API for each enemy member's estimated stats
            var userStats = new List<(string Username, long TotalStats)>();
            foreach (var memberId in enemyMembers)
            {
                string yataApiUrl = $"https://yata.yt/api/v1/bs/{memberId}/?key=RHHROUZTAPoHRJdNy";
                Console.WriteLine($"Making request to YATA API for member {memberId}: {yataApiUrl}");

                string yataApiResponse = await httpClient.GetStringAsync(yataApiUrl);
                Console.WriteLine($"YATA API Response for member {memberId}: {yataApiResponse}");

                JObject yataData = JObject.Parse(yataApiResponse);
                var userData = yataData[memberId];

                if (userData != null)
                {
                    string username = tornStatsData[factionKey]["members"][memberId]["name"].ToString();
                    long totalStats = userData["total"].ToObject<long>();

                    userStats.Add((username, totalStats));
                }
            }

            // Step 8: Sort the members by total estimated stats in descending order
            var sortedStats = userStats.OrderByDescending(s => s.TotalStats).ToList();

            // Step 9: Paginate the sorted stats into multiple embeds if necessary
            int statsPerPage = 25;
            int currentPage = 0;

            while (currentPage * statsPerPage < sortedStats.Count)
            {
                var embed = new DiscordEmbedBuilder()
                    .WithTitle($"Enemy Faction - {enemyFactionName} (Page {currentPage + 1})")
                    .WithColor(DiscordColor.Red);

                var statsOnPage = sortedStats.Skip(currentPage * statsPerPage).Take(statsPerPage);

                foreach (var (username, totalStats) in statsOnPage)
                {
                    embed.AddField($"{username}", $"Total Estimated Stats: {totalStats.ToString("N0", CultureInfo.InvariantCulture)}", inline: false);
                }

                // Send the embed as a follow-up response
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));

                currentPage++;
            }
        }

        [SlashCommand("get_targetable_enemies", "Retrieve a list of enemy targets within your battle stats range using TornStats.")]
        public async Task GetTargetableEnemiesCommand(InteractionContext ctx)
        {
            // Step 1: Acknowledge the command to prevent timeout
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            //Step 2: Check user service
            string discordID = ctx.User.Id.ToString();
            string apiKey = null;
            long tornID = 0;
            int userFactionId = 0;

            UserService userService = new UserService();
            var result = await userService.GetUserDetailsAsync(discordID);
            if (result.Success)
            {
                var userData = result.Data;
                Console.WriteLine($"API Key: {userData.ApiKey}");
                Console.WriteLine($"TornID: {userData.TornID}");
                Console.WriteLine($"FactionID: {userData.FactionID}");

                apiKey = userData.ApiKey;
                tornID = userData.TornID;
                userFactionId = userData.FactionID;
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Error: {result.ErrorMessage}"));
                return;
            }

            // Step 3: Use TornStats API to retrieve user's total stats
            string userStatsApiUrl = $"https://www.tornstats.com/api/v2/{apiKey}/battlestats/graph";
            Console.WriteLine($"Making request to TornStats API for battle stats: {userStatsApiUrl}");
            string userStatsResponse = await httpClient.GetStringAsync(userStatsApiUrl);
            Console.WriteLine($"TornStats API response for battle stats: {userStatsResponse}");

            JObject userStatsData = JObject.Parse(userStatsResponse);
            if (userStatsData["status"] != null && !userStatsData["status"].ToObject<bool>())
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Error from TornStats API: {userStatsData["message"]}"));
                return;
            }

            // Step 4: Extract the user's total battle stats from TornStats response this is to compate against yata
            var userBattleStats = userStatsData["data"]
            .OrderByDescending(stat => stat["timestamp"].ToObject<long>())
            .First();  //pull the most recent result you ape
            long userTotalStats = userBattleStats["total"].ToObject<long>();
            Console.WriteLine($"User {discordID} has total stats: {userTotalStats} (Most recent timestamp)");


            // Calculate 20% range might jig this as there estimated but not sure
            long lowerBound = (long)(userTotalStats * 0.8);
            long upperBound = (long)(userTotalStats * 1.2);
            Console.WriteLine($"User's targetable range is: {lowerBound} - {upperBound}");

            //// Step 5: Use user's faction ID to query the ranked wars match-up using torns and ripping the war id
            //int userFactionId = await DBUtils.GetfactionID(discordID, connection);
            //Console.WriteLine($"User {discordID} is part of faction {userFactionId}");

            string rankedWarApiUrl = $"https://api.torn.com/faction/{userFactionId}?key={apiKey}&selections=rankedwars";
            Console.WriteLine($"Making request to Torn API for ranked wars: {rankedWarApiUrl}");
            string rankedWarResponse = await httpClient.GetStringAsync(rankedWarApiUrl);
            Console.WriteLine($"Torn API response for ranked wars: {rankedWarResponse}");

            JObject rankedWarData = JObject.Parse(rankedWarResponse);
            if (rankedWarData["error"] != null)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Error from Torn API: {rankedWarData["error"]["error"]}"));
                return;
            }

            // Step 6: Get the current ranked war ID and the enemy faction ID
            var rankedWars = rankedWarData["rankedwars"];
            if (rankedWars.Count() == 0)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("No active ranked wars found."));
                return;
            }

            // Extract just the war ID as a number
            string warID = rankedWars.First.Path.Split('.').Last();
            JObject factions = (JObject)rankedWars.First.First["factions"];
            string enemyFactionId = factions.Properties().First(f => f.Name != userFactionId.ToString()).Name;
            string enemyFactionName = factions[enemyFactionId]["name"].ToString();

            Console.WriteLine($"User is fighting against faction {enemyFactionName} with ID {enemyFactionId}");

            // Step 7: Use TornStats API to retrieve the enemy faction's member list this is a big list btw might be overkill. 
            string tornStatsApiUrl = $"https://www.tornstats.com/api/v2/{apiKey}/wars/{warID}";
            Console.WriteLine($"Making request to TornStats API for enemy members: {tornStatsApiUrl}");
            string tornStatsResponse = await httpClient.GetStringAsync(tornStatsApiUrl);
            Console.WriteLine($"TornStats API response for enemy members: {tornStatsResponse}");

            JObject tornStatsData = JObject.Parse(tornStatsResponse);
            if (tornStatsData["error"] != null)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Error from TornStats API: {tornStatsData["error"]["error"]}"));
                return;
            }

            // Step 8: Extract enemy faction members from TornStats API pull from "members" in response
            string factionKey = tornStatsData["war"]["faction_a_id"].ToString() == enemyFactionId ? "faction_a" : "faction_b";
            var enemyMembers = tornStatsData[factionKey]["members"].Cast<JProperty>().Select(m => m.Name).ToList();
            Console.WriteLine($"Enemy members found: {string.Join(", ", enemyMembers)}");

            // Step 9: Query YATA API for each enemy member's estimated stats using user's API key lots of calls may need somekind of delay/limit
            var targetableStats = new List<(string memberid, string Username, long TotalStats)>();
            foreach (var memberId in enemyMembers)
            {
                // Get the username from TornStats
                string username = tornStatsData[factionKey]["members"][memberId]["name"].ToString();
                Console.WriteLine($"Fetching YATA stats for member {memberId} ({username})");

                // Get the stats from YATA using the user's API key
                string yataApiUrl = $"https://yata.yt/api/v1/bs/{memberId}/?key={apiKey}";
                Console.WriteLine($"Making request to YATA API for member {memberId}: {yataApiUrl}");

                string yataApiResponse = await httpClient.GetStringAsync(yataApiUrl);
                Console.WriteLine($"YATA API Response for member {memberId}: {yataApiResponse}");

                JObject yataData = JObject.Parse(yataApiResponse);
                var userData = yataData[memberId];

                if (userData != null)
                {
                    long totalStats = userData["total"].ToObject<long>();
                    Console.WriteLine($"Member {username} has total stats: {totalStats}");

                    // Check if the member's stats are within 20% of the user's stats
                    if (totalStats >= lowerBound && totalStats <= upperBound)
                    {
                        Console.WriteLine($"Member {username} is within the user's stats range");
                        targetableStats.Add((memberId, username, totalStats));
                    }
                }
            }

            // Step 10: Sort the targets by total estimated stats in descending order
            var sortedTargets = targetableStats.OrderByDescending(s => s.TotalStats).ToList();


            // Step 11: Paginate the sorted targets into multiple embeds if necessary
            if (sortedTargets.Count == 0)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"You have no targets in your stat range!"));
                return;
            }
            int statsPerPage = 25;
            int currentPage = 0;

            while (currentPage * statsPerPage < sortedTargets.Count)
            {
                var embed = new DiscordEmbedBuilder()
                    .WithTitle($"Enemy Targets in Range (Page {currentPage + 1})")
                    .WithColor(DiscordColor.Red);

                var statsOnPage = sortedTargets.Skip(currentPage * statsPerPage).Take(statsPerPage);

                foreach (var (memberId, username, totalStats) in statsOnPage)
                {
                    embed.AddField($"{username}", $"Total Estimated Stats: {totalStats.ToString("N0", CultureInfo.InvariantCulture)}", inline: false);
                }

                // Send the embed as a follow-up response
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));

                currentPage++;
            }
           
        }
    }
}
