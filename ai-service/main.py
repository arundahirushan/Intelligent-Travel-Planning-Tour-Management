"""
main.py — ai-service FastAPI entrypoint (placeholder)

IMPORTANT: This service is INTERNAL ONLY.
It must never be called directly by the React web client or the Flutter mobile
app. All calls go through TourManagement.Api (ASP.NET Core) which acts as the
gateway.

Exposes endpoints that TourManagement.Api calls to:
  - Start an agent workflow
  - Get workflow status
  - Retrieve execution results

Real endpoint implementations will be added in a later prompt.
"""

from fastapi import FastAPI

app = FastAPI(
    title="TourManagement AI Service",
    description="Internal-only LangGraph agent service. Called by TourManagement.Api only.",
    version="0.1.0",
)


@app.get("/health")
def health_check():
    """Simple liveness probe so the API can verify the service is up."""
    return {"status": "ok"}

# Workflow endpoints will be added here in a later prompt.
