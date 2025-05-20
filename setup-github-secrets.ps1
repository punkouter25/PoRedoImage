# This script automates the setup of GitHub Actions secrets for Azure deployment
# Required: Azure CLI and GitHub CLI (gh) installed and configured

# Azure Settings
$subscriptionId = (az account show --query id -o tsv)
$resourceGroup = "PoRedoImage"
$appServiceName = "PoRedoImage"
$location = "eastus"  # Change to your preferred region
$repoName = $null # Will be detected or prompted for

# GitHub Repository Detection
function Get-GitHubRepoName {
    try {
        # Try to get the repository from git config
        $remoteUrl = git config --get remote.origin.url
        if ($remoteUrl) {
            # Extract username/repo from different formats
            if ($remoteUrl -match "github.com[:/]([^/]+)/([^/.]+)") {
                $username = $Matches[1]
                $repo = $Matches[2]
                return "$username/$repo"
            }
        }
    }
    catch {
        # Git command failed, we'll use the manual input instead
        Write-Host "Could not automatically detect GitHub repository name."
    }
    return $null
}

# Detect GitHub repository or prompt for it
$detectedRepo = Get-GitHubRepoName
if ($detectedRepo) {
    $repoName = $detectedRepo
    Write-Host "Detected GitHub repository: $repoName"
    $confirm = Read-Host "Is this correct? (Y/n)"
    if ($confirm -and $confirm.ToLower() -eq "n") {
        $repoName = $null
    }
}

if (-not $repoName) {
    $username = Read-Host "Enter your GitHub username"
    $repoName = "${username}/PoRedoImage"
    Write-Host "Using GitHub repository: $repoName"
}

# Check for Azure and GitHub CLI
Write-Host "Checking for required tools..." -ForegroundColor Cyan
$azVersion = (az version) | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error "Azure CLI not found. Please install it: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
}

$ghVersion = (gh --version) | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error "GitHub CLI not found. Please install it: https://cli.github.com/manual/installation"
    exit 1
}

# Check Azure login status
Write-Host "Checking Azure login status..." -ForegroundColor Cyan
$loginStatus = az account show 2>$null
if (-not $loginStatus) {
    Write-Host "Not logged in to Azure. Logging in now..." -ForegroundColor Yellow
    az login
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Azure login failed. Please try again."
        exit 1
    }
}

# Check GitHub login status
Write-Host "Checking GitHub login status..." -ForegroundColor Cyan
$ghAuth = gh auth status 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Not logged in to GitHub. Logging in now..." -ForegroundColor Yellow
    gh auth login
    if ($LASTEXITCODE -ne 0) {
        Write-Error "GitHub login failed. Please try again."
        exit 1
    }
}

# Create service principal
Write-Host "Creating Azure service principal for GitHub Actions..." -ForegroundColor Cyan
$spName = "github-actions-poredoimage"
$sp = $null

# Check if service principal already exists
$existingSp = az ad sp list --display-name $spName --query "[0].appId" -o tsv 2>$null
if ($existingSp) {
    Write-Host "Service principal '$spName' already exists with ID: $existingSp" -ForegroundColor Yellow
    $recreate = Read-Host "Do you want to recreate it? (y/N)"
    if ($recreate -and $recreate.ToLower() -eq "y") {
        az ad sp delete --id $existingSp
        $sp = $null
    } else {
        $sp = az ad sp show --id $existingSp | ConvertFrom-Json
    }
}

if (-not $sp) {
    Write-Host "Creating new service principal..." -ForegroundColor Yellow
    $sp = az ad sp create-for-rbac --name $spName `
        --role "Contributor" `
        --scopes "/subscriptions/$subscriptionId/resourceGroups/$resourceGroup" | ConvertFrom-Json
    
    if (-not $sp) {
        Write-Error "Failed to create service principal"
        exit 1
    }
    Write-Host "Service principal created successfully." -ForegroundColor Green
}

# Set GitHub Secrets for Azure authentication
Write-Host "Setting up GitHub secrets for Azure authentication..." -ForegroundColor Cyan
gh secret set AZURE_CLIENT_ID --body "$($sp.appId)" --repo $repoName
gh secret set AZURE_TENANT_ID --body "$($sp.tenant)" --repo $repoName
gh secret set AZURE_SUBSCRIPTION_ID --body "$subscriptionId" --repo $repoName

