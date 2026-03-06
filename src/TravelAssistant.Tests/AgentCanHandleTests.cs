using TravelAssistant.Abstractions;
using TravelAssistant.Agents.PointsOfInterest;
using TravelAssistant.Agents.FlightSearch;
using TravelAssistant.Agents.HotelSearch;

namespace TravelAssistant.Tests;

public class AgentCanHandleTests
{
    private readonly IPromptLoader _mockLoader = new MockPromptLoader();

    [Theory]
    [InlineData("Find attractions in Paris", true)]
    [InlineData("What restaurants are nearby?", true)]
    [InlineData("Book a flight to London", false)]
    public void PoiAgent_CanHandle_ReturnsExpected(string query, bool expected)
    {
        var agent = new PoiAgent(_mockLoader);
        var context = new TravelContext();

        var result = agent.CanHandle(query, context);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Find flights to Tokyo", true)]
    [InlineData("What airline should I use?", true)]
    [InlineData("Find a hotel in Rome", false)]
    public void FlightAgent_CanHandle_ReturnsExpected(string query, bool expected)
    {
        var agent = new FlightAgent(_mockLoader);
        var context = new TravelContext();

        var result = agent.CanHandle(query, context);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Book a hotel in Berlin", true)]
    [InlineData("Where should I stay?", true)]
    [InlineData("What attractions are in Barcelona?", false)]
    public void HotelAgent_CanHandle_ReturnsExpected(string query, bool expected)
    {
        var agent = new HotelAgent(_mockLoader);
        var context = new TravelContext();

        var result = agent.CanHandle(query, context);

        Assert.Equal(expected, result);
    }
}

/// <summary>
/// Simple mock implementation for testing.
/// </summary>
public class MockPromptLoader : IPromptLoader
{
    public Task<PromptTemplate> LoadAsync(string agentId, string promptName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PromptTemplate
        {
            SystemPrompt = $"Mock system prompt for {agentId}/{promptName}",
            UserMessageTemplate = "{{query}}"
        });
    }
}
