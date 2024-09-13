using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using MySql.Data.MySqlClient;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TornWarTracker.Data_Structures.tornDataStructures.warDataStructures.FactionRankedWars;
using static TornWarTracker.Torn_API.tornAPIUtils;

namespace TornWarTracker.DB
{
    public class DBUtils
    {
        public static async Task<string> GetAPIKey(string discordID, MySqlConnection connection)
        {
            // Query the database to get the API key linked with the user's Discord ID
            string query_apikey = "SELECT Torn_API FROM members WHERE discord_id = @DiscordID";
            using (var cmd = new MySqlCommand(query_apikey, connection))
            {
                cmd.Parameters.AddWithValue("@DiscordID", discordID);

                var apiKeyResult = await cmd.ExecuteScalarAsync();
                if (apiKeyResult == null)
                {                    
                    return null;
                }

                return apiKeyResult.ToString();
            }
        }

        public static async Task<long> GetTornID(string discordID, MySqlConnection connection)
        {
            // Query the database to get the TORNID  linked with the user's Discord ID
            string query_tornID = "SELECT Torn_ID FROM members WHERE discord_id = @DiscordID";
            using (var cmd = new MySqlCommand(query_tornID, connection))
            {
                cmd.Parameters.AddWithValue("@DiscordID", discordID);

                var tornIDResult = await cmd.ExecuteScalarAsync();
                if (tornIDResult == null)
                {                    
                    return 0;
                }

                return Convert.ToInt64(tornIDResult);
            }
        }

        public static async Task<int> GetfactionID( string discordID, MySqlConnection connection)
        {
            // Query the database to get the factionID linked with the user's Discord ID
            //string query_factionID = "SELECT faction_id FROM members WHERE discord_id = @DiscordID";
            string query_factionID = "SELECT members.faction_id FROM members WHERE members.discord_id = @DiscordID";
            using (var cmd = new MySqlCommand(query_factionID, connection))
            {
                cmd.Parameters.AddWithValue("@DiscordID", discordID);

                var factionIDResult = await cmd.ExecuteScalarAsync();
                if (factionIDResult == null)
                {                    
                    return 0;
                }

                return Convert.ToInt32(factionIDResult);
            }
        }

        //public static async Task<string> GetfactionName(string discordID, MySqlConnection connection)
        //{
        //    // Query the database to get the factionName linked with the faction ID
        //    string query_factionName = @"SELECT factions.faction_name FROM factions WHERE factions.faction_id = @DiscordID";
        //    using (var cmd = new MySqlCommand(query_factionName, connection))
        //    {
        //        cmd.Parameters.AddWithValue("@DiscordID", discordID);

        //        var factionNameResult = await cmd.ExecuteScalarAsync();
        //        if (factionNameResult == null)
        //        {
        //            return null;
        //        }

        //        return factionNameResult.ToString();
        //    }
        //}

        public static async Task<bool> VerifyPayment(int factionId, MySqlConnection connection)
        {
            string checkPaymentQuery = "SELECT payment_received FROM factions WHERE faction_id = @FactionID LIMIT 1";
            using (var cmd = new MySqlCommand(checkPaymentQuery, connection))
            {
                cmd.Parameters.AddWithValue("@FactionID", factionId);

                var paymentStatus = await cmd.ExecuteScalarAsync();

                if (paymentStatus == null || !(bool)paymentStatus)
                {
                    return false;
                }
                else { return true; }
            }
        }
    }
}
