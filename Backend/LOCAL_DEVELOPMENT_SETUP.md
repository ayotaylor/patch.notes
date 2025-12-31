# Local Development Setup

## Setting Up User Secrets

For local development, use .NET User Secrets to store sensitive configuration values. This keeps secrets out of your appsettings files and git repository.

### 1. Initialize User Secrets (if not already done)

The project already has a UserSecretsId configured in Backend.csproj.

### 2. Add Required Secrets

Run these commands from the Backend directory:

```bash
cd /Users/ayo/workspace/patch.notes/Backend

# MySQL Connection String (REQUIRED)
dotnet user-secrets set "ConnectionStrings:mysqldb" "Server=localhost;Port=3306;Database=patchnotes;User=your_user;Password=your_password;"

# JWT Secret Key (REQUIRED)
dotnet user-secrets set "Jwt:SecretKey" "your-local-development-secret-key-minimum-32-characters"

# IGDB API (Optional - only if using game database features)
dotnet user-secrets set "Igdb:ClientId" "your-igdb-client-id"
dotnet user-secrets set "Igdb:ClientSecret" "your-igdb-client-secret"

# Groq API (Optional - only if using AI features)
dotnet user-secrets set "Groq:ApiKey" "your-groq-api-key"

# OAuth (Optional - only if using social login)
dotnet user-secrets set "Authentication:Google:ClientId" "your-google-client-id"
dotnet user-secrets set "Authentication:Google:ClientSecret" "your-google-client-secret"
dotnet user-secrets set "Authentication:Facebook:AppId" "your-facebook-app-id"
dotnet user-secrets set "Authentication:Facebook:AppSecret" "your-facebook-app-secret"
```

### 3. View Your Secrets

```bash
# List all secrets
dotnet user-secrets list

# Clear all secrets (if you need to start over)
dotnet user-secrets clear
```

### 4. Generate a Secure JWT Secret

```bash
# Generate a random 32-byte base64 secret
openssl rand -base64 32
```

## Configuration Priority

.NET Core configuration loads in this order (later sources override earlier ones):

1. `appsettings.json` (base configuration)
2. `appsettings.Development.json` (when ASPNETCORE_ENVIRONMENT=Development)
3. User Secrets (development only)
4. Environment Variables
5. Command-line arguments

## Local vs Production

### Local Development
- **Environment:** Development
- **Key Vault:** Disabled (AzureKeyVaultUrl is empty in appsettings.Development.json)
- **Secrets:** Stored in User Secrets
- **MySQL:** Local MySQL instance
- **Qdrant:** Local Qdrant at localhost:6333

### Production (Azure)
- **Environment:** Production
- **Key Vault:** Enabled (AzureKeyVaultUrl set in appsettings.Production.json)
- **Secrets:** Loaded from Azure Key Vault
- **MySQL:** Azure MySQL Database
- **Qdrant:** Configure via Key Vault secret when ready

## Required Secrets for Azure Key Vault

Ensure these secrets are set in Azure Key Vault for production:

```bash
# Required
ConnectionStrings--mysqldb
Jwt--SecretKey

# Optional (add as needed)
Igdb--ClientId
Igdb--ClientSecret
Groq--ApiKey
Qdrant--Url
Authentication--Google--ClientId
Authentication--Google--ClientSecret
Authentication--Facebook--AppId
Authentication--Facebook--AppSecret
```

Note: Azure Key Vault uses `--` instead of `:` for nested configuration keys.

## Troubleshooting

### "MySQL connection string 'mysqldb' is not configured"
- Add the connection string to user secrets (see step 2 above)

### "JWT Secret Key is not configured"
- Add Jwt:SecretKey to user secrets

### App works locally but not in Azure
- Verify all required secrets are in Azure Key Vault
- Check that managed identity has "Key Vault Secrets User" role
- View Azure logs: `az containerapp logs show -n patchnotes-backend -g patchnotes --follow`
