using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

            var requestBody = req.ReadAsStringAsync().Result;

            log.LogInformation($"Request body: {requestBody}");

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

            var sensors = SensorsFunctions.GetSensors(requestBody);
            var data = new List<dynamic>();
            foreach (var sensorsType in sensors)
            {
                string name;

                var baseline = DateTime.UtcNow;
                var value = SensorsFunctions.CalculateWeightedAvg(sensorsType.Value.Select(x => SensorsFunctions.CreateWeightedSensor(x, baseline)));

                if (sensorsType.Key == "ZHATemperature")
                {
                    name = "sensor:101:temp";
                    value /= (decimal) 100.0;
                }
                else if (sensorsType.Key == "ZHAPressure")
                {
                    name = "sensor:102:pressure";
                    //value = value;
                }
                else if (sensorsType.Key == "ZHAHumidity")
                {
                    name = "sensor:103:humidity";
                    value /= (decimal) 100.0;
                } 
                else
                {
                    return new BadRequestObjectResult(sensors);
                }

                data.Add(new { Name = name, Value = value });
            }

            var httpClient = new HttpClient();
            
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

                log.LogInformation($"{name}: {value.ToString(CultureInfo.InvariantCulture)}");

                var field = int.Parse(name.Split(':')[1]) - 100;
                var fieldValue = $"&field{field}=" + value.ToString(CultureInfo.InvariantCulture);

                await httpClient.GetAsync($"https://api.thingspeak.com/update?api_key={channelKey}{fieldValue}");
            }

            return new OkResult();
        }
    }
}
