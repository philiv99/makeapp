# MakeApp

> AI-Powered Feature Development Workflow Automation Platform

MakeApp is a development workflow automation platform that orchestrates AI-driven code generation using GitHub Copilot. It provides both a **PowerShell CLI** for interactive use and a **C#.NET Web API** for programmatic integration, enabling automated branch management, feature requirement capture, and pull request creation.

## Overview

MakeApp automates the end-to-end feature development workflow:

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│  Select Repo    │───▶│ Feature Input    │───▶│ Verify Config   │
│  & Branch       │    │ (Interactive/    │    │ (Copilot, MCP)  │
│                 │    │  File/API)       │    │                 │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                                        │
                                                        ▼
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│  Create PR      │◀───│ Git Operations   │◀───│ Agent           │
│  (optional)     │    │ (stage/commit/   │    │ Orchestration   │
│                 │    │  push)           │    │ Loop            │
└─────────────────┘    └──────────────────┘    └─────────────────┘
```

## Features

- 🌿 **Repository & Branch Management** - Scan, select, and create branches across repositories
- 📝 **Feature Requirements** - Capture requirements via CLI prompts, files, or API
- 🤖 **AI Agent Orchestration** - Iterative GitHub Copilot execution with plan generation
- ✅ **Configuration Management** - Auto-check/create Copilot instructions & MCP configs
- 📦 **Git Automation** - Stage, commit, push, and create PRs automatically
- 🔌 **RESTful API** - Programmatic access to all workflow capabilities
- 🧠 **Memory System** - Persist and recall context across sessions
- 🏗️ **Sandbox Mode** - Isolated testing environment for safe experimentation

---

## Project Structure

```
makeapp/
├── makeapp.ps1              # PowerShell CLI entry point
├── config/                   # Configuration files
│   ├── defaults.json         # Default configuration
│   └── sandbox_defaults.json # Sandbox mode configuration
├── modules/                  # PowerShell modules
│   ├── BranchManager.ps1     # Git branch operations
│   ├── CopilotConfig.ps1     # Copilot configuration management
│   ├── Executor.ps1          # Command orchestration
│   ├── FeaturePrompt.ps1     # Feature requirements capture
│   ├── GitAutomation.ps1     # Git commit/push/PR operations
│   └── Sandbox.ps1           # Sandbox environment management
├── makeapp_api/              # C#.NET Web API solution
│   ├── src/
│   │   ├── MakeApp.Api/        # Web API controllers & configuration
│   │   ├── MakeApp.Application/ # Services, DTOs, validators
│   │   ├── MakeApp.Core/        # Domain entities & interfaces
│   │   └── MakeApp.Infrastructure/ # External service implementations
│   ├── tests/                 # Unit & integration tests
│   └── templates/             # Agent & copilot instruction templates
└── sandbox/                  # Sandbox workspace directory
```

---

## Requirements

| Component | Version | Description |
|-----------|---------|-------------|
| **PowerShell** | 7.0+ | Required for CLI |
| **Git** | Latest | Version control |
| **GitHub CLI** | Latest | GitHub operations & Copilot CLI |
| **GitHub Copilot** | Active subscription | AI code generation |
| **.NET SDK** | 8.0+ | Required for Web API |

### Verify Prerequisites

```powershell
# PowerShell version
$PSVersionTable.PSVersion

# Git
git --version

# GitHub CLI
gh --version
gh auth status

# .NET SDK (for API)
dotnet --version
```

---

## Installation

### Clone Repository

```powershell
git clone https://github.com/philiv99/makeapp.git
cd makeapp
```

### CLI Setup (Optional)

```powershell
# Add alias to PowerShell profile
Add-Content $PROFILE 'Set-Alias makeapp "C:\path\to\makeapp\makeapp.ps1"'
. $PROFILE
```

### API Setup

```powershell
cd makeapp_api/src/MakeApp.Api/MakeApp.Api
dotnet restore
dotnet run
```

The API will be available at `https://localhost:5001` with Swagger UI at `/swagger`.

---

## PowerShell CLI Usage

