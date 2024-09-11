using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TornWarTracker.Commands.Slash
{
    public class  modalTest : ApplicationCommandModule

    {
        [SlashCommand("modalTest", "Show form")]
        public async Task Modal(InteractionContext ctx)
        {
            var modal = new DiscordInteractionResponseBuilder()
                .WithTitle("Test Form")
                .WithCustomId("modaltest")
                .AddComponents(new TextInputComponent(label: "Label","textbox1","Type stuff here"));

            await ctx.CreateResponseAsync(InteractionResponseType.Modal, modal);

        }

    }
}
