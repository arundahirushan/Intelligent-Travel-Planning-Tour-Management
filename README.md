# Intelligent Travel Planning — Tour Management System

SE3090 · Integrated Full-Stack and Agentic AI Application

## What is this?

A full-stack, AI-augmented tour management platform that covers the complete
travel lifecycle: trip and itinerary planning, accommodation booking, transport
and activity coordination, and supplier/contract management. An internal
Python/LangGraph multi-agent pipeline powers intelligent recommendations and
workflow automation; the React web app and Flutter mobile app both talk
exclusively to the ASP.NET Core API, which in turn calls the AI service.

## Folder layout

| Folder | Purpose |
|---|---|
| `client/` | React web app (Vite) — customer-facing portal |
| `server/` | ASP.NET Core 8 Web API + PostgreSQL via EF Core — main backend |
| `mobile/` | Flutter app — used by Admin & Super Admin roles |
| `ai-service/` | Python / LangGraph agent service — **internal only**, called only by `server/` |
| `docs/` | Architecture Decision Records (ADR) and ER diagrams |
| `.github/` | GitHub Actions CI workflows |

## Quick start (structure only — no running services yet)

```bash
# Backend
cd server && dotnet restore TourManagement.sln && dotnet build TourManagement.sln

# Frontend
cd client && npm install && npm run dev

# Mobile
cd mobile/tour_management_mobile && flutter pub get

# AI service
cd ai-service && pip install -r requirements.txt
```

> **Note:** Real configuration (DB connection strings, LLM keys, JWT secrets) is
> never committed. Use user-secrets / environment variables / `.env` files that
> are excluded from git.
