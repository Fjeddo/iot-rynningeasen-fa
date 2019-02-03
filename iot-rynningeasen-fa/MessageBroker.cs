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
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestMessage req, TraceWriter log, ExecutionContext context)
        {
            var configuration = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);

            log.Info("C# HTTP IoT Messagebroker function processed a request.");

            var httpClient = new HttpClient();

            string requestBody = req.Content.ReadAsStringAsync().Result;
            log.Info($"Request body: {requestBody}");
            log.Info($"Debug: {configuration["iot-www-api-location"]}");

            var dummy = httpClient.PostAsync(configuration["iot-www-api-location"].ToString(), new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json")).Result;

            dynamic[] data = JsonConvert.DeserializeObject<dynamic[]>(requestBody);

            foreach (var @group in data)
            {
                var channelKey = configuration["ts-ck-temperature"]; // 693480 - temp
                if (@group.Name == "pressure")
                {
                    channelKey = configuration["ts-ck-pressure"]; // 693482 - pressure
                }

                var fields = "";
                foreach (var value in @group.Values)
                {
                    log.Info($"{value.Name}: {value.Value}");

                    var @field = int.Parse(value.Name.Split(':')[1]) - 100;
                    fields += $"&field{@field}=" + value.Value;
                }

                dummy = httpClient.GetAsync($"https://api.thingspeak.com/update?api_key={channelKey}{fields}").Result;
            }

            return req.CreateResponse(HttpStatusCode.OK, $"Done");
        }
    }
}