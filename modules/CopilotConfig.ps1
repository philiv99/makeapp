#Requires -Version 7.0
<#
.SYNOPSIS
    Copilot configuration module for MakeApp CLI
.DESCRIPTION
    Provides functions for verifying and managing GitHub Copilot configurations,
    MCP settings, and agent permissions.
#>

function Test-CopilotInstructions {
    <#
    .SYNOPSIS
        Checks if .github/copilot-instructions.md exists and is valid
    .PARAMETER RepoPath
        Path to the repository
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$RepoPath
    )
    
    $instructionsPath = Join-Path $RepoPath ".github" "copilot-instructions.md"
    
    $result = [PSCustomObject]@{
        Exists = $false
        Path = $instructionsPath
        IsValid = $false
        ContentLength = 0
        HasSections = $false
        Warnings = @()
    }
    
    if (Test-Path $instructionsPath) {
        $result.Exists = $true
        $content = Get-Content $instructionsPath -Raw
        $result.ContentLength = $content.Length
        
        # Check for common sections
        $hasCodingStyle = $content -match "(?i)(coding\s*style|code\s*style|style\s*guide)"
        $hasPatterns = $content -match "(?i)(pattern|convention|standard)"
        $hasInstructions = $content -match "(?i)(instruction|guideline|rule)"
        
        $result.HasSections = $hasCodingStyle -or $hasPatterns -or $hasInstructions
        $result.IsValid = $result.ContentLength -gt 100 -and $result.HasSections
        
        if ($result.ContentLength -lt 100) {
            $result.Warnings += "Instructions file is very short (less than 100 characters)"
        }
        if (-not $result.HasSections) {
            $result.Warnings += "Instructions file may be missing key sections (coding style, patterns, guidelines)"
        }
    }
    
    return $result
}

function New-CopilotInstructions {
    <#
    .SYNOPSIS
        Creates a default copilot-instructions.md file
    .PARAMETER RepoPath
        Path to the repository
    .PARAMETER ProjectType
        Type of project (powershell, node, dotnet, python, etc.)
    .PARAMETER Force
        Overwrite existing file
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$RepoPath,
        
        [Parameter()]
        [ValidateSet("powershell", "node", "dotnet", "python", "generic")]
        [string]$ProjectType = "generic",
        
        [Parameter()]
        [switch]$Force
    )
    
    $githubDir = Join-Path $RepoPath ".github"
    $instructionsPath = Join-Path $githubDir "copilot-instructions.md"
    
    # Check if already exists
    if ((Test-Path $instructionsPath) -and -not $Force) {
        Write-Warning "Copilot instructions already exist. Use -Force to overwrite."
        return $instructionsPath
    }
    
    # Ensure .github directory exists
    if (-not (Test-Path $githubDir)) {
        New-Item -ItemType Directory -Path $githubDir -Force | Out-Null
    }
    
    # Generate content based on project type
    $content = Get-CopilotInstructionsTemplate -ProjectType $ProjectType
    
    $content | Set-Content $instructionsPath -Encoding UTF8
    Write-Host "Created copilot-instructions.md at: $instructionsPath" -ForegroundColor Green
    
    return $instructionsPath
}

function Get-CopilotInstructionsTemplate {
    <#
    .SYNOPSIS
        Gets the template content for copilot-instructions.md
    .PARAMETER ProjectType
        Type of project
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$ProjectType = "generic"
    )
    
    $baseContent = @"
# GitHub Copilot Instructions

This file provides context and guidelines for GitHub Copilot when working in this repository.

## Project Overview

<!-- Describe your project here -->

## Code Style Guidelines

