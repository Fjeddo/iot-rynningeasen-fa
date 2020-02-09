using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace IoTRynningeasenFACore
{
    public static class MessageBroker
    {
        [FunctionName("IoTMessageBroker")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var configuration = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);

            log.LogInformation("C# HTTP IoT Messagebroker function processed a request.");

            var httpClient = new HttpClient();

            var requestBody = req.ReadAsStringAsync().Result;
            log.LogInformation($"Request body: {requestBody}");
            log.LogInformation($"Config test: {configuration["iot-www2-api-location"]}");
            //log.Info($"Debug: {configuration["iot-www-api-location"]}");
            //log.Info($"Debug: {configuration["iot-www2-api-location"]}");

            // For debug purposes...
            if (requestBody.ToLowerInvariant().Contains("debug"))
            {
                log.LogInformation("Debug: Returning with noop");
                var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content =
                        new ObjectContent(typeof(object), JsonConvert.DeserializeObject(requestBody), new JsonMediaTypeFormatter())
                };

                return new ResponseMessageResult(httpResponseMessage);
            }
            // End for debug purposes...

            var data = JsonConvert.DeserializeObject<dynamic[]>(requestBody);

            foreach (var o in data)
            {
                var name = o.Name.ToString();
                var value = (double)o.Value;

                var route = "temperature";

                if (name.Contains(":pressure"))
                {
                    route = "pressure";
                }
                else if (name.Contains(":humidity"))
                {
                    route = "humidity";
                }

                /*
                await httpClient.PostAsync(
                    $"{configuration["iot-www-api-location"]}/{route}",
                    new StringContent(JsonConvert.SerializeObject(o), System.Text.Encoding.UTF8, "application/json"));
                */

                /*
                await httpClient.PostAsync(
                    $"{configuration["iot-www2-api-location"]}/{route}",
                    new StringContent(JsonConvert.SerializeObject(o), System.Text.Encoding.UTF8, "application/json"));
                */

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

                log.LogInformation($"{name}: {value}");

                var field = int.Parse(name.Split(':')[1]) - 100;
                var fieldValue = $"&field{field}=" + value;

                await httpClient.GetAsync($"https://api.thingspeak.com/update?api_key={channelKey}{fieldValue}");
            }

            return new OkResult();
        }
    }
}
