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

        public async Task Tracker(InteractionContext ctx)
        {

            try
            {
                // Simulate long-running task
                for (int i = 0; i < 2; i++)
                {
                    Console.WriteLine($"War tracking iteration {i + 1}");
                    await Task.Delay(TimeSpan.FromSeconds(10)); 
                 // Perform your data gathering and database posting here

                }

                //await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("War Tracker has completed."));
            }
            finally
            {
                _warTrackerRunning[ctx.Guild.Id] = false;
            }
        }
    }
}
