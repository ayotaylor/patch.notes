#!/bin/bash

# Script to add required secrets to Azure Key Vault
# Run this after the initial deployment

set -e

KEY_VAULT_NAME="patchnotesdb-secret"

echo "======================================"
echo "Adding Secrets to Azure Key Vault"
echo "======================================"
echo ""
echo "Key Vault: $KEY_VAULT_NAME"
echo ""

# Function to add secret with user input
add_secret() {
    local secret_name=$1
    local secret_description=$2
    local optional=$3

    echo "---"
    echo "Secret: $secret_name"
    echo "Description: $secret_description"

    if [ "$optional" = "optional" ]; then
        read -p "Add this secret? (y/n): " add_secret
        if [ "$add_secret" != "y" ]; then
            echo "Skipped."
            return
        fi
    fi

    read -sp "Enter value for $secret_name: " secret_value
    echo ""

    if [ -z "$secret_value" ]; then
        echo "⚠ Skipped (empty value)"
        return
    fi

    az keyvault secret set \
        --vault-name $KEY_VAULT_NAME \
        --name "$secret_name" \
        --value "$secret_value" > /dev/null

    echo "✓ Secret '$secret_name' added successfully"
}

echo "This script will help you add required secrets to Key Vault."
echo "Note: The MySQL connection string (ConnectionStrings--mysqldb) should already be configured."
echo ""
read -p "Press enter to continue or Ctrl+C to cancel..."

# Required secrets
echo ""
echo "=== REQUIRED SECRETS ==="
echo ""

add_secret "Jwt--SecretKey" "Secret key for JWT token signing (min 32 characters recommended)" "required"

# API Keys
echo ""
echo "=== API KEYS ==="
echo ""

add_secret "Igdb--ClientId" "IGDB (Internet Game Database) API Client ID" "optional"
add_secret "Igdb--ClientSecret" "IGDB API Client Secret" "optional"
add_secret "Groq--ApiKey" "Groq AI API Key for language model" "optional"

# OAuth Providers
echo ""
echo "=== OAUTH PROVIDERS (Optional) ==="
echo ""

add_secret "Authentication--Google--ClientId" "Google OAuth Client ID" "optional"
add_secret "Authentication--Google--ClientSecret" "Google OAuth Client Secret" "optional"
add_secret "Authentication--Facebook--AppId" "Facebook OAuth App ID" "optional"
add_secret "Authentication--Facebook--AppSecret" "Facebook OAuth App Secret" "optional"

# Infrastructure
echo ""
echo "=== INFRASTRUCTURE ==="
echo ""

add_secret "Qdrant--Url" "Qdrant vector database URL (e.g., http://your-qdrant:6333)" "optional"

# GPU Service (if using)
echo ""
echo "=== GPU SERVICE (Optional) ==="
echo ""
read -p "Are you using a GPU embedding service? (y/n): " use_gpu
if [ "$use_gpu" = "y" ]; then
    add_secret "EmbeddingService--GpuServiceUrl" "GPU Embedding Service URL" "required"
    az keyvault secret set \
        --vault-name $KEY_VAULT_NAME \
        --name "EmbeddingService--UseGpuService" \
        --value "true" > /dev/null
    echo "✓ GPU service enabled"
fi

echo ""
echo "======================================"
echo "Secrets Configuration Complete! ✓"
echo "======================================"
echo ""
echo "To view all secrets in the Key Vault:"
echo "  az keyvault secret list --vault-name $KEY_VAULT_NAME -o table"
echo ""
echo "To restart the container app and pick up new secrets:"
echo "  az containerapp revision restart --name patchnotes-backend --resource-group patchnotes-rg"
echo ""
