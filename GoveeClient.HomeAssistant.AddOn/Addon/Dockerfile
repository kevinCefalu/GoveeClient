ARG BUILD_FROM=ghcr.io/home-assistant/amd64-base:latest
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY GoveeClient.Shared/GoveeClient.Shared.csproj GoveeClient.Shared/
COPY GoveeClient/GoveeClient.csproj GoveeClient/
COPY GoveeClient.HomeAssistant.AddOn/GoveeClient.HomeAssistant.AddOn.csproj GoveeClient.HomeAssistant.AddOn/

RUN dotnet restore GoveeClient.HomeAssistant.AddOn/GoveeClient.HomeAssistant.AddOn.csproj

COPY GoveeClient.Shared/ GoveeClient.Shared/
COPY GoveeClient/ GoveeClient/
COPY GoveeClient.HomeAssistant.AddOn/ GoveeClient.HomeAssistant.AddOn/

RUN dotnet publish GoveeClient.HomeAssistant.AddOn/GoveeClient.HomeAssistant.AddOn.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "GoveeClient.HomeAssistant.AddOn.dll"]
