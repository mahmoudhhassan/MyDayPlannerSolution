using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using Microsoft.SemanticKernel.Plugins.OpenApi.Extensions;

namespace AgenticExperiences.MyDayPlannerAgent
{

    public class SKPlannerAgent
    {
        private const string CopilotAgentPluginsDirectory = "CopilotAgentPlugins";

        private string _rootDirectory;
        private string[] availableCopilotPlugins;

        private string _acceptLanguage;

        private static ILogger<AgenticExperiences.MyDayPlannerAgent.MyDayPlannerAgent> _logger;

        private Kernel kernel;
        private PromptExecutionSettings promptExecutionSettings;

        private BearerAuthenticationProviderWithCancellationToken _bearerAuthenticationProviderWithCancellationToken;

        public SKPlannerAgent(string rootDirectory, string bearerToken, string acceptLanguage, ILogger<AgenticExperiences.MyDayPlannerAgent.MyDayPlannerAgent> logger)
        {
            _rootDirectory = rootDirectory;
            availableCopilotPlugins = Directory.GetDirectories(Path.Combine(rootDirectory, CopilotAgentPluginsDirectory));
            _bearerAuthenticationProviderWithCancellationToken = new BearerAuthenticationProviderWithCancellationToken(bearerToken);
            _acceptLanguage = acceptLanguage;
            _logger = logger;
            
            (kernel, promptExecutionSettings) = KernelFactory();
        }


        public async Task ConfigAsync()
        {            
            kernel.AutoFunctionInvocationFilters.Add(new ExpectedSchemaFunctionFilter());

            foreach (var selectedPluginName in availableCopilotPlugins)
            {
                await AddCopilotAgentPluginAsync(kernel, selectedPluginName);
            }

        }

        public async Task<string> ExecuteAsync()
        {
            var promptTemplate = 
            @"You are a helpful Day Planner Agent, you are responsible for retrieving all today {{$today}} calendar meetings and generate a detailed report. 
            This report should include key details about each meeting and guidance on how I can best prepare for them.
            You must follow exactly the following steps:
            **Step 1**: Get all today calendar meetings
            **Step 2**: For every meeting extract and generate the following:
            -	Meeting Title
            -	Start Time
            -	End Time
            -	Attendees: FirstName Last Name for every attendee with comma separation (Ex: <firstName1 lastName1>, <firstName2 lastName2>)
            -	Meeting Summary: Based on the meeting title and meeting body. This property must be written in user language {{$userLanguage}}
            -	Preparation Recommendation: A thorough and detailed recommendation on how to effectively prepare for this meeting. This property must be written in user language {{$userLanguage}}";
            var result = await kernel.InvokePromptAsync(promptTemplate, new KernelArguments(promptExecutionSettings){
                { "today", DateTime.Today },
                { "userLanguage", _acceptLanguage }
             });

            return result.ToString();
        }

        public async Task<string> ExecuteWithStructuredOutputAsync(string promptTemplate)
        {

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            (promptExecutionSettings as AzureOpenAIPromptExecutionSettings).ResponseFormat = typeof(DayPlanneResult);
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var result = await kernel.InvokePromptAsync("You must **Always order by meeting start time**. " + promptTemplate, new KernelArguments(promptExecutionSettings));

            return result.ToString();
        }

        private (Kernel kernel, PromptExecutionSettings promptSettings) KernelFactory()
        {
            var llmType = "azureopenai";

            switch (llmType)
            {
                case "azureopenai":
                    return InitializeAzureOpenAiKernel(enableLogging: true);
                case "ollama":
                    return InitializeKernelForOllama(enableLogging: true);
                default:
                    throw new InvalidOperationException("Please provide valid LLM type in appsettings.Development.json file.");
            }
        }

