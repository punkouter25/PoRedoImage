# ImageGc - Image Analysis with Azure AI

An intelligent image analysis application that uses Azure AI services to analyze images and generate enhanced descriptions. The project uses Azure Computer Vision to analyze images and Azure OpenAI to enhance descriptions and generate images based on analysis.

## CI/CD Status

[![Build and Test](https://github.com/punkouter25/PoRedoImage/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/punkouter25/PoRedoImage/actions/workflows/build-and-test.yml)
[![Deploy to Azure](https://github.com/punkouter25/PoRedoImage/actions/workflows/deploy-to-azure.yml/badge.svg)](https://github.com/punkouter25/PoRedoImage/actions/workflows/deploy-to-azure.yml)
[![Security Scan](https://github.com/punkouter25/PoRedoImage/actions/workflows/security-scan.yml/badge.svg)](https://github.com/punkouter25/PoRedoImage/actions/workflows/security-scan.yml)

## Features

- Image analysis with Azure Computer Vision
- Enhanced descriptions using Azure OpenAI
- Image generation with DALL-E
- Blazor WebAssembly client with responsive UI
- ASP.NET Core backend API

## Architecture

- **Client**: Blazor WebAssembly application
- **Server**: ASP.NET Core API
- **Shared**: Shared models and contracts

## Azure Services Used

- Azure Computer Vision
- Azure OpenAI Service
- Azure App Service
- Azure Application Insights

## CI/CD Pipeline

The project uses GitHub Actions for continuous integration and deployment:

1. **Build and Test**: Builds the solution and runs tests
2. **Deploy to Azure**: Deploys the Server application to Azure App Service
3. **Docker Build**: Builds and pushes Docker images to GitHub Container Registry
4. **Security Scan**: Performs security scanning and vulnerability checks

To deploy the application to your Azure resources, see the [GitHub Actions Deployment Guide](./docs/github-actions-deployment-guide.md) for setup instructions.

## Getting Started

1. Clone the repository
2. Install .NET 9.0 SDK
3. Configure Azure services (use `azure-environment-setup.yml` workflow)
4. Set up GitHub Actions deployment:
   - Run `setup-github-secrets.ps1` to configure GitHub secrets using Azure CLI and GitHub CLI
   - Or follow the manual setup in [GitHub Actions Deployment Guide](./docs/github-actions-deployment-guide.md)
5. Update `appsettings.json` with your Azure service credentials
6. Run the application locally

## Local Development

```bash
cd ImageGc
dotnet restore
dotnet build
cd Server
dotnet run
```

## Containerization

The project includes a Dockerfile for containerization. You can build and run the container locally:

```bash
cd ImageGc
docker build -t imagegc:latest .
docker run -p 5000:80 imagegc:latest
```

## Environment Configuration

The application supports different environments through configuration files. Configure the following services in your `appsettings.json`:

1. **Computer Vision**:
   - Endpoint
   - API Key
   - API Version

2. **OpenAI**:
   - Endpoint
   - API Key
   - Chat Completions Deployment
   - Image Generation Deployment
   - Fallback Chat Model

## License

MIT
