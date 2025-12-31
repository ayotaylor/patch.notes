#!/bin/bash

# Azure Deployment Script for PatchNotes Backend
# This script will guide you through deploying to Azure Container Apps

set -e  # Exit on error

echo "======================================"
echo "PatchNotes Backend - Azure Deployment"
echo "======================================"
echo ""

# Configuration - UPDATE THESE VALUES
RESOURCE_GROUP="patchnotes"  # Update with your resource group name
LOCATION="eastus"  # Update with your preferred location
ACR_NAME="patchnotesacr"  # Update with your desired ACR name (must be globally unique, lowercase, no hyphens)
CONTAINER_APP_NAME="patchnotes-backend"
KEY_VAULT_NAME="patchnotesdb-secret"
IMAGE_NAME="patchnotes-backend"
IMAGE_TAG="latest"

echo "Configuration:"
echo "  Resource Group: $RESOURCE_GROUP"
echo "  Location: $LOCATION"
echo "  ACR Name: $ACR_NAME"
echo "  Container App: $CONTAINER_APP_NAME"
echo "  Key Vault: $KEY_VAULT_NAME"
echo ""
read -p "Press enter to continue or Ctrl+C to cancel..."

# Step 1: Verify Azure CLI login
echo ""
echo "Step 1: Verifying Azure CLI login..."
if ! az account show &> /dev/null; then
    echo "Not logged in to Azure. Running 'az login'..."
    az login
else
    echo "✓ Already logged in to Azure"
    az account show --query "{Subscription:name, TenantId:tenantId}" -o table
fi

# Step 2: Create Azure Container Registry
echo ""
echo "Step 2: Creating Azure Container Registry..."
if az acr show --name $ACR_NAME --resource-group $RESOURCE_GROUP &> /dev/null; then
    echo "✓ ACR '$ACR_NAME' already exists"
else
    echo "Creating ACR '$ACR_NAME'..."
    az acr create \
        --resource-group $RESOURCE_GROUP \
        --name $ACR_NAME \
        --sku Basic \
        --location $LOCATION \
        --admin-enabled true
    echo "✓ ACR created successfully"
fi

# Step 3: Build Docker image
echo ""
echo "Step 3: Building Docker image..."
echo "Building $IMAGE_NAME:$IMAGE_TAG..."
docker build -t $IMAGE_NAME:$IMAGE_TAG .

if [ $? -eq 0 ]; then
    echo "✓ Docker image built successfully"
else
    echo "✗ Docker build failed"
    exit 1
fi

# Step 4: Tag image for ACR
echo ""
echo "Step 4: Tagging image for ACR..."
ACR_LOGIN_SERVER="${ACR_NAME}.azurecr.io"
docker tag $IMAGE_NAME:$IMAGE_TAG $ACR_LOGIN_SERVER/$IMAGE_NAME:$IMAGE_TAG
echo "✓ Image tagged as $ACR_LOGIN_SERVER/$IMAGE_NAME:$IMAGE_TAG"

# Step 5: Login to ACR and push image
echo ""
echo "Step 5: Pushing image to Azure Container Registry..."
az acr login --name $ACR_NAME
docker push $ACR_LOGIN_SERVER/$IMAGE_NAME:$IMAGE_TAG
echo "✓ Image pushed successfully"

# Step 6: Get Container App details
echo ""
echo "Step 6: Getting Container App details..."
CONTAINER_APP_ENV=$(az containerapp show \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --query "properties.environmentId" -o tsv | xargs basename)
echo "  Environment: $CONTAINER_APP_ENV"

# Step 7: Enable managed identity (if not already enabled)
echo ""
echo "Step 7: Enabling managed identity for Container App..."
IDENTITY_PRINCIPAL_ID=$(az containerapp identity assign \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --system-assigned \
    --query "principalId" -o tsv 2>/dev/null || \
    az containerapp show \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --query "identity.principalId" -o tsv)

if [ -z "$IDENTITY_PRINCIPAL_ID" ]; then
    echo "✗ Failed to get or create managed identity"
    exit 1
fi
echo "✓ Managed Identity Principal ID: $IDENTITY_PRINCIPAL_ID"

# Step 8: Grant Key Vault access to managed identity
echo ""
echo "Step 8: Granting Key Vault access to managed identity..."
az keyvault set-policy \
    --name $KEY_VAULT_NAME \
    --object-id $IDENTITY_PRINCIPAL_ID \
    --secret-permissions get list

echo "✓ Key Vault access granted"

# Step 9: Update Container App with new image
echo ""
echo "Step 9: Updating Container App with new image..."
az containerapp update \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --image $ACR_LOGIN_SERVER/$IMAGE_NAME:$IMAGE_TAG \
    --set-env-vars \
        ASPNETCORE_ENVIRONMENT=Production \
        ASPNETCORE_URLS=http://+:8080

echo "✓ Container App updated successfully"

# Step 10: Get Container App URL
echo ""
echo "Step 10: Getting Container App URL..."
APP_URL=$(az containerapp show \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --query "properties.configuration.ingress.fqdn" -o tsv)

echo ""
echo "======================================"
echo "Deployment Complete! ✓"
echo "======================================"
echo ""
echo "Application URL: https://$APP_URL"
echo ""
echo "Next steps:"
echo "1. Add required secrets to Key Vault (see add-secrets-to-keyvault.sh)"
echo "2. Configure Qdrant URL in Key Vault when ready"
echo "3. Test the application"
echo ""
echo "View logs with:"
echo "  az containerapp logs show --name $CONTAINER_APP_NAME --resource-group $RESOURCE_GROUP --follow"
echo ""
