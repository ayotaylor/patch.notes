#!/bin/bash

# Check container app health and get detailed status

CONTAINER_APP_NAME="patchnotes-backend"
RESOURCE_GROUP="patchnotes"

echo "======================================"
echo "Container App Health Check"
echo "======================================"
echo ""

echo "Step 1: Container App Details"
echo "---"
az containerapp show \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --query "{Name:name, ProvisioningState:properties.provisioningState, RunningStatus:properties.runningStatus, Replicas:properties.template.scale.minReplicas, MaxReplicas:properties.template.scale.maxReplicas}" \
    -o json

echo ""
echo "Step 2: Revision Status"
echo "---"
az containerapp revision list \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --query "[].{Revision:name, Active:properties.active, Replicas:properties.replicas, TrafficWeight:properties.trafficWeight, ProvisioningState:properties.provisioningState, HealthState:properties.healthState}" \
    -o table

echo ""
echo "Step 3: Replica Details"
echo "---"
echo "Getting replica information..."
ACTIVE_REVISION=$(az containerapp revision list \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --query "[?properties.active==\`true\`].name | [0]" -o tsv)

if [ -z "$ACTIVE_REVISION" ]; then
    echo "⚠ No active revision found"
else
    echo "Active revision: $ACTIVE_REVISION"
    echo ""
    az containerapp replica list \
        --name $CONTAINER_APP_NAME \
        --resource-group $RESOURCE_GROUP \
        --revision $ACTIVE_REVISION 2>/dev/null || echo "No replicas running"
fi

echo ""
echo "Step 4: Container Console Logs (from Azure)"
echo "---"
echo "Attempting to get console logs..."
az containerapp logs show \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --type console \
    --tail 100 2>/dev/null || echo "Console logs not available"

echo ""
echo "Step 5: System Logs"
echo "---"
echo "Attempting to get system logs..."
az containerapp logs show \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --type system \
    --tail 100 2>/dev/null || echo "System logs not available"

echo ""
echo "======================================"
echo "Health Check Complete"
echo "======================================"
echo ""
echo "If no replicas are running, common causes:"
echo "  1. Missing required secrets (Jwt--SecretKey, ConnectionStrings--mysqldb)"
echo "  2. MySQL connection failure"
echo "  3. Embedding dimension validation failure"
echo "  4. Invalid configuration"
echo ""
echo "To view live logs as the app starts:"
echo "  az containerapp logs show -n $CONTAINER_APP_NAME -g $RESOURCE_GROUP --follow"
echo ""
