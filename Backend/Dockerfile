FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the .csproj first for caching
COPY ["Backend/MyApi.csproj", "Backend/"]
RUN dotnet restore "Backend/MyApi.csproj"

# Copy the rest and build
COPY . .
WORKDIR "/src/Backend"
RUN dotnet publish "MyApi.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
# Azure Container Apps default port
ENV ASPNETCORE_URLS=http://+:8080 
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MyApi.dll"]
