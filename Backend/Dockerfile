FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Label is required for GHCR to link to your repo
LABEL org.opencontainers.image.source=https://github.com/YOUR_USER/YOUR_REPO

COPY ["MyApi.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .
EXPOSE 8080
ENTRYPOINT ["dotnet", "MyApi.dll"]
