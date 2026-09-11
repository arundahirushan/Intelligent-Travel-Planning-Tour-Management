using Microsoft.AspNetCore.Mvc;

namespace TourManagement.Api.Controllers;

// WorkflowsController — placeholder
// Will hold the client-facing endpoints for:
//   - Starting an agent workflow
//   - Checking workflow status
//   - Approving / rejecting / revising a workflow
//   - Viewing execution summaries (Section 5 of the spec)
// The actual logic delegates to IAgentServiceClient which calls the internal Python ai-service.

[ApiController]
[Route("api/[controller]")]
public class WorkflowsController : ControllerBase
{
    // Endpoints will be added in a later prompt.
}
