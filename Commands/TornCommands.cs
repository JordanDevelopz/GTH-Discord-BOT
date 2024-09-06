using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;

namespace TornWarTracker.Commands
{
    public class TornCommands : BaseCommandModule
    {
        [Command("Hi")]
        public async Task TestCommand(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync($"Hello {ctx.User.Username}");

        }

    }
}
