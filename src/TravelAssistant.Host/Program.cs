using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using TravelAssistant.Abstractions;
using TravelAssistant.Agents.PointsOfInterest;
using TravelAssistant.Agents.FlightSearch;
using TravelAssistant.Agents.HotelSearch;
using TravelAssistant.Host;
using TravelAssistant.Host.Orchestration;

var builder = Host.CreateApplicationBuilder(args);

// Register prompt loader
builder.Services.AddSingleton<IPromptLoader, YamlPromptLoader>();

// Build and register Semantic Kernel.
// Configure via appsettings.json or environment variables:
//   AzureOpenAI__Endpoint       — Azure OpenAI resource endpoint
//   AzureOpenAI__ApiKey         — API key (or use managed identity)
//   AzureOpenAI__DeploymentName — Chat model deployment (default: gpt-4o)
var kernelBuilder = Kernel.CreateBuilder();

var endpoint = builder.Configuration["AzureOpenAI:Endpoint"];
var apiKey = builder.Configuration["AzureOpenAI:ApiKey"];
var deployment = builder.Configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o";

if (!string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(apiKey))
{
#pragma warning disable SKEXP0010 // AzureOpenAI connector is experimental in 1.x
    kernelBuilder.AddAzureOpenAIChatCompletion(deployment, endpoint, apiKey);
#pragma warning restore SKEXP0010
}
else
{
    Console.WriteLine("⚠️  Azure OpenAI not configured — agents will throw when ProcessAsync is called.");
    Console.WriteLine("    Set AzureOpenAI:Endpoint and AzureOpenAI:ApiKey in configuration.");
}

builder.Services.AddSingleton(kernelBuilder.Build());

// Register agents
builder.Services.AddSingleton<ITravelAgent, PoiAgent>();
builder.Services.AddSingleton<ITravelAgent>(sp =>
    new FlightAgent(
        sp.GetRequiredService<IPromptLoader>(),
        sp.GetRequiredService<Kernel>()));
builder.Services.AddSingleton<ITravelAgent, HotelAgent>();

// Register orchestrator
builder.Services.AddSingleton<TravelOrchestrator>();

var host = builder.Build();

// Demo: Run a sample query
var orchestrator = host.Services.GetRequiredService<TravelOrchestrator>();

Console.WriteLine("=== Travel Assistant ===");
Console.WriteLine("Enter your travel query (or 'quit' to exit):");
Console.WriteLine();

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();
    
    if (string.IsNullOrWhiteSpace(input) || input.Equals("quit", StringComparison.OrdinalIgnoreCase))
        break;

    var response = await orchestrator.ProcessQueryAsync(input);
    
    Console.WriteLine();
    foreach (var agentResponse in response)
    {
        Console.WriteLine($"[{agentResponse.AgentId}] {agentResponse.Message}");
    }
    Console.WriteLine();
}

