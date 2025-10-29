using Microsoft.AspNetCore.Mvc;
using Azure;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using Microsoft.AspNetCore.Mvc;
using Azure;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using OpenAI.Chat;

//namespace AiAgentProject.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MyAgentController : ControllerBase
{
     private readonly IConfiguration _configuration;
    private readonly AzureOpenAIClient _azureClient;
    private readonly string _deploymentName;

    private string endpoint;
    private string apiKey;
    private string deploymentName;


    private readonly ILogger<MyAgentController> _logger;

    public MyAgentController(ILogger<MyAgentController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        var endpointString = _configuration["AZURE_OPENAI_ENDPOINT"];
        if (string.IsNullOrWhiteSpace(endpointString))
            throw new Exception("AZURE_OPENAI_ENDPOINT is not configured.");

        var endpoint = endpointString;  
        apiKey = _configuration["FOUNDRY_API_KEY"] ?? throw new Exception("FOUNDRY_API_KEY not configured.");
        deploymentName = _configuration["AZURE_OPENAI_DEPLOYMENT"] ?? "gpt-4.1";
        _azureClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    }


    [HttpGet]
    public IActionResult Get()
    {
        return Ok("MyAgent controller is working!");
    }

    [HttpGet("GetResponse")]
    public async Task<IActionResult> GetResponse()
    {
        //if (request == null || string.IsNullOrWhiteSpace(request.Prompt))
        //    return BadRequest(new { error = "Request body required: { \"prompt\": \"your prompt\" }" });
        // var deploymentName = "gpt-4.1";
        Console.WriteLine("my end point is...........  " + endpoint);
        var endpointUri = new Uri("https://nway-ai-test-001.cognitiveservices.azure.com/");
        AzureOpenAIClient azureClient = new(endpointUri,
        new AzureKeyCredential(apiKey));
        ChatClient chatClient = azureClient.GetChatClient(deploymentName);

        var requestOptions = new ChatCompletionOptions()
        {
           // MaxCompletionTokens = 13107,
            Temperature = 1.0f,
            TopP = 1.0f,
            FrequencyPenalty = 0.0f,
            PresencePenalty = 0.0f,

        };

        List<ChatMessage> messages = new List<ChatMessage>()
        {
        new SystemChatMessage("You are a helpful assistant."),
        new UserChatMessage("I am going to Paris, what should I see?"),
        };

        var response = chatClient.CompleteChat(messages, requestOptions);
        System.Console.WriteLine(response.Value.Content[0].Text);
        return Ok(new { response = response.Value.Content[0].Text });

    }

}