using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TourManagement.Api.AgentIntegration;

public class AgentServiceClient : IAgentServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly string _aiSecret;

    public AgentServiceClient(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _aiSecret = config.GetValue<string>("AiServiceSettings:Secret") 
            ?? throw new InvalidOperationException("AiServiceSettings:Secret is not configured.");
    }

    public async Task<AgentProposalResult> GenerateProposalAsync(string proposalId, int tripId, string requestSnapshotJson)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/generate");
        request.Headers.Add("X-AI-Secret", _aiSecret);

        var payload = new
        {
            proposalId = proposalId,
            tripId = tripId,
            inputSnapshot = JsonSerializer.Deserialize<JsonElement>(requestSnapshotJson)
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        
        // Handle non-success responses explicitly
        if (!response.IsSuccessStatusCode)
        {
            return new AgentProposalResult
            {
                Status = "GenerationFailed",
                Payload = "{\"error\": \"AI service returned a non-success status code.\"}"
            };
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        try
        {
            // We expect the AI service to return a structured JSON matching AgentProposalResult
            var result = JsonSerializer.Deserialize<AgentProposalResult>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
            {
                return new AgentProposalResult
                {
                    Status = "GenerationFailed",
                    Payload = "{\"error\": \"Failed to deserialize AI service response.\"}"
                };
            }
            
            return result;
        }
        catch (Exception ex)
        {
            return new AgentProposalResult
            {
                Status = "GenerationFailed",
                Payload = JsonSerializer.Serialize(new { error = "Invalid JSON returned by AI service.", details = ex.Message })
            };
        }
    }
}
