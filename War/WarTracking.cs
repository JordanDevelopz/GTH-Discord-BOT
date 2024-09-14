using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TornWarTracker.Commands.Slash;
using TornWarTracker.Torn_API;
using static TornWarTracker.Data_Structures.tornDataStructures;
using static TornWarTracker.Torn_API.tornAPIUtils;

namespace TornWarTracker.War
{
    public class WarTracking
    {
        public static class WarTrackerState
        {
            public static ConcurrentDictionary<int, bool> WarTrackerRunning = new ConcurrentDictionary<int, bool>();
        }


        public async Task Tracker(InteractionContext ctx, string apiKey,int rankedWarID, int factionID,int enemyFactionID,JObject members, long warStartTime)

        {

            // Create a HashSet of attackID
            HashSet<long> attackIDSet = new HashSet<long>();


            //perform loop until current time = starttime
            

            //bool updateTriggered = false;

            while (true)
            {
                //get current time in epoch
                DateTimeOffset currentTime = DateTimeOffset.UtcNow;
                // Convert to Unix epoch time
                long unixTime = currentTime.ToUnixTimeSeconds();

                long timeUntilTarget = warStartTime - unixTime;

                if (timeUntilTarget <= 0)
                {
                    Console.WriteLine("Performing the task at: " + DateTime.UtcNow);
                    break;
                }

                // Calculate delay based on the time until the target

                //bool update = false;
                int delay;
                if (timeUntilTarget > 3600)  // More than 1 hour but less than 1 day
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"War Tracker can only be started one hour before war!"));
                    return;
                }
                else if (timeUntilTarget > 60) // More than 1 minutes but less than 1 hour
                {
                    //update = true;
                    delay = 60000; // 1 min delay
                }
                else
                {
                    delay = 1000; // 1 second delay
                }
                                

                Console.WriteLine($"Waiting for {delay / 1000} seconds. Time until target: {timeUntilTarget} seconds.");
                await Task.Delay(delay);
                //await Task.Delay(delay).ContinueWith(t =>
                //{
                //    if (!updateTriggered && update)
                //    {
                //        updateTriggered = true;
                //        //make embed for time updates
                //        var embedSuccess = new DiscordEmbedBuilder
                //        {
                //            Title = "WAR TRACKER",
                //            Description = "Your war starts soon!",
                //            Color = DiscordColor.DarkRed
                //        };
                //        embedSuccess.AddField("Start Time: ", DateTimeOffset.FromUnixTimeSeconds(timeUntilTarget).DateTime.ToString());

                //        ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embedSuccess));

