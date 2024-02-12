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

        public async Task SendHttp(int op, int intensity, int duration)
        {
            using (HttpClient client = new HttpClient())
            {
                var requestData = new
                {
                    Username = username,
                    Name = senderName,
                    Code = code,
                    Intensity = intensity,
                    Duration = duration,
                    APIKey = apiKey,
                    Op = op
                };
                var requestDataBeep = new
                {
                    Username = username,
                    Name = senderName,
                    Code = code,
                    Duration = duration,
                    APIKey = apiKey,
                    Op = op
                };

                string jsonBody = "";
                string operation = "";

                if (op == 0) //shock
                {
                    jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);
                    operation = "shock";

                }
                else if (op == 1) // vib
                {
                    jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);
                    operation = "vibrate";
                }
                else if (op == 2) // beep
                {
                    jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestDataBeep);
                    operation = "beep";
                }

                using (HttpContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json"))
                {
                    // Send POST request
                    HttpResponseMessage response = await client.PostAsync(apiEndpoint, content);

                    if (response.IsSuccessStatusCode)
                    {
                        // Send Sucess back to Plugin.cs to handle logging
                        PiShockPlugin.Instance.OnSuccess(operation, intensity, duration);
                    }
                    else
                    {
                        // Send Error back to Plugin.cs to handle logging
                        string responseContent = await response.Content.ReadAsStringAsync();
                        PiShockPlugin.Instance.OnError(response.StatusCode, response.ReasonPhrase, responseContent);
                    }
                }
            }

        }

        public async Task Shock(int intensity, int duration)
        {
            await SendHttp(0, intensity, duration);
        }

        public async Task Vibrate(int intensity, int duration)
        {
            await SendHttp(1, intensity, duration);
        }

        public async Task Beep(int duration)
        {
            await SendHttp(2, 0, duration);
        }
    }
}
