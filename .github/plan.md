# MakeApp CLI - Development Plan

## Overview

PowerShell CLI tool that orchestrates feature development workflows using GitHub Copilot CLI, automating branch management, AI-driven code generation, and PR creation.

## Project Structure

```
makeapp/
├── .github/
│   ├── plan.md                    # This file
│   ├── copilot-instructions.md    # Repository-wide Copilot instructions
│   └── workflows/                 # GitHub Actions (future)
├── .vscode/
│   └── mcp.json                   # MCP server configurations
├── makeapp.ps1                    # Main CLI entry point
├── modules/
│   ├── Sandbox.ps1                # Sandbox management functions
│   ├── BranchManager.ps1          # Branch management functions
│   ├── FeaturePrompt.ps1          # Feature requirements capture
│   ├── CopilotConfig.ps1          # Copilot configuration verifier
│   ├── Executor.ps1               # Copilot CLI orchestrator
│   └── GitAutomation.ps1          # Git/PR automation
├── config/
│   ├── defaults.json              # Default configuration
│   └── sandbox_defaults.json      # Sandbox-specific configuration
├── sandbox/                       # Sandbox environment (gitignored, created at runtime)
│   ├── sandbox-repo/              # Test repository
│   ├── workspace/                 # Working directory
│   ├── temp/                      # Temporary files
│   ├── logs/                      # Log files
│   └── cache/                     # Cache directory
└── README.md                      # Documentation
```

## Sandbox Mode

The sandbox provides an isolated environment for testing MakeApp workflows without affecting real repositories.

### Sandbox Commands

```powershell
# Create sandbox (interactive menu)
./makeapp.ps1 sandbox

# Create sandbox with specific project type
./makeapp.ps1 sandbox create -ProjectType powershell
./makeapp.ps1 sandbox create -ProjectType node
./makeapp.ps1 sandbox create -ProjectType python

# Check sandbox status
./makeapp.ps1 sandbox status

# Enter sandbox directory
./makeapp.ps1 sandbox enter

# Reset sandbox to clean state
./makeapp.ps1 sandbox reset

# Delete sandbox
./makeapp.ps1 sandbox delete

# Run workflow in sandbox mode
./makeapp.ps1 start -Sandbox
```

### Sandbox Configuration

The sandbox uses `config/sandbox_defaults.json` with these key differences from production:
- All paths point to local `sandbox/` folder
- Auto-approve commands enabled
- Auto-commit enabled
- Push/PR disabled by default
- Verbose logging enabled
- Reduced timeouts and limits

## Implementation Steps

### Step 1: Create Project Structure
- Create `makeapp.ps1` main entry point
- Create `modules/` directory for helper functions
- Create `config/` directory for templates
- Create `.github/` for Copilot instructions and workflow templates

### Step 2: Build Branch Management Module
**File:** `modules/BranchManager.ps1`

Functions:
- `Get-AvailableRepos` - List available repositories
- `Get-RepoBranches` - List branches for a repository
- `New-FeatureBranch` - Create new branch from main
- `Switch-ToBranch` - Checkout existing branch
- `Test-GitState` - Validate clean git state

### Step 3: Create Feature Prompt Module
**File:** `modules/FeaturePrompt.ps1`

Functions:
- `Get-FeatureRequirements` - Interactive prompt for requirements
- `Import-FeatureFromFile` - Load requirements from file
- `Save-FeatureInstructions` - Store structured instructions
- `Format-CopilotPrompt` - Format requirements for Copilot CLI

### Step 4: Implement Copilot Configuration Verifier
**File:** `modules/CopilotConfig.ps1`

Functions:
- `Test-CopilotInstructions` - Check `.github/copilot-instructions.md` exists
- `New-CopilotInstructions` - Create default instructions file
- `Test-McpConfig` - Validate `.vscode/mcp.json` configuration
- `Set-AgentPermissions` - Ensure agent permissions are configured

### Step 5: Build Execution Orchestrator
**File:** `modules/Executor.ps1`

Functions:
- `Invoke-CopilotCommand` - Execute `gh copilot` CLI commands
- `Start-AgentOrchestration` - Handle iterative agent prompts
- `Get-CopilotOutput` - Capture and parse outputs
- `Wait-ForCompletion` - Monitor until feature completion

### Step 6: Create Git/PR Automation Module
**File:** `modules/GitAutomation.ps1`

Functions:
- `Add-AllChanges` - Stage all changes
- `New-FeatureCommit` - Create commit with feature description
- `Push-FeatureBranch` - Push to remote
- `New-PullRequest` - Create PR to main via `gh pr create`
- `Send-UserNotification` - Notify user of completion

## CLI Workflow

```
┌─────────────────────────────────────────────────────────────┐
│                     makeapp.ps1                             │
├─────────────────────────────────────────────────────────────┤
│  1. Select/Create Branch                                    │
│     └─> BranchManager.ps1                                   │
│                                                             │
│  2. Enter Feature Requirements                              │
│     └─> FeaturePrompt.ps1                                   │
│                                                             │
│  3. Verify Configurations                                   │
│     └─> CopilotConfig.ps1                                   │
│                                                             │
│  4. Execute with Copilot CLI                                │
│     └─> Executor.ps1                                        │
│         ├─> gh copilot suggest                              │
│         ├─> gh copilot explain                              │
│         └─> Agent orchestration loop                        │
│                                                             │
│  5. Commit, Push, Create PR                                 │
│     └─> GitAutomation.ps1                                   │
│                                                             │
│  6. Notify User                                             │
│     └─> Toast notification / Terminal output                │
└─────────────────────────────────────────────────────────────┘
```

