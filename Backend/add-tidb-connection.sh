#!/bin/bash

# Add TiDB Cloud connection string to Azure Key Vault

KEY_VAULT_NAME="patchnotesdb-secret"

echo "======================================"
echo "Add TiDB Connection String to Key Vault"
echo "======================================"
echo ""

echo "This script will add your TiDB Cloud MySQL connection string to Azure Key Vault."
echo ""
echo "Your TiDB connection string should look like:"
echo "  Server=gateway01.us-west-2.prod.aws.tidbcloud.com;Port=4000;Database=your_db;User=your_user;Password=your_password;SslMode=VerifyFull;"
echo ""
echo "Important: Make sure your connection string includes:"
echo "  - Server (TiDB gateway host)"
echo "  - Port (usually 4000)"
echo "  - Database name"
echo "  - User"
echo "  - Password"
echo "  - SslMode=VerifyFull or SslMode=Required"
echo ""

read -sp "Paste your TiDB connection string: " CONNECTION_STRING
echo ""

if [ -z "$CONNECTION_STRING" ]; then
    echo "✗ Connection string cannot be empty"
    exit 1
fi

echo ""
echo "Adding connection string to Key Vault..."
echo ""

az keyvault secret set \
    --vault-name $KEY_VAULT_NAME \
    --name "ConnectionStrings--mysqldb" \
    --value "$CONNECTION_STRING"

if [ $? -eq 0 ]; then
    echo ""
    echo "======================================"
    echo "Success! ✓"
    echo "======================================"
    echo ""
    echo "Connection string added to Key Vault as 'ConnectionStrings--mysqldb'"
    echo ""
    echo "Next steps:"
    echo "1. Wait 30 seconds for Azure to propagate the secret"
    echo "2. Test your application:"
    echo "   curl https://api.patchnotes.cool"
    echo ""
    echo "3. View logs to verify it starts successfully:"
    echo "   az containerapp logs show -n patchnotes-backend -g patchnotes --follow"
    echo ""
    echo "Note: The container app will automatically restart and pick up the new connection string."
    echo ""
else
    echo ""
    echo "✗ Failed to add secret to Key Vault"
    echo "Make sure you have the 'Key Vault Secrets Officer' role."
    echo ""
fi
