# MakeApp CLI

> AI-Powered Feature Development Workflow Automation

MakeApp is a PowerShell CLI tool that orchestrates feature development workflows using GitHub Copilot CLI, automating branch management, AI-driven code generation, and PR creation.

## Features

- 🌿 **Branch Management** - Interactive repo/branch selection with auto-create
- 📝 **Feature Prompts** - Capture requirements via interactive prompts or files
- 🤖 **Agent Orchestration** - Iterative Copilot CLI execution with planning
- ✅ **Configuration Verification** - Auto-check/create Copilot instructions & MCP configs
- 📦 **Git Automation** - Stage, commit, push, and create PRs automatically
- 🔔 **Notifications** - Terminal, toast, and webhook notifications

## Requirements

- **PowerShell 7+** - [Install PowerShell](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell)
- **Git** - [Install Git](https://git-scm.com/downloads)
- **GitHub CLI** - [Install gh](https://cli.github.com/)
- **GitHub Copilot** - Active subscription with CLI extension

## Installation

1. Clone this repository:
   ```powershell
   git clone https://github.com/your-org/makeapp.git
   cd makeapp
   ```

2. (Optional) Add to your PATH or create an alias:
   ```powershell
   # Add to PowerShell profile
   Set-Alias makeapp "C:\path\to\makeapp\makeapp.ps1"
   ```

3. Authenticate with GitHub CLI:
   ```powershell
   gh auth login
   ```

## Quick Start

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

## Usage

### Actions

| Action | Description |
|--------|-------------|
| `start` | Start the interactive feature development workflow (default) |
| `init` | Initialize MakeApp configuration in current repository |
| `config` | View and edit MakeApp configuration |
| `status` | Show current status of MakeApp and repository |
| `template` | Create a new feature template file |
| `help` | Show help message |

### Options

| Option | Description |
|--------|-------------|
| `-RepoPath` | Path to the repository |
| `-FeatureFile` | Path to a feature definition file (.md or .json) |
| `-BranchName` | Name of branch to create or switch to |
| `-AutoApprove` | Auto-approve all commands (use with caution) |
| `-NoPush` | Don't push changes to remote |
| `-NoPR` | Don't create a pull request |
| `-Verbose` | Enable verbose output |

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
```

## Workflow

```
┌─────────────────────────────────────────────────────────────┐
│                     makeapp.ps1                             │
├─────────────────────────────────────────────────────────────┤
│  1. Select/Create Branch                                    │
│     └─> Interactive repo & branch selection                 │
│                                                             │
│  2. Enter Feature Requirements                              │
│     └─> Interactive prompts or load from file               │
│                                                             │
│  3. Verify Configurations                                   │
│     └─> Check/create copilot-instructions.md & mcp.json     │
│                                                             │
│  4. Execute with Copilot CLI                                │
│     └─> Agent orchestration with iterative planning         │
│                                                             │
│  5. Commit, Push, Create PR                                 │
│     └─> Git automation with notifications                   │
└─────────────────────────────────────────────────────────────┘
```

## Configuration

Configuration is stored in `~/.makeapp/config.json`. You can also modify defaults in `config/defaults.json`.

### Key Settings

```json
{
  "folders": {
    "repos": "C:\\development\\repos"
  },
  "github": {
    "owner": "your-username",
    "defaultBaseBranch": "main"
  },
  "git": {
    "autoStage": true,
    "autoCommit": false,
    "autoPush": false,
    "autoCreatePr": false
  },
  "agent": {
    "maxIterations": 50,
    "autoApproveCommands": false
  }
}
```

### Environment Variables

| Variable | Description |
|----------|-------------|
| `GITHUB_TOKEN` | GitHub authentication token |
| `MAKEAPP_REPOS` | Override repos folder path |
| `MAKEAPP_VERBOSE` | Enable verbose output |

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

## Project Structure

```
makeapp/
├── .github/
│   ├── copilot-instructions.md    # Copilot context for this repo
│   └── plan.md                    # Development plan
├── .vscode/
│   └── mcp.json                   # MCP server configurations
├── config/
│   └── defaults.json              # Default configuration
├── modules/
│   ├── BranchManager.ps1          # Branch management functions
│   ├── FeaturePrompt.ps1          # Feature requirements capture
│   ├── CopilotConfig.ps1          # Copilot configuration verifier
│   ├── Executor.ps1               # Copilot CLI orchestrator
│   └── GitAutomation.ps1          # Git/PR automation
├── makeapp.ps1                    # Main CLI entry point
└── README.md                      # This file
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

MIT License - see LICENSE file for details.

---

Made with ❤️ and GitHub Copilot
