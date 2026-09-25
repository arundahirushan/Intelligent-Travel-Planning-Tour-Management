# ── Build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj first for layer caching
COPY server/TourManagement.Api/TourManagement.Api.csproj ./server/TourManagement.Api/
RUN dotnet restore ./server/TourManagement.Api/TourManagement.Api.csproj

# Copy the full server source and publish
COPY server/TourManagement.Api/ ./server/TourManagement.Api/
RUN dotnet publish ./server/TourManagement.Api/TourManagement.Api.csproj -c Release -o /app/out

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/out .

# Render requires port 10000
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "TourManagement.Api.dll"]
