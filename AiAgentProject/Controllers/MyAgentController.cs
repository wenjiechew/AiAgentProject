using Microsoft.AspNetCore.Mvc;
using Azure;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using Microsoft.AspNetCore.Mvc;
using Azure;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using OpenAI.Chat;
using Microsoft.VisualBasic;

//namespace AiAgentProject.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MyAgentController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly AzureOpenAIClient _azureClient;

    private readonly ILogger<MyAgentController> _logger;
    private Uri endpointUri;
    private string apiKey;
    private string deploymentName;

    public MyAgentController(ILogger<MyAgentController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration; 
        endpointUri = new Uri("https://nway-ai-test-001.cognitiveservices.azure.com/");
        apiKey = "2T3G1HdnZ6NfRdkvYpSPNyVrBrX5WRF...JQQJ99BJACYeBjFXJ3w3AAAAACOG3kkF";
  
        deploymentName = "gpt-4.1";
        _azureClient = new AzureOpenAIClient(endpointUri, new AzureKeyCredential(apiKey));
    }


    [HttpGet]
    public IActionResult Get()
    {
        return Ok("MyAgent controller is working!");
    }

    [HttpGet("GetResponse")]
    public async Task<IActionResult> GetResponse()
    {
        ChatClient chatClient = _azureClient.GetChatClient(deploymentName);

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

     // ✅ POST endpoint: api/myagentpost/postmessage
    [HttpPost("PostMessage")]
    public async Task<IActionResult> PostMessage([FromBody] PromptRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Prompt))
        {
            return BadRequest(new { error = "Request must contain a 'prompt' field in JSON." });
        }

        try
        {
            ChatClient chatClient = _azureClient.GetChatClient(deploymentName);

            var chatMessages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a helpful AI assistant."),
                new UserChatMessage(request.Prompt)
            };

            var options = new ChatCompletionOptions
            {
                Temperature = 0.7f,
                TopP = 1.0f
            };

            // ✅ Call Azure OpenAI chat completion
            var response = await chatClient.CompleteChatAsync(chatMessages, options);

            var reply = response.Value.Content[0].Text;
            _logger.LogInformation("AI Response: {Reply}", reply);

            return Ok(new { prompt = request.Prompt, response = reply });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending prompt to Azure OpenAI");
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
     // ✅ Simple request model
    public class PromptRequest
    {
        public string? Prompt { get; set; }
    }

}