        private static (Kernel, PromptExecutionSettings) InitializeAzureOpenAiKernel(bool enableLogging)
        {            
            var apiKey = Environment.GetEnvironmentVariable("AzureOpenAI__ApiKey");
            var chatDeploymentName = Environment.GetEnvironmentVariable("AzureOpenAI__ChatDeploymentName");
            var chatModelId = Environment.GetEnvironmentVariable("AzureOpenAI__ChatModelId");
            var endpoint =  Environment.GetEnvironmentVariable("AzureOpenAI__Endpoint");

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(chatDeploymentName) || string.IsNullOrEmpty(chatModelId) || string.IsNullOrEmpty(endpoint))
            {
                throw new InvalidOperationException("Please provide valid AzureOpenAI configuration in appsettings.Development.json file.");
            }

            var builder = Kernel.CreateBuilder();
            if (enableLogging)
            {
                builder.Services.AddLogging(loggingBuilder =>
                    {
                        loggingBuilder.AddFilter(level => true);
                        loggingBuilder.AddProvider(new SemanticKernelLoggerProvider(_logger));
                    });
            }
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            return (builder.AddAzureOpenAIChatCompletion(
                    deploymentName: chatDeploymentName,
                    endpoint: endpoint,
                    serviceId: "AzureOpenAIChat",
                    apiKey: apiKey,
                    modelId: chatModelId).Build(),
#pragma warning disable SKEXP0001
                    new AzureOpenAIPromptExecutionSettings
                    {
                        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(
                        options: new FunctionChoiceBehaviorOptions
                        {
                            AllowStrictSchemaAdherence = true
                        }
                    )
                    });
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore SKEXP0001
        }

