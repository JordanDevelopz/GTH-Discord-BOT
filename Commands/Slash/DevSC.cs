using DSharpPlus.Entities;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TornWarTracker.Torn_API.tornAPIUtils;
using TornWarTracker.Torn_API;

namespace TornWarTracker.Commands.Slash
{
    [SlashCommandGroup("Dev", "developer only related tasks")]
    public class DevSC : ApplicationCommandModule
    {
        [SlashCommand("authenticate_faction", "Authenticate a faction and mark them as paid.")]
        [SlashCommandPermissions(Permissions.Administrator)]  // Ensures only admins can use this command
        public async Task AuthenticateFactionCommand(InteractionContext ctx,
    [Option("FactionID", "Enter the Faction ID")] long FactionID,
    [Option("FactionName", "Enter the Faction Name")] string FactionName,
    [Option("PaymentReceived", "Payment received status (true/false)")] bool PaymentReceived)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            DatabaseConnection dbConnection = new DatabaseConnection();
            MySqlConnection connection = dbConnection.GetConnection();

            if (connection != null)
            {
                try
                {
                    // Step 1: Insert or update faction information
                    string query = @"
                        INSERT INTO factions (faction_id, faction_name, payment_received)
                        VALUES (@FactionID, @FactionName, @PaymentReceived)
                        ON DUPLICATE KEY UPDATE
                        faction_name = @FactionName, payment_received = @PaymentReceived;";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@FactionID", FactionID);
                        cmd.Parameters.AddWithValue("@FactionName", FactionName);
                        cmd.Parameters.AddWithValue("@PaymentReceived", PaymentReceived);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    // Step 2: Send a success message with details
                    var embedSuccess = new DiscordEmbedBuilder
                    {
                        Title = "Faction Authentication",
                        Description = $"Faction {FactionName} has been authenticated.",
                        Color = DiscordColor.Green
                    };
                    embedSuccess.AddField("Faction ID", FactionID.ToString());
                    embedSuccess.AddField("Faction Name", FactionName);
                    embedSuccess.AddField("Payment Received", PaymentReceived ? "Yes" : "No");

                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(embedSuccess.Description).AsEphemeral(true));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error inserting/updating faction payment: {ex.Message}");
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("An error occurred while authenticating the faction. Please try again."));
                }
                finally
                {
                    dbConnection.CloseConnection(connection);
                }
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Database connection failed. Please try again later."));
            }
        }




    }
}
