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
        [FunctionName("add-todo")]
        public async Task<IActionResult> Run(
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
    }

    public class TodoItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }
    }
}
