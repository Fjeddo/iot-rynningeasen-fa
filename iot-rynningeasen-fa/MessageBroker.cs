using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

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

            var data = JsonConvert.DeserializeObject<dynamic[]>(requestBody);

            foreach (var o in data)
            {
                var name = o.Name.ToString();
                var value = (double) o.Value;

                var route = name.Contains(":temp") ? "temperature" : "pressure";

                await httpClient.PostAsync(
                    $"{configuration["iot-www-api-location"]}/{route}",
                    new StringContent(o, System.Text.Encoding.UTF8, "application/json"));

                var channelKey = configuration["ts-ck-temperature"]; // 693480 - temp
                if (route == "pressure")
                {
                    channelKey = configuration["ts-ck-pressure"]; // 693482 - pressure
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