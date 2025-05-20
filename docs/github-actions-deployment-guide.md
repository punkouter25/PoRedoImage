# GitHub Actions Deployment Guide for Image Translation App

This document explains how to set up GitHub Actions to deploy your Image Translation application to Azure App Service.

## Prerequisites

1. An Azure account with an active subscription
2. A resource group for your application (or permission to create one)
3. GitHub repository with your application code
4. Administrative access to the GitHub repository to configure secrets

## Setup Instructions

### 1. Create Azure Resources

Before setting up GitHub Actions, ensure you have the following Azure resources:

- Azure App Service on Windows with .NET 9.x runtime
- Azure OpenAI service
- Azure Computer Vision service
- Azure Application Insights (can be created during deployment)

You can create these resources using the Azure Portal, Azure CLI, or the provided PowerShell script (`deploy-to-azure.ps1`).

### 2. Create a Service Principal for GitHub Actions and Configure GitHub Secrets

You can create a service principal and configure GitHub secrets using Azure CLI and GitHub CLI (gh). This makes the process more automated:

```powershell
# Login to Azure
az login

# Set variables - replace with your actual values
$subscriptionId = (az account show --query id -o tsv)
$resourceGroup = "PoRedoImage"  # Should match AZURE_RESOURCE_GROUP in workflow
$appServiceName = "PoRedoImage" # Should match AZURE_WEBAPP_NAME in workflow
$repoName = "YOUR_GITHUB_USERNAME/PoRedoImage"  # Replace with your GitHub username/repo

# Create service principal with Contributor role on the resource group
$sp = az ad sp create-for-rbac --name "github-actions-poredoimage" `
    --role "Contributor" `
    --scopes "/subscriptions/$subscriptionId/resourceGroups/$resourceGroup" `
    | ConvertFrom-Json

# Log in to GitHub CLI (if not already logged in)
gh auth status || gh auth login

# Set the required GitHub secrets using GitHub CLI
gh secret set AZURE_CLIENT_ID --body "$($sp.appId)" --repo $repoName
gh secret set AZURE_TENANT_ID --body "$($sp.tenant)" --repo $repoName
gh secret set AZURE_SUBSCRIPTION_ID --body "$subscriptionId" --repo $repoName

# Get App Service Configuration values
$appInsightsConnectionString = az webapp config appsettings list --name $appServiceName --resource-group $resourceGroup --query "[?name=='ApplicationInsights:ConnectionString'].value" -o tsv

# Get OpenAI configuration (you'll need to fill in these values)
$computerVisionEndpoint = az cognitiveservices account show --name YOUR_COMPUTER_VISION_NAME --resource-group $resourceGroup --query properties.endpoint -o tsv
# For the keys, you'll need to query them separately
$computerVisionKey = az cognitiveservices account keys list --name YOUR_COMPUTER_VISION_NAME --resource-group $resourceGroup --query key1 -o tsv
$openAIEndpoint = az cognitiveservices account show --name YOUR_OPENAI_NAME --resource-group $resourceGroup --query properties.endpoint -o tsv
$openAIKey = az cognitiveservices account keys list --name YOUR_OPENAI_NAME --resource-group $resourceGroup --query key1 -o tsv

# Set these values as GitHub secrets
gh secret set APPLICATIONINSIGHTS_CONNECTION_STRING --body "$appInsightsConnectionString" --repo $repoName
gh secret set COMPUTER_VISION_ENDPOINT --body "$computerVisionEndpoint" --repo $repoName
gh secret set COMPUTER_VISION_KEY --body "$computerVisionKey" --repo $repoName
gh secret set OPENAI_ENDPOINT --body "$openAIEndpoint" --repo $repoName
gh secret set OPENAI_KEY --body "$openAIKey" --repo $repoName
gh secret set OPENAI_CHAT_COMPLETIONS_DEPLOYMENT --body "YOUR_CHAT_DEPLOYMENT_NAME" --repo $repoName
gh secret set OPENAI_IMAGE_GENERATION_DEPLOYMENT --body "YOUR_IMAGE_DEPLOYMENT_NAME" --repo $repoName

Write-Host "GitHub secrets have been configured successfully"
```

> **Note**: Replace placeholders like `YOUR_GITHUB_USERNAME`, `YOUR_COMPUTER_VISION_NAME`, `YOUR_OPENAI_NAME`, and deployment names with your actual values.

### 3. Manually Configure GitHub Secrets (Alternative Method)

If you prefer to set up secrets manually, in your GitHub repository, go to **Settings > Secrets and variables > Actions** and add the following secrets:

#### Azure Authentication
- `AZURE_CLIENT_ID`: The client ID of the service principal
- `AZURE_TENANT_ID`: The tenant ID of the service principal
- `AZURE_SUBSCRIPTION_ID`: Your Azure subscription ID

#### Application Settings
- `APPLICATIONINSIGHTS_CONNECTION_STRING`: The connection string for Azure Application Insights
- `COMPUTER_VISION_ENDPOINT`: The endpoint URL of your Azure Computer Vision service
- `COMPUTER_VISION_KEY`: The API key for your Azure Computer Vision service
- `OPENAI_ENDPOINT`: The endpoint URL of your Azure OpenAI service
- `OPENAI_KEY`: The API key for your Azure OpenAI service
- `OPENAI_CHAT_COMPLETIONS_DEPLOYMENT`: The deployment name for chat completions (e.g., "gpt-35-turbo")
- `OPENAI_IMAGE_GENERATION_DEPLOYMENT`: The deployment name for image generation (e.g., "dall-e-3")

### 4. Run the Workflow

The workflow is triggered automatically when you push changes to the `main` branch. You can also trigger it manually:

1. Go to the **Actions** tab in your GitHub repository
2. Select the "Deploy Image Translation App to Azure" workflow
3. Click **Run workflow** and select the `main` branch

## Troubleshooting

If you encounter issues during deployment:

1. Check the workflow execution logs in GitHub Actions
2. Verify all secrets are correctly configured
3. Ensure the Azure service principal has sufficient permissions
4. Check the Azure App Service logs for runtime errors

## Security Notes

- Never commit your secrets to the repository
- Regularly rotate API keys and service principal credentials
- Consider implementing conditional deployments for production environments
- Review deployed resources regularly to ensure they meet security requirements
