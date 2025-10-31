using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using AiAgentProject.Domains;
using Azure;
using Azure.AI.OpenAI;
using Azure.AI.Vision.ImageAnalysis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System;
using System.Linq;
using System.Collections.Generic;

//namespace AiAgentProject.Controllers;

namespace AiAgentProject.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MyAgentController : ControllerBase
{
    private readonly ILogger<MyAgentController> _logger;
    private readonly AzureOpenAiOptions _options;
    private readonly AzureOpenAIClient _azureClient;
    private readonly ImageAnalysisClient _imageClient;
    
    private readonly HttpClient _httpClient = new ();
    private const int MaxImageBytes = 1024 * 1024 * 10;

    
    private Uri endpointUri;
    private string apiKey;
    private string deploymentName;

    public MyAgentController(ILogger<MyAgentController> logger, IOptions<AzureOpenAiOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        
        deploymentName = _options.Deployment;
        _azureClient = new AzureOpenAIClient(new Uri(_options.Endpoint), new AzureKeyCredential(_options.ApiKey));
        _imageClient = new ImageAnalysisClient(new Uri(_options.Endpoint), new AzureKeyCredential(_options.ApiKey));
    }
    
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("MyAgent controller is working!");
    }

    [HttpPost("SamplePromptResponse")]
    public async Task<IActionResult> SamplePromptResponse([FromBody] AskRequest request)
    {
        try
        {
            ChatClient chatClient = _azureClient.GetChatClient(deploymentName);
            
            
            // 🚧 Guard: block base64-encoded prompts (prevents encoded jailbreaks like the provided sample)
            if (!string.IsNullOrWhiteSpace(request?.Question) && Helper.Helper.IsBase64Payload(request.Question))
            {
                _logger.LogWarning("Blocked base64-encoded question payload.");
                return BadRequest(new { error = "Encoded payloads are not allowed. Please send plain-text questions." });
            }
            
            
            // Basic prompt-injection and safety checks
            var qi = request.Question;
            if (Regex.IsMatch(qi, @"\b(ignore (the )?previous|ignore previous instructions|ignore all previous|act as|just act)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                return BadRequest(new { error = "Request appears to attempt to bypass safety instructions. Refusing to comply." });
            }

            if (Regex.IsMatch(qi, @"\b(panic|attack|bomb|kill|suicide|weapon|harm|terror)\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                return BadRequest(new { error = "Request contains disallowed or potentially harmful content." });
            }
            
            var chatMessages = new List<ChatMessage>
            {
                new SystemChatMessage(
                    "You are an AI assistant for Utopia City. Assume all locations are in Utopia City unless otherwise specified. " +
                    "Answer questions based on Utopia City's fictional data. Answer ONLY from provided documents from knowledge base. " +
                    "If unknown, ignore. Be concise and cite source identifiers."
                ),
                

                new UserChatMessage(request.Question)
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
    
    
// New endpoint: Assess image safety
    [HttpPost("AssessImage")]
    public async Task<IActionResult> AssessImage([FromBody] ImageAssessmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.ImageUrl))
        {
            return BadRequest(new { error = "Request must contain an 'image_url' field in JSON." });
        }

        if (!Uri.TryCreate(request.ImageUrl, UriKind.Absolute, out var uri) ||
            !(uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return BadRequest(new { error = "Invalid 'image_url' provided." });
        }

        try
        {
            // Attempt to get headers only to avoid downloading the full image
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, request.ImageUrl);
            var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Non-success status when fetching image headers: {Status}", response.StatusCode);
                return Ok(new { safe = false });
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            var contentLength = response.Content.Headers.ContentLength;

            bool isImage = contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

            bool safe;
            if (!isImage)
            {
                safe = false;
            }
            else if (contentLength.HasValue)
            {
                safe = contentLength.Value <= MaxImageBytes;
            }
            else
            {
                // Unknown length — treat as safe (conservative choice) after confirming content-type
                safe = true;
            }

            return Ok(new { safe });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while assessing image safety");
            // On unexpected errors, return false as the safety flag (still responds with JSON { safe: false })
            return Ok(new { safe = false });
        }
    }// New endpoint: Assess image safety
    
    
    
    [HttpPost("AssessImage2")]
    public async Task<IActionResult> AssessImage2([FromBody] ImageAssessmentRequest request)
    {
        
        if (string.IsNullOrWhiteSpace(request?.ImageUrl))
        {
            return BadRequest(new { error = "Request must contain an 'image_url' field in JSON." });
        }

        if (!Uri.TryCreate(request.ImageUrl, UriKind.Absolute, out var uri) ||
            !(uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return BadRequest(new { error = "Invalid 'image_url' provided." });
        }

        try
        {
            // Step 2: Initialize Azure AI Vision Client
            var client = new ImageAnalysisClient(new Uri(_options.Endpoint), new AzureKeyCredential(_options.ApiKey));

            // Step 3: Request features — tags & caption can reveal unsafe content
            var features = VisualFeatures.Caption | VisualFeatures.Tags;

            ImageAnalysisResult result = await client.AnalyzeAsync(new Uri(request.ImageUrl), features);

            // Step 4: Analyze results
            bool isSafe = true;

            // Example: flag common unsafe tags
            string[] unsafeTags = new[] { "weapon", "violence", "blood", "nudity", "gun", "fight", "war", "knife","sharp", "explosive","gun","inflammable","pyrotechnics", "weapons" };
            
            

            if (result.Tags != null)
            {
                
                var ans = result.Tags.Values.ToList();
                
                
                // Convert ImageTagCollection to array so LINQ works
                //var tagsArray = JsonSerializer.Deserialize(result.Tags); 
              
                var matchedUnsafeTags = ans
                    .Where(t => unsafeTags.Contains(t.Name, StringComparer.OrdinalIgnoreCase)
                                && t.Confidence > 0.35)
                    .Select(t => $"{t.Name} ({t.Confidence:P0})")
                    .ToList();

                foreach (var tag in ans)
                {
                    if (!string.IsNullOrWhiteSpace(tag.Name) &&
                        unsafeTags.Contains(tag.Name, StringComparer.OrdinalIgnoreCase) &&
                        tag.Confidence > 0.34)
                    {
                        matchedUnsafeTags.Add($"{tag.Name} ({tag.Confidence:P0})");
                    }
                }

                if (matchedUnsafeTags.Count > 0)
                {
                    isSafe = false;
                    _logger.LogWarning("Unsafe tags detected in image {ImageUrl}: {Tags}",
                        request.ImageUrl, string.Join(", ", matchedUnsafeTags));
                }
                
                
            }

            // Step 5: Log and return
            var response = new ImageSafetyResponse { Safe = isSafe };

            _logger.LogInformation("Image safety result for {ImageUrl}: Safe={Safe}", request.ImageUrl, isSafe);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while assessing image safety");
            // On unexpected errors, return false as the safety flag (still responds with JSON { safe: false })
            return Ok(new { safe = false });
        }
    }

    
    [HttpPost("AssessImage3")]
    public async Task<IActionResult> AssessImage3([FromBody] ImageAssessmentRequest request)
    {
        
        try
        {
            ChatClient chatClient = _azureClient.GetChatClient(deploymentName);
            
            var chatMessages = new List<ChatMessage>
            {
                new SystemChatMessage(@"You are an AI assistant that can understand visual content, and perform classification based on the images, decide if the objects are safe to stream aboard the colony. read from json request and read ""image_url"" 
                and response only with safe or unsafe with this format -> { ""safe"": false}"),
                

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
            return Ok(answer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing sample prompt");
            return StatusCode(500, new { error = ex.Message });
        }
    }


    // Request model for image assessment (supports snake_case JSON like "image_url" and "call_index")
    public sealed class ImageAssessmentRequest
    {
        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("team")]
        public string Team { get; set; } = string.Empty;

        [JsonPropertyName("call_index")]
        public int CallIndex { get; set; }
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