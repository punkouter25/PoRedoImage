# GitHub Actions Run Book

This document contains helpful commands and procedures for working with GitHub Actions in this repository.

## Prerequisites

- [GitHub CLI](https://cli.github.com/) installed and authenticated
- Proper permissions to the repository

## Common GitHub CLI Commands

### List Workflows

```powershell
gh workflow list
```

### View Workflow Runs

```powershell
gh run list
```

### View a Specific Workflow

```powershell
gh workflow view build-and-test.yml
```

### View a Specific Run

```powershell
gh run view <run-id>
```

### Download Artifacts from a Run

```powershell
gh run download <run-id>
```

### Manually Trigger a Workflow

```powershell
gh workflow run ci-cd-pipeline.yml
```

### Manually Trigger the Azure Environment Setup Workflow

```powershell
gh workflow run azure-environment-setup.yml -f environment=development -f region=eastus
```

## Working with Secrets

### List Repository Secrets

```powershell
gh secret list
```

### Set a Secret

```powershell
gh secret set SECRET_NAME -b "secret value"
```

Required secrets for this repository:
- AZURE_CREDENTIALS
- AZURE_WEBAPP_NAME
- OPENAI_ENDPOINT (for OpenAI service)
- OPENAI_KEY (for OpenAI service)

## Troubleshooting GitHub Actions

### Check Workflow Logs

```powershell
gh run view <run-id> --log
```

### Check Failed Steps

```powershell
gh run view <run-id> --log-failed
```

### Disable a Workflow

```powershell
gh workflow disable <workflow-name>
```

### Re-enable a Workflow

```powershell
gh workflow enable <workflow-name>
```

## Deployment Process

1. Ensure all tests pass in the `Build and Test` workflow
2. Push changes to the `main` branch to trigger the deployment workflow
3. The `Deploy to Azure` workflow will deploy to the Azure Web App
4. Verify the deployment is successful in Azure Portal

## Monitoring

1. Check GitHub Actions dashboard for workflow status
2. Check Azure Application Insights for application performance
3. Review logs in Azure App Service

## Useful Links

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [GitHub CLI Documentation](https://cli.github.com/manual/)
- [Azure Web Apps Documentation](https://docs.microsoft.com/en-us/azure/app-service/)