        private static (Kernel, PromptExecutionSettings) InitializeKernelForOllama(bool enableLogging)
        {
            var chatModelId = Environment.GetEnvironmentVariable("Ollama__ChatModelId");
            var endpoint = Environment.GetEnvironmentVariable("Ollama__Endpoint");
            if (string.IsNullOrEmpty(chatModelId) || string.IsNullOrEmpty(endpoint))
            {
                throw new InvalidOperationException("Please provide valid Ollama configuration in appsettings.Development.json file.");
            }

            var builder = Kernel.CreateBuilder();
            if (enableLogging)
            {
                builder.Services.AddLogging(loggingBuilder =>
                    {
                        loggingBuilder.AddFilter(level => true);
                        loggingBuilder.AddProvider(new SemanticKernelLoggerProvider(_logger));
                    });
            }
#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0001
            return (builder.AddOllamaChatCompletion(
                    chatModelId,
                    new Uri(endpoint)).Build(),
                    new OllamaPromptExecutionSettings
                    {
                        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(
                        options: new FunctionChoiceBehaviorOptions
                        {
                            AllowStrictSchemaAdherence = true
                        }
                    )
                    });
#pragma warning restore SKEXP0001
#pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        }

        private async Task AddCopilotAgentPluginAsync(Kernel kernel, string pluginDirectory)
        {
            var pluginName = Path.GetFileName(pluginDirectory);

#pragma warning disable SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var copilotAgentPluginParameters = new CopilotAgentPluginParameters
            {
                FunctionExecutionParameters = new()
            {
                { "https://graph.microsoft.com/v1.0", new OpenApiFunctionExecutionParameters(authCallback: this._bearerAuthenticationProviderWithCancellationToken.AuthenticateRequestAsync, enableDynamicOperationPayload: false, enablePayloadNamespacing: true) { ParameterFilter = s_restApiParameterFilter} }
            },
            };
#pragma warning restore SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            try
            {
#pragma warning disable SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                KernelPlugin plugin =
                await kernel.ImportPluginFromCopilotAgentPluginAsync(
                    pluginName,
                    GetCopilotAgentManifestPath(pluginName),
                    copilotAgentPluginParameters)
                    .ConfigureAwait(false);
#pragma warning restore SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            }
            catch (Exception ex)
            {
                kernel.LoggerFactory.CreateLogger("Plugin Creation").LogError(ex, "Plugin creation failed. Message: {0}", ex.Message);
                throw new AggregateException($"Plugin creation failed for {pluginName}", ex);
            }
        }

        private string GetCopilotAgentManifestPath(string name)
        {
            return Path.Combine(_rootDirectory, CopilotAgentPluginsDirectory, name, $"{name[..^6].ToLowerInvariant()}-apiplugin.json");
        }

        #region MagicDoNotLookUnderTheHood
        private static readonly HashSet<string> s_fieldsToIgnore = new(
            [
                "@odata.type",
            "attachments",
            "bccRecipients",
            "bodyPreview",
            "categories",
            "ccRecipients",
            "conversationId",
            "conversationIndex",
            "extensions",
            "flag",
            "from",
            "hasAttachments",
            "id",
            "inferenceClassification",
            "internetMessageHeaders",
            "isDeliveryReceiptRequested",
            "isDraft",
            "isRead",
            "isReadReceiptRequested",
            "multiValueExtendedProperties",
            "parentFolderId",
            "receivedDateTime",
            "replyTo",
            "sender",
            "sentDateTime",
            "singleValueExtendedProperties",
            "uniqueBody",
            "webLink",
        ],
            StringComparer.OrdinalIgnoreCase
        );
        private const string RequiredPropertyName = "required";
        private const string PropertiesPropertyName = "properties";
        /// <summary>
        /// Trims the properties from the request body schema.
        /// Most models in strict mode enforce a limit on the properties.
        /// </summary>
        /// <param name="schema">Source schema</param>
        /// <returns>the trimmed schema for the request body</returns>
        private static KernelJsonSchema? TrimPropertiesFromRequestBody(KernelJsonSchema? schema)
        {
            if (schema is null)
            {
                return null;
            }

            var originalSchema = JsonSerializer.Serialize(schema.RootElement);
            var node = JsonNode.Parse(originalSchema);
            if (node is not JsonObject jsonNode)
            {
                return schema;
            }

            TrimPropertiesFromJsonNode(jsonNode);

            return KernelJsonSchema.Parse(node.ToString());
        }
        private static void TrimPropertiesFromJsonNode(JsonNode jsonNode)
        {
            if (jsonNode is not JsonObject jsonObject)
            {
                return;
            }
            if (jsonObject.TryGetPropertyValue(RequiredPropertyName, out var requiredRawValue) && requiredRawValue is JsonArray requiredArray)
            {
                jsonNode[RequiredPropertyName] = new JsonArray(requiredArray.Where(x => x is not null).Select(x => x!.GetValue<string>()).Where(x => !s_fieldsToIgnore.Contains(x)).Select(x => JsonValue.Create(x)).ToArray());
            }
            if (jsonObject.TryGetPropertyValue(PropertiesPropertyName, out var propertiesRawValue) && propertiesRawValue is JsonObject propertiesObject)
            {
                var properties = propertiesObject.Where(x => s_fieldsToIgnore.Contains(x.Key)).Select(static x => x.Key).ToArray();
                foreach (var property in properties)
                {
                    propertiesObject.Remove(property);
                }
            }
            foreach (var subProperty in jsonObject)
            {
                if (subProperty.Value is not null)
                {
                    TrimPropertiesFromJsonNode(subProperty.Value);
                }
            }
        }
#pragma warning disable SKEXP0040
        private static readonly RestApiParameterFilter s_restApiParameterFilter = (RestApiParameterFilterContext context) =>
        {
#pragma warning restore SKEXP0040
            if ("me_sendMail".Equals(context.Operation.Id, StringComparison.OrdinalIgnoreCase) &&
                "payload".Equals(context.Parameter.Name, StringComparison.OrdinalIgnoreCase))
            {
                context.Parameter.Schema = TrimPropertiesFromRequestBody(context.Parameter.Schema);
                return context.Parameter;
            }
            return context.Parameter;
        };
        private sealed class ExpectedSchemaFunctionFilter : IAutoFunctionInvocationFilter
        {//TODO: this eventually needs to be added to all CAP or DA but we're still discussing where should those facilitators live
            public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
            {
                await next(context).ConfigureAwait(false);

                if (context.Result.ValueType == typeof(RestApiOperationResponse))
                {
                    var openApiResponse = context.Result.GetValue<RestApiOperationResponse>();
                    if (openApiResponse?.ExpectedSchema is not null)
                    {
                        openApiResponse.ExpectedSchema = null;
                    }
                }
            }
        }
        #endregion

    }
}