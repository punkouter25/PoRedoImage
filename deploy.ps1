#Requires -Modules Az.Accounts, Az.Resources, Az.Websites, Az.CognitiveServices, Az.ApplicationInsights
#Requires -Version 5.1

<#
.SYNOPSIS
Builds, packages, and deploys the PoRedoImageGc application to Azure App Service.

.DESCRIPTION
This script performs the following actions:
1. Defines Azure resource names and locations.
2. Publishes the .NET application in Release mode.
3. Zips the published application files.
4. Creates an Azure App Service Plan (if needed).
5. Creates an Azure Web App (if needed).
6. Retrieves connection strings/keys/endpoints for dependent Azure services (App Insights, Computer Vision, OpenAI).
7. Configures the Web App's application settings.
8. Deploys the zipped application to the Web App.
9. Cleans up the local zip file.

.NOTES
- Ensure you are logged into Azure CLI (`az login`) before running.
- Ensure the Azure CLI (`az`) and required PowerShell modules (`Az.*`) are installed.
- The script assumes the dependent Azure resources (Resource Group, Computer Vision, OpenAI, App Insights) already exist.
#>

# --- Configuration ---
$ErrorActionPreference = "Stop" # Exit script on error

# Azure Resource Details (Modify if necessary)
$resourceGroupName          = "poredoimage"
$location                   = "eastus" # Location for App Service Plan and Web App
$appServicePlanName         = "PoRedoImagePlan"
$webAppNameSuffix           = Get-Random -Maximum 9999 # Add random suffix for unique web app name
$webAppName                 = "PoRedoImageWebApp-$webAppNameSuffix"
$appServicePlanSku          = "B1" # Basic tier - choose appropriate SKU for your needs

# Dependent Azure Resource Names (Ensure these exist in $resourceGroupName)
$computerVisionResourceName = "PoRedoImageVisionEastUS" # The EastUS one we created
$openAIResourceName         = "poRedoImage-openai"
$appInsightsResourceName    = "PoRedoImageAppInsights"

# Deployment Details
$linuxFxVersion             = "DOTNETCORE`|9.0" # Target .NET runtime stack for Linux App Service (escape pipe for PowerShell)
$solutionPath               = "ImageGc/ImageGc.sln"
$serverProjectPath          = "ImageGc/Server/Server.csproj"
$publishFolder              = "publish"
$zipFileName                = "app.zip"
$openAITextDeploymentName   = "gpt-4o"     # Ensure this deployment exists in your OpenAI resource
$openAIImageDeploymentName  = "dall-e-3"   # Ensure this deployment exists in your OpenAI resource

# --- Script Start ---
Write-Host "Starting Azure Deployment for PoRedoImageGc..."
Write-Host "Resource Group: $resourceGroupName"
Write-Host "Web App Name: $webAppName"
Write-Host "Location: $location"

# --- 1. Build and Publish Application ---
Write-Host "`n[1/6] Publishing application..."
if (Test-Path $publishFolder) {
    Write-Host "Removing existing publish folder..."
    Remove-Item -Recurse -Force $publishFolder
}
dotnet publish $serverProjectPath -c Release -o $publishFolder --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed!"
    exit 1
}
Write-Host "Application published successfully to '$publishFolder'."

# --- 2. Zip Published Files ---
Write-Host "`n[2/6] Zipping published files..."
$publishFullPath = Join-Path -Path $PWD -ChildPath $publishFolder
$zipFileFullPath = Join-Path -Path $PWD -ChildPath $zipFileName
if (Test-Path $zipFileFullPath) {
    Write-Host "Removing existing zip file..."
    Remove-Item -Force $zipFileFullPath
}
Compress-Archive -Path "$publishFullPath\*" -DestinationPath $zipFileFullPath -Force
Write-Host "Published files zipped successfully to '$zipFileName'."

# --- 3. Create Azure App Service Plan ---
Write-Host "`n[3/6] Ensuring App Service Plan '$appServicePlanName' exists..."
$plan = Get-AzAppServicePlan -ResourceGroupName $resourceGroupName -Name $appServicePlanName -ErrorAction SilentlyContinue
if ($null -eq $plan) {
    Write-Host "App Service Plan not found. Creating..."
    New-AzAppServicePlan -ResourceGroupName $resourceGroupName -Name $appServicePlanName -Location $location -Tier $appServicePlanSku
    Write-Host "App Service Plan created successfully."
} else {
    Write-Host "App Service Plan already exists."
}

# --- 4. Create Azure Web App ---
Write-Host "`n[4/6] Ensuring Web App '$webAppName' exists..."
$webApp = Get-AzWebApp -ResourceGroupName $resourceGroupName -Name $webAppName -ErrorAction SilentlyContinue
if ($null -eq $webApp) {
    Write-Host "Web App not found. Creating using Azure CLI..."
    # Use Azure CLI 'az webapp create' as New-AzWebApp parameters can vary by module version
    # Hardcode the runtime string to avoid PowerShell interpretation issues
    az webapp create --resource-group $resourceGroupName --plan $appServicePlanName --name $webAppName --runtime "DOTNETCORE:9.0" --deployment-local-git # Adding --deployment-local-git might help initialize some settings
    if ($LASTEXITCODE -ne 0) {
        Write-Error "az webapp create failed!"
        exit 1
    }
    Write-Host "Web App created successfully via Azure CLI."
} else {
    Write-Host "Web App already exists."
}

