
## **AI Development Protocol v2.1: Standard Operating Procedure for Project Execution**

### **1.0 Core Protocol & Workflow**

#### **1.1. Guiding Principles**
*   **Source of Truth:** The `prd.md` file is the definitive source for all product requirements. It must never be modified. If any rule in this protocol conflicts with a requirement in `prd.md`, the `prd.md` takes precedence.
*   **Design Philosophy:** Prioritize simplicity, functional correctness, and future expandability. Avoid premature optimization.

#### **1.2. Step-Driven Execution**
*   **Process Adherence:** If a `steps.md` file exists, strictly follow the high-level steps defined within it.
*   **Progress Tracking:** Mark completed steps in `steps.md` using the format: `- [x] Step X: Description`.
*   **Confirmation Point:** Before proceeding to a new step, request user confirmation using the success report template below.
*   **Failure Reporting:** If a step fails (e.g., code does not compile, tests fail), stop immediately. Report the failure, provide the full content of `log.txt`, state the exact error message, and await user instructions.

#### **1.3. User Interaction**
*   **Success Report Template:**
    ```
    I have completed Step X: [Step Description].

    The code compiles, all tests pass, and the log file shows no errors.

    Would you like me to:
    A) Make adjustments to the current step
    B) Proceed to Step Y: [Next Step Description]
    ```
*   **Proactive Suggestions:** After successfully completing any task, offer 5 relevant subsequent tasks that could be performed to advance the project.
*   **File Cleanup:** When encountering unused files or code, list all potentially removable items in a single request and await user confirmation before deleting anything.








### **2.0 Project & Solution Structure**

This section defines the mandatory file and folder structure for all new solutions, adhering to modern .NET best practices.

*   **Solution Naming:** The solution name is derived from the `prd.md` Title and must be prefixed with `Po` (e.g., `PoProjectName`).
*   **Root Directory Structure:** All files must be contained within a root directory named after the solution. If there is a Godot Project put that in the Client folder

    ```
    /PoProjectName/
    ├── .github/
    │   └── workflows/
    │       └── deploy.yml
    ├── .vscode/
    │   ├── launch.json
    │   └── tasks.json
    ├── AzuriteData/
    ├── PoProjectName.Api/
    │   └── PoProjectName.Api.csproj
    ├── PoProjectName.Client/
    │   └── PoProjectName.Client.csproj
    ├── .editorconfig
    ├── .gitignore
    ├── PoProjectName.sln
    ├── log.txt
    ├── prd.md
    ├── README.md
    └── steps.md
    ```

