# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY src/THcommunity/THcommunity.csproj src/THcommunity/
RUN dotnet restore src/THcommunity/THcommunity.csproj

# Copy everything and publish
COPY . .
WORKDIR /src/src/THcommunity
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

# curl is needed for the container healthcheck
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# The .NET runtime image already ships with a non-root "app" user
COPY --from=build /app/publish .
USER app

ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "THcommunity.dll"]
