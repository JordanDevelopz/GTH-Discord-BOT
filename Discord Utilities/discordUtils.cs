using DSharpPlus.Entities;
using DSharpPlus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TornWarTracker.Discord_Utilities
{
    public class discordUtils
    {
        public static async Task UploadMemoryStreamToDiscord(DiscordClient client, DiscordChannel channel, MemoryStream memoryStream, string fileName)
        {
            var messageBuilder = new DiscordMessageBuilder()
                .WithContent("Here is the member data:")
                .AddFile(fileName, memoryStream);

            await channel.SendMessageAsync(messageBuilder);
        }
    }
}
