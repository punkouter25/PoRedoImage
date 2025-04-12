
This document outlines rules for an AI coding assistant to build .NET applications with Blazor WebAssembly frontend and ASP.NET Core Web API backend. The development follows a 10-step process tracked in steps.md. The AI assistant must:
Follow steps in order from steps.md (never modify this file)
Stop and request confirmation after completing each step
Reference prd.md for product requirements (never modify this file)
Focus on simplicity while designing for future expandability
Project Setup
Repository Initialization
Create appropriate .gitignore for .NET projects as the first action
Set up GitHub workflow files for CI/CD in .github/workflows directory
Initialize Git repository with main branch for primary development
Solution Structure
basic
Copy
YourSolutionName/                    # Root directory
├── YourSolutionName.sln             # Solution file at root level
├── steps.md                         # Tracks high-level development steps (provided)
├── prd.md                           # Product requirements (provided)
├── log.txt                          # Debug log file (created each run)
├── Client/                          # Blazor WebAssembly project
├── Server/                          # ASP.NET Core Web API project
├── Shared/                          # Shared project for models
├── Tests/                           # XUnit test projects
└── [Other projects]/                # Each in their own directory

Create all required projects using dotnet new commands
Projects should be created in their designated folders
Use .NET 9.x framework for all projects
Use the Blazor WebAssembly hosted template so client and server run together
Azure Resource Setup
Initial development should be local, with Azure deployment as final step
Create a resource group named after the application when ready for deployment
Use Azure Table Storage as the primary database solution (with SAS connection strings)
Use the cheapest Azure resource tiers that will accomplish the requirements
Set up individual Azure CLI commands (not scripts) to create all needed resources
Configure Application Insights for monitoring and diagnostics
Store sensitive configuration in Azure App Service configuration
When possible, use Azure CLI instead of Azure UI for configuration
Mandatory Diagnostics Page
The AI must automatically create a diagnostics page in every project without being asked:
Create Diag.razor page accessible at /diag endpoint
Display connection statuses in grid format with green (good) and red (bad) indicators
Verify and display status of:
Data connections (Azure Table Storage connectivity)
API health checks
Internet connection status
Authentication services status (if applicable)
Any other critical dependencies
Include link to main page at the bottom after diagnostics complete
Log all diagnostic results to:
Application Insights
Console
Serilog
log.txt
Development Approach
Architecture Selection
Choose appropriate architecture based on project requirements:
Vertical slice architecture with feature folders and CQRS pattern
 reasonml
Copy
Features/
├── ProductManagement/
│   ├── Commands/
│   │   ├── CreateProduct/
│   │   │   ├── CreateProductCommand.cs
│   │   │   ├── CreateProductCommandHandler.cs
│   │   │   └── CreateProductCommandValidator.cs
│   ├── Queries/
│   │   ├── GetProducts/
│   │   │   ├── GetProductsQuery.cs
│   │   │   ├── GetProductsQueryHandler.cs
│   │   │   └── ProductDto.cs
│   ├── Controllers/
│   └── Pages/


OR Onion Architecture with Core/Infrastructure/Application/WebApi layers
Implementation Guidelines
Limit classes to under 250 lines
Add comprehensive XML documentation comments
Note design patterns in comments: // Using Observer Pattern for notification system
Create realistic dummy data that mimics expected production data
Delete template Home.razor and create custom Home.razor
Design with simplicity as priority while allowing for future feature expansion
Step Workflow
For each of the 10 high-level steps in steps.md:
Plan the feature based on current step requirements
Design components using SOLID principles and appropriate patterns
Create empty test files for functionality to be implemented
Implement business logic with proper documentation
Create UI components following Blazor guidelines
Implement detailed logging for all connections and key operations
Update the AI's tracking of steps.md progress (mark current step as being worked on)
Request explicit confirmation before proceeding:
 vbnet
Copy
I've completed Step X: [Step Description]. 
The code compiles and all tests pass.
Would you like me to:
1. Explain any part of the implementation in more detail
2. Make adjustments to the current step
3. Proceed to Step Y: [Next Step Description]


Wait for user confirmation before moving to next step
Logging & Diagnostics
Comprehensive Logging Strategy
All applications must implement logging across three destinations:
Console output (for development debugging)
Serilog (structured logging for production)
log.txt file (created new for each run, readable by the LLM after execution)
Log Content Requirements
Include timestamps with all log entries
Log component names and operation context
Implement extra detailed logging around:
Database connections
API calls
Authentication events
Error conditions
Focus on logging key decision points and state changes
Avoid repetitive logging of the same information
Application Insights Integration
Track page views, feature usage, and user flows
Monitor performance metrics (load times, API response times)
Log exceptions with full context
Create custom events for business-relevant operations
Set up availability tests for critical endpoints
UI Development
Blazor Guidelines
Use built-in Blazor state management (no third-party state libraries)
Implement responsive design for all components
Use Radzen Blazor UI library for enhanced controls when needed
UX Design Principles
Create intuitive, consistent interfaces across the application
Ensure all screens are mobile-ready and responsive
Provide clear feedback for user actions
Implement progressive loading for data-intensive views
Error Handling & Reliability
Error Management Approach
Implement global exception handler middleware for API
Use try/catch blocks at service boundaries
Return appropriate HTTP status codes from API endpoints
Log exceptions with context information
Present user-friendly error messages in UI
Use circuit breaker pattern for external service calls
Dependency Injection
Follow standard DI practices based on service lifetime requirements:
Transient: For lightweight, stateless services
Scoped: For services that maintain state within a request
Singleton: For services that maintain state across requests
Register services in appropriate Program.cs or Startup.cs
Authentication & Security
Implement Google authentication with Azure Entra ID when authentication is required
Follow security best practices for token handling and storage
Use proper authorization policies for API endpoints
Testing Approach
Write XUnit tests before implementing UI components
Add descriptive debug statements with meaningful context information
Focus on testing business logic and core functionality
Create focused XUnit tests for business logic
Verify all API connections with appropriate test data
For external APIs requiring keys, create dedicated connection tests
Data Storage & Management
Azure Table Storage Implementation
Use Azure Table Storage as primary data store
Implement appropriate repository patterns for data access
Create optimized partition and row key strategies for expected query patterns
Ensure proper error handling for storage operations
Use SAS connection strings for all Azure Table Storage connections
Deployment Process
Development to Production
Focus on getting code working locally first
Use Azure CLI commands to deploy to cloud resources as final step
Configure environment-specific settings appropriately
Verify all connections between components in cloud environment
CI/CD Setup
Configure GitHub Actions for build, test, and deployment
Set up appropriate environment variables and secrets
Feature Toggles
Use configuration-based feature flags for simplicity
Implement through appsettings.json or Azure App Configuration
Use conditional rendering in UI based on feature state
Document which features are behind flags
NuGet Package Management
Add packages using dotnet add package commands
Document purpose of each package in comments
Prefer well-maintained, actively developed packages
Localization
Use English for all user interface elements and messages
No need for multi-language support
Azure Best Practices
When generating code for Azure, running terminal commands for Azure, or performing operations related to Azure, follow Azure best practices:
Prefer managed services over IaaS solutions
Implement proper retry policies for all Azure service calls
Use appropriate connection pooling
Follow least privilege principle for all service identities
Implement proper Azure resource tagging
Remember to check steps.md regularly to track progress through the 10 high-level steps. Focus on getting functionality working correctly before optimization. Log meaningful information to help diagnose issues between runs.

