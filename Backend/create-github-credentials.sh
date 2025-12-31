#!/bin/bash

# Create Azure service principal for GitHub Actions

echo "======================================"
echo "Create Azure Service Principal for GitHub Actions"
echo "======================================"
echo ""

# Get subscription ID
SUBSCRIPTION_ID=$(az account show --query "id" -o tsv)
SUBSCRIPTION_NAME=$(az account show --query "name" -o tsv)

echo "Current Subscription:"
echo "  Name: $SUBSCRIPTION_NAME"
echo "  ID: $SUBSCRIPTION_ID"
echo ""

RESOURCE_GROUP="patchnotes"

echo "Resource Group: $RESOURCE_GROUP"
echo ""
echo "This will create a service principal with 'Contributor' role on the resource group."
echo ""
read -p "Press enter to continue or Ctrl+C to cancel..."
echo ""

# Create service principal
echo "Creating service principal..."
echo ""

CREDENTIALS=$(az ad sp create-for-rbac \
  --name "github-actions-patchnotes-backend" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP \
  --sdk-auth)

if [ $? -eq 0 ]; then
    echo ""
    echo "======================================"
    echo "Service Principal Created! ✓"
    echo "======================================"
    echo ""
    echo "Copy the JSON below and add it to GitHub as a secret named 'AZURE_CREDENTIALS':"
    echo ""
    echo "------- START COPYING HERE -------"
    echo "$CREDENTIALS"
    echo "------- END COPYING HERE ---------"
    echo ""
    echo "Steps to add to GitHub:"
    echo "1. Go to your GitHub repository"
    echo "2. Settings → Secrets and variables → Actions"
    echo "3. Click 'New repository secret'"
    echo "4. Name: AZURE_CREDENTIALS"
    echo "5. Value: Paste the JSON above"
    echo "6. Click 'Add secret'"
    echo ""
else
    echo ""
    echo "✗ Failed to create service principal"
    echo ""
    echo "Common reasons:"
    echo "1. You need 'Owner' or 'User Access Administrator' role on the subscription/resource group"
    echo "2. The service principal name already exists (try a different name)"
    echo ""
fi
