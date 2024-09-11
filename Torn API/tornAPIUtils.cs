using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TornWarTracker.Torn_API
{
    public class tornAPIUtils
    {
        public static class ErrorCodes
        {
            public static readonly Dictionary<int, string> ErrorDescriptions = new Dictionary<int, string>
    {
        { 0, "Unknown error: Unhandled error, should not occur." },
        { 1, "Key is empty: Private key is empty in current request." },
        { 2, "Incorrect Key: Private key is wrong/incorrect format." },
        { 3, "Wrong type: Requesting an incorrect basic type." },
        { 4, "Wrong fields: Requesting incorrect selection fields." },
        { 5, "Too many requests: Requests are blocked for a small period of time because of too many requests per user (max 100 per minute)." },
        { 6, "Incorrect ID: Wrong ID value." },
        { 7, "Incorrect ID-entity relation: You are not permitted to access this information." },
        { 8, "IP block: Current IP is banned for a small period of time because of abuse." },
        { 9, "API disabled: Api system is currently disabled." },
        { 10, "Key owner is in federal jail: Current key can't be used because owner is in federal jail." },
        { 11, "Key change error: You can only change your API key once every 60 seconds." },
        { 12, "Key read error: Error reading key from Database." },
        { 13, "The key is temporarily disabled due to owner inactivity: The key owner hasn't been online for more than 7 days." },
        { 14, "Daily read limit reached: Too many records have been pulled today from our cloud services." },
        { 15, "Temporary error: An error code specifically for testing purposes that has no dedicated meaning." },
        { 16, "Access level of this key is not high enough: A selection is being called of which this key does not have permission to access." },
        { 17, "Backend error occurred, please try again." },
        { 18, "API key has been paused by the owner." },
        { 19, "Must be migrated to crimes 2.0." },
        { 20, "Race not yet finished." },
        { 21, "Incorrect category: Wrong cat value." }
    };
        }

        public static async Task APIErrorReporting(JObject jsonData, InteractionContext ctx = null)
        {
            // Extract the error code from the JSON data
            int errorCode = (int)jsonData["error"]["code"];

            // Look up the error message in the ErrorCodes dictionary
            string errorMessage = ErrorCodes.ErrorDescriptions.ContainsKey(errorCode)
                ? ErrorCodes.ErrorDescriptions[errorCode]
                : "Unknown error code.";

            // If the context is provided, send a response with the error message
            if (ctx != null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .WithContent($"Error {errorCode}: {errorMessage}"));
            }
        }

        public class Checks
        {

        }

        public class User
        {
            public static async Task<int> GetFactionID(InteractionContext ctx, long TornID, string TornApiKey)
            {
                string apiUrl = $"https://api.torn.com/user/{TornID}?key={TornApiKey}";
                //string apiUrl = $"https://api.torn.com/user/?selections=profile&key={TornApiKey}";
                string jsonResponse = await requestAPI.GetFrom(apiUrl);

                if (jsonResponse == null)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Invalid API Key."));
                    return 0;
                }

                // Parse the JSON response
                var jsonData = JObject.Parse(jsonResponse);
                if (jsonData["error"] != null)
                {
                    await APIErrorReporting(jsonData, ctx);
                    return 0;
                }

                int factionId = (int)jsonData["faction"]["faction_id"];

                if (factionId == 0)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You are not in a faction. Service terminated."));
                    return 0;
                }
                Console.WriteLine($"Faction ID: {factionId}");
                return (int)factionId;
            }
        }

        public class Faction
        {
            public static async Task<JObject> GetAttacks(InteractionContext ctx, string TornApiKey, int factionID)
            {
                string apiUrl = $"https://api.torn.com/faction/{factionID}?selections=attacks&key={TornApiKey}";
                string jsonResponse = await requestAPI.GetFrom(apiUrl);

                if (jsonResponse == null)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
            .WithContent("Invalid API Request."));
                    return null;
                }

                // Parse the JSON response
                var jsonData = JObject.Parse(jsonResponse);

                // Check for error in the response
                if (jsonData["error"] != null)
                {
                    await APIErrorReporting(jsonData,ctx);
                    return null;
                }

                return jsonData;
            }

            public static async Task<JObject> BasicData(string TornApiKey,int factionID )
            {
                string apiUrl = $"https://api.torn.com/faction/{factionID}?selections=&key={TornApiKey}";
                string jsonResponse = await requestAPI.GetFrom(apiUrl);

                if (jsonResponse == null)
                {
                    return null;
                }

                // Parse the JSON response
                var jsonData = JObject.Parse(jsonResponse);

                // Check for error in the response
                if (jsonData["error"] != null)
                {
                    await APIErrorReporting(jsonData);
                    return null;
                }

                return jsonData;
            }
        }

        public class Torn
        {
            public static async Task<long> GetCurrentTimeStamp(string TornApiKey)
            {
                string apiUrl = $"https://api.torn.com/torn/?selections=timestamp&key={TornApiKey}";
                string jsonResponse = await requestAPI.GetFrom(apiUrl);

                Console.WriteLine(jsonResponse);

                if (jsonResponse == null)
                {
                    return 0;
                }


                // Parse the JSON response
                var jsonData = JObject.Parse(jsonResponse);
                if (jsonData["error"] != null)
                {
                    return 0;
                }

                // Extract the timestamp value
                long timestamp = jsonData["timestamp"].Value<long>();                

                return timestamp;
            }
        }

    }
}
