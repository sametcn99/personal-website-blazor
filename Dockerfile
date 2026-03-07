# Build and publish stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["personal-website-blazor.csproj", "./"]
COPY ["src/Application/personal-website-blazor.Application.csproj", "src/Application/"]
COPY ["src/Client/personal-website-blazor.Client.csproj", "src/Client/"]
COPY ["src/Domain/personal-website-blazor.Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/personal-website-blazor.Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Shared/personal-website-blazor.Shared.csproj", "src/Shared/"]
RUN dotnet restore "personal-website-blazor.csproj"

# Copy everything else and publish directly
COPY . .
RUN dotnet publish "personal-website-blazor.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Expose port (Render.com will use the PORT environment variable)
EXPOSE 8080

# Copy published app
COPY --from=build /app/publish .

# Set environment variables for production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_USE_POLLING_FILE_WATCHER=1
ENV ASPNETCORE_hostBuilder__reloadConfigOnChange=false
ENV PORT=8080
# .NET config key: GitHub:Token -> environment variable: GitHub__Token
ENV GitHub__Token=change_me

# Run the application
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet personal-website-blazor.dll"]