"@

    $projectSpecific = switch ($ProjectType) {
        "powershell" {
            @"

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
- Main entry point in root as `.ps1`
- Configuration in `/config` as JSON

"@
        }
        "node" {
            @"

### JavaScript/TypeScript Conventions
- Use TypeScript for type safety when possible
- Prefer `const` over `let`, avoid `var`
- Use async/await over callbacks or raw promises
- Use meaningful variable and function names
- Keep functions small and focused
- Use ESLint and Prettier for formatting
- Write unit tests for new functionality

### File Organization
- Source code in `/src` directory
- Tests in `/tests` or `/__tests__` directories
- Configuration in root (package.json, tsconfig.json, etc.)

"@
        }
        "dotnet" {
            @"

### C#/.NET Conventions
- Follow Microsoft's C# coding conventions
- Use nullable reference types
- Prefer LINQ for collection operations
- Use async/await for I/O operations
- Include XML documentation for public APIs
- Use dependency injection
- Follow SOLID principles

### File Organization
- One class per file
- Organize by feature/domain, not by type
- Tests in separate test projects

"@
        }
        "python" {
            @"

### Python Conventions
- Follow PEP 8 style guide
- Use type hints for function signatures
- Use docstrings for modules, classes, and functions
- Prefer f-strings for string formatting
- Use virtual environments
- Include requirements.txt or pyproject.toml

### File Organization
- Package code in `/src` or project-named directory
- Tests in `/tests` directory
- Configuration in `pyproject.toml` or `setup.py`

"@
        }
        default {
            @"

### General Conventions
- Write clean, readable code
- Include comments for complex logic
- Follow existing patterns in the codebase
- Keep functions/methods focused and small
- Handle errors appropriately
- Write tests for new functionality

"@
        }
    }

    $commonSections = @"

## Architecture Patterns

<!-- Describe key architectural decisions and patterns used -->

## Testing Guidelines

- Write unit tests for new functionality
- Maintain existing test coverage
- Use descriptive test names
- Follow Arrange-Act-Assert pattern

## Documentation

- Update README.md for user-facing changes
- Include inline comments for complex logic
- Keep API documentation up to date

## Security Considerations

- Never commit secrets or credentials
- Validate all user inputs
- Follow principle of least privilege
- Use environment variables for configuration

## Dependencies

- Minimize external dependencies
- Keep dependencies up to date
- Document why each dependency is needed

---
*This file helps GitHub Copilot understand the project context and generate more relevant suggestions.*
"@

    return $baseContent + $projectSpecific + $commonSections
}

function Test-McpConfig {
    <#
    .SYNOPSIS
        Validates .vscode/mcp.json configuration
    .PARAMETER RepoPath
        Path to the repository
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$RepoPath
    )
    
    $mcpPath = Join-Path $RepoPath ".vscode" "mcp.json"
    
    $result = [PSCustomObject]@{
        Exists = $false
        Path = $mcpPath
        IsValid = $false
        Servers = @()
        Errors = @()
    }
    
    if (Test-Path $mcpPath) {
        $result.Exists = $true
        
        try {
            $content = Get-Content $mcpPath -Raw | ConvertFrom-Json
            
            if ($content.servers) {
                $result.Servers = $content.servers.PSObject.Properties.Name
                $result.IsValid = $true
            }
            elseif ($content.mcpServers) {
                $result.Servers = $content.mcpServers.PSObject.Properties.Name
                $result.IsValid = $true
            }
            else {
                $result.Errors += "MCP config has no servers defined"
            }
        }
        catch {
            $result.Errors += "Failed to parse MCP config: $_"
        }
    }
    
    return $result
}

