# Azure Container Apps Deployment Guide

## Prerequisites
- Azure Container App: `patchnotes-backend`
- Azure Key Vault: `patchnotesdb-secret`
- Azure MySQL Database (connection string stored in Key Vault)

## Required Azure Secrets in Key Vault

The following secrets must be configured in your Azure Key Vault (`patchnotesdb-secret.vault.azure.net`):

### Database
- `ConnectionStrings--mysqldb` - MySQL connection string (already configured)

### JWT Configuration
- `Jwt--SecretKey` - Secret key for JWT token signing

### External APIs
- `Igdb--ClientId` - IGDB API client ID
- `Igdb--ClientSecret` - IGDB API client secret
- `Groq--ApiKey` - Groq AI API key
- `Authentication--Google--ClientId` - Google OAuth client ID
- `Authentication--Google--ClientSecret` - Google OAuth client secret
- `Authentication--Facebook--AppId` - Facebook OAuth app ID
- `Authentication--Facebook--AppSecret` - Facebook OAuth app secret

### Infrastructure Services
- `Qdrant--Url` - Qdrant vector database URL (e.g., `http://your-qdrant-service:6333`)
- `EmbeddingService--GpuServiceUrl` - GPU embedding service URL (optional, only if UseGpuService is true)

## Environment Variables to Configure in Azure Container App

In addition to Key Vault secrets, configure these environment variables in your Container App:

```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
```

Optional configuration (can be set via environment variables or Key Vault):
```bash
EmbeddingService__UseGpuService=false  # Set to true if you have GPU service
```

## Deployment Steps

### 1. Build and Push Docker Image

```bash
# Navigate to Backend directory
cd /Users/ayo/workspace/patch.notes/Backend

# Build the Docker image
docker build -t patchnotes-backend:latest .

# Tag for Azure Container Registry (replace <your-acr> with your registry name)
docker tag patchnotes-backend:latest <your-acr>.azurecr.io/patchnotes-backend:latest

# Login to Azure Container Registry
az acr login --name <your-acr>

# Push the image
docker push <your-acr>.azurecr.io/patchnotes-backend:latest
```

### 2. Update Container App

```bash
# Update the container app with the new image
az containerapp update \
  --name patchnotes-backend \
  --resource-group <your-resource-group> \
  --image <your-acr>.azurecr.io/patchnotes-backend:latest
```

### 3. Configure Managed Identity (if not already done)

Your Container App needs a managed identity to access Key Vault:

```bash
# Enable system-assigned managed identity
az containerapp identity assign \
  --name patchnotes-backend \
  --resource-group <your-resource-group> \
  --system-assigned

# Grant the managed identity access to Key Vault
az keyvault set-policy \
  --name patchnotesdb-secret \
  --object-id <managed-identity-principal-id> \
  --secret-permissions get list
```

### 4. Verify Deployment

```bash
# Check container app status
az containerapp show \
  --name patchnotes-backend \
  --resource-group <your-resource-group>

# View logs
az containerapp logs show \
  --name patchnotes-backend \
  --resource-group <your-resource-group> \
  --follow
```

## Important Infrastructure Dependencies

### Qdrant Vector Database
Your application requires a Qdrant vector database. You have two options:

1. **Self-hosted in Azure Container Apps**:
   ```bash
   az containerapp create \
     --name qdrant \
     --resource-group <your-resource-group> \
     --environment <your-environment> \
     --image qdrant/qdrant:latest \
     --target-port 6333 \
     --ingress internal \
     --min-replicas 1 \
     --max-replicas 1
   ```
   Then set Key Vault secret: `Qdrant--Url` = `http://qdrant:6333`

2. **Qdrant Cloud**: Use managed Qdrant service and configure the URL in Key Vault

### ML Model Files
The Docker image includes the ONNX model files from the `MLModel` directory. Ensure these files exist:
- `model.onnx`
- `vocab.txt`
- `tokenizer.json` (if present)
- `config.json` (if present)

## CORS Configuration

The application is configured to allow requests from:
- `http://localhost:8080` (development)
- `http://localhost:3000` (development)
- `https://www.patchnotes.cool` (production)

Update [Program.cs:209](Program.cs#L209) if you need to add additional origins.

## Database Migrations

If you need to run migrations on the Azure MySQL database:

```bash
# From your local machine with connection string
dotnet ef database update --connection "<your-mysql-connection-string>"
```

Or configure a startup migration in the application (can be added to Program.cs if needed).

## Monitoring and Troubleshooting

### View Application Logs
```bash
az containerapp logs show \
  --name patchnotes-backend \
  --resource-group <your-resource-group> \
  --follow
```

### Common Issues

1. **Key Vault Access Denied**: Ensure managed identity has proper permissions
2. **Database Connection Failed**: Verify MySQL connection string in Key Vault
3. **Qdrant Connection Failed**: Ensure Qdrant service is running and accessible
4. **Embedding Dimension Validation Failed**: The app validates embedding dimensions at startup. Check logs for specific error messages.

## Security Considerations

1. All secrets should be stored in Azure Key Vault, not in appsettings.json
2. Use managed identity for authentication (no connection strings for Key Vault)
3. Ensure MySQL connection uses SSL/TLS
4. Keep the container image updated with latest security patches
5. Review CORS policy for production domains

## Performance Optimization

Current configuration:
- Database connection pool size: 64 connections
- gRPC message size: 16MB max
- Command timeout: 120 seconds
- Retry policy: 3 attempts with 5-second delay

Adjust these in [Program.cs](Program.cs) based on your workload requirements.
