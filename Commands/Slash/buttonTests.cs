using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using System;
using System.Threading.Tasks;

namespace TornWarTracker.Commands.Slash
{
    public class buttonTests : ApplicationCommandModule
    {
        [SlashCommand("test", "Use to test")]
        public async Task testcommand(InteractionContext ctx)
        {
            var interactivity = Program._discord.GetInteractivity();

            //ask user if they want to start war tracker
            var yesButton = new DiscordButtonComponent(ButtonStyle.Success, "yes", "Yes");
            var noButton = new DiscordButtonComponent(ButtonStyle.Danger, "no", "No");

            var messageBuilder = new DiscordInteractionResponseBuilder()
                .WithContent("Do you want War Tracker to start automatically when the war starts?")
                .AddComponents(yesButton, noButton)
                .AsEphemeral(true);

            // Send the message with buttons
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, messageBuilder);

            var sentMessage = await ctx.GetOriginalResponseAsync();

            var result = await interactivity.WaitForButtonAsync(sentMessage, ctx.User, TimeSpan.FromMinutes(1));

            // Check if a button was clicked and handle the result
            if (result.TimedOut)
            {
                
                return;
            }

            // Handle which button was pressed
            if (result.Result.Id == "yes")
            {
                Console.Write("YES WAS PRESSED");
                // Logic for when the user presses "Yes"

                await sentMessage.RespondAsync("YES WAS PRESSED");
            }
            else if (result.Result.Id == "no")
            {
                Console.Write("NO WAS PRESSED");
                // Logic for when the user presses "No"

                await sentMessage.RespondAsync("NO WAS PRESSED");
            }

        }


    }
}
