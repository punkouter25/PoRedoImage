#!/usr/bin/env pwsh
# Script to build and deploy PoRedoImage to Azure App Service

# Set error action preference to stop on errors
$ErrorActionPreference = "Stop"

# Define variables
$resourceGroupName = "PoRedoImage"
$appServiceName = "PoRedoImage"
$location = "eastus"  # Change to your preferred region
$publishFolder = "./Server/publish"
$zipFilePath = "./deploy.zip"
$appServicePlanResourceGroup = "PoShared"  # Resource group containing the existing App Service Plan
$appServicePlanName = "PoSharedPlan"  # Name of your existing App Service Plan

# Step 1: Build and publish the application
Write-Host "Step 1: Building and publishing the application..." -ForegroundColor Cyan
dotnet publish -c Release -o $publishFolder ./ImageGc.sln
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed. Exiting..."
    exit 1
}
Write-Host "Application built successfully." -ForegroundColor Green

# Step 2: Zip the published files
Write-Host "Step 2: Creating deployment package..." -ForegroundColor Cyan
if (Test-Path $zipFilePath) {
    Remove-Item $zipFilePath -Force
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($publishFolder, $zipFilePath)
Write-Host "Deployment package created: $zipFilePath" -ForegroundColor Green

# Step 3: Check if resource group exists, create if not
Write-Host "Step 3: Checking resource group..." -ForegroundColor Cyan
$rgExists = az group exists --name $resourceGroupName | ConvertFrom-Json
if (-not $rgExists) {
    Write-Host "Resource group '$resourceGroupName' does not exist. Creating..." -ForegroundColor Yellow
    az group create --name $resourceGroupName --location $location
    Write-Host "Resource group created." -ForegroundColor Green
} else {
    Write-Host "Resource group '$resourceGroupName' already exists." -ForegroundColor Green
}

# Step 4: Verify the existing App Service Plan in PoShared resource group
Write-Host "Step 4: Verifying the existing App Service Plan in PoShared resource group..." -ForegroundColor Cyan
$appServicePlanExists = az appservice plan list --resource-group $appServicePlanResourceGroup --query "[?name=='$appServicePlanName']" | ConvertFrom-Json
if (-not $appServicePlanExists -or $appServicePlanExists.Length -eq 0) {
    Write-Error "App Service Plan '$appServicePlanName' does not exist in resource group '$appServicePlanResourceGroup'. Please check the name and resource group."
    exit 1
} else {
    Write-Host "App Service Plan '$appServicePlanName' found in resource group '$appServicePlanResourceGroup'." -ForegroundColor Green
}

# Step 5: Check if App Service exists, create if not
Write-Host "Step 5: Checking App Service..." -ForegroundColor Cyan
$appServiceExists = az webapp list --resource-group $resourceGroupName --query "[?name=='$appServiceName']" | ConvertFrom-Json
if (-not $appServiceExists -or $appServiceExists.Length -eq 0) {
    Write-Host "App Service '$appServiceName' does not exist. Creating using plan from '$appServicePlanResourceGroup' resource group..." -ForegroundColor Yellow
    az webapp create --name $appServiceName --resource-group $resourceGroupName --plan $appServicePlanName --resource-group-plan $appServicePlanResourceGroup --runtime "DOTNET:8" 
    Write-Host "App Service created." -ForegroundColor Green
} else {
    Write-Host "App Service '$appServiceName' already exists." -ForegroundColor Green
}

# Step 6: Enable Application Insights
Write-Host "Step 6: Setting up Application Insights..." -ForegroundColor Cyan
$appInsightsExists = az monitor app-insights component show --app $appServiceName --resource-group $resourceGroupName 2>$null
if (-not $appInsightsExists) {
    Write-Host "Creating Application Insights component..." -ForegroundColor Yellow
    az monitor app-insights component create --app $appServiceName --resource-group $resourceGroupName --location $location --kind web
    Write-Host "Application Insights component created." -ForegroundColor Green
} else {
    Write-Host "Application Insights component already exists." -ForegroundColor Green
}

# Step 7: Configure app settings for Application Insights
$appInsightsKey = az monitor app-insights component show --app $appServiceName --resource-group $resourceGroupName --query "instrumentationKey" -o tsv
Write-Host "Step 7: Configuring App Service initial settings..." -ForegroundColor Cyan
az webapp config appsettings set --resource-group $resourceGroupName --name $appServiceName --settings `
  "ApplicationInsights:ConnectionString=InstrumentationKey=$appInsightsKey" `
  "ASPNETCORE_ENVIRONMENT=Production"

# Step 8: Deploy the application
Write-Host "Step 8: Deploying application..." -ForegroundColor Cyan
az webapp deployment source config-zip --resource-group $resourceGroupName --name $appServiceName --src $zipFilePath
Write-Host "Application deployed successfully." -ForegroundColor Green

# Step 9: Configure sensitive settings
Write-Host "Step 9: Configure sensitive application settings..." -ForegroundColor Cyan

# Get Computer Vision settings
$computerVisionEndpoint = Read-Host -Prompt "Enter Azure Computer Vision API Endpoint"
$computerVisionKey = Read-Host -Prompt "Enter Azure Computer Vision API Key" -AsSecureString
$computerVisionKeyPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($computerVisionKey))

# Get OpenAI settings
$openAIEndpoint = Read-Host -Prompt "Enter Azure OpenAI API Endpoint"
$openAIKey = Read-Host -Prompt "Enter Azure OpenAI API Key" -AsSecureString
$openAIKeyPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($openAIKey))

$chatCompletionsDeployment = Read-Host -Prompt "Enter Azure OpenAI Chat Completions Deployment (e.g., gpt-35-turbo)"
$imageGenerationDeployment = Read-Host -Prompt "Enter Azure OpenAI Image Generation Deployment (e.g., dall-e-3)"

# Set the app settings
Write-Host "Configuring sensitive application settings in Azure App Service..." -ForegroundColor Yellow
az webapp config appsettings set --resource-group $resourceGroupName --name $appServiceName --settings `
    "ComputerVision:Endpoint=$computerVisionEndpoint" `
    "ComputerVision:Key=$computerVisionKeyPlain" `
    "ComputerVision:ApiVersion=2023-10-01" `
    "OpenAI:Endpoint=$openAIEndpoint" `
    "OpenAI:Key=$openAIKeyPlain" `
    "OpenAI:ChatCompletionsDeployment=$chatCompletionsDeployment" `
    "OpenAI:ImageGenerationDeployment=$imageGenerationDeployment"

# Clear sensitive variables from memory
$computerVisionKey = $null
$computerVisionKeyPlain = $null
$openAIKey = $null
$openAIKeyPlain = $null

# Step 10: Enable managed identity for the App Service
Write-Host "Step 10: Enabling system-assigned managed identity..." -ForegroundColor Cyan
az webapp identity assign --resource-group $resourceGroupName --name $appServiceName
Write-Host "Managed identity enabled." -ForegroundColor Green

# Step 11: Get the App URL
$appUrl = az webapp show --name $appServiceName --resource-group $resourceGroupName --query "defaultHostName" -o tsv
Write-Host "`nDeployment complete!" -ForegroundColor Green
Write-Host "Your application is now available at: https://$appUrl" -ForegroundColor Green
Write-Host "App Service name: $appServiceName" -ForegroundColor Green
Write-Host "Resource group: $resourceGroupName" -ForegroundColor Green
Write-Host "`nNote: It may take a few minutes for the application to fully start up." -ForegroundColor Yellow
Write-Host "Important: Please check the Azure portal to verify all settings were applied correctly." -ForegroundColor Yellow