Write-Host "Azure authentication secrets set successfully." -ForegroundColor Green

# Check if resources exist and get config
Write-Host "Getting Azure resources configuration..." -ForegroundColor Cyan

# Check if App Service exists
$appServiceExists = az webapp show --name $appServiceName --resource-group $resourceGroup 2>$null
if (-not $appServiceExists) {
    Write-Host "App Service '$appServiceName' does not exist in resource group '$resourceGroup'." -ForegroundColor Yellow
    Write-Host "Please ensure your resources are created before running the deployment workflow." -ForegroundColor Yellow
}

# Read settings from appsettings.json
Write-Host "Reading settings from appsettings.json..." -ForegroundColor Cyan

$appSettingsPath = Join-Path -Path $PSScriptRoot -ChildPath "Server\appsettings.json"
if (-not (Test-Path $appSettingsPath)) {
    Write-Error "appsettings.json file not found at: $appSettingsPath"
    exit 1
}

try {
    $appSettings = Get-Content -Path $appSettingsPath -Raw | ConvertFrom-Json
    
    # Extract Computer Vision settings
    $computerVisionEndpoint = $appSettings.ComputerVision.Endpoint
    $computerVisionKeyPlain = $appSettings.ComputerVision.Key
    
    # Extract OpenAI settings
    $openAIEndpoint = $appSettings.OpenAI.Endpoint
    $openAIKeyPlain = $appSettings.OpenAI.Key
    $chatCompletionsDeployment = $appSettings.OpenAI.ChatCompletionsDeployment
    $imageGenerationDeployment = $appSettings.OpenAI.ImageGenerationDeployment
    
    Write-Host "Successfully loaded settings from appsettings.json" -ForegroundColor Green
    Write-Host "Computer Vision Endpoint: $computerVisionEndpoint"
    Write-Host "OpenAI Endpoint: $openAIEndpoint"
    Write-Host "Chat Completions Deployment: $chatCompletionsDeployment"
    Write-Host "Image Generation Deployment: $imageGenerationDeployment"
} 
catch {
    Write-Error "Failed to read settings from appsettings.json: $_"
    exit 1
}

# Application Insights connection string
$appInsightsConnectionString = ""
$appInsightsExists = az monitor app-insights component show --app $appServiceName --resource-group $resourceGroup 2>$null
if ($appInsightsExists) {
    $appInsightsConnectionString = az monitor app-insights component show --app $appServiceName --resource-group $resourceGroup --query "connectionString" -o tsv
} else {
    Write-Host "Application Insights will be created during deployment." -ForegroundColor Yellow
}

# Set GitHub Secrets for application settings
Write-Host "Setting up GitHub secrets for application settings..." -ForegroundColor Cyan

gh secret set COMPUTER_VISION_ENDPOINT --body "$computerVisionEndpoint" --repo $repoName
gh secret set COMPUTER_VISION_KEY --body "$computerVisionKeyPlain" --repo $repoName
gh secret set OPENAI_ENDPOINT --body "$openAIEndpoint" --repo $repoName
gh secret set OPENAI_KEY --body "$openAIKeyPlain" --repo $repoName
gh secret set OPENAI_CHAT_COMPLETIONS_DEPLOYMENT --body "$chatCompletionsDeployment" --repo $repoName
gh secret set OPENAI_IMAGE_GENERATION_DEPLOYMENT --body "$imageGenerationDeployment" --repo $repoName

if ($appInsightsConnectionString) {
    gh secret set APPLICATIONINSIGHTS_CONNECTION_STRING --body "$appInsightsConnectionString" --repo $repoName
}

# Clean sensitive data from memory
$computerVisionKey = $null
$computerVisionKeyPlain = $null
$openAIKey = $null
$openAIKeyPlain = $null
[System.GC]::Collect()

Write-Host "`nSetup completed successfully!" -ForegroundColor Green
Write-Host "Your GitHub Actions workflow is now configured with the necessary secrets for Azure deployment." -ForegroundColor Green
Write-Host "You can now push your code to the 'main' branch or manually trigger the workflow from GitHub Actions." -ForegroundColor Green
