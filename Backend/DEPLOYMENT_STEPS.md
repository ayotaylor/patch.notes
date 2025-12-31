# Quick Deployment Steps

## Prerequisites

1. **Install Azure CLI** (if not already installed):
   ```bash
   # macOS
   brew install azure-cli

   # Or download from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli
   ```

2. **Install Docker** (if not already installed):
   - Docker Desktop for Mac: https://www.docker.com/products/docker-desktop

3. **Verify installations**:
   ```bash
   az --version
   docker --version
   ```

## Before Running Deployment

1. **Update configuration values** in `deploy-to-azure.sh`:
   - `RESOURCE_GROUP` - Your Azure resource group name
   - `LOCATION` - Azure region (e.g., "eastus", "westus2")
   - `ACR_NAME` - Desired name for Azure Container Registry (must be globally unique, lowercase alphanumeric only)

2. **Gather your secrets** for Key Vault (you'll need these):
   - ✓ MySQL connection string (already configured)
   - JWT Secret Key (generate a random 32+ character string)
   - IGDB API credentials (if using game database features)
   - Groq API Key (if using AI recommendations)
   - Google/Facebook OAuth credentials (if using social login)
   - Qdrant URL (when you're ready to configure it)

## Deployment Process

### Step 1: Run Main Deployment Script

```bash
cd /Users/ayo/workspace/patch.notes/Backend
./deploy-to-azure.sh
```

This script will:
- Create Azure Container Registry
- Build Docker image
- Push image to ACR
- Enable managed identity
- Grant Key Vault access
- Deploy to your Container App

Expected time: 5-10 minutes

### Step 2: Add Secrets to Key Vault

```bash
./add-secrets-to-keyvault.sh
```

This interactive script will prompt you for each secret. You can skip optional ones.

### Step 3: Restart Container App (to pick up secrets)

```bash
az containerapp revision restart \
  --name patchnotes-backend \
  --resource-group <your-resource-group>
```

### Step 4: Verify Deployment

```bash
# Get the app URL
az containerapp show \
  --name patchnotes-backend \
  --resource-group <your-resource-group> \
  --query "properties.configuration.ingress.fqdn" -o tsv

# View logs
az containerapp logs show \
  --name patchnotes-backend \
  --resource-group <your-resource-group> \
  --follow
```

## Manual Deployment (Alternative)

If you prefer to run commands manually instead of using the script:

### 1. Create ACR
```bash
RESOURCE_GROUP="patchnotes-rg"
ACR_NAME="patchnotesacr"  # Must be unique

az acr create \
  --resource-group $RESOURCE_GROUP \
  --name $ACR_NAME \
  --sku Basic \
  --admin-enabled true
```

### 2. Build and Push Image
```bash
# Build
docker build -t patchnotes-backend:latest .

# Tag
docker tag patchnotes-backend:latest $ACR_NAME.azurecr.io/patchnotes-backend:latest

# Login and push
az acr login --name $ACR_NAME
docker push $ACR_NAME.azurecr.io/patchnotes-backend:latest
```

### 3. Enable Managed Identity
```bash
az containerapp identity assign \
  --name patchnotes-backend \
  --resource-group $RESOURCE_GROUP \
  --system-assigned
```

### 4. Grant Key Vault Access
```bash
# Get the principal ID
PRINCIPAL_ID=$(az containerapp show \
  --name patchnotes-backend \
  --resource-group $RESOURCE_GROUP \
  --query "identity.principalId" -o tsv)

# Grant access
az keyvault set-policy \
  --name patchnotesdb-secret \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

### 5. Update Container App
```bash
az containerapp update \
  --name patchnotes-backend \
  --resource-group $RESOURCE_GROUP \
  --image $ACR_NAME.azurecr.io/patchnotes-backend:latest
```

## Testing After Deployment

1. **Health Check**: Visit `https://<your-app-url>/` to verify the app is running

2. **Check Logs**: Look for any startup errors
   ```bash
   az containerapp logs show \
     --name patchnotes-backend \
     --resource-group $RESOURCE_GROUP \
     --follow
   ```

3. **Verify Secrets**: The app should connect to MySQL and load secrets from Key Vault

4. **Test API Endpoints**: Try accessing your API endpoints

## Common Issues

### Issue: "Embedding dimension validation failed"
- The app validates ML model dimensions at startup
- Check that `MLModel/model.onnx` and `MLModel/vocab.txt` are included in the container
- View logs to see the specific error message

### Issue: "Cannot connect to MySQL"
- Verify the `ConnectionStrings--mysqldb` secret in Key Vault is correct
- Ensure the MySQL server allows connections from Azure services
- Check if MySQL firewall rules are configured

### Issue: "Key Vault access denied"
- Verify managed identity is enabled
- Confirm Key Vault access policy is set correctly
- Wait a few minutes for Azure AD propagation

### Issue: "Qdrant connection failed"
- Expected if you haven't configured Qdrant yet
- Add `Qdrant--Url` secret to Key Vault when ready
- Restart the container app after adding the secret

## Updating Your Application

When you make code changes:

```bash
# Rebuild and push
docker build -t patchnotes-backend:latest .
docker tag patchnotes-backend:latest $ACR_NAME.azurecr.io/patchnotes-backend:latest
az acr login --name $ACR_NAME
docker push $ACR_NAME.azurecr.io/patchnotes-backend:latest

# Update container app
az containerapp update \
  --name patchnotes-backend \
  --resource-group $RESOURCE_GROUP \
  --image $ACR_NAME.azurecr.io/patchnotes-backend:latest
```

## Monitoring

```bash
# View recent logs
az containerapp logs show \
  --name patchnotes-backend \
  --resource-group $RESOURCE_GROUP \
  --tail 100

# Follow logs in real-time
az containerapp logs show \
  --name patchnotes-backend \
  --resource-group $RESOURCE_GROUP \
  --follow

# View container app details
az containerapp show \
  --name patchnotes-backend \
  --resource-group $RESOURCE_GROUP
```

## Rollback

If you need to rollback to a previous version:

```bash
# List revisions
az containerapp revision list \
  --name patchnotes-backend \
  --resource-group $RESOURCE_GROUP \
  -o table

# Activate a previous revision
az containerapp revision activate \
  --name patchnotes-backend \
  --resource-group $RESOURCE_GROUP \
  --revision <revision-name>
```
