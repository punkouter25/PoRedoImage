#!/usr/bin/env pwsh
# Script to create Azure Service Principal for GitHub Actions

# Define variables
$resourceGroupName = "PoRedoImage"
$subscriptionId = ""  # Will be populated from current context

# Get the subscription ID from the current Azure context
Write-Host "Getting current Azure subscription..."
$subscription = az account show | ConvertFrom-Json
$subscriptionId = $subscription.id
$subscriptionName = $subscription.name
Write-Host "Current subscription: $subscriptionName ($subscriptionId)"

# Create the service principal
Write-Host "`nCreating service principal for GitHub Actions..."
$sp = az ad sp create-for-rbac --name "github-actions-poredeimage" `
    --role "Contributor" `
    --scopes "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName" `
    --sdk-auth | ConvertFrom-Json

# Format the output for GitHub secrets
Write-Host "`n------------------- AZURE_CREDENTIALS Secret Value -------------------"
Write-Host $sp | ConvertTo-Json -Compress
Write-Host "----------------------------------------------------------------"
Write-Host "`nIMPORTANT: Add this JSON string as a secret named 'AZURE_CREDENTIALS' in your GitHub repository."
Write-Host "Go to: https://github.com/punkouter25/PoRedoImage/settings/secrets/actions"

# Create individual service principal credential values
Write-Host "`n------------------- Individual Secret Values -------------------"
Write-Host "AZURE_CLIENT_ID: $($sp.clientId)"
Write-Host "AZURE_TENANT_ID: $($sp.tenantId)"
Write-Host "AZURE_SUBSCRIPTION_ID: $subscriptionId"
Write-Host "----------------------------------------------------------------"

# Instructions for getting Computer Vision and OpenAI secrets
Write-Host "`nTo get the Computer Vision and OpenAI secrets, run:"
Write-Host "az cognitiveservices account keys list --name YOUR_RESOURCE_NAME --resource-group $resourceGroupName"
Write-Host "az cognitiveservices account show --name YOUR_RESOURCE_NAME --resource-group $resourceGroupName"

# Final instructions
Write-Host "`nAdd all the following secrets to your GitHub repository:"
Write-Host "- AZURE_CREDENTIALS (JSON value above)"
Write-Host "- COMPUTER_VISION_ENDPOINT"
Write-Host "- COMPUTER_VISION_KEY"
Write-Host "- OPENAI_ENDPOINT"
Write-Host "- OPENAI_KEY"
Write-Host "- OPENAI_CHAT_DEPLOYMENT"
Write-Host "- OPENAI_IMAGE_DEPLOYMENT"
Write-Host "- APPINSIGHTS_CONNECTION_STRING"
