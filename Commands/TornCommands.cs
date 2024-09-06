using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace TornWarTracker.Commands
{
    public class TornCommands : BaseCommandModule
    {
        [Command("Hi")]
        [Cooldown(5,360,CooldownBucketType.User)]
        public async Task TestCommand(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync($"Hello {ctx.User.Username}");

        }

    }
}
