#!/bin/bash

# Diagnose startup failures

CONTAINER_APP_NAME="patchnotes-backend"
RESOURCE_GROUP="patchnotes"

echo "======================================"
echo "Diagnose Startup Failure"
echo "======================================"
echo ""

echo "Step 1: Checking container console logs..."
echo "---"
az containerapp logs show \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --type console \
    --tail 100

echo ""
echo "Step 2: Checking system logs..."
echo "---"
az containerapp logs show \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --type system \
    --tail 50

echo ""
echo "Step 3: Checking current secrets in Key Vault..."
echo "---"
az keyvault secret list --vault-name patchnotesdb-secret --query "[].name" -o table

echo ""
echo "======================================"
echo "Common Startup Issues"
echo "======================================"
echo ""
echo "Look for these errors in the logs above:"
echo "  1. 'Value cannot be null' → Missing required secret"
echo "  2. 'Unable to connect' → MySQL connection issue"
echo "  3. 'Embedding dimension validation failed' → ML model issue"
echo "  4. 'SecretClient' or 'Key Vault' errors → RBAC permissions issue"
echo ""
