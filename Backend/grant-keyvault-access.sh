#!/bin/bash

# Grant yourself access to manage Key Vault secrets
# Run this once to enable adding/modifying secrets

KEY_VAULT_NAME="patchnotesdb-secret"

echo "======================================"
echo "Grant Key Vault Access to Current User"
echo "======================================"
echo ""

# Get current user's object ID
echo "Getting your Azure user information..."
USER_OBJECT_ID=$(az ad signed-in-user show --query "id" -o tsv)
USER_UPN=$(az ad signed-in-user show --query "userPrincipalName" -o tsv)

echo "User: $USER_UPN"
echo "Object ID: $USER_OBJECT_ID"
echo ""

# Get Key Vault resource ID
echo "Getting Key Vault information..."
KEY_VAULT_ID=$(az keyvault show \
    --name $KEY_VAULT_NAME \
    --query "id" -o tsv)

echo "Key Vault: $KEY_VAULT_NAME"
echo ""

# Assign Key Vault Secrets Officer role
echo "Assigning 'Key Vault Secrets Officer' role..."
echo "This will allow you to create, read, update, and delete secrets."
echo ""

az role assignment create \
    --role "Key Vault Secrets Officer" \
    --assignee-object-id $USER_OBJECT_ID \
    --assignee-principal-type User \
    --scope $KEY_VAULT_ID

if [ $? -eq 0 ]; then
    echo ""
    echo "======================================"
    echo "Access Granted! ✓"
    echo "======================================"
    echo ""
    echo "IMPORTANT: Role assignments may take 1-2 minutes to propagate."
    echo "Wait a couple minutes before running add-secrets-to-keyvault.sh"
    echo ""
else
    echo ""
    echo "✗ Failed to grant access"
    echo ""
    echo "You may need to:"
    echo "1. Have 'Owner' or 'User Access Administrator' role on the Key Vault or subscription"
    echo "2. Ask your Azure admin to grant you the 'Key Vault Secrets Officer' role"
    echo ""
fi
