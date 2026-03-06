using TravelAssistant.Abstractions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace TravelAssistant.Host;

/// <summary>
/// Loads prompt templates from .prompt.yaml files.
/// </summary>
public class YamlPromptLoader : IPromptLoader
{
    private readonly string _basePath;
    private readonly IDeserializer _deserializer;

    public YamlPromptLoader()
    {
        // Prompts are located relative to the output directory
        _basePath = AppDomain.CurrentDomain.BaseDirectory;
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public async Task<PromptTemplate> LoadAsync(
        string agentId, 
        string promptName, 
        CancellationToken cancellationToken = default)
    {
        // Look for prompt in the Prompts directory
        var promptPath = Path.Combine(_basePath, "Prompts", agentId, $"{promptName}.prompt.yaml");
        
        if (!File.Exists(promptPath))
        {
            throw new FileNotFoundException($"Prompt file not found: {promptPath}");
        }

        var yaml = await File.ReadAllTextAsync(promptPath, cancellationToken);
        var promptFile = _deserializer.Deserialize<PromptFile>(yaml);

        return new PromptTemplate
        {
            SystemPrompt = promptFile.SystemPrompt ?? string.Empty,
            UserMessageTemplate = promptFile.UserMessageTemplate,
            Settings = new PromptSettings
            {
                Model = promptFile.Settings?.Model,
                Temperature = promptFile.Settings?.Temperature ?? 0.7,
                MaxTokens = promptFile.Settings?.MaxTokens ?? 2048
            }
        };
    }

    // Internal class for YAML deserialization
    private class PromptFile
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public PromptFileSettings? Settings { get; set; }
        public string? SystemPrompt { get; set; }
        public string? UserMessageTemplate { get; set; }
    }

    private class PromptFileSettings
    {
        public string? Model { get; set; }
        public double Temperature { get; set; } = 0.7;
        public int MaxTokens { get; set; } = 2048;
    }
}
