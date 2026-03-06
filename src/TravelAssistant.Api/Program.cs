using Microsoft.SemanticKernel;
using TravelAssistant.Abstractions;
using TravelAssistant.Agents.PointsOfInterest;
using TravelAssistant.Agents.FlightSearch;
using TravelAssistant.Agents.HotelSearch;
using TravelAssistant.Host;
using TravelAssistant.Host.Orchestration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Configure CORS for React dev server (Vite default: http://localhost:5173)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
