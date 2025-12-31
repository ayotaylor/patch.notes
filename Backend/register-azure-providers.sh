#!/bin/bash

# Register required Azure resource providers
# Run this before the deployment script if you get registration errors

echo "======================================"
echo "Registering Azure Resource Providers"
echo "======================================"
echo ""

# Array of required providers for this deployment
PROVIDERS=(
    "Microsoft.ContainerRegistry"
    "Microsoft.App"
    "Microsoft.OperationalInsights"
)

echo "This will register the following Azure resource providers:"
for provider in "${PROVIDERS[@]}"; do
    echo "  - $provider"
done
echo ""

read -p "Press enter to continue or Ctrl+C to cancel..."
echo ""

# Register each provider
for provider in "${PROVIDERS[@]}"; do
    echo "Registering $provider..."

    # Check current state
    STATE=$(az provider show --namespace $provider --query "registrationState" -o tsv 2>/dev/null)

    if [ "$STATE" = "Registered" ]; then
        echo "✓ $provider is already registered"
    else
        echo "  Current state: $STATE"
        echo "  Registering..."
        az provider register --namespace $provider --wait
        echo "✓ $provider registered successfully"
    fi
    echo ""
done

echo "======================================"
echo "Registration Complete! ✓"
echo "======================================"
echo ""
echo "You can now run the deployment script:"
echo "  ./deploy-to-azure.sh"
echo ""
