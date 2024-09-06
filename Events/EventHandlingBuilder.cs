using DSharpPlus.EventArgs;
using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TornWarTracker.Events
{
    public class EventHandlingBuilder
    {
        public class MessageHandler
        {
            public async Task _discord_MessageCreated(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs args)
            {
                string specificText = "Hey, War tracker";
                var responses = new Dictionary<string, string>
    {
        { "l4leezus", "Hey, Dad!" },
        { "alaska2116", "Hey, it's my Uncle, Alaska!" },
        { "jpb5537", "Hold up, it's the Boss. Hey JB!" },
        { "theslimp", "Shhh, a founder is speaking. Hey Slimp" },
        { "itsmrd", "Hey, Dank.. Or should I say; Mr capital D!" },
        { "shortcircuit4029", "Some call him Obi, but I wasn't born then. Hey Haakon!" },
        { "mahmoudbakis29208224063", "Mr Lucky has entered the chat, Howdy!" },
        { "turbo9393", "The council has spoken. Hey Turbo!" },
        { "wookies1373", "Hey Wooks. Mind where you put that sword!" },
        { "bobbia_72614", "If it isn't the master of max jumps. Hey Bobbia!" }
    };

                if (args.Message.Content.IndexOf(specificText, StringComparison.OrdinalIgnoreCase) >= 0 && responses.ContainsKey(args.Author.Username))
                {
                    await args.Message.RespondAsync(responses[args.Author.Username]);
                }
            }
        }
    }
}
