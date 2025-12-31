#!/bin/bash

# Restart the container app to pick up new secrets

CONTAINER_APP_NAME="patchnotes-backend"
RESOURCE_GROUP="patchnotes"

echo "======================================"
echo "Restart Container App"
echo "======================================"
echo ""

echo "Getting active revision..."
ACTIVE_REVISION=$(az containerapp revision list \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --query "[?properties.active==\`true\`].name | [0]" -o tsv)

if [ -z "$ACTIVE_REVISION" ]; then
    echo "✗ Could not find active revision"
    echo ""
    echo "Listing all revisions:"
    az containerapp revision list \
        --name $CONTAINER_APP_NAME \
        --resource-group $RESOURCE_GROUP \
        -o table
    exit 1
fi

echo "Active revision: $ACTIVE_REVISION"
echo ""
echo "Restarting revision..."

az containerapp revision restart \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --revision $ACTIVE_REVISION

echo ""
echo "======================================"
echo "Restart Complete! ✓"
echo "======================================"
echo ""
echo "View logs to verify the app started successfully:"
echo "  az containerapp logs show -n $CONTAINER_APP_NAME -g $RESOURCE_GROUP --follow"
echo ""
