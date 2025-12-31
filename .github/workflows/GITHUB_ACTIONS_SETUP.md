# GitHub Actions CI/CD Setup

This repository uses GitHub Actions to automatically deploy the backend and frontend to Azure when code is pushed to the `main` branch.

## Workflows

### Backend Deployment (`deploy-backend.yml`)
- **Triggers:** Push to `main` branch when `Backend/**` files change, or manual trigger
- **Actions:**
  1. Builds Docker image from `Backend/Dockerfile`
  2. Pushes image to Azure Container Registry (`patchnotesacr`)
  3. Deploys to Azure Container App (`patchnotes-backend`)
  4. Performs health check

### Frontend Deployment (`deploy-frontend.yml`)
- **Triggers:** Push to `main` branch when `frontend/**` files change, or manual trigger
- **Actions:**
  1. Builds Vue.js application
  2. Deploys to Azure Static Web Apps
  3. Accessible at https://www.patchnotes.cool

## Required GitHub Secrets

You need to configure these secrets in your GitHub repository settings (Settings → Secrets and variables → Actions):

### 1. `AZURE_CREDENTIALS` (Backend)

Azure service principal credentials for backend deployment.

**You mentioned you already have this configured.** ✓

If you need to recreate it:

```bash
az ad sp create-for-rbac \
  --name "github-actions-patchnotes" \
  --role contributor \
  --scopes /subscriptions/<subscription-id>/resourceGroups/patchnotes \
  --sdk-auth
```

Copy the entire JSON output and add it as a secret named `AZURE_CREDENTIALS`.

### 2. `AZURE_STATIC_WEB_APPS_API_TOKEN` (Frontend)

API token for Azure Static Web Apps deployment.

**You already have this configured.** ✓

To find it:
```bash
az staticwebapp secrets list \
  --name <your-static-web-app-name> \
  --query "properties.apiKey" -o tsv
```

## Verifying Setup

### Check Existing Secrets

Go to your GitHub repository:
- Settings → Secrets and variables → Actions → Repository secrets

You should see:
- ✓ `AZURE_CREDENTIALS`
- ✓ `AZURE_STATIC_WEB_APPS_API_TOKEN`

### Manual Deployment

You can manually trigger deployments:
1. Go to Actions tab in GitHub
2. Select the workflow (Backend or Frontend)
3. Click "Run workflow" → "Run workflow"

## Monitoring Deployments

### GitHub Actions
- View workflow runs: https://github.com/YOUR_USERNAME/patch.notes/actions
- Each run shows:
  - Build logs
  - Docker build output
  - Deployment status
  - Health check results

### Azure Logs

View backend logs:
```bash
az containerapp logs show -n patchnotes-backend -g patchnotes --follow
```

## Deployment Flow

### Backend Changes
```
Push to main → GitHub Actions triggers
  ↓
Build Docker image with latest code
  ↓
Tag image with commit SHA + latest
  ↓
Push to Azure Container Registry
  ↓
Update Container App with new image
  ↓
Health check
  ↓
✓ Deployment complete
```

### Frontend Changes
```
Push to main → GitHub Actions triggers
  ↓
Build Vue.js app (npm run build)
  ↓
Deploy to Azure Static Web Apps
  ↓
✓ Deployment complete
```

## Troubleshooting

### Backend deployment fails
1. Check GitHub Actions logs for errors
2. Verify `AZURE_CREDENTIALS` secret is valid
3. Check Azure Container App logs:
   ```bash
   az containerapp logs show -n patchnotes-backend -g patchnotes --tail 100
   ```

### Frontend deployment fails
1. Check GitHub Actions logs
2. Verify `AZURE_STATIC_WEB_APPS_API_TOKEN` is valid
3. Check build errors in the logs

### Image not updating
- Check that the commit SHA changed
- Verify image was pushed to ACR:
  ```bash
  az acr repository show-tags --name patchnotesacr --repository patchnotes-backend
  ```

## Next Steps After Setup

1. **Delete old workflow:**
   - Archive or delete `.github/workflows/deploy.yml` (the old combined workflow)

2. **Test the workflows:**
   - Make a small change to `Backend/README.md` or similar
   - Push to main
   - Verify deployment in GitHub Actions

3. **Monitor first deployment:**
   - Watch logs to ensure everything works
   - Verify app is accessible at https://api.patchnotes.cool

## Environment-Specific Configuration

### Local Development
- Uses user secrets (not committed to git)
- See `Backend/LOCAL_DEVELOPMENT_SETUP.md`

### Production (Azure)
- Uses Azure Key Vault for secrets
- Automatically loaded via `appsettings.Production.json`
- Required Key Vault secrets:
  - `ConnectionStrings--mysqldb`
  - `Jwt--SecretKey`
  - Optional: IGDB, Groq, OAuth credentials

## Cost Optimization

The backend uses **min-replicas = 0** to save costs:
- Scales to 0 when no traffic
- Starts automatically on first request (30-90 second cold start)
- Stays within Azure free tier for low-traffic apps