# --- 5. Configure Application Settings ---
Write-Host "`n[5/6] Configuring Web App application settings..."

# Get dependent resource details
Write-Host "Retrieving keys and endpoints..."
try {
    $appInsightsConnectionString = (Get-AzApplicationInsights -ResourceGroupName $resourceGroupName -Name $appInsightsResourceName).ConnectionString
    $cvKey = (Get-AzCognitiveServicesAccountKey -ResourceGroupName $resourceGroupName -Name $computerVisionResourceName).Key1
    $cvEndpoint = (Get-AzCognitiveServicesAccount -ResourceGroupName $resourceGroupName -Name $computerVisionResourceName).Endpoint
    $openAIKey = (Get-AzCognitiveServicesAccountKey -ResourceGroupName $resourceGroupName -Name $openAIResourceName).Key1
    $openAIEndpoint = (Get-AzCognitiveServicesAccount -ResourceGroupName $resourceGroupName -Name $openAIResourceName).Endpoint

    if (-not $appInsightsConnectionString) { throw "Failed to get App Insights Connection String." }
    if (-not $cvKey) { throw "Failed to get Computer Vision Key." }
    if (-not $cvEndpoint) { throw "Failed to get Computer Vision Endpoint." }
    if (-not $openAIKey) { throw "Failed to get OpenAI Key." }
    if (-not $openAIEndpoint) { throw "Failed to get OpenAI Endpoint." }

} catch {
    Write-Error "Failed to retrieve Azure resource details: $($_.Exception.Message)"
    exit 1
}

# Prepare settings - Use double-colon format for nested JSON keys
$appSettings = @{
    "APPLICATIONINSIGHTS_CONNECTION_STRING" = $appInsightsConnectionString # Standard key used by .NET SDK
    "AzureComputerVision:Endpoint"          = $cvEndpoint
    "AzureComputerVision:Key"               = $cvKey
    "AzureOpenAI:Endpoint"                  = $openAIEndpoint
    "AzureOpenAI:Key"                       = $openAIKey
    "AzureOpenAI:DeploymentName"            = $openAITextDeploymentName
    "AzureOpenAI:ImageDeploymentName"       = $openAIImageDeploymentName
    "ASPNETCORE_ENVIRONMENT"                = "Production" # Set environment to Production
}

Write-Host "Updating settings in Azure using Azure CLI..."
# Construct settings string for Azure CLI (key=value pairs, use double underscore for nesting)
$cliAppSettings = @(
    "APPLICATIONINSIGHTS_CONNECTION_STRING=$appInsightsConnectionString"
    "AzureComputerVision__Endpoint=$cvEndpoint"
    "AzureComputerVision__Key=$cvKey"
    "AzureOpenAI__Endpoint=$openAIEndpoint"
    "AzureOpenAI__Key=$openAIKey"
    "AzureOpenAI__DeploymentName=$openAITextDeploymentName"
    "AzureOpenAI__ImageDeploymentName=$openAIImageDeploymentName"
    "ASPNETCORE_ENVIRONMENT=Production"
) -join " "

az webapp config appsettings set --resource-group $resourceGroupName --name $webAppName --settings $cliAppSettings
if ($LASTEXITCODE -ne 0) {
    Write-Error "az webapp config appsettings set failed!"
    exit 1
}

# Enable Web Sockets using Azure CLI
Write-Host "Enabling Web Sockets using Azure CLI..."
az webapp config set --resource-group $resourceGroupName --name $webAppName --web-sockets-enabled true
if ($LASTEXITCODE -ne 0) {
    # Decide if this is critical enough to exit, maybe just warn? For now, let's warn.
    Write-Warning "Failed to enable Web Sockets using Azure CLI. Deployment will continue."
}

Write-Host "Application settings configured successfully."

# --- 6. Deploy Application ---
Write-Host "`n[6/6] Deploying application zip file '$zipFileName'..."
az webapp deployment source config-zip --resource-group $resourceGroupName --name $webAppName --src $zipFileFullPath --timeout 1800 # Increase timeout
if ($LASTEXITCODE -ne 0) {
    Write-Error "az webapp deployment source config-zip failed!"
    # Note: Sometimes deployment fails but eventually succeeds. Check Azure portal.
} else {
    Write-Host "Application deployed successfully."
}

# --- Cleanup ---
Write-Host "`nCleaning up local zip file..."
Remove-Item -Force $zipFileFullPath

Write-Host "`n--- Deployment Complete ---"
Write-Host "Web App URL: https://$($webAppName).azurewebsites.net"
Write-Host "Remember to check the Azure portal for deployment status and logs if issues occur."
