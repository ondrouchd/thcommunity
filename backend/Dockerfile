# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy csproj and restore
COPY src/THcommunity/THcommunity.csproj src/THcommunity/
RUN dotnet restore src/THcommunity/THcommunity.csproj

# Copy everything and build
COPY . .
WORKDIR /src/src/THcommunity
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
EXPOSE 8080

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser
USER appuser

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "THcommunity.dll"]
