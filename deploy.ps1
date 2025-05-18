#!/usr/bin/env pwsh
# Script to build and deploy the ImageGc application to Azure App Service

# Define variables
$resourceGroupName = "PoRedoImage"
$webAppName = "PoRedoImage"
$projectDir = ".\Server"
$publishDir = ".\publish"

Write-Host "Building and publishing the application..."

# Navigate to the project directory
Push-Location $projectDir

# Clean and build the solution
dotnet clean --configuration Release
dotnet build --configuration Release

# Publish the server project with self-contained deployment
dotnet publish --configuration Release --output $publishDir --no-build

# Navigate back to the root directory
Pop-Location

Write-Host "Application built and published to $publishDir"

# Package the published files into a ZIP file
Write-Host "Creating ZIP archive of the published files..."
Compress-Archive -Path "$publishDir\*" -DestinationPath "$publishDir.zip" -Force

Write-Host "Deploying to Azure App Service '$webAppName' in resource group '$resourceGroupName'..."

# Check if the App Service Plan exists, create it if not
$appPlanName = "PoRedoImagePlan"
$appPlanExists = az appservice plan show --resource-group $resourceGroupName --name $appPlanName 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Creating App Service Plan '$appPlanName'..."
    az appservice plan create --resource-group $resourceGroupName --name $appPlanName --sku B1 --is-linux false
}
else {
    Write-Host "App Service Plan '$appPlanName' already exists."
}

# Check if the web app exists, create it if not
$webappExists = az webapp show --resource-group $resourceGroupName --name $webAppName 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Creating Azure App Service '$webAppName'..."
    az webapp create --resource-group $resourceGroupName --name $webAppName --plan $appPlanName --runtime "dotnet:9"
      # Configure the webapp settings
    Write-Host "Configuring App Service settings..."
    az webapp config set --resource-group $resourceGroupName --name $webAppName --always-on true --net-framework-version "v9.0" --http20-enabled true
    
    # Configure application settings (Environment Variables)
    Write-Host "Configuring application settings..."
    az webapp config appsettings set --resource-group $resourceGroupName --name $webAppName --settings ASPNETCORE_ENVIRONMENT="Production" WEBSITE_RUN_FROM_PACKAGE="1"
    
    # Enable Application Insights
    Write-Host "Enabling Application Insights..."
    az webapp config appsettings set --resource-group $resourceGroupName --name $webAppName --settings APPINSIGHTS_INSTRUMENTATIONKEY=""
}
else {
    Write-Host "Azure App Service '$webAppName' already exists."
}

# Deploy to Azure App Service
Write-Host "Deploying the application to Azure App Service..."
az webapp deploy --resource-group $resourceGroupName --name $webAppName --src-path "$publishDir.zip" --type zip

Write-Host "Deployment process initiated."
Write-Host "Check the Azure portal for deployment status."
