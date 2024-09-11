using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TornWarTracker.Data_Creation
{
    public class WarData
    {
        public static async Task<MemoryStream> WriteMembersToMemoryStream(JObject enemyfactionBasic)
        {
            var members = (JObject)enemyfactionBasic["members"];
            var memoryStream = new MemoryStream();
            using (StreamWriter writer = new StreamWriter(memoryStream))
            {
                foreach (var member in members.Properties())
                {
                    string memberId = member.Name;
                    var memberDetails = member.Value;
                    string memberName = (string)memberDetails["name"];
                    int memberLevel = (int)memberDetails["level"];
                    long lastActionTimestamp = (long)memberDetails["last_action"]["timestamp"];

                    DateTime dateTimeStart = DateTimeOffset.FromUnixTimeSeconds(lastActionTimestamp).DateTime;
                    DateTime dateTimeCurrent = DateTime.UtcNow; // Assuming current time is UTC
                    TimeSpan timeDifference = dateTimeCurrent - dateTimeStart;

                    string lastactionTime = $"{timeDifference.Days} days, {timeDifference.Hours} hours, {timeDifference.Minutes} minutes, {timeDifference.Seconds} seconds";

                    await writer.WriteLineAsync($"Member ID: {memberId}");
                    await writer.WriteLineAsync($"Name: {memberName}");
                    await writer.WriteLineAsync($"Level: {memberLevel}");
                    await writer.WriteLineAsync($"Last Action Time: {lastactionTime}");
                    await writer.WriteLineAsync(); // Blank line for separation
                }
            }
            memoryStream.Position = 0; // Reset the position to the beginning of the stream
            return memoryStream;
        }
    }
}
