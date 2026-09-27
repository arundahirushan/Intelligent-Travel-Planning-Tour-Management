import os
import json
from google import genai
from google.genai import types
from typing import Type, TypeVar, Any
from pydantic import BaseModel
from dotenv import load_dotenv

load_dotenv()

T = TypeVar('T', bound=BaseModel)

class GeminiClient:
    def __init__(self):
        self.api_key = os.environ.get("GEMINI_API_KEY")
        self.model_name = os.environ.get("GEMINI_MODEL", "gemini-3.5-flash-lite")
        if self.api_key:
            self.client = genai.Client(api_key=self.api_key)
        else:
            self.client = None

    def generate_structured(self, prompt: str, schema: Type[T]) -> T:
        if not self.client:
            raise ValueError("GEMINI_API_KEY is not configured.")

        response = self.client.models.generate_content(
            model=self.model_name,
            contents=prompt,
            config=types.GenerateContentConfig(
                response_mime_type="application/json",
                response_schema=schema,
                temperature=0.1
            )
        )
        
        try:
            return schema.model_validate_json(response.text)
        except Exception as e:
            raise ValueError(f"Failed to parse or validate LLM response: {e}")

_client_instance = None

def get_gemini_client() -> GeminiClient:
    global _client_instance
    if _client_instance is None:
        _client_instance = GeminiClient()
    return _client_instance
