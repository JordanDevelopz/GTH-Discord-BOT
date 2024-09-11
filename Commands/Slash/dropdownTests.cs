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
    public class dropdownTests : ApplicationCommandModule
    {
        [SlashCommand("DropdownTest","Test dropdowns")]
        public async Task DropDownList(InteractionContext ctx)
        {

            List<DiscordSelectComponentOption> optionsList = new List<DiscordSelectComponentOption>();
            optionsList.Add(new DiscordSelectComponentOption("Option 1", "option1"));
            optionsList.Add(new DiscordSelectComponentOption("Option 2", "option2"));
            optionsList.Add(new DiscordSelectComponentOption("Option 3", "option3"));

            var options = optionsList.AsEnumerable();

            var dropDown = new DiscordSelectComponent("dropdownlist","Select...", options);

            var dropdownmsg = new DiscordMessageBuilder()
                .AddEmbed(new DiscordEmbedBuilder().WithColor(DiscordColor.Gold)
                .WithTitle("Test Dropdown"))
                .AddComponents(dropDown);

            await ctx.Channel.SendMessageAsync(dropdownmsg);
            await ctx.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Thanks!").AsEphemeral(true));
        }
    }
}
