using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AgenticExperiences.MyDayPlannerAgent
{
    public class MyDayPlannerAgent
    {
        private readonly ILogger<MyDayPlannerAgent> _logger;

        public MyDayPlannerAgent(ILogger<MyDayPlannerAgent> logger)
        {
            _logger = logger;
        }

        [Function("MyDayPlannerAgent")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req, FunctionContext executionContext)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var acessToken = string.Empty;

            var functionRootDirectory = Path.GetDirectoryName(executionContext.FunctionDefinition.PathToAssembly);
            
            req.Headers.TryGetValue("Authorization", out var authorization);
            req.Headers.TryGetValue("Accept-Language", out var acceptLanguage);

            if(!string.IsNullOrEmpty(authorization))
            {
                acessToken = authorization.FirstOrDefault().Split(" ")[1];
            }
            
            if(!string.IsNullOrEmpty(acceptLanguage))
            {
                acceptLanguage = acceptLanguage.FirstOrDefault().Split(",")[0];
            }

            var plannerAgent =  new SKPlannerAgent(functionRootDirectory, acessToken, acceptLanguage, _logger);

            await plannerAgent.ConfigAsync();

            var response = await plannerAgent.ExecuteAsync();
            var dayPlanneResultString = await plannerAgent.ExecuteWithStructuredOutputAsync(response);
            var dayPlanneResult = JsonSerializer.Deserialize<DayPlanneResult>(dayPlanneResultString.ToString());

            return new JsonResult(dayPlanneResult);
        }
    }
}
