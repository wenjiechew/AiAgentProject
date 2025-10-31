using System.Text.Json;
using AiAgentProject.Domains;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

//namespace AiAgentProject.Controllers;

namespace AiAgentProject.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MyAgentController : ControllerBase
{
    private readonly ILogger<MyAgentController> _logger;
    private readonly AzureOpenAiOptions _options;
    private readonly AzureOpenAIClient _azureClient;

    
    private Uri endpointUri;
    private string apiKey;
    private string deploymentName;

    public MyAgentController(ILogger<MyAgentController> logger, IOptions<AzureOpenAiOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        
        deploymentName = _options.Deployment;
        _azureClient = new AzureOpenAIClient(new Uri(_options.Endpoint), new AzureKeyCredential(_options.ApiKey));
    }


    [HttpGet]
    public IActionResult Get()
    {
        return Ok("MyAgent controller is working!");
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
                new SystemChatMessage(_options.DefaultSystemPrompt),
                new UserChatMessage(request.Prompt)
            };



            var options = new ChatCompletionOptions
            {
                Temperature = _options.Temperature,
                TopP = _options.TopP,
                FrequencyPenalty = _options.FrequencyPenalty,
                PresencePenalty = _options.PresencePenalty
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
    
    
    
    [HttpPost("SamplePromptResponse")]
    public async Task<IActionResult> SamplePromptResponse([FromBody] AskRequest request)
    {
        try
        {
            // Sample prompt
            
            // Create chat client
            ChatClient chatClient = _azureClient.GetChatClient(deploymentName);

            var chatMessages = new List<ChatMessage>
            {
                new SystemChatMessage(_options.DefaultSystemPrompt),
                new UserChatMessage(JsonSerializer.Serialize(request))
            };

            var options = new ChatCompletionOptions
            {
                Temperature = _options.Temperature,
                TopP = _options.TopP,
                FrequencyPenalty = _options.FrequencyPenalty,
                PresencePenalty = _options.PresencePenalty
            };

            // Call Azure OpenAI
            var response = await chatClient.CompleteChatAsync(chatMessages, options);

            var answer = response.Value.Content[0].Text;
            
            _logger.LogInformation("AI Response: {Answer}", answer);
            
            var res = new AnswerResponse { Answer = answer };

            // Return JSON in the requested format
            return Ok(res);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing sample prompt");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    public sealed class AskRequest
    {
        public string Question { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Team { get; set; } = string.Empty;
        public int CallIndex { get; set; }
    }

    public sealed class AnswerResponse
    {
        public string Answer { get; set; } = string.Empty;
    }
    
    
    // ✅ Simple request model
    public class PromptRequest
    {
        public string? Prompt { get; set; }
    }

}