These two files should be in the api project:
   appsettings.development.json (contains key/values/connection string for local)
   appsettings.json ( (contains key/values/connection string for Azure app service)

*   **Core File Definitions:**
    *   **`PoProjectName.sln`:** The .NET solution file, located in the root.
    *   **`.gitignore`:** A standard `.gitignore` file for .NET projects 
    *   **`README.md`:** A readme file containing a summary of what the app does
    *   **`.editorconfig`:** A default .NET `.editorconfig` file to enforce consistent coding styles.
    *   **`prd.md`:** The immutable product requirements document.
    *   **`steps.md`:** (If used) The high-level development checklist.
    *   **`log.txt`:** A single, comprehensive log file. See Section 6.0 for details.
*   **Project Folders:** Each project (.csproj) must reside in its own folder at the root level, named identically to the project.
*   **Configuration Folders:**
    *   **`.github/workflows/`:** Contains all GitHub Actions CI/CD workflow files.
    *   **`.vscode/`:** Contains `launch.json` and `tasks.json` configured for local F5 debugging.
    *   **`AzuriteData/`:** Local data store for the Azurite emulator (must be in `.gitignore`).










































### **3.0 Backend Development (C# / .NET API)**

#### **3.1. General Standards**
*   **Framework:** Target the .NET 9.x framework (or the latest stable version).
*   **Architecture:** Default to **Vertical Slice Architecture**. If `prd.md` describes a highly complex domain with significant shared logic, **Onion Architecture** may be used. The chosen pattern must be justified with a comment in `Program.cs`.
*   **Coding Standards:** Adhere to SOLID principles and Gof design patterns. Add comments to explain the use of specific design patterns (e.g., `// Using Repository Pattern to abstract data access from business logic.`).
*   **Dependency Injection (DI):** Register all services in `Program.cs` or dedicated extension methods, using appropriate lifetimes (Transient, Scoped, Singleton).
*   **Data Tables** If the app uses Azure Table Storage (Azurite when running locally), Use the resource in PoShared resource group - It is named PoSharedTableStorage. Use Azure CLI to get connection string - Prefix all table names of the app with the solution name (If solution is named PoSomeApp then the table name is appended with PoSomeAppTableName for example
*   **Other Azure APIs and resources**  The resource group PoShared contains all the shared resources that the app might use (AI APIs, Application Insights, Log Analytics, etc.)
*   **Location of Keys/Values/Connection Strings:** appsettings.Development.json is where these values are when running all locally. Appsettings.json and Azure App Service Environment variables is where the values are when running in Azure 








#### **3.2. API Implementation**
*   **Project Setup:** Use `dotnet new webapi`.
*   **Error Handling:** Implement global exception handler middleware. Use `try/catch` blocks at external boundaries (e.g., database calls, third-party API requests) and return appropriate, consistent HTTP status codes.
*   **Resiliency:** For calls to potentially unreliable external services, implement the Circuit Breaker pattern.










### **4.0 Client Development (UI)**

IF USING BLAZOR
#### **4.1. Blazor WebAssembly**
*   **Project Setup:** Create a hosted Blazor WebAssembly project. The client app must be hosted by the ASP.NET Core server app.
*   **Page Title:** Set the application name (from `prd.md`) as the `<title>` in `index.html`.
*   **UI/UX:** Implement a responsive design. The `Home.razor` component is the primary landing page. Use the **Radzen Blazor UI library** for applications requiring complex controls.

IF USING GODOT
#### **4.2. Godot .NET (Game Client)**
*   **Environment:** Use Godot 4.x with C#.
*   **Structure:** All Godot artifacts reside in `Client/`. Scenes in `Client/Scenes/`, scripts in `Client/Scripts/`.
*   **Workflow:** Define static scene structure in `.tscn` files. Implement all dynamic logic and state management in `.cs` scripts. Connect signals programmatically in C# for type safety and clarity.





























### **5.0 Testing & Quality Assurance**

*   **Framework:** Use **XUnit** for all tests.
*   **Test-First Approach:** For new features, create services and their corresponding integration tests first. Verify that all tests pass before beginning UI implementation.
*   **API Testing:** Use `Microsoft.AspNetCore.Mvc.Testing` to test API controllers in-memory without hosting the API separately.
*   **Coverage:** Write tests for all new business logic, core functionality, and data access. Create dedicated connection tests for external services.





































### **6.0 Logging & Diagnostics**

#### **6.1. Centralized Logging**
*   **Targets:** Implement a robust logging strategy that outputs simultaneously to the **Console**, **Serilog (to file)**, and **Application Insights**.
*   **Log File (`log.txt`):** A single `log.txt` file must be created or overwritten in the root directory on each application run. It must contain the most recent, detailed, and timestamped logs from both server and client components to provide a complete diagnostic trace.
*   **Log Content:** All entries must include timestamps, component/class names, and operational context.

#### **6.2. Mandatory Diagnostics View**
*   A diagnostics view is mandatory for all applications with a UI.
*   **Implementation:**
    *   **Blazor:** Create a `Diag.razor` page accessible at the `/diag` route.
    *   **Godot:** Create a `Diag.tscn` scene accessible from the main menu.
*   **Functionality:** This view must perform and display the real-time status (e.g., Green/OK, Red/Error) of:
    *   Database connection (Azure Table Storage / Azurite).
    *   Backend API health check.
    *   Basic internet connectivity.
    *   Any other critical external dependencies.
*   All diagnostic check results must be written to all configured log targets.























