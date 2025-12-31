#!/bin/bash

# Verify the deployment status and check for issues

CONTAINER_APP_NAME="patchnotes-backend"
RESOURCE_GROUP="patchnotes"

echo "======================================"
echo "Verify Azure Deployment"
echo "======================================"
echo ""

echo "Step 1: Checking Container App status..."
echo "---"
az containerapp show \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --query "{Name:name, Status:properties.runningStatus, URL:properties.configuration.ingress.fqdn, Replicas:properties.template.scale}" \
    -o table

echo ""
echo "Step 2: Getting Application URL..."
echo "---"
APP_URL=$(az containerapp show \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --query "properties.configuration.ingress.fqdn" -o tsv)

if [ -z "$APP_URL" ]; then
    echo "⚠ No public URL found. Ingress may not be configured."
else
    echo "Application URL: https://$APP_URL"
    echo ""
    echo "Testing endpoint..."
    HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "https://$APP_URL" --max-time 10)

    if [ "$HTTP_STATUS" = "200" ]; then
        echo "✓ Application is responding (HTTP $HTTP_STATUS)"
    else
        echo "⚠ Application returned HTTP $HTTP_STATUS"
    fi
fi

echo ""
echo "Step 3: Checking recent logs (last 50 lines)..."
echo "---"
az containerapp logs show \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --tail 50 \
    --format text

echo ""
echo "======================================"
echo "Deployment Verification Complete"
echo "======================================"
echo ""
echo "Next steps:"
echo ""
echo "1. Review logs above for any errors"
echo ""
echo "2. Common issues to check:"
echo "   - MySQL connection errors → Verify ConnectionStrings--mysqldb secret"
echo "   - JWT errors → Verify Jwt--SecretKey secret is set"
echo "   - Qdrant errors → Expected if not configured yet"
echo "   - Embedding dimension errors → Check MLModel files are in container"
echo ""
echo "3. View live logs:"
echo "   az containerapp logs show -n $CONTAINER_APP_NAME -g $RESOURCE_GROUP --follow"
echo ""
echo "4. Test your API endpoints:"
echo "   curl https://$APP_URL/api/your-endpoint"
echo ""
