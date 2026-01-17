# MakeApp CLI - Complete Usage Instructions

> **AI-Powered Feature Development Workflow Automation**

This guide provides step-by-step instructions for using MakeApp to create applications with AI-assisted development workflows.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Installation](#installation)
3. [Quick Start](#quick-start)
4. [Creating Your First App](#creating-your-first-app)
5. [Understanding the Workflow](#understanding-the-workflow)
6. [Using Feature Files](#using-feature-files)
7. [Sandbox Mode](#sandbox-mode)
8. [Configuration](#configuration)
9. [Command Reference](#command-reference)
10. [Examples](#examples)
11. [Troubleshooting](#troubleshooting)

---

## Prerequisites

Before using MakeApp, ensure you have the following installed:

| Requirement | Version | Installation |
|-------------|---------|--------------|
| **PowerShell** | 7.0+ | `scoop install pwsh` or [Download](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell) |
| **Git** | Latest | `scoop install git` or [Download](https://git-scm.com/downloads) |
| **GitHub CLI** | Latest | `scoop install gh` or [Download](https://cli.github.com/) |
| **GitHub Copilot** | Active subscription | Required for AI code generation |

### Verify Prerequisites

```powershell
# Check PowerShell version (must be 7+)
$PSVersionTable.PSVersion

# Check Git
git --version

# Check GitHub CLI
gh --version

# Verify GitHub CLI authentication
gh auth status
```

If not authenticated, run:
```powershell
gh auth login
```

---

## Installation

### Option 1: Clone Repository

```powershell
# Clone the repository
git clone https://github.com/your-org/makeapp.git
cd makeapp
```

### Option 2: Add to PATH

```powershell
# Add to your PowerShell profile for easy access
Add-Content $PROFILE 'Set-Alias makeapp "C:\path\to\makeapp\makeapp.ps1"'

# Reload profile
. $PROFILE
```

---

## Quick Start

The fastest way to start using MakeApp:

```powershell
# Navigate to your project
cd C:\your\project

# Start the interactive workflow
./makeapp.ps1 start
```

This launches an interactive session that guides you through:
1. Repository and branch selection
2. Feature requirements input
3. AI-assisted code generation
4. Git commit, push, and PR creation

---

## Creating Your First App

### Step 1: Initialize a Repository

If starting from scratch, first initialize MakeApp in your repository:

```powershell
# Navigate to your project folder
cd C:\development\repos\my-project

# Initialize MakeApp configuration
./makeapp.ps1 init
```

This creates:
- `.github/copilot-instructions.md` - Instructions for GitHub Copilot
- `.vscode/mcp.json` - MCP (Model Context Protocol) configuration

### Step 2: Start the Workflow

```powershell
./makeapp.ps1 start
```

### Step 3: Select or Create a Branch

When prompted:
```
Current branch: main
Change branch? (y/N)
```

- Press **Enter** to stay on the current branch
- Type **y** to select or create a new branch

For new features, create a feature branch:
```
Enter new branch name: feature/my-new-feature
```

### Step 4: Enter Feature Requirements

MakeApp will prompt you for:

| Field | Description | Example |
|-------|-------------|---------|
| **Title** | Short name for the feature | "Add User Authentication" |
| **Description** | Detailed explanation | "Implement JWT-based login with email/password" |
| **Acceptance Criteria** | What "done" looks like | "Users can login", "JWT tokens expire in 24h" |
| **Technical Notes** | Implementation hints | "Use bcrypt for passwords" |
| **Priority** | Importance level | High, Medium, Low |

### Step 5: Review and Execute

MakeApp will:
1. ✅ Verify configurations are in place
2. 🤖 Start AI agent orchestration
3. 📝 Generate code based on requirements
4. ✔️ Prompt for approval at each step

### Step 6: Complete the Workflow

After code generation:
```
=== Step 5: Complete Workflow ===
```

MakeApp will:
- Stage changes to Git
- Create a commit with descriptive message
- Push to remote (unless `-NoPush`)
- Create a Pull Request (unless `-NoPR`)

---

## Understanding the Workflow

```
┌──────────────────────────────────────────────────────────────┐
│                     MakeApp Workflow                          │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ 1. SELECT REPOSITORY & BRANCH                       │    │
│  │    • Choose existing repo or create new             │    │
│  │    • Create feature branch from main                │    │
│  └─────────────────────────────────────────────────────┘    │
│                           ↓                                  │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ 2. DEFINE FEATURE REQUIREMENTS                      │    │
│  │    • Enter title, description, criteria             │    │
│  │    • Or load from feature file (.md/.json)          │    │
│  └─────────────────────────────────────────────────────┘    │
│                           ↓                                  │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ 3. VERIFY CONFIGURATIONS                            │    │
│  │    • Check copilot-instructions.md                  │    │
│  │    • Check .vscode/mcp.json                         │    │
│  │    • Auto-fix missing configs                       │    │
│  └─────────────────────────────────────────────────────┘    │
│                           ↓                                  │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ 4. EXECUTE AI ORCHESTRATION                         │    │
│  │    • GitHub Copilot generates code                  │    │
│  │    • Iterative planning and execution               │    │
│  │    • Up to 50 iterations (configurable)             │    │
│  └─────────────────────────────────────────────────────┘    │
│                           ↓                                  │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ 5. COMPLETE WORKFLOW                                │    │
│  │    • Git add, commit, push                          │    │
│  │    • Create Pull Request                            │    │
│  │    • Send notifications                             │    │
│  └─────────────────────────────────────────────────────┘    │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

---

## Using Feature Files

Instead of entering requirements interactively, you can use pre-defined feature files.

### Markdown Format (.md)

Create a file like `features/add-auth.md`:

```markdown
# Add User Authentication

## Description
Implement JWT-based authentication for the REST API with secure 
password handling and token refresh capabilities.

## Acceptance Criteria
- [ ] Users can register with email and password
- [ ] Users can log in and receive a JWT token
- [ ] Protected routes require valid JWT
- [ ] Tokens expire after 24 hours
- [ ] Users can refresh tokens before expiry

## Technical Notes
- Use bcrypt for password hashing (min 10 rounds)
- Store refresh tokens in database
- Implement rate limiting on auth endpoints

## Priority
High
```

### JSON Format (.json)

Create a file like `features/add-auth.json`:

```json
{
  "Title": "Add User Authentication",
  "Description": "Implement JWT-based authentication for the REST API.",
  "AcceptanceCriteria": [
    "Users can register with email and password",
    "Users can log in and receive a JWT token",
    "Protected routes require valid JWT",
    "Tokens expire after 24 hours"
  ],
  "TechnicalNotes": [
    "Use bcrypt for password hashing",
    "Implement rate limiting"
  ],
  "Priority": "High"
}
```

### Using Feature Files

```powershell
# Start workflow with a feature file
./makeapp.ps1 start -FeatureFile ./features/add-auth.md

# Or with JSON
./makeapp.ps1 start -FeatureFile ./features/add-auth.json
```

### Create a Template

```powershell
# Generate a blank feature template
./makeapp.ps1 template
```

---

## Sandbox Mode

Sandbox mode provides an **isolated environment** for testing MakeApp without affecting your real projects.

### What Sandbox Mode Does

- Creates isolated `sandbox/` directory
- Initializes a test Git repository
- Optionally creates a GitHub repo for testing
- Allows safe experimentation

### Sandbox Commands

```powershell
# Create a new sandbox (interactive menu)
./makeapp.ps1 sandbox

# Create sandbox with specific project type
./makeapp.ps1 sandbox create -ProjectType python
./makeapp.ps1 sandbox create -ProjectType node
./makeapp.ps1 sandbox create -ProjectType powershell
./makeapp.ps1 sandbox create -ProjectType generic

# Check sandbox status
./makeapp.ps1 sandbox status

# Enter the sandbox directory
./makeapp.ps1 sandbox enter

# Reset sandbox to initial state
./makeapp.ps1 sandbox reset

# Delete sandbox completely
./makeapp.ps1 sandbox delete
```

### Start Workflow in Sandbox

```powershell
# Run workflow in sandbox mode
./makeapp.ps1 start -Sandbox

# Sandbox with specific project type
./makeapp.ps1 start -Sandbox -ProjectType python
```

### Sandbox Configuration

Edit `config/sandbox_defaults.json` to customize:

```json
{
  "github": {
    "owner": "your-github-username",
    "defaultRepo": "makeapp-sandbox-test",
    "autoCreateRepo": true
  },
  "sandbox": {
    "enabled": true,
    "repoName": "sandbox-repo",
    "initWithSampleFiles": true,
    "sampleProjectType": "python"
  }
}
```

---

## Configuration

### Configuration Files

| File | Location | Purpose |
|------|----------|---------|
| **defaults.json** | `config/defaults.json` | System-wide defaults |
| **User config** | `~/.makeapp/config.json` | User-specific overrides |
| **sandbox_defaults.json** | `config/sandbox_defaults.json` | Sandbox environment settings |

### View/Edit Configuration

```powershell
# View current configuration
./makeapp.ps1 config

# This shows all settings and prompts for edits
```

### Key Configuration Options

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
| `MAKEAPP_REPOS` | Override default repos folder |
| `MAKEAPP_VERBOSE` | Enable verbose output |

---

## Command Reference

### Main Actions

| Command | Description |
|---------|-------------|
| `./makeapp.ps1 start` | Start interactive feature workflow |
| `./makeapp.ps1 init` | Initialize MakeApp in repository |
| `./makeapp.ps1 config` | View/edit configuration |
| `./makeapp.ps1 status` | Show status of MakeApp and repo |
| `./makeapp.ps1 template` | Create a feature template file |
| `./makeapp.ps1 sandbox` | Manage sandbox environment |
| `./makeapp.ps1 help` | Show help message |

### Command Options

| Option | Description |
|--------|-------------|
| `-RepoPath <path>` | Specify repository path |
| `-FeatureFile <path>` | Load feature from file |
| `-BranchName <name>` | Create/switch to branch |
| `-AutoApprove` | Auto-approve all commands |
| `-NoPush` | Don't push to remote |
| `-NoPR` | Don't create pull request |
| `-Sandbox` | Run in sandbox mode |
| `-ProjectType <type>` | Project type (powershell/node/python/generic) |
| `-Force` | Force overwrite existing |
| `-Verbose` | Enable verbose output |

---

## Examples

### Example 1: Create a Python Hello World App

```powershell
# Create sandbox with Python project
./makeapp.ps1 sandbox create -ProjectType python

# Start workflow in sandbox
./makeapp.ps1 start -Sandbox
```

When prompted, enter:
- **Title**: Create Hello World App
- **Description**: Create a simple Python application that prints "Hello, World!"
- **Acceptance Criteria**: main.py exists, prints Hello World when run

### Example 2: Add a Feature to Existing Project

```powershell
# Navigate to your project
cd C:\development\repos\my-api

# Initialize MakeApp (first time only)
./makeapp.ps1 init

# Start with a feature file
./makeapp.ps1 start -FeatureFile ./features/add-logging.md -BranchName feature/logging
```

### Example 3: Automated CI/CD Usage

```powershell
# Non-interactive with auto-approval
./makeapp.ps1 start `
    -FeatureFile ./features/update-deps.json `
    -BranchName chore/dependency-update `
    -AutoApprove `
    -NoPR
```

### Example 4: Create Feature Without Pushing

```powershell
# Develop locally without pushing to GitHub
./makeapp.ps1 start -NoPush -NoPR
```

### Example 5: Quick Status Check

```powershell
# Check MakeApp and repo status
./makeapp.ps1 status
```

Output:
```
=== MakeApp Status ===

Configuration:
  User config: ✓ Found
  Default config: ✓ Found
  Repos folder: C:\development\repos

Repository (C:\development\repos\my-project):
  Branch: feature/new-api
  Uncommitted changes: No
  Untracked files: No
```

---

## Troubleshooting

### Common Issues

#### "PowerShell version 7.0 or higher required"

```powershell
# Install PowerShell 7
scoop install pwsh

# Run MakeApp with pwsh explicitly
pwsh ./makeapp.ps1 start
```

#### "GitHub CLI not authenticated"

```powershell
# Login to GitHub
gh auth login

# Verify authentication
gh auth status
```

#### "Not a git repository"

```powershell
# Initialize git in current directory
git init

# Or navigate to an existing repo
cd C:\your\git\repo
```

#### "Module not found" Error

```powershell
# Ensure you're running from the makeapp directory
cd C:\path\to\makeapp
./makeapp.ps1 start
```

#### Sandbox Creation Fails

```powershell
# Delete existing sandbox and recreate
./makeapp.ps1 sandbox delete -Force
./makeapp.ps1 sandbox create -ProjectType python
```

#### Git Push Fails

```powershell
# Check remote configuration
git remote -v

# Verify GitHub authentication
gh auth status

# Try manual push
git push -u origin your-branch-name
```

### Getting Help

```powershell
# Show help
./makeapp.ps1 help

# Show verbose output for debugging
./makeapp.ps1 start -Verbose
```

---

## Tips & Best Practices

### 1. Use Feature Files for Repeatable Workflows

Store feature templates in a `features/` folder for team reuse.

### 2. Start with Sandbox Mode

Test new workflows in sandbox before applying to real projects.

### 3. Keep Acceptance Criteria Specific

Good: "User login form validates email format before submission"  
Bad: "Login should work"

### 4. Use Branch Naming Conventions

- `feature/description` - New features
- `bugfix/description` - Bug fixes
- `chore/description` - Maintenance tasks

### 5. Review Generated Code

Always review AI-generated code before approving commits.

### 6. Configure Git Settings Appropriately

For production workflows, keep `autoCommit` and `autoPush` as `false` to review changes.

---

## Support

- **Issues**: [GitHub Issues](https://github.com/your-org/makeapp/issues)
- **Documentation**: [GitHub Wiki](https://github.com/your-org/makeapp/wiki)

---

*Last updated: January 2026*
