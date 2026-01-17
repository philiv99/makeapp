# GitHub Copilot Instructions

This file provides context and guidelines for GitHub Copilot when working in this repository.

## Project Overview

MakeApp is a PowerShell CLI tool that orchestrates feature development workflows using GitHub Copilot CLI. It automates branch management, AI-driven code generation, and PR creation.

## Code Style Guidelines

### PowerShell Conventions
- Use approved verbs for function names (Get-, Set-, New-, Remove-, etc.)
- Include comment-based help for all public functions
- Use `[CmdletBinding()]` for advanced functions
- Prefer splatting for commands with many parameters
- Use PascalCase for function names and parameters
- Use `$script:` scope for module-level variables
- Include `#Requires -Version 7.0` for PowerShell 7+ features
- Handle errors with try/catch and Write-Error
- Return objects, not formatted text

### File Organization
- One module per file in `/modules` directory
- Main entry point in root as `makeapp.ps1`
- Configuration in `/config` as JSON
- Documentation in `.github/` directory

## Architecture Patterns

### Module Structure
Each module in `/modules` should:
1. Start with `#Requires -Version 7.0`
2. Include a module synopsis and description
3. Define functions with proper CmdletBinding
4. Export functions via `Export-ModuleMember`

### Configuration Flow
1. Load defaults from `config/defaults.json`
2. Override with user config from `~/.makeapp/config.json`
3. Override with environment variables
4. Override with command-line parameters

### Error Handling
- Use `Write-Error` for errors that should stop execution
- Use `Write-Warning` for non-fatal issues
- Use `try/catch` blocks for operations that may fail
- Return `$false` or `$null` to indicate failure from functions

## Testing Guidelines

- Write Pester tests for all public functions
- Use descriptive test names following "It should..." pattern
- Mock external dependencies (git, gh CLI)
- Follow Arrange-Act-Assert pattern

## Documentation

- Update README.md for user-facing changes
- Include inline comments for complex logic
- Keep the plan.md updated with changes

## Security Considerations

- Never commit secrets or credentials
- Use environment variables for API keys
- Validate all user inputs before execution
- Block dangerous commands by default

## Dependencies

- **PowerShell 7+** - Core runtime
- **Git** - Version control operations
- **GitHub CLI (`gh`)** - GitHub operations and Copilot CLI
- **BurntToast** (optional) - Windows toast notifications

---
*This file helps GitHub Copilot understand the project context and generate more relevant suggestions.*

remember to keep the docs/convert-to-api.md file steps status up to date