import os
import requests
from typing import Dict, Any, List

class InternalAgentClient:
    def __init__(self, proposal_id: str, ai_secret: str = None, base_url: str = "http://localhost:5032/api/internal"):
        self.proposal_id = proposal_id
        self.ai_secret = ai_secret or os.environ.get("AI_SERVICE_SECRET", "super-secret-key")
        self.base_url = base_url
        self.headers = {
            "X-AI-Secret": self.ai_secret,
            "X-AI-ProposalId": self.proposal_id,
            "Content-Type": "application/json"
        }

    def get_trip(self) -> Dict[str, Any]:
        response = requests.get(f"{self.base_url}/trips/current", headers=self.headers, timeout=10)
        response.raise_for_status()
        return response.json().get("data", {})

    def search_hotels(self, destination_id: int, check_in: str, check_out: str) -> List[Dict[str, Any]]:
        payload = {
            "destinationId": destination_id,
            "checkInDate": check_in,
            "checkOutDate": check_out,
            "capacity": 1
        }
        response = requests.post(f"{self.base_url}/hotels/search", json=payload, headers=self.headers, timeout=10)
        response.raise_for_status()
        return response.json().get("data", [])

    def search_vehicles(self, start_date: str, end_date: str, capacity: int = 1) -> List[Dict[str, Any]]:
        payload = {
            "startDate": start_date,
            "endDate": end_date,
            "capacity": capacity
        }
        response = requests.post(f"{self.base_url}/vehicles/search", json=payload, headers=self.headers, timeout=10)
        response.raise_for_status()
        return response.json().get("data", [])

    def get_weather(self, destination: str, date: str) -> Dict[str, Any]:
        response = requests.get(f"{self.base_url}/weather", params={"destination": destination, "date": date}, headers=self.headers, timeout=10)
        response.raise_for_status()
        return response.json().get("data", {})