function New-McpConfig {
    <#
    .SYNOPSIS
        Creates a default MCP configuration file
    .PARAMETER RepoPath
        Path to the repository
    .PARAMETER Servers
        Array of server configurations to include
    .PARAMETER Force
        Overwrite existing file
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$RepoPath,
        
        [Parameter()]
        [hashtable[]]$Servers,
        
        [Parameter()]
        [switch]$Force
    )
    
    $vscodeDir = Join-Path $RepoPath ".vscode"
    $mcpPath = Join-Path $vscodeDir "mcp.json"
    
    if ((Test-Path $mcpPath) -and -not $Force) {
        Write-Warning "MCP config already exists. Use -Force to overwrite."
        return $mcpPath
    }
    
    # Ensure .vscode directory exists
    if (-not (Test-Path $vscodeDir)) {
        New-Item -ItemType Directory -Path $vscodeDir -Force | Out-Null
    }
    
    # Default configuration
    $config = @{
        servers = @{
            "filesystem" = @{
                "command" = "npx"
                "args" = @("-y", "@modelcontextprotocol/server-filesystem", $RepoPath)
            }
        }
        inputs = @()
    }
    
    # Add custom servers if provided
    if ($Servers) {
        foreach ($server in $Servers) {
            if ($server.Name -and $server.Command) {
                $config.servers[$server.Name] = @{
                    "command" = $server.Command
                    "args" = $server.Args ?? @()
                }
            }
        }
    }
    
    $config | ConvertTo-Json -Depth 10 | Set-Content $mcpPath -Encoding UTF8
    Write-Host "Created MCP config at: $mcpPath" -ForegroundColor Green
    
    return $mcpPath
}

function Set-AgentPermissions {
    <#
    .SYNOPSIS
        Configures agent permissions for the session
    .PARAMETER RepoPath
        Path to the repository
    .PARAMETER AllowAll
        Allow all commands (use with caution)
    .PARAMETER AllowedCommands
        List of allowed commands
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$RepoPath,
        
        [Parameter()]
        [switch]$AllowAll,
        
        [Parameter()]
        [string[]]$AllowedCommands
    )
    
    $config = Get-MakeAppConfig
    
    if ($AllowAll) {
        Write-Warning "Enabling all commands. This allows the agent to execute any command."
        $confirm = Read-Host "Are you sure? (yes/no)"
        if ($confirm -ne "yes") {
            Write-Host "Cancelled" -ForegroundColor Yellow
            return $false
        }
        $config.agent.autoApproveCommands = $true
    }
    
    if ($AllowedCommands) {
        $config.agent.allowedCommands = $AllowedCommands
    }
    
    # Store in session
    $script:AgentPermissions = @{
        RepoPath = $RepoPath
        AutoApprove = $config.agent.autoApproveCommands
        AllowedCommands = $config.agent.allowedCommands
        BlockedCommands = $config.agent.blockedCommands
    }
    
    Write-Host "Agent permissions configured" -ForegroundColor Green
    Write-Host "  Auto-approve: $($script:AgentPermissions.AutoApprove)" -ForegroundColor White
    Write-Host "  Allowed commands: $($script:AgentPermissions.AllowedCommands -join ', ')" -ForegroundColor White
    
    return $true
}

function Test-CommandAllowed {
    <#
    .SYNOPSIS
        Checks if a command is allowed to execute
    .PARAMETER Command
        The command to check
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Command
    )
    
    $config = Get-MakeAppConfig
    
    # Check blocked commands first
    foreach ($blocked in $config.agent.blockedCommands) {
        if ($Command -match [regex]::Escape($blocked)) {
            return [PSCustomObject]@{
                Allowed = $false
                Reason = "Command matches blocked pattern: $blocked"
            }
        }
    }
    
    # If auto-approve is enabled, allow
    if ($config.agent.autoApproveCommands) {
        return [PSCustomObject]@{
            Allowed = $true
            Reason = "Auto-approve enabled"
        }
    }
    
    # Check allowed commands
    $commandName = ($Command -split '\s+')[0]
    foreach ($allowed in $config.agent.allowedCommands) {
        if ($commandName -eq $allowed -or $commandName -match "^.*[\\/]$([regex]::Escape($allowed))(\.exe)?$") {
            return [PSCustomObject]@{
                Allowed = $true
                Reason = "Command in allowed list"
            }
        }
    }
    
    # Not explicitly allowed - prompt user
    return [PSCustomObject]@{
        Allowed = $false
        Reason = "Command not in allowed list. User approval required."
        RequiresApproval = $true
    }
}

