import os
import pytest
from tools.client import InternalAgentClient

def test_internal_agent_client_omitted_url_uses_env(monkeypatch):
    monkeypatch.setenv("TOUR_MANAGEMENT_API_URL", "http://test-env-host:5160/api/internal")
    client = InternalAgentClient(proposal_id="test-prop-123")
    assert client.base_url == "http://test-env-host:5160/api/internal"
    assert client.headers["X-AI-ProposalId"] == "test-prop-123"
    assert client.headers["Content-Type"] == "application/json"

def test_internal_agent_client_explicit_url_overrides_env(monkeypatch):
    monkeypatch.setenv("TOUR_MANAGEMENT_API_URL", "http://test-env-host:5160/api/internal")
    client = InternalAgentClient(proposal_id="test-prop-123", base_url="http://explicit-host:9999/api/custom")
    assert client.base_url == "http://explicit-host:9999/api/custom"
    assert client.headers["X-AI-ProposalId"] == "test-prop-123"
