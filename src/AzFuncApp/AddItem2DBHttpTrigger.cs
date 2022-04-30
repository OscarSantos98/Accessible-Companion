using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Globalization;

namespace Company.Function
{
    public static class AddItem2DBHttpTrigger
    {
        [FunctionName("AddItem2DBHttpTrigger")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName: "my-database",
                collectionName: "my-container",
                ConnectionStringSetting = "CosmosDbConnectionString")]IAsyncCollector<dynamic> documentsOut,
                ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            Dictionary<string, float> coordinatesDict = new();
            List<string> coordinates = new();
            for (int i = 1; i <= 4; i++)
            {
                string x = req.Query[$"x{i}"];
                string y = req.Query[$"y{i}"];
                coordinates.Add(x);
                coordinates.Add(y);
                coordinatesDict.Add($"x{i}", float.Parse(x, CultureInfo.InvariantCulture.NumberFormat));
                coordinatesDict.Add($"y{i}", float.Parse(y, CultureInfo.InvariantCulture.NumberFormat));

            }

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string msg = null; // when everything works fine this value remains in null
            foreach (string coordinate in coordinates)
            {
                if (string.IsNullOrEmpty(coordinate))
                {
                    msg = "please specify all coordinates so that they can be stored in the database.";
                    break;
                }
            }

            if (string.IsNullOrEmpty(msg))
            {
                // Add a JSON document to the output container.
                await documentsOut.AddAsync(new
                {
                    // create a random ID
                    id = System.Guid.NewGuid().ToString(),
                    coordinates = coordinatesDict
                });
            }

            if (!string.IsNullOrEmpty(name))
            {
                // Add a JSON document to the output container.
                await documentsOut.AddAsync(new
                {
                    // create a random ID
                    id = System.Guid.NewGuid().ToString(),
                    name = name
                });
            }

            string responseMessage = string.IsNullOrEmpty(msg)
                ? "This HTTP triggered function executed successfully. Coordinates saved in the database"
                : $"This HTTP triggered function executed successfully. However, {msg}";

            return new OkObjectResult(responseMessage);
        }
    }
}
