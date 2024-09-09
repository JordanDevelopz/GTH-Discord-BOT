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

namespace TornWarTracker.Commands.Slash
{
    [SlashCommandGroup("War", "Perform war related tasks")]
    public class WarSC : ApplicationCommandModule
    {
        private static ConcurrentDictionary<ulong, bool> warTrackerRunning = new ConcurrentDictionary<ulong, bool>();
        [SlashCommand("RW_Tracker", "Use this command to begin the war tracker")]
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

                    Console.WriteLine($"API key recieved {apiKey}");

                    int factionID = 0;
                    string getFactionIDQuery = "SELECT faction_id FROM members WHERE Torn_ID = @TornID LIMIT 1";
                    using (var cmd = new MySqlCommand(getFactionIDQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@TornID", TornID);

                        var factionIDResult = await cmd.ExecuteScalarAsync();
                        if (factionIDResult == null)
                        {
                            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                                .WithContent("Issue getting faction ID. Please use /register to register before starting the War Tracker."));
                            return;
                        }

                        factionID = Convert.ToInt32(factionIDResult);
                    }
                    Console.WriteLine($"factionID recieved {factionID}");


                    //check payment
                    bool paid = await tornAPIUtils.PaymentVerification.VerifyPayment(factionID, connection);
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

                    //this is used to test user has suitable api access to faction data
                    JObject attacksdata = await tornAPIUtils.Faction.GetAttacks(ctx, apiKey, factionID);
                    if (attacksdata == null)
                    {
                        //no need to respond here as response is generated through get attacks > APIErrorReporting
                        return;
                    }

                    //check ranked war status of faction
                    JObject factionbasic = await tornAPIUtils.Faction.BasicData(apiKey, factionID);
                    if (factionbasic == null)
                    {
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                            .WithContent("War Tracker is unable to obtain your faction data."));
                        return;
                    }
                    // Extract the ranked_wars section
                    //var rankedWars = factionbasic["ranked_wars"];

                    //long starttime = 0;
                    //long endtime = 0;
                    //int target = 0;
                    //int winner = 0;
                    //try
                    //{
                    //    starttime = (long)factionbasic["ranked_wars"]["start"];
                    //    endtime = (long)factionbasic["ranked_wars"]["end"];
                    //    target = (int)factionbasic["ranked_wars"]["target"];
                    //    winner = (int)factionbasic["ranked_wars"]["winner"];

                    //    if (endtime == 0)
                    //    {

                    //    }
                    //}
                    //catch
                    //{
                    //    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    //        .WithContent("Your faction is currently not matched for war. Cannot run War Tracker!"));
                    //    return;
                    //}



                    //get current timestamp: epoch in seconds
                    long currenttime = await tornAPIUtils.Torn.GetCurrentTimeStamp(apiKey);
                    Console.WriteLine(currenttime);

                    DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(currenttime).DateTime;

                    var embedSuccess = new DiscordEmbedBuilder
                    {
                        Title = "Ranked War",
                        Color = DiscordColor.Red,
                    };
                    embedSuccess.AddField("Start Time:", dateTime.ToString());
                    embedSuccess.AddField("Enemy Faction: ", "Test");

                    await ctx.Channel.SendMessageAsync(embed: embedSuccess);



                    return;

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

        [SlashCommand("RW_Tracker_Status", "Use this command to check the status of the war tracker")]
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







        //[SlashCommand("test", "testing.")]
        //[SlashCommandPermissions(Permissions.All)]
        //public async Task testcommand(InteractionContext ctx, [Option("TornID", "Enter your Torn ID")] long TornID)
        //{
        //    DatabaseConnection dbConnection = new DatabaseConnection();
        //    MySqlConnection connection = dbConnection.GetConnection();
        //    // Log request URL
        //    Console.WriteLine($"Requesting Torn API with TornID: {TornID}");

        //    string apiKey = null;
        //    string getApiKeyQuery = "SELECT Torn_API FROM members WHERE Torn_ID = @TornID LIMIT 1";
        //    using (var cmd = new MySqlCommand(getApiKeyQuery, connection))
        //    {
        //        cmd.Parameters.AddWithValue("@TornID", TornID);

        //        var apiKeyResult = await cmd.ExecuteScalarAsync();
        //        if (apiKeyResult == null)
        //        {
        //            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
        //                .WithContent("You are not registered. Please use /register to register before starting the War Tracker."));
        //            return;
        //        }

        //        apiKey = apiKeyResult.ToString();
        //    }



        //    long currenttime = await tornAPIUtils.Torn.GetCurrentTimeStamp(apiKey);
        //    // Convert the timestamp to DateTime
        //    DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(currenttime).DateTime;
        //    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
        //                .WithContent(dateTime.ToString()));
        //}
    }
}
