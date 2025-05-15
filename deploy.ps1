#!/usr/bin/env pwsh
# Script to build and deploy the ImageGc application to Azure App Service

# Define variables
$resourceGroupName = "PoRedoImage"
$webAppName = "imagegc-app"
$projectDir = ".\ImageGc\Server"
$publishDir = ".\publish"

Write-Host "Building and publishing the application..."

# Navigate to the project directory
Push-Location $projectDir

# Clean and build the solution
dotnet clean --configuration Release
dotnet build --configuration Release

# Publish the server project
dotnet publish --configuration Release --output $publishDir --no-build

# Navigate back to the root directory
Pop-Location

Write-Host "Application built and published to $publishDir"

Write-Host "Deploying to Azure App Service '$webAppName' in resource group '$resourceGroupName'..."

# Deploy to Azure App Service
# Deploy to Azure App Service
# Corrected the source path to the published files using a direct relative path
az webapp deploy --resource-group $resourceGroupName --name $webAppName --src-path ".\ImageGc\Server\publish" --verbose

Write-Host "Deployment process initiated."
Write-Host "Check the Azure portal for deployment status."
