using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;

namespace ToDo
{
    public class Function1
    {
        [FunctionName("probe")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]
            HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            var requestBody = String.Empty;
            using (StreamReader streamReader = new StreamReader(req.Body))
            {
                requestBody = await streamReader.ReadToEndAsync();
            }
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            return name != null
                ? (ActionResult)new OkObjectResult($"Hello, {name}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }

        [FunctionName("add-item")]
        public async Task<IActionResult> AddTodo(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [CosmosDB(Connection = "CosmosConnection")] CosmosClient cosmosClient, ILogger log)
        {
            var container = cosmosClient.GetContainer("ToDoList", "Items");

            var requestBody = "";
            using (var streamReader = new StreamReader(req.Body))
            {
                requestBody = await streamReader.ReadToEndAsync();
            }

            var todoItem = JsonConvert.DeserializeObject<TodoItem>(requestBody);

            await container.CreateItemAsync(todoItem, new PartitionKey(todoItem.Id));
            return new OkResult();
        }

        [FunctionName("get-item")]
        public async Task<IActionResult> GetTodo(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "item/{id}")] HttpRequest req,
            string id,
            [CosmosDB(Connection = "CosmosConnection")] CosmosClient cosmosClient, ILogger log)
        {
            var container = cosmosClient.GetContainer("ToDoList", "Items");

            var todoItem = await container.ReadItemAsync<TodoItem>(id, new PartitionKey(id));

            return new OkObjectResult(todoItem.Resource);
        }
    }

    public class TodoItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }
    }
}
