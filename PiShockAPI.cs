using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace PiShock
{
    internal class PiShockAPI
    {
        public string username { private get; set; }
        public string apiKey { private get; set; }
        public string code { private get; set; }
        public string senderName { private get; set; }

        private string apiEndpoint = "https://do.pishock.com/api/apioperate/";

        public async Task Shock(int intensity, int duration)
        {
            using (HttpClient client = new HttpClient())
            {
                // Data request
                var requestData = new
                {
                    Username = username,
                    Name = senderName,
                    Code = code,
                    Intensity = intensity,
                    Duration = duration,
                    APIKey = apiKey,
                    Op = 0
                };

                // Serialize the request data to JSON
                string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);

                // Create StringContent with the correct content type
                using (HttpContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json"))
                {
                    // Send POST request
                    HttpResponseMessage response = await client.PostAsync(apiEndpoint, content);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Request sent successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");

                        string responseContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Response Content: {responseContent}");
                    }
                }
            }
        }

        public async Task Vibrate(int intensity, int duration)
        {
            using (HttpClient client = new HttpClient())
            {
                // Request data
                var requestData = new
                {
                    Username = username,
                    Name = senderName,
                    Code = code,
                    Intensity = intensity,
                    Duration = duration,
                    APIKey = apiKey,
                    Op = 0
                };

                // Serializze request data to JSON
                string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);

                // Create StringContent with the correct content type
                using (HttpContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json"))
                {
                    // Send POST request
                    HttpResponseMessage response = await client.PostAsync(apiEndpoint, content);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Request sent successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"Error: {response.StatusCode} = {response.ReasonPhrase}");

                        string responseContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Response Content: {responseContent}");
                    }
                }
            }
        }

        public async Task Beep(int duration)
        {
            using (HttpClient client = new HttpClient())
            {
                var requestData = new
                {
                    // Request data
                    Username = username,
                    Name = senderName,
                    Code = code,
                    Intensity = duration,
                    APIKey = apiKey,
                    Op = 0
                };

                // Serialize request data to JSON
                string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);

                // Create StringContent with the correct content type
                using (HttpContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json"))
                {
                    HttpResponseMessage response = await client.PostAsync(apiEndpoint, content);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Request sent successfully");
                    }
                    else
                    {
                        Console.WriteLine($"Error: {response.StatusCode} = {response.ReasonPhrase}");

                        string responseContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Response Content: {responseContent}");
                    }
                }
            }
        }
    }
}