### Quick Start

```powershell
# Start interactive workflow
./makeapp.ps1 start

# Initialize MakeApp in a repository
./makeapp.ps1 init

# Check status
./makeapp.ps1 status

# View configuration
./makeapp.ps1 config
```

### Actions

| Action | Description |
|--------|-------------|
| `start` | Start the interactive feature development workflow (default) |
| `init` | Initialize MakeApp configuration in current repository |
| `config` | View and edit MakeApp configuration |
| `status` | Show current status of MakeApp and repository |
| `template` | Create a new feature template file |
| `sandbox` | Manage sandbox testing environment |
| `help` | Show help message |

### Options

| Option | Description |
|--------|-------------|
| `-RepoPath` | Path to the repository |
| `-FeatureFile` | Path to a feature definition file (.md or .json) |
| `-BranchName` | Name of branch to create or switch to |
| `-ProjectType` | Project type: `powershell`, `node`, `python`, `generic` |
| `-AutoApprove` | Auto-approve all commands (use with caution) |
| `-NoPush` | Don't push changes to remote |
| `-NoPR` | Don't create a pull request |
| `-Sandbox` | Run in sandbox mode |
| `-Force` | Force operation |

### Examples

```powershell
# Start with a specific repo
./makeapp.ps1 start -RepoPath C:\repos\my-project

# Use a pre-defined feature file
./makeapp.ps1 start -FeatureFile ./features/add-authentication.md

# Create a new branch and start
./makeapp.ps1 start -BranchName feature/new-api

# Auto-approve commands (for CI/automation)
./makeapp.ps1 start -AutoApprove -NoPR

# Run in sandbox mode for safe testing
./makeapp.ps1 start -Sandbox
```

---

## Web API

The MakeApp Web API exposes RESTful endpoints for programmatic workflow automation.

### API Endpoints

#### Repositories

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/repos` | List available repositories |
| GET | `/api/v1/repos/{owner}/{name}` | Get repository info |
| GET | `/api/v1/repos/{owner}/{name}/status` | Get repository config status |
| GET | `/api/v1/repos/{owner}/{name}/branches` | List branches |
| POST | `/api/v1/repos/{owner}/{name}/branches` | Create a branch |

#### Features

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/features` | List features |
| GET | `/api/v1/features/{id}` | Get feature details |
| POST | `/api/v1/features` | Create a new feature |
| PUT | `/api/v1/features/{id}` | Update a feature |

#### Git Operations

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/repos/{owner}/{name}/git/changes/unstaged` | Get unstaged changes |
| GET | `/api/v1/repos/{owner}/{name}/git/changes/staged` | Get staged changes |
| GET | `/api/v1/repos/{owner}/{name}/git/commits/unpushed` | Get unpushed commits |
| POST | `/api/v1/repos/{owner}/{name}/git/stage` | Stage changes |
| POST | `/api/v1/repos/{owner}/{name}/git/commit` | Create commit |
| POST | `/api/v1/repos/{owner}/{name}/git/push` | Push to remote |
| POST | `/api/v1/repos/{owner}/{name}/git/pull-request` | Create pull request |

#### Apps

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/apps` | Create a new app with MakeApp structure |
| GET | `/api/v1/apps/{id}` | Get app information |
| GET | `/api/v1/apps/{id}/status` | Get app status |

### Health Checks

| Endpoint | Description |
|----------|-------------|
| `/health` | Basic health check |
| `/health/ready` | Readiness check |

---

## Architecture

The Web API follows **Clean Architecture** with four layers:

### Core Layer (`MakeApp.Core`)
Domain entities, interfaces, and business rules with no external dependencies.

- **Entities**: `RepositoryInfo`, `BranchInfo`, `Feature`, `Workflow`, `App`
- **Interfaces**: `IRepositoryService`, `IGitService`, `IGitHubService`, `IOrchestrationService`
- **Configuration**: `MakeAppOptions`, `UserConfiguration`

### Application Layer (`MakeApp.Application`)
Use cases, DTOs, validators, and service orchestration.

