#!/bin/bash

# Fix ingress to make the app publicly accessible

CONTAINER_APP_NAME="patchnotes-backend"
RESOURCE_GROUP="patchnotes"

echo "======================================"
echo "Fix Container App Ingress"
echo "======================================"
echo ""

echo "Updating container app to enable external ingress..."
az containerapp ingress update \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --type external \
    --target-port 8080 \
    --transport auto

echo ""
echo "✓ Ingress updated to external"
echo ""

# Get new URL
APP_URL=$(az containerapp show \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --query "properties.configuration.ingress.fqdn" -o tsv)

echo "New Application URL: https://$APP_URL"
echo ""
echo "Note: The app may still not respond if there are startup errors."
echo "Check logs with the next script."
echo ""