                //    }
                //    Console.WriteLine("War Tracker Tick");
                //});
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"The War has begun! War Tracker is now recording, <@{ctx.User.Id}>. Good luck!"));

            //start tracking data

            long endTime = 0;
            bool erroredout = false;
            string issue = null;

            //create tally data
            Dictionary<long, WarTally> warTallyDictionary = new Dictionary<long, WarTally>();

            //create an entry for all members of the faction
            foreach (var member in members.Properties())
            {
                string memberId = member.Name;
                long tornid = Convert.ToInt64(memberId);
                WarTally warTal = new WarTally();
                if (!warTallyDictionary.ContainsKey(tornid))
                {
                    warTal.warTallyID = rankedWarID;
                    warTal.factionID = factionID;
                    warTal.tornID = tornid;
                    warTal.hits = 0;
                    warTal.assists = 0;
                    warTal.interupts = 0;
                    warTal.respectBest = 0.00;
                    warTal.respectBonus = 0.00;
                    warTal.respectGained = 0.00;
                    warTal.respectLost = 0.00;
                    warTal.respectNet = 0.00;
                    warTal.respectEnemyGain = 0.00;
                    warTal.respectEnemyLost = 0.00;
                    warTal.fairFight = 0;
                    warTal.retalsOut = 0;
                    warTal.retalsIn = 0;
                    warTal.defendsWon = 0;
                    warTal.defendsInterupt = 0;
                    warTal.defendsLost = 0;
                    warTal.outsideHits = 0;
                    warTal.outsideRespect = 0;
                    warTal.outsideLost = 0;
                    warTal.outsideDefendsWon = 0;
                    warTal.outsideDefendsLost = 0;
                    warTal.overseas = 0;
                    warTal.energyUsedOut = 0;
                    warTal.energyUsedIn = 0;
                    warTal.hospd = 0;
                    warTal.mugged = 0;
                    warTal.attackLeave = 0;

                    warTallyDictionary.Add(tornid,warTal);
                }
            }

            //create blank vals and dump to db
            await InitialWarTallyToDB(ctx, warTallyDictionary);

            //create long to hold last valid attack time from previous attack log: used to ensure no duplicates
            long lastAttackTimeFromPrevious = 0;
            //create tally data
            while (true)
            {

                //check if war has ended by getting faction basic data
                JObject factionBasic = await tornAPIUtils.Faction.BasicData(apiKey, factionID);
                if (factionBasic == null)
                {
                    erroredout = true;
                    issue = "Issue getting faction basic data. Could not retrieve end time of war";
                    break;
                }

                //Use ranked_wars from faction basic to see if war has ended.
                var rankedWar = (JObject)factionBasic["ranked_wars"].First.First;
                if (rankedWar != null)
                {
                    //end time check: if end time = 0 , war is ongoing.
                    endTime = (long)rankedWar["war"]["end"];
                    if (endTime == 0)
                    {
                        //Get attacks from faction
                        Attacks factionAttacks = await tornAPIUtils.Faction.GetAttacksAsFactionAttacks(ctx, apiKey, factionID);
                        if (factionAttacks != null)
                        {
                            //do the main tally tasks
                            WarTallies(factionAttacks, warStartTime, endTime, enemyFactionID, ref lastAttackTimeFromPrevious, ref warTallyDictionary);


                            //add to db here
                            await UpdateWarTallyToDB(ctx, warTallyDictionary);
                        }
                        else
                        {
                            erroredout = true;
                            issue = "Issue getting faction attack data during war tracker. War tracker cancelled!";
                            break;
                        }
                    }
                    else
                    {
                        //capture last hits after war has ended: due to 2 second delay, some may be missed otherwise
                        //Get attacks from faction
                        Attacks factionAttacks = await tornAPIUtils.Faction.GetAttacksAsFactionAttacks(ctx, apiKey, factionID);
                        if (factionAttacks != null)
                        {
                            //do the main tally tasks
                            WarTallies(factionAttacks, warStartTime, endTime, enemyFactionID, ref lastAttackTimeFromPrevious, ref warTallyDictionary);

                            //add to db here
                            await UpdateWarTallyToDB(ctx, warTallyDictionary);
                        }
                        else
                        {
                            erroredout = true;
                            issue = "Issue getting faction attack data during war tracker. War tracker cancelled!";
                            break;
                        }
                        break;
                    }
                }
                else
                {
                    erroredout = true;
                    issue = "Issue getting ranked war data during war tracker. faction>basic>ranked_war data was null";
                    break;
                }

                //await Task.Delay(2000);
                await Task.Delay(2000).ContinueWith(t =>
                {
                    Console.WriteLine("2 seconds have passed");
                });

            }

            if (erroredout)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"War Tracker encounterd the following issue: {issue}, <@{ctx.User.Id}>!"));
                return;
            }
        }

        private void WarTallies( Attacks fAttacks, long warStartTime, long warEndtime, int enemyFactionID,ref long lastAttackTimeFromPrevious, ref Dictionary<long, WarTally> warTallyDictionary)
        {
            foreach (var attackKVP in fAttacks.AttackList)
            {
                Attack atk = attackKVP.Value;
                //Get AttackID

                //get timestamp
                long timeStarted = attackKVP.Value.TimestampStarted;
                if (timeStarted >= lastAttackTimeFromPrevious)
                {
                    lastAttackTimeFromPrevious = timeStarted;
                }
                else
                {
                    continue;
                }

                //if war endtime !=0, proceed to next step
                if (warEndtime != 0 || timeStarted <= warEndtime)
                {
                    //if attack time > war start time, proceed to next step
                    if (timeStarted >= warStartTime)
                    {
                        if (warTallyDictionary.ContainsKey(atk.AttackerId))
                        {
                            //member made attack

                            WarTally wt;
                            if (warTallyDictionary[atk.AttackerId] == null)
                            {
                                wt = new WarTally();
                            }
                            else
                            {
                                wt = warTallyDictionary[atk.AttackerId];
                            }                            

                            switch (atk.Result)
                            {
                                case "Assist":
                                    wt.energyUsedOut += 25;
                                    if (atk.DefenderFaction == enemyFactionID)
                                    {                                        
                                        wt.assists += 1;                                        
                                    }
                                    break;
                                case "Attacked":
                                    wt.energyUsedOut += 25;
                                    if (atk.DefenderFaction == enemyFactionID)
                                    {
                                        wt.hits += 1;
                                        wt.respectGained += atk.RespectGain;
                                        wt.respectBonus += atk.Modifiers.ChainBonus;
                                        wt.fairFight += atk.Modifiers.FairFight;
                                        wt.retalsOut += (atk.Modifiers.Retaliation > 1) ? 1 : 0;
                                        wt.overseas += (atk.Modifiers.Overseas > 1) ? 1 : 0;
                                        wt.attackLeave += 1;
                                        if (atk.RespectGain > wt.respectBest)
                                        {
                                            wt.respectBest = atk.RespectGain;
                                        }
                                    }
                                    else
                                    {
                                        wt.outsideHits += 1;
                                        wt.attackLeave += 1;
                                    }
                                    break;
                                case "Mugged":
                                    wt.energyUsedOut += 25;
                                    if (atk.DefenderFaction == enemyFactionID)
                                    {
                                        wt.hits += 1;
                                        wt.respectGained += atk.RespectGain;
                                        wt.respectBonus += atk.Modifiers.ChainBonus;
                                        wt.fairFight += atk.Modifiers.FairFight;
                                        wt.retalsOut += (atk.Modifiers.Retaliation > 1) ? 1 : 0;
                                        wt.overseas += (atk.Modifiers.Overseas > 1) ? 1 : 0;
                                        wt.mugged += 1;
                                        if (atk.RespectGain > wt.respectBest)
                                        {
                                            wt.respectBest = atk.RespectGain;
                                        }
                                    }
                                    else
                                    {
                                        wt.outsideHits += 1;
                                        wt.mugged += 1;
                                    }
                                    break;
                                case "Hospitalized":
                                    wt.energyUsedOut += 25;
                                    if (atk.DefenderFaction == enemyFactionID)
                                    {
                                        wt.hits += 1;
                                        wt.respectGained += atk.RespectGain;
                                        wt.respectEnemyLost += atk.RespectLoss;
                                        wt.respectBonus += atk.Modifiers.ChainBonus;
                                        wt.fairFight += atk.Modifiers.FairFight;
                                        wt.retalsOut += (atk.Modifiers.Retaliation > 1) ? 1 : 0;
                                        wt.overseas += (atk.Modifiers.Overseas > 1) ? 1 : 0;
                                        wt.hospd +=1;
                                        if (atk.RespectGain > wt.respectBest)
                                        {
                                            wt.respectBest = atk.RespectGain;
                                        }
                                    }
                                    else
                                    {
                                        wt.outsideHits += 1;
                                        wt.hospd += 1;
                                    }
                                    break;
                                case "Lost":
                                    wt.energyUsedOut += 25;

                                    break;
                                case "Interrupted":

                                    break;
                                
                                case "Stalemate":
                                    wt.energyUsedOut += 25;

                                    break;
                                case "Timeout":
                                    wt.energyUsedOut += 25;

                                    break;
                                case "Escape":
                                    wt.energyUsedOut += 25;

                                    break;
                            }



                            warTallyDictionary[atk.AttackerId] = wt;

                        }
                        else if (warTallyDictionary.ContainsKey(atk.DefenderId))
                        {
                            //member defended

                            WarTally wt;
                            if (warTallyDictionary[atk.AttackerId] == null)
                            {
                                wt = new WarTally();
                            }
                            else
                            {
                                wt = warTallyDictionary[atk.AttackerId];
                            }

                            switch (atk.Result)
                            {
                                case "Attacked":
                                    wt.energyUsedIn += 25;
                                    if (atk.DefenderFaction == enemyFactionID)
                                    {
                                        wt.defendsLost += 1;
                                        wt.respectLost += atk.RespectLoss;
                                        wt.respectEnemyGain += atk.RespectGain;                                        

       
                                    }
                                    else
                                    {
                                        wt.outsideHits += 1;
                                        wt.attackLeave += 1;
                                    }
                                    break;
                            }


                            warTallyDictionary[atk.AttackerId] = wt;
                        }

                    }

                }

            }
        }

        private async Task InitialWarTallyToDB(InteractionContext ctx, Dictionary<long, WarTally> warTallyDictionary)
        {
            DatabaseConnection dbConnection = new DatabaseConnection();
            MySqlConnection connection = dbConnection.GetConnection();

            if (connection != null)
            {
                try
                {
                    foreach (WarTally wt in warTallyDictionary.Values)
                    {
                        // Check if the warTallyID already exists in the database
                        string checkQuery = "SELECT COUNT(*) FROM WarTally WHERE warTallyID = @warTallyID AND tornID = @tornID";
                        using (var checkCmd = new MySqlCommand(checkQuery, connection))
                        {
                            checkCmd.Parameters.AddWithValue("@warTallyID", wt.warTallyID);
                            checkCmd.Parameters.AddWithValue("@tornID", wt.tornID);

                            int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                            if (count > 0)
                            {
                                Console.WriteLine($"Skipping insertion for warTallyID {wt.warTallyID} and tornID {wt.tornID} as it already exists in the database.");
                                continue; // Skip this iteration if the entry already exists
                            }
                        }

                        // Proceed with insertion as the warTallyID does not exist
                        string insertQuery = "INSERT INTO WarTally (warTallyID, factionid, tornID, hits, assists, interupts, respectBest, respectBonus, respectGained, respectLost, respectNet, respectEnemyGain, respectEnemyLost, fairFight, retalsOut, retalsIn, defendsWon, defendsInterupt, defendsLost, outsideHits, outsideRespect, outsideLost, outsideDefendsWon, outsideDefendsLost, overseas, energyUsedOut, energyUsedIn, hospd, mugged, attackLeave)" +
                                             "VALUES (@warTallyID, @factionid, @tornID, @hits, @assists, @interupts, @respectBest, @respectBonus, @respectGained, @respectLost, @respectNet, @respectEnemyGain, @respectEnemyLost, @fairFight, @retalsOut, @retalsIn, @defendsWon, @defendsInterupt, @defendsLost, @outsideHits, @outsideRespect, @outsideLost, @outsideDefendsWon, @outsideDefendsLost, @overseas, @energyUsedOut, @energyUsedIn, @hospd, @mugged, @attackLeave)";
                        Console.WriteLine(insertQuery);

                        using (var cmd = new MySqlCommand(insertQuery, connection))
                        {
                            cmd.Parameters.AddWithValue("@warTallyID", wt.warTallyID);
                            cmd.Parameters.AddWithValue("@factionid", wt.factionID);
                            cmd.Parameters.AddWithValue("@tornID", wt.tornID);
                            cmd.Parameters.AddWithValue("@hits", wt.hits);
                            cmd.Parameters.AddWithValue("@assists", wt.assists);
                            cmd.Parameters.AddWithValue("@interupts", wt.interupts);
                            cmd.Parameters.AddWithValue("@respectBest", wt.respectBest);
                            cmd.Parameters.AddWithValue("@respectBonus", wt.respectBonus);
                            cmd.Parameters.AddWithValue("@respectGained", wt.respectGained);
                            cmd.Parameters.AddWithValue("@respectLost", wt.respectLost);
                            cmd.Parameters.AddWithValue("@respectNet", wt.respectNet);
                            cmd.Parameters.AddWithValue("@respectEnemyGain", wt.respectEnemyGain);
                            cmd.Parameters.AddWithValue("@respectEnemyLost", wt.respectEnemyLost);
                            cmd.Parameters.AddWithValue("@fairFight", wt.fairFight);
                            cmd.Parameters.AddWithValue("@retalsOut", wt.retalsOut);
                            cmd.Parameters.AddWithValue("@retalsIn", wt.retalsIn);
                            cmd.Parameters.AddWithValue("@defendsWon", wt.defendsWon);
                            cmd.Parameters.AddWithValue("@defendsInterupt", wt.defendsInterupt);
                            cmd.Parameters.AddWithValue("@defendsLost", wt.defendsLost);
                            cmd.Parameters.AddWithValue("@outsideHits", wt.outsideHits);
                            cmd.Parameters.AddWithValue("@outsideRespect", wt.outsideRespect);
                            cmd.Parameters.AddWithValue("@outsideLost", wt.outsideLost);
                            cmd.Parameters.AddWithValue("@outsideDefendsWon", wt.outsideDefendsWon);
                            cmd.Parameters.AddWithValue("@outsideDefendsLost", wt.outsideDefendsLost);
                            cmd.Parameters.AddWithValue("@overseas", wt.overseas);
                            cmd.Parameters.AddWithValue("@energyUsedOut", wt.energyUsedOut);
                            cmd.Parameters.AddWithValue("@energyUsedIn", wt.energyUsedIn);
                            cmd.Parameters.AddWithValue("@hospd", wt.hospd);
                            cmd.Parameters.AddWithValue("@mugged", wt.mugged);
                            cmd.Parameters.AddWithValue("@attackLeave", wt.attackLeave);

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error inserting into the database: {ex.Message}");
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("An error occurred while pushing wartally to database"));
                }
                finally
                {
                    dbConnection.CloseConnection(connection);
                }
            }
        }
        //private async Task InitialWarTallyToDB(InteractionContext ctx, Dictionary<long, WarTally> warTallyDictionary)
        //{
        //    DatabaseConnection dbConnection = new DatabaseConnection();
        //    MySqlConnection connection = dbConnection.GetConnection();

        //    if (connection != null)
        //    {
        //        try
        //        {
        //            foreach (WarTally wt in warTallyDictionary.Values)
        //            {
        //                //upload to db
        //                string query = "INSERT INTO WarTally (warTallyID, factionid, tornID, hits, assists, interupts, respectBest, respectBonus, respectGained, respectLost, respectNet, respectEnemyGain, respectEnemyLost, fairFight, retalsOut, retalsIn, defendsWon, defendsInterupt, defendsLost, outsideHits, outsideRespect, outsideLost, outsideDefendsWon, outsideDefendsLost, overseas, energyUsedOut, energyUsedIn, hospd, mugged, attackLeave)" +
        //                               "VALUES (@warTallyID, @factionid, @tornID, @hits, @assists, @interupts, @respectBest, @respectBonus, @respectGained, @respectLost, @respectNet, @respectEnemyGain, @respectEnemyLost, @fairFight, @retalsOut, @retalsIn, @defendsWon, @defendsInterupt, @defendsLost, @outsideHits, @outsideRespect, @outsideLost, @outsideDefendsWon, @outsideDefendsLost, @overseas, @energyUsedOut, @energyUsedIn, @hospd, @mugged, @attackLeave)";
        //                Console.WriteLine(query);

        //                using (var cmd = new MySqlCommand(query, connection))
        //                {
        //                    cmd.Parameters.AddWithValue("@warTallyID", wt.warTallyID);
        //                    cmd.Parameters.AddWithValue("@factionid", wt.factionID);
        //                    cmd.Parameters.AddWithValue("@tornID", wt.tornID);
        //                    cmd.Parameters.AddWithValue("@hits", wt.hits);
        //                    cmd.Parameters.AddWithValue("@assists", wt.assists);
        //                    cmd.Parameters.AddWithValue("@interupts", wt.interupts);
        //                    cmd.Parameters.AddWithValue("@respectBest", wt.respectBest);
        //                    cmd.Parameters.AddWithValue("@respectBonus", wt.respectBonus);
        //                    cmd.Parameters.AddWithValue("@respectGained", wt.respectGained);
        //                    cmd.Parameters.AddWithValue("@respectLost", wt.respectLost);
        //                    cmd.Parameters.AddWithValue("@respectNet", wt.respectNet);
        //                    cmd.Parameters.AddWithValue("@respectEnemyGain", wt.respectEnemyGain);
        //                    cmd.Parameters.AddWithValue("@respectEnemyLost", wt.respectEnemyLost);
        //                    cmd.Parameters.AddWithValue("@fairFight", wt.fairFight);
        //                    cmd.Parameters.AddWithValue("@retalsOut", wt.retalsOut);
        //                    cmd.Parameters.AddWithValue("@retalsIn", wt.retalsIn);
        //                    cmd.Parameters.AddWithValue("@defendsWon", wt.defendsWon);
        //                    cmd.Parameters.AddWithValue("@defendsInterupt", wt.defendsInterupt);
        //                    cmd.Parameters.AddWithValue("@defendsLost", wt.defendsLost);
        //                    cmd.Parameters.AddWithValue("@outsideHits", wt.outsideHits);
        //                    cmd.Parameters.AddWithValue("@outsideRespect", wt.outsideRespect);
        //                    cmd.Parameters.AddWithValue("@outsideLost", wt.outsideLost);
        //                    cmd.Parameters.AddWithValue("@outsideDefendsWon", wt.outsideDefendsWon);
        //                    cmd.Parameters.AddWithValue("@outsideDefendsLost", wt.outsideDefendsLost);
        //                    cmd.Parameters.AddWithValue("@overseas", wt.overseas);
        //                    cmd.Parameters.AddWithValue("@energyUsedOut", wt.energyUsedOut);
        //                    cmd.Parameters.AddWithValue("@energyUsedIn", wt.energyUsedIn);
        //                    cmd.Parameters.AddWithValue("@hospd", wt.hospd);
        //                    cmd.Parameters.AddWithValue("@mugged", wt.mugged);
        //                    cmd.Parameters.AddWithValue("@attackLeave", wt.attackLeave);

        //                    await cmd.ExecuteNonQueryAsync();
        //                }
        //            }

        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"Error inserting into the database: {ex.Message}");
        //            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("An error occurred while pushing wartally to database"));
        //        }
        //        finally
        //        {
        //            dbConnection.CloseConnection(connection);
        //        }
        //    }
        //}
        private async Task UpdateWarTallyToDB(InteractionContext ctx, Dictionary<long, WarTally> warTallyDictionary)
        {
            DatabaseConnection dbConnection = new DatabaseConnection();
            MySqlConnection connection = dbConnection.GetConnection();

            if (connection != null)
            {
                try
                {
                    foreach (WarTally wt in warTallyDictionary.Values )
                    {
                        //upload to db
                        string query = "UPDATE SET WarTally (warTallyID, factionid, tornID, hits, assists, interupts, respectBest, respectBonus, respectGained, respectLost, respectNet, respectEnemyGain, respectEnemyLost, fairFight, retalsOut, retalsIn, defendsWon, defendsInterupt, defendsLost, outsideHits, outsideRespect, outsideLost, outsideDefendsWon, outsideDefendsLost, overseas, energyUsedOut, energyUsedIn, hospd, mugged, attackLeave)" +
                                       "VALUES (@warTallyID, @factionid, @tornID, @hits, @assists, @interupts, @respectBest, @respectBonus, @respectGained, @respectLost, @respectNet, @respectEnemyGain, @respectEnemyLost, @fairFight, @retalsOut, @retalsIn, @defendsWon, @defendsInterupt, @defendsLost, @outsideHits, @outsideRespect, @outsideLost, @outsideDefendsWon, @outsideDefendsLost, @overseas, @energyUsedOut, @energyUsedIn, @hospd, @mugged, @attackLeave)";

                        using (var cmd = new MySqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@warTallyID", wt.warTallyID);
                            cmd.Parameters.AddWithValue("@factionid", wt.factionID);
                            cmd.Parameters.AddWithValue("@tornID", wt.tornID);
                            cmd.Parameters.AddWithValue("@hits", wt.hits);
                            cmd.Parameters.AddWithValue("@assists", wt.assists);
                            cmd.Parameters.AddWithValue("@respectBest", wt.respectBest);
                            cmd.Parameters.AddWithValue("@respectBonus", wt.respectBonus);
                            cmd.Parameters.AddWithValue("@respectGained", wt.respectGained);
                            cmd.Parameters.AddWithValue("@respectLost", wt.respectLost);
                            cmd.Parameters.AddWithValue("@respectNet", wt.respectNet);
                            cmd.Parameters.AddWithValue("@respectEnemyGain", wt.respectEnemyGain);
                            cmd.Parameters.AddWithValue("@respectEnemyLost", wt.respectEnemyLost);
                            cmd.Parameters.AddWithValue("@fairFight", wt.fairFight);
                            cmd.Parameters.AddWithValue("@retalsOut", wt.retalsOut);
                            cmd.Parameters.AddWithValue("@retalsIn", wt.retalsIn);
                            cmd.Parameters.AddWithValue("@defendsWon", wt.defendsWon);
                            cmd.Parameters.AddWithValue("@defendsInterupt", wt.defendsInterupt);
                            cmd.Parameters.AddWithValue("@defendsLost", wt.defendsLost);
                            cmd.Parameters.AddWithValue("@outsideHits", wt.outsideHits);
                            cmd.Parameters.AddWithValue("@outsideRespect", wt.outsideRespect);
                            cmd.Parameters.AddWithValue("@outsideLost", wt.outsideLost);
                            cmd.Parameters.AddWithValue("@outsideDefendsWon", wt.outsideDefendsWon);
                            cmd.Parameters.AddWithValue("@outsideDefendsLost", wt.outsideDefendsLost);
                            cmd.Parameters.AddWithValue("@overseas", wt.overseas);
                            cmd.Parameters.AddWithValue("@energyUsedOut", wt.energyUsedOut);
                            cmd.Parameters.AddWithValue("@energyUsedIn", wt.energyUsedIn);
                            cmd.Parameters.AddWithValue("@hospd", wt.hospd);
                            cmd.Parameters.AddWithValue("@mugged", wt.mugged);
                            cmd.Parameters.AddWithValue("@attackLeave", wt.attackLeave);

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error inserting into the database: {ex.Message}");
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("An error occurred while updating wartally to database"));
                }
                finally
                {
                    dbConnection.CloseConnection(connection);
                }
            }
        }

    }
}
