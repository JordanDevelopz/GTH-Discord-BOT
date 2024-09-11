using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TornWarTracker.Commands.Slash;

namespace TornWarTracker.War
{
    public class WarTracking
    {
        private ConcurrentDictionary<ulong, bool> _warTrackerRunning;

        public WarTracking(ConcurrentDictionary<ulong, bool> warTrackerRunning)
        {
            _warTrackerRunning = warTrackerRunning;
        }

        public async Task Tracker(InteractionContext ctx,long warStartTime)
        {

            // Create a HashSet of attackID
            HashSet<long> attackIDSet = new HashSet<long>();

            

            ////perform loop until current time = starttime
            //while (true)
            //{
            //    //get current time in epoch
            //    DateTimeOffset currentTime = DateTimeOffset.UtcNow;
            //    // Convert to Unix epoch time
            //    long unixTime = currentTime.ToUnixTimeSeconds();

            //    long timeUntilTarget = warStartTime - unixTime;

            //    if (timeUntilTarget <= 0)
            //    {
            //        Console.WriteLine("Performing the task at: " + DateTime.UtcNow);
            //        break;
            //    }
            //}

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"War Tracker has begun recording, <@{ctx.User.Id}>."));






            _warTrackerRunning[ctx.Guild.Id] = false;


        }


    }
}
