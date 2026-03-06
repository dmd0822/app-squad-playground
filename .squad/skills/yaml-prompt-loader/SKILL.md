# Skill: YAML Prompt Loader Pattern

## Summary

A reusable pattern for loading AI prompt templates from external YAML files at runtime, keeping prompts separate from C# code.

## When to Use

- Multi-agent AI applications where prompts change frequently
- Projects where non-developers need to edit prompts
- When you want to A/B test prompt variations without rebuilding
- Semantic Kernel or other LLM orchestration projects

## Pattern

### 1. Prompt File Format (`.prompt.yaml`)

```yaml
name: agent_action
description: What this prompt does

settings:
  model: gpt-4o
  temperature: 0.7
  max_tokens: 2048

system_prompt: |
  You are a helpful assistant...

user_message_template: |
  Context: {{context}}
  User query: {{query}}
```

### 2. Directory Structure

```
{AgentProject}/
└── Prompts/
    └── {action}.prompt.yaml
```

### 3. Copy to Output (`.csproj`)

```xml
<ItemGroup>
  <None Include="..\{AgentProject}\Prompts\**\*.yaml">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <Link>Prompts\{agentId}\%(RecursiveDir)%(Filename)%(Extension)</Link>
  </None>
</ItemGroup>
```

### 4. Loader Interface

```csharp
public interface IPromptLoader
{
    Task<PromptTemplate> LoadAsync(
        string agentId, 
        string promptName, 
        CancellationToken cancellationToken = default);
}
```

### 5. Implementation (uses YamlDotNet)

```csharp
public class YamlPromptLoader : IPromptLoader
{
    private readonly string _basePath = AppDomain.CurrentDomain.BaseDirectory;
    private readonly IDeserializer _deserializer;

    public YamlPromptLoader()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public async Task<PromptTemplate> LoadAsync(string agentId, string promptName, CancellationToken ct)
    {
        var path = Path.Combine(_basePath, "Prompts", agentId, $"{promptName}.prompt.yaml");
        var yaml = await File.ReadAllTextAsync(path, ct);
        return _deserializer.Deserialize<PromptTemplate>(yaml);
    }
}
```

## Dependencies

- `YamlDotNet` NuGet package

## Benefits

- Prompts editable without code changes
- Version control and code review for prompts
- Easy to deploy prompt updates independently
- Testable (mock `IPromptLoader` in unit tests)

## Reference Implementation

See `src/TravelAssistant.Host/YamlPromptLoader.cs` in this repository.
