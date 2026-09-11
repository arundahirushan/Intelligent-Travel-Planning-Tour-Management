namespace TourManagement.Api.AgentIntegration;

// AgentServiceClient — placeholder implementation
// HttpClient-based implementation of IAgentServiceClient.
// Will POST to the internal Python ai-service endpoints and deserialise responses.
// Base URL is configured via AiServiceSettings (from appsettings / env vars).

public class AgentServiceClient : IAgentServiceClient
{
    private readonly HttpClient _httpClient;

    public AgentServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // Method implementations added in a later prompt.
}
