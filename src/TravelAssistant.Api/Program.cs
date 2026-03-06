using Azure.AI.Projects;
using Azure.Identity;
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

// Configure Azure AI Foundry Agent Framework.
// Configure via appsettings.json or environment variables:
//   AZURE_AI_FOUNDRY_PROJECT_ENDPOINT — Azure AI Foundry project endpoint
//   AZURE_AI_FOUNDRY_MODEL_DEPLOYMENT — Model deployment name (default: gpt-4o)
var projectEndpoint = builder.Configuration["AZURE_AI_FOUNDRY_PROJECT_ENDPOINT"];
var modelDeployment = builder.Configuration["AZURE_AI_FOUNDRY_MODEL_DEPLOYMENT"] ?? "gpt-4o";

AIProjectClient? projectClient = null;
if (!string.IsNullOrWhiteSpace(projectEndpoint))
{
    projectClient = new AIProjectClient(
        new Uri(projectEndpoint),
        new DefaultAzureCredential());
    builder.Services.AddSingleton(projectClient);
}
else
{
    Console.WriteLine("⚠️  Azure AI Foundry not configured — agents will return fallback responses.");
    Console.WriteLine("    Set AZURE_AI_FOUNDRY_PROJECT_ENDPOINT in configuration.");
}

// Register agents
builder.Services.AddSingleton<ITravelAgent>(sp =>
    new PoiAgent(sp.GetRequiredService<IPromptLoader>(), projectClient, modelDeployment));

builder.Services.AddSingleton<ITravelAgent>(sp =>
    new FlightAgent(sp.GetRequiredService<IPromptLoader>(), projectClient, modelDeployment));

builder.Services.AddSingleton<ITravelAgent>(sp =>
    new HotelAgent(sp.GetRequiredService<IPromptLoader>(), projectClient, modelDeployment));

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
