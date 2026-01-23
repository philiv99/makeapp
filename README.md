# MakeApp

> AI-Powered Feature Development Workflow Automation

MakeApp automates the end-to-end feature development workflow using GitHub Copilot. It handles branch management, captures feature requirements, orchestrates AI-driven code generation, and automates git operations including pull request creation.

## What It Does

MakeApp streamlines the development process by:

1. **Managing Repositories & Branches** — Scan and select repositories, create or switch feature branches
2. **Capturing Feature Requirements** — Define features interactively, via files, or through the API
3. **Configuring AI Context** — Auto-generate Copilot instructions and MCP configurations
4. **Orchestrating AI Code Generation** — Execute GitHub Copilot with iterative planning and memory
5. **Automating Git Workflows** — Stage, commit, push, and create pull requests automatically

## Interfaces

MakeApp provides two ways to automate your workflow:

- **PowerShell CLI** (`makeapp.ps1`) — Interactive command-line tool for developers
- **REST API** (`makeapp_api/`) — Programmatic access for integrations and automation

## Key Capabilities

| Capability | Description |
|------------|-------------|
| Repository Management | List repos, get status, manage branches |
| Feature Management | Create, track, and manage feature requirements |
| Git Operations | Stage, commit, push, and create PRs |
| Agent Orchestration | Iterative AI execution with planning |
| Memory System | Persist and recall context across sessions |
| Sandbox Mode | Isolated environment for safe experimentation |

## Requirements

- PowerShell 7.0+
- Git
- GitHub CLI with Copilot extension
- .NET 8.0+ (for API)

## Quick Start

```powershell
# CLI: Start interactive workflow
./makeapp.ps1 start

# API: Run the web server
cd makeapp_api/src/MakeApp.Api/MakeApp.Api
dotnet run
```

## License

MIT License