- **Services**: `FeatureService`, `AppService`, `PlanGeneratorService`, `PromptFormatterService`
- **DTOs**: Request/response data transfer objects
- **Mappings**: AutoMapper profiles

### Infrastructure Layer (`MakeApp.Infrastructure`)
External service implementations.

- **Git Operations**: `GitService`, `BranchService`
- **GitHub Integration**: `GitHubService`, `RepositoryCreationService`
- **File System**: `FileSystemService`
- **Memory System**: `MemoryService`
- **Sandbox**: `SandboxService`

### API Layer (`MakeApp.Api`)
Controllers, middleware, and API configuration.

- **Controllers**: `ReposController`, `FeaturesController`, `GitController`, `AppsController`
- **Features**: API versioning, Swagger/OpenAPI, health checks, Serilog logging

---

## Configuration

### CLI Configuration (`config/defaults.json`)

```json
{
  "folders": {
    "repos": "C:\\development\\repos",
    "workspace": "C:\\development\\workspace"
  },
  "github": {
    "defaultBaseBranch": "main",
    "tokenEnvVar": "GITHUB_TOKEN"
  },
  "llm": {
    "defaultProvider": "copilot",
    "providers": {
      "copilot": { "model": "gpt-4o" },
      "openai": { "enabled": false },
      "anthropic": { "enabled": false }
    }
  }
}
```

### API Configuration (`appsettings.json`)

```json
{
  "MakeApp": {
    "Agent": {
      "MaxRetries": 3,
      "MaxIterations": 50,
      "AutoApprove": false
    },
    "Git": {
      "DefaultBranch": "main",
      "AutoPush": true
    }
  }
}
```

---

## Development

### Running Tests

```powershell
# Run all tests
cd makeapp_api
dotnet test

# Run specific test project
dotnet test tests/MakeApp.Api.Tests/MakeApp.Api.Tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Building the API

```powershell
cd makeapp_api/src/MakeApp.Api/MakeApp.Api
dotnet build
dotnet publish -c Release -o ./publish
```

---

## Workflow

The interactive workflow guides you through feature development:

```
┌─────────────────────────────────────────────────────────────┐
│                   MakeApp Workflow                          │
├─────────────────────────────────────────────────────────────┤
│  1. Select/Create Branch                                    │
│     └─> Interactive repo & branch selection                 │
│                                                             │
│  2. Enter Feature Requirements                              │
│     └─> Interactive prompts, file, or API request           │
│                                                             │
│  3. Verify Configurations                                   │
│     └─> Check/create copilot-instructions.md & mcp.json     │
│                                                             │
│  4. Execute with Copilot CLI                                │
│     └─> Agent orchestration with iterative planning         │
│                                                             │
│  5. Commit, Push, Create PR                                 │
│     └─> Automated git operations with notifications         │
└─────────────────────────────────────────────────────────────┘
```

---

## Feature Files

You can define features in Markdown or JSON format:

### Markdown Format

```markdown
# Add User Authentication

## Description
Implement JWT-based authentication for the API.

## Acceptance Criteria
- [ ] Users can register with email/password
- [ ] Users can log in and receive JWT
- [ ] Protected routes require valid JWT

## Technical Notes
- Use bcrypt for password hashing
- JWT expiry: 24 hours
```

### JSON Format

```json
{
  "Title": "Add User Authentication",
  "Description": "Implement JWT-based authentication for the API.",
  "AcceptanceCriteria": [
    "Users can register with email/password",
    "Users can log in and receive JWT"
  ],
  "TechnicalNotes": [
    "Use bcrypt for password hashing"
  ],
  "Priority": "High"
}
```

---

## Environment Variables

| Variable | Description |
|----------|-------------|
| `GITHUB_TOKEN` | GitHub authentication token |
| `MAKEAPP_REPOS` | Override repos folder path |
| `MAKEAPP_VERBOSE` | Enable verbose output |

---

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Run tests (`dotnet test`)
5. Submit a pull request

---

## License

MIT License - see LICENSE file for details.

---

Made with ❤️ and GitHub Copilot
