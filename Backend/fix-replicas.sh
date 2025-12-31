#!/bin/bash

# Set minimum replicas to 1 so the app stays running

CONTAINER_APP_NAME="patchnotes-backend"
RESOURCE_GROUP="patchnotes"

echo "======================================"
echo "Set Minimum Replicas"
echo "======================================"
echo ""

echo "Setting minimum replicas to 1..."
echo "This ensures the app stays running and we can see logs."
echo ""

az containerapp update \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --min-replicas 1 \
    --max-replicas 10

echo ""
echo "✓ Replica configuration updated"
echo ""
echo "Waiting for replica to start (this may take 30-60 seconds)..."
echo "You can monitor progress with:"
echo "  az containerapp logs show -n $CONTAINER_APP_NAME -g $RESOURCE_GROUP --follow"
echo ""

# Wait a bit for the replica to start
sleep 10

echo "Checking replica status..."
az containerapp replica list \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --revision patchnotes-backend--szql7a0 2>/dev/null || echo "No replicas yet, still starting..."

echo ""
echo "Getting application URL..."
APP_URL=$(az containerapp show \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --query "properties.configuration.ingress.fqdn" -o tsv)

echo "Application URL: https://$APP_URL"
echo ""
echo "Note: It may take a minute for the replica to start."
echo "Check logs for any startup errors."
echo ""
