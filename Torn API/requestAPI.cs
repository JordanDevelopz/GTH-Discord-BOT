using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TornWarTracker.Torn_API
{
    public class requestAPI
    {

        public static async Task<string> GetFrom(string url)
        {
            try
            {
                string apiResponse = await GetApiResponse(url);
                Debug.WriteLine(apiResponse); // Print the response from the API
                return apiResponse;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
            }
            return null;
        }




        public static async Task<string> GetApiResponse(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception($"API request failed with status code: {response.StatusCode}");
                }
            }
        }
    }
}
