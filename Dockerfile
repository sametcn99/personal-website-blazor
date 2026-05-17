# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore
COPY personal-website-blazor.csproj .
RUN dotnet restore

# Copy everything else and publish
COPY . .
RUN dotnet publish "personal-website-blazor.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN mkdir -p /app/data-protection-keys

EXPOSE 8080

VOLUME ["/app/data-protection-keys"]

# Copy published app
COPY --from=build /app/publish .

# Production settings
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_hostBuilder__reloadConfigOnChange=false
ENV PORT=8080
ENV DataProtection__KeysPath=/app/data-protection-keys

ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet personal-website-blazor.dll"]
