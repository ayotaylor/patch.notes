# Use the official .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY Backend.csproj .
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Copy the published app
COPY --from=build /app/publish .

# Ensure ML Model files are present
COPY MLModel ./MLModel

# Set environment variables for Azure Container Apps
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port 8080 (Azure Container Apps default)
EXPOSE 8080

# Run the application
ENTRYPOINT ["dotnet", "Backend.dll"]
