import os
import requests
from typing import Dict, Any, List

class InternalAgentClient:
    def __init__(self, proposal_id: str, ai_secret: str = None, base_url: str = "http://localhost:5032/api/internal"):
        self.proposal_id = proposal_id
        self.ai_secret = ai_secret or os.environ.get("AI_SECRET", "dev-secret-do-not-use-in-prod")
        self.base_url = base_url or os.environ.get("TOUR_MANAGEMENT_API_URL", "http://localhost:5032/api/internal")
        self.headers = {
            "X-AI-Secret": self.ai_secret,
            "X-AI-ProposalId": self.proposal_id,
            "Content-Type": "application/json"
        }

    def get_trip(self) -> Dict[str, Any]:
        response = requests.get(f"{self.base_url}/trips/current", headers=self.headers, timeout=10)
        response.raise_for_status()
        return response.json().get("data", {})

    def search_hotels(self, destination_id: int, check_in: str, check_out: str, group_size: int) -> List[Dict[str, Any]]:
        payload = {
            "destinationId": destination_id,
            "checkInDate": check_in,
            "checkOutDate": check_out,
            "numberOfGuests": group_size,
            "allowMixedRooms": True
        }
        response = requests.post(f"{self.base_url}/hotels/search", json=payload, headers=self.headers, timeout=10)
        response.raise_for_status()
        return response.json().get("data", [])

    def search_vehicles(self, start_date: str, end_date: str, capacity: int = 1) -> List[Dict[str, Any]]:
        payload = {
            "startDate": start_date,
            "endDate": end_date,
            "minCapacity": capacity
        }
        response = requests.post(f"{self.base_url}/vehicles/search", json=payload, headers=self.headers, timeout=10)
        response.raise_for_status()
        return response.json().get("data", [])

    def get_weather(self, destination: str, date: str) -> Dict[str, Any]:
        response = requests.get(f"{self.base_url}/weather", params={"destination": destination, "date": date}, headers=self.headers, timeout=10)
        response.raise_for_status()
        return response.json().get("data", {})