## Configuration Options

All configuration is stored in a single JSON file at `~/.makeapp/config.json`:

```json
{
  "folders": {
    "repos": "C:\\development\\repos",
    "workspace": "C:\\development\\workspace",
    "temp": "C:\\temp\\makeapp",
    "logs": "C:\\temp\\makeapp\\logs",
    "cache": "C:\\temp\\makeapp\\cache"
  },
  "github": {
    "owner": "your-github-username",
    "defaultOrg": "",
    "defaultRepo": "",
    "defaultBaseBranch": "main",
    "apiBaseUrl": "https://api.github.com",
    "token": "",
    "tokenEnvVar": "GITHUB_TOKEN"
  },
  "llm": {
    "defaultProvider": "copilot",
    "providers": {
      "copilot": {
        "enabled": true,
        "model": "gpt-4o",
        "apiKey": "",
        "apiKeyEnvVar": "GITHUB_COPILOT_TOKEN"
      },
      "openai": {
        "enabled": false,
        "model": "gpt-4o",
        "apiKey": "",
        "apiKeyEnvVar": "OPENAI_API_KEY",
        "apiBaseUrl": "https://api.openai.com/v1"
      },
      "azure": {
        "enabled": false,
        "model": "gpt-4o",
        "apiKey": "",
        "apiKeyEnvVar": "AZURE_OPENAI_API_KEY",
        "endpoint": "",
        "deploymentName": ""
      },
      "anthropic": {
        "enabled": false,
        "model": "claude-sonnet-4-20250514",
        "apiKey": "",
        "apiKeyEnvVar": "ANTHROPIC_API_KEY",
        "apiBaseUrl": "https://api.anthropic.com"
      },
      "ollama": {
        "enabled": false,
        "model": "llama3",
        "apiBaseUrl": "http://localhost:11434"
      }
    }
  },
  "agent": {
    "maxIterations": 50,
    "autoApproveCommands": false,
    "allowedCommands": ["git", "gh", "npm", "dotnet", "python", "pip"],
    "blockedCommands": ["rm -rf", "format", "del /s"],
    "workingDirectory": ""
  },
  "git": {
    "autoStage": true,
    "autoCommit": false,
    "autoPush": false,
    "autoCreatePr": false,
    "commitMessagePrefix": "feat: ",
    "prTemplate": "",
    "signCommits": false
  },
  "ui": {
    "theme": "default",
    "progressBarWidth": 50,
    "spinnerStyle": "dots",
    "showTimestamps": true,
    "verboseOutput": false,
    "colorOutput": true
  },
  "timeouts": {
    "gitOperationSeconds": 60,
    "apiRequestSeconds": 30,
    "llmResponseSeconds": 120,
    "commandExecutionSeconds": 300,
    "branchCheckoutSeconds": 30,
    "prCreationSeconds": 60
  },
  "limits": {
    "maxFileSizeKb": 1024,
    "maxFilesPerCommit": 50,
    "maxPromptLength": 10000,
    "maxResponseLength": 50000,
    "maxLogFileSizeMb": 100,
    "maxCacheAgeDays": 7
  },
  "notifications": {
    "method": "terminal",
    "webhookUrl": "",
    "slackChannel": "",
    "teamsWebhook": "",
    "toastEnabled": true,
    "soundEnabled": false
  },
  "logging": {
    "level": "info",
    "logToFile": true,
    "logToConsole": true,
    "includeTimestamps": true,
    "retentionDays": 30
  },
  "mcp": {
    "enabled": true,
    "configPath": ".vscode/mcp.json",
    "servers": {}
  }
}
```

### Configuration Priority

Configuration values are resolved in this order (highest priority first):

1. **Command-line arguments** - Override any setting for single execution
2. **Environment variables** - For sensitive data like API keys
3. **Config file** (`~/.makeapp/config.json`) - Persistent user preferences
4. **Built-in defaults** - Fallback values

### Environment Variables (for sensitive data)

| Variable | Description |
|----------|-------------|
| `GITHUB_TOKEN` | GitHub authentication token |
| `GITHUB_COPILOT_TOKEN` | GitHub Copilot token |
| `OPENAI_API_KEY` | OpenAI API key |
| `AZURE_OPENAI_API_KEY` | Azure OpenAI API key |
| `ANTHROPIC_API_KEY` | Anthropic API key |
| `MAKEAPP_CONFIG` | Override config file path |

## Dependencies

- **PowerShell 7+** - Core runtime
- **Git** - Version control operations
- **GitHub CLI (`gh`)** - GitHub operations and Copilot CLI
- **BurntToast** (optional) - Windows toast notifications

## Open Questions

1. **GitHub CLI Authentication**
   - Use `gh auth login` interactively?
   - Expect pre-configured credentials via environment variables?

2. **Notification Method**
   - Windows toast notifications (BurntToast module)?
   - Terminal output only?
   - Webhook integration (Slack/Teams)?

3. **Config Storage**
   - JSON config file at `~/.makeapp/config.json`?
   - Environment variables?
   - Command-line flags only?

## Future Enhancements

- [ ] GitHub Actions workflow templates
- [ ] Multi-repo support
- [ ] Custom agent definitions
- [ ] Progress dashboard
- [ ] History/audit log
- [ ] Rollback capabilities
