#!/usr/bin/env pwsh
# Script to configure sensitive application settings for the Azure App Service

# Define variables
$resourceGroupName = "PoRedoImage"
$webAppName = "PoRedoImage"

# Prompt for sensitive configuration values
$computerVisionEndpoint = Read-Host -Prompt "Enter Azure Computer Vision API Endpoint"
$computerVisionKey = Read-Host -Prompt "Enter Azure Computer Vision API Key" -AsSecureString
$computerVisionKeyPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($computerVisionKey))

$openAIEndpoint = Read-Host -Prompt "Enter Azure OpenAI API Endpoint"
$openAIKey = Read-Host -Prompt "Enter Azure OpenAI API Key" -AsSecureString
$openAIKeyPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($openAIKey))

$chatCompletionsDeployment = Read-Host -Prompt "Enter Azure OpenAI Chat Completions Deployment (e.g., gpt-35-turbo)"
$imageGenerationDeployment = Read-Host -Prompt "Enter Azure OpenAI Image Generation Deployment (e.g., dall-e-3)"

# Set the app settings
Write-Host "Configuring sensitive application settings..."

az webapp config appsettings set --resource-group $resourceGroupName --name $webAppName --settings `
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

Write-Host "App settings have been configured for $webAppName."
Write-Host "Important: Please check the Azure portal to verify all settings were applied correctly."
