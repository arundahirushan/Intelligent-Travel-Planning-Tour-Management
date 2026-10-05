namespace TourManagement.Api.AgentIntegration;

/// <summary>
/// Single source of truth for how long an AI proposal generation may run.
/// The same values drive the HTTP client timeout, the "already generating" check and the
/// stale-proposal rule, so an active generation can never be declared stale early.
/// </summary>
public static class AgentTimeouts
{
    /// <summary>
    /// Maximum time the API waits for the Python agent service. This is the real upper bound on
    /// a generation: after it elapses the request throws and the proposal is saved as failed.
    /// </summary>
    public static readonly TimeSpan AgentRequest = TimeSpan.FromMinutes(5);

    /// <summary>Extra time allowed for building the request and saving the result after the agent call ends.</summary>
    public static readonly TimeSpan SaveGrace = TimeSpan.FromMinutes(1);

    /// <summary>
    /// A proposal still marked Generating after this long was abandoned (for example the API restarted
    /// mid-run) and is moved to GenerationFailed.
    /// </summary>
    public static readonly TimeSpan GeneratingStaleAfter = AgentRequest + SaveGrace;
}
