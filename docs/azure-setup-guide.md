# Azure Environment Setup Guide

This document explains how to set up the required Azure resources for the ImageGc application.

## Required Azure Resources

1. **Azure App Service**
   - Hosts the ASP.NET Core server and serves the Blazor WebAssembly client

2. **Azure Computer Vision**
   - Used for image analysis and generating basic descriptions

3. **Azure OpenAI Service**
   - Used for enhancing descriptions and generating images with DALL-E

4. **Azure Application Insights**
   - Used for monitoring and telemetry

## Automated Setup with GitHub Actions

The easiest way to set up all required resources is to use the `azure-environment-setup.yml` GitHub Actions workflow:

1. Go to your GitHub repository **Actions** tab
2. Select the **Azure Environment Setup** workflow
3. Click **Run workflow**
4. Configure the following parameters:
   - **Environment**: Choose `development` or `production`
   - **Region**: Select an Azure region (e.g. `eastus`, `westus2`)
5. Click **Run workflow**

The workflow will create all required resources with proper naming conventions and configuration.

## Manual Setup

If you prefer to set up the resources manually, follow these steps:

### 1. Resource Group

Create a resource group to contain all resources:

```bash
az group create --name ImageGc-ResourceGroup --location eastus
```

### 2. App Service Plan and Web App

```bash
# Create App Service Plan
az appservice plan create --name ImageGc-AppServicePlan --resource-group ImageGc-ResourceGroup --sku P1V2 --is-linux

# Create Web App
az webapp create --name imagegc-app --resource-group ImageGc-ResourceGroup --plan ImageGc-AppServicePlan --runtime "DOTNET:9.0"
```

### 3. Computer Vision

```bash
az cognitiveservices account create --name imagegc-vision --resource-group ImageGc-ResourceGroup --kind ComputerVision --sku S1 --location eastus
```

### 4. Azure OpenAI Service

```bash
# Create OpenAI service
az cognitiveservices account create --name imagegc-openai --resource-group ImageGc-ResourceGroup --kind OpenAI --sku S0 --location eastus

# Deploy models (requires Azure CLI with OpenAI extension)
az cognitiveservices account deployment create --name imagegc-openai --resource-group ImageGc-ResourceGroup --deployment-name gpt-35-turbo --model-name gpt-35-turbo --model-version "0301"
az cognitiveservices account deployment create --name imagegc-openai --resource-group ImageGc-ResourceGroup --deployment-name dall-e-3 --model-name dall-e-3 --model-version "1"
```

### 5. Application Insights

```bash
az monitor app-insights component create --app imagegc-insights --resource-group ImageGc-ResourceGroup --location eastus
```

## Configuration

After setting up resources, you need to configure the application settings:

1. Get the keys and endpoints for each service:

```bash
# Get Computer Vision key and endpoint
VISION_KEY=$(az cognitiveservices account keys list --name imagegc-vision --resource-group ImageGc-ResourceGroup --query "key1" --output tsv)
VISION_ENDPOINT=$(az cognitiveservices account show --name imagegc-vision --resource-group ImageGc-ResourceGroup --query "properties.endpoint" --output tsv)

# Get OpenAI key and endpoint
OPENAI_KEY=$(az cognitiveservices account keys list --name imagegc-openai --resource-group ImageGc-ResourceGroup --query "key1" --output tsv)
OPENAI_ENDPOINT=$(az cognitiveservices account show --name imagegc-openai --resource-group ImageGc-ResourceGroup --query "properties.endpoint" --output tsv)

# Get Application Insights connection string
APP_INSIGHTS_CONNECTION=$(az monitor app-insights component show --app imagegc-insights --resource-group ImageGc-ResourceGroup --query "connectionString" --output tsv)
```

2. Update Web App settings:

```bash
az webapp config appsettings set --name imagegc-app --resource-group ImageGc-ResourceGroup --settings \
  ComputerVision__Endpoint=$VISION_ENDPOINT \
  ComputerVision__Key=$VISION_KEY \
  ComputerVision__ApiVersion="2023-10-01" \
  OpenAI__Endpoint=$OPENAI_ENDPOINT \
  OpenAI__Key=$OPENAI_KEY \
  OpenAI__ChatCompletionsDeployment="gpt-35-turbo" \
  OpenAI__ImageGenerationDeployment="dall-e-3" \
  OpenAI__FallbackChatModel="gpt-35-turbo" \
  ApplicationInsights__ConnectionString=$APP_INSIGHTS_CONNECTION
```

## Deployment

Once all resources are set up, you can deploy the application:

1. Using the CI/CD pipeline:
   - Push changes to the main branch to trigger automatic deployment

2. Manual deployment with Azure CLI:
   - Build the application locally
   - Use `az webapp deployment source` to deploy from GitHub or local ZIP file

## Monitoring

After deployment, you can monitor the application using:

1. Azure Application Insights
2. Azure Monitor
3. Azure Log Analytics

Access the application at: `https://imagegc-app.azurewebsites.net`
