# Build and publish stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["personal-website-blazor.csproj", "./"]
RUN dotnet restore "personal-website-blazor.csproj"

# Copy everything else and publish directly
COPY . .
RUN dotnet publish "personal-website-blazor.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Expose port (Render.com will use the PORT environment variable)
EXPOSE 8080

# Copy published app
COPY --from=build /app/publish .

# Set environment variables for production
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_USE_POLLING_FILE_WATCHER=1
ENV ASPNETCORE_hostBuilder__reloadConfigOnChange=false
ENV GITHUB_TOKEN=change_me

# Run the application
ENTRYPOINT ["dotnet", "personal-website-blazor.dll"]
