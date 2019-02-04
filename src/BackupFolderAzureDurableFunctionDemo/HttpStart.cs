using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BackupFolderAzureDurableFunctionDemo.Services.Ioc;
using BackupFolderAzureDurableFunctionDemo.Services.Logging;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace BackupFolderAzureDurableFunctionDemo
{
    public static class HttpStart
    {
        [FunctionName(nameof(HttpStart))]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, methods: "post", Route = "BackupFolder")] HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClientBase starter,
            [Inject] ILog log)
        {
            dynamic eventData = await req.Content.ReadAsStringAsync();
            string instanceId = await starter.StartNewAsync("BackupFolder", eventData);

            log.Info($"Started orchestration with ID = '{instanceId}'.");

            var res = starter.CreateCheckStatusResponse(req, instanceId);
            res.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(10));
            return res;
        }
    }
}
