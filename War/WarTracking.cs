using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TornWarTracker.Commands.Slash;
using TornWarTracker.Torn_API;
using static TornWarTracker.Data_Structures.tornDataStructures;

namespace TornWarTracker.War
{
    public class WarTracking
    {
        public static class WarTrackerState
        {
            public static ConcurrentDictionary<int, bool> WarTrackerRunning = new ConcurrentDictionary<int, bool>();
        }


        public async Task Tracker(InteractionContext ctx, string apiKey, int factionID,int enemyFactionID,JObject members, long warStartTime)

        {

            // Create a HashSet of attackID
            HashSet<long> attackIDSet = new HashSet<long>();


            //perform loop until current time = starttime
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"War Tracker is actively waiting for the start of War. I'll let you know when it starts! <@{ctx.User.Id}>."));

            bool updateTriggered = false;

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

                bool update = false;
                int delay;
                if (timeUntilTarget >= 86400) // More than 1 day
                {
                    delay = 21600000; // 6 hour delay
                }
                else if (timeUntilTarget > 3600)  // More than 1 hour but less than 1 day
                {
                    delay = 3600000; // 1 hour delay
                }
                else if (timeUntilTarget > 600) // More than 10 minutes but less than 1 hour
                {
                    delay = 60000; // 10 min delay
                }
                else if (timeUntilTarget > 60) // More than 1 minutes but less than 10 mins
                {
                    update = true;
                    delay = 60000; // 1 min delay
                }
                else
                {
                    delay = 1000; // 1 second delay
                }
                                

                Console.WriteLine($"Waiting for {delay / 1000} seconds. Time until target: {timeUntilTarget} seconds.");
                await Task.Delay(delay).ContinueWith(t =>
                {
                    if (!updateTriggered && update)
                    {
                        updateTriggered = true;
                        //make embed for time updates
                        var embedSuccess = new DiscordEmbedBuilder
                        {
                            Title = "WAR TRACKER",
                            Description = "Your war starts soon!",
                            Color = DiscordColor.DarkRed
                        };
                        embedSuccess.AddField("Start Time: ", DateTimeOffset.FromUnixTimeSeconds(timeUntilTarget).DateTime.ToString());

                        ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embedSuccess));

                    }
                    Console.WriteLine("War Tracker Tick");
                });
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
                    warTallyDictionary.Add(tornid,warTal);
                }
            }

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
                        Attacks factionAttacks = await tornAPIUtils.Faction.GetAttacksAsFactionAttacks(ctx,apiKey, factionID);
                        if (factionAttacks != null)
                        {
                            //do the main tally tasks
                            WarTallies( factionAttacks,warStartTime, endTime, enemyFactionID,ref lastAttackTimeFromPrevious, ref warTallyDictionary);
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

                await Task.Delay(2000);
                //await Task.Delay(2000).ContinueWith(t =>
                //{
                //    Console.WriteLine("2 seconds have passed");
                //});

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
                //if war endtime !=0, proceed to next step
                if (warEndtime != 0)
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
                                        wt.Assists += 1;                                        
                                    }
                                    break;
                                case "Attacked":
                                    wt.energyUsedOut += 25;
                                    if (atk.DefenderFaction == enemyFactionID)
                                    {
                                        wt.Hits += 1;
                                        wt.respectGained += atk.RespectGain;
                                        wt.respectBonus += atk.Modifiers.ChainBonus;
                                        wt.fairFight += atk.Modifiers.FairFight;
                                        wt.retalsOut += (atk.Modifiers.Retaliation > 1) ? 1 : 0;
                                        wt.overseas += (atk.Modifiers.Overseas > 1) ? 1 : 0;
                                        wt.leave += 1;
                                        if (atk.RespectGain > wt.respectBest)
                                        {
                                            wt.respectBest = atk.RespectGain;
                                        }
                                    }
                                    else
                                    {
                                        wt.outsideHits += 1;
                                        wt.leave += 1;
                                    }
                                    break;
                                case "Mugged":
                                    wt.energyUsedOut += 25;
                                    if (atk.DefenderFaction == enemyFactionID)
                                    {
                                        wt.Hits += 1;
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
                                        wt.Hits += 1;
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
                                        wt.leave += 1;
                                    }
                                    break;
                            }


                            warTallyDictionary[atk.AttackerId] = wt;
                        }

                    }

                }

            }
        }




    }
}
