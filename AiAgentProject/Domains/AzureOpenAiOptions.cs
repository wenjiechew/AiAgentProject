namespace AiAgentProject.Domains;

public sealed class AzureOpenAiOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Deployment { get; set; } = "gpt-4.1";

    public string DefaultSystemPrompt { get; set; } = "You are a helpful AI assistant.";
    public Dictionary<string, string>? InstructionProfiles { get; set; }

    public float Temperature { get; set; } = 0.7f;
    public float TopP { get; set; } = 1.0f;
    public float FrequencyPenalty { get; set; } = 0.0f;
    public float PresencePenalty { get; set; } = 0.0f;

    public GuardrailOptions Guardrails { get; set; } = new();

    public sealed class GuardrailOptions
    {
        public int MaxCustomInstructionChars { get; set; } = 1200;
        public bool DisallowSecrets { get; set; } = true;
    }
}