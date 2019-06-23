using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IoTRynningeasenFA
{
    public static class MessageBroker
    {
        [FunctionName("IoTMessageBroker")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestMessage req, TraceWriter log)
        {
            var configuration = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);

            log.Info("C# HTTP IoT Messagebroker function processed a request.");

            var httpClient = new HttpClient();

            var requestBody = req.Content.ReadAsStringAsync().Result;
            log.Info($"Request body: {requestBody}");
            log.Info($"Debug: {configuration["iot-www-api-location"]}");
            log.Info($"Debug: {configuration["iot-www2-api-location"]}");

            // For debug purposes...
            if (requestBody.ToLowerInvariant().Contains("debug"))
            {
                log.Info("Debug: Returning with noop");
                var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = 
                        new ObjectContent(typeof(object), JsonConvert.DeserializeObject(requestBody), new JsonMediaTypeFormatter())
                };

                return httpResponseMessage;
            }
            // End for debug purposes...

            var data = JsonConvert.DeserializeObject<dynamic[]>(requestBody);

            foreach (var o in data)
            {
                var name = o.Name.ToString();
                var value = (double) o.Value;

                var route = "temperature";

                if (name.Contains(":pressure"))
                {
                    route = "pressure";
                }
                else if(name.Contains(":humidity"))
                {
                    route = "humidity";
                }

                await httpClient.PostAsync(
                    $"{configuration["iot-www-api-location"]}/{route}",
                    new StringContent(JsonConvert.SerializeObject(o), System.Text.Encoding.UTF8, "application/json"));
                
                await httpClient.PostAsync(
                    $"{configuration["iot-www2-api-location"]}/{route}",
                    new StringContent(JsonConvert.SerializeObject(o), System.Text.Encoding.UTF8, "application/json"));

                var channelKey = configuration["ts-ck-temperature"];
                switch (route)
                {
                    case "pressure":
                        channelKey = configuration["ts-ck-pressure"];
                        break;
                    case "humidity":
                        channelKey = configuration["ts-ck-humidity"]; 
                        break;
                }

                log.Info($"{name}: {value}");

                var field = int.Parse(name.Split(':')[1]) - 100;
                var fieldValue = $"&field{field}=" + value;

                await httpClient.GetAsync($"https://api.thingspeak.com/update?api_key={channelKey}{fieldValue}");
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}
