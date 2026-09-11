namespace TourManagement.Api.AgentIntegration;

// IAgentServiceClient — placeholder interface
// Contract for calling the internal Python ai-service over HTTP.
// The React and Flutter clients must NEVER call the ai-service directly —
// all agent interactions go through this interface via WorkflowsController.

public interface IAgentServiceClient
{
    // Methods such as StartWorkflowAsync, GetStatusAsync, etc.
    // will be defined here in a later prompt.
}
