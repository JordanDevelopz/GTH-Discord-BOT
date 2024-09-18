using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TornWarTracker.DB
{

    
    public class GS_DBUtils
    {
        public static readonly string masterSheetID = "1rtJmWG6g6SZ37MDV2kzI_ysOtMVV2XvKM0Z-sinxLyo";
        public static readonly string factionDataSheet = "faction_data";
        public static readonly string memberSheet = "member_data";
        public static async Task<int> GetFactionID(string discordID, SheetsService service)
        {
            // Define the range to read the entire column.
            string range = $"{memberSheet}!A:A";

            SpreadsheetsResource.ValuesResource.GetRequest request =
        service.Spreadsheets.Values.Get(masterSheetID, range);

            // Execute the request asynchronously.
            ValueRange response = await request.ExecuteAsync();
            IList<IList<object>> values = response.Values;

            if (values != null && values.Count > 0)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    if (values[i].Count > 0 && values[i][0].ToString() == discordID)
                    {
                        // Define the range to read the return column in the found row.
                        string returnRange = $"{memberSheet}!C{i + 1}:C{i + 1}";
                        SpreadsheetsResource.ValuesResource.GetRequest returnRequest =
                            service.Spreadsheets.Values.Get(masterSheetID, returnRange);

                        // Execute the request asynchronously.
                        ValueRange returnResponse = await returnRequest.ExecuteAsync();
                        IList<IList<object>> returnValues = returnResponse.Values;

                        if (returnValues != null && returnValues.Count > 0 && returnValues[0].Count > 0)
                        {
                            if (int.TryParse(returnValues[0][0].ToString(), out int factionID))
                            {
                                return factionID;
                            }
                        }
                    }
                }
            }

            // Return null if the value is not found.
            return 0;
        }

        public static async Task<string> GetAPIKey(string discordID, SheetsService service)
        {
            // Define the range to read the entire column.
            string range = $"{memberSheet}!A:A";

            SpreadsheetsResource.ValuesResource.GetRequest request =
        service.Spreadsheets.Values.Get(masterSheetID, range);

            // Execute the request asynchronously.
            ValueRange response = await request.ExecuteAsync();
            IList<IList<object>> values = response.Values;

            if (values != null && values.Count > 0)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    if (values[i].Count > 0 && values[i][0].ToString() == discordID)
                    {
                        // Define the range to read the return column in the found row.
                        string returnRange = $"{memberSheet}!F{i + 1}:F{i + 1}";
                        SpreadsheetsResource.ValuesResource.GetRequest returnRequest =
                            service.Spreadsheets.Values.Get(masterSheetID, returnRange);

                        // Execute the request asynchronously.
                        ValueRange returnResponse = await returnRequest.ExecuteAsync();
                        IList<IList<object>> returnValues = returnResponse.Values;

                        if (returnValues != null && returnValues.Count > 0 && returnValues[0].Count > 0)
                        {
                            return returnValues[0][0].ToString();
                        }
                    }
                }
            }

            // Return null if the value is not found.
            return null;
        }

        public static async Task<long> GetTornID(string discordID, SheetsService service)
        {
            // Define the range to read the entire column.
            string range = $"{memberSheet}!A:A";

            SpreadsheetsResource.ValuesResource.GetRequest request =
        service.Spreadsheets.Values.Get(masterSheetID, range);

            // Execute the request asynchronously.
            ValueRange response = await request.ExecuteAsync();
            IList<IList<object>> values = response.Values;

            if (values != null && values.Count > 0)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    if (values[i].Count > 0 && values[i][0].ToString() == discordID)
                    {
                        // Define the range to read the return column in the found row.
                        string returnRange = $"{memberSheet}!D{i + 1}:D{i + 1}";
                        SpreadsheetsResource.ValuesResource.GetRequest returnRequest =
                            service.Spreadsheets.Values.Get(masterSheetID, returnRange);

                        // Execute the request asynchronously.
                        ValueRange returnResponse = await returnRequest.ExecuteAsync();
                        IList<IList<object>> returnValues = returnResponse.Values;

                        if (returnValues != null && returnValues.Count > 0 && returnValues[0].Count > 0)
                        {
                            if (long.TryParse(returnValues[0][0].ToString(), out long tornID))
                            {
                                return tornID;
                            }
                        }
                    }
                }
            }

            // Return null if the value is not found.
            return 0;
        }

        public static async Task WriteSheet(String spreadsheetId, string sheetName, string range, string value)
        {
            try
            {
                using (var googleSheetsService = new GoogleSheetsService())
                {
                    var service = googleSheetsService.GetService();

                    // Define request parameters.
                    String searchRange = $"{sheetName}!" + range; // "Info!F3";

                    // Create the data to be written.
                    var valueRange = new ValueRange();
                    var oblist = new List<IList<object>> { new List<object> { value } };
                    valueRange.Values = oblist;

                    // Create the update request.
                    var updateRequest = service.Spreadsheets.Values.Update(valueRange, spreadsheetId, searchRange);
                    updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;

                    // Execute the update request.
                    var updateResponse = await updateRequest.ExecuteAsync();
                    Debug.WriteLine("Updated");
                }
            }
            catch (Google.GoogleApiException e)
            {
                Debug.WriteLine("Google API Exception: " + e.Message);
                // Additional logging or handling based on the exception details
            }
            catch (Exception e)
            {
                Debug.WriteLine("General Exception: " + e.Message);
                // Additional logging or handling based on the exception details
            }
        }

        public static async Task<string> ReadSheet(String spreadsheetId, string sheetName, string range)
        {
            using (var googleSheetsService = new GoogleSheetsService())
            {
                var service = googleSheetsService.GetService();

                // Define request parameters.
                String searchRange = $"{sheetName}!" + range; // "Info!F3";

                SpreadsheetsResource.ValuesResource.GetRequest request =
                    service.Spreadsheets.Values.Get(spreadsheetId, searchRange);

                // Prints the names and majors of students in a sample spreadsheet:
                ValueRange response = await request.ExecuteAsync();
                IList<IList<Object>> values = response.Values;
                if (values != null && values.Count > 0)
                {
                    // Print the value in cell F3.
                    Debug.WriteLine("Value in F3: {0}", values[0][0]);
                    return values[0][0].ToString();
                }
                else
                {
                    Debug.WriteLine("No data found.");
                    return null;
                }
            }
        }
    }
}