function Test-AllConfigurations {
    <#
    .SYNOPSIS
        Tests all Copilot-related configurations for a repository
    .PARAMETER RepoPath
        Path to the repository
    .PARAMETER AutoFix
        Automatically create missing configurations
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$RepoPath,
        
        [Parameter()]
        [switch]$AutoFix
    )
    
    Write-Host "`n=== Checking Copilot Configurations ===" -ForegroundColor Cyan
    
    $results = @{
        CopilotInstructions = $null
        McpConfig = $null
        GitHubCli = $null
        AllValid = $true
    }
    
    # Check copilot-instructions.md
    Write-Host "`nChecking .github/copilot-instructions.md..." -ForegroundColor White
    $results.CopilotInstructions = Test-CopilotInstructions -RepoPath $RepoPath
    
    if ($results.CopilotInstructions.Exists) {
        if ($results.CopilotInstructions.IsValid) {
            Write-Host "  ✓ Valid copilot-instructions.md found" -ForegroundColor Green
        }
        else {
            Write-Host "  ⚠ copilot-instructions.md exists but may need improvement" -ForegroundColor Yellow
            $results.CopilotInstructions.Warnings | ForEach-Object { Write-Host "    - $_" -ForegroundColor Yellow }
        }
    }
    else {
        Write-Host "  ✗ copilot-instructions.md not found" -ForegroundColor Red
        $results.AllValid = $false
        
        if ($AutoFix) {
            Write-Host "  Creating default copilot-instructions.md..." -ForegroundColor Cyan
            New-CopilotInstructions -RepoPath $RepoPath -ProjectType "powershell"
        }
    }
    
    # Check MCP config
    Write-Host "`nChecking .vscode/mcp.json..." -ForegroundColor White
    $results.McpConfig = Test-McpConfig -RepoPath $RepoPath
    
    if ($results.McpConfig.Exists) {
        if ($results.McpConfig.IsValid) {
            Write-Host "  ✓ Valid MCP config found" -ForegroundColor Green
            Write-Host "    Servers: $($results.McpConfig.Servers -join ', ')" -ForegroundColor Gray
        }
        else {
            Write-Host "  ⚠ MCP config has issues" -ForegroundColor Yellow
            $results.McpConfig.Errors | ForEach-Object { Write-Host "    - $_" -ForegroundColor Yellow }
        }
    }
    else {
        Write-Host "  ✗ MCP config not found" -ForegroundColor Red
        $results.AllValid = $false
        
        if ($AutoFix) {
            Write-Host "  Creating default mcp.json..." -ForegroundColor Cyan
            New-McpConfig -RepoPath $RepoPath
        }
    }
    
    # Check GitHub CLI
    Write-Host "`nChecking GitHub CLI (gh)..." -ForegroundColor White
    $ghVersion = gh --version 2>$null
    if ($ghVersion) {
        Write-Host "  ✓ GitHub CLI installed: $($ghVersion[0])" -ForegroundColor Green
        
        # Check authentication
        $authStatus = gh auth status 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✓ GitHub CLI authenticated" -ForegroundColor Green
        }
        else {
            Write-Host "  ✗ GitHub CLI not authenticated" -ForegroundColor Red
            Write-Host "    Run 'gh auth login' to authenticate" -ForegroundColor Yellow
            $results.AllValid = $false
        }
        
        $results.GitHubCli = @{
            Installed = $true
            Version = $ghVersion[0]
            Authenticated = $LASTEXITCODE -eq 0
        }
    }
    else {
        Write-Host "  ✗ GitHub CLI not found" -ForegroundColor Red
        Write-Host "    Install from: https://cli.github.com/" -ForegroundColor Yellow
        $results.AllValid = $false
        $results.GitHubCli = @{
            Installed = $false
        }
    }
    
    Write-Host "`n=== Configuration Check Complete ===" -ForegroundColor Cyan
    if ($results.AllValid) {
        Write-Host "All configurations are valid!" -ForegroundColor Green
    }
    else {
        Write-Host "Some configurations need attention" -ForegroundColor Yellow
    }
    
    return $results
}
