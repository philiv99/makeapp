#!/usr/bin/env pwsh
#Requires -Version 7.0
<#
.SYNOPSIS
    MakeApp CLI - AI-powered feature development workflow automation
.DESCRIPTION
    A PowerShell CLI tool that orchestrates feature development workflows using
    GitHub Copilot CLI, automating branch management, AI-driven code generation,
    and PR creation.
.PARAMETER Action
    The action to perform: start, config, init, status, template
.PARAMETER RepoPath
    Path to the repository (uses current directory if not specified)
.PARAMETER FeatureFile
    Path to a feature definition file
.PARAMETER BranchName
    Name of the branch to create or switch to
.EXAMPLE
    ./makeapp.ps1 start
    Starts the interactive feature development workflow
.EXAMPLE
    ./makeapp.ps1 init
    Initializes MakeApp configuration in the current repository
.EXAMPLE
    ./makeapp.ps1 start -FeatureFile ./features/my-feature.md
    Starts workflow with a pre-defined feature file
#>

[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet("start", "config", "init", "status", "template", "sandbox", "help")]
    [string]$Action = "start",
    
    [Parameter(Position = 1)]
    [string]$SubAction,
    
    [Parameter()]
    [string]$RepoPath,
    
    [Parameter()]
    [string]$FeatureFile,
    
    [Parameter()]
    [string]$BranchName,
    
    [Parameter()]
    [switch]$AutoApprove,
    
    [Parameter()]
    [switch]$NoPush,
    
    [Parameter()]
    [switch]$NoPR,
    
    [Parameter()]
    [switch]$Sandbox,
    
    [Parameter()]
    [ValidateSet("powershell", "node", "python", "generic")]
    [string]$ProjectType = "powershell",
    
    [Parameter()]
    [switch]$Force
)

# Script root for module loading
$script:MakeAppRoot = $PSScriptRoot
$script:ConfigPath = Join-Path $env:USERPROFILE ".makeapp" "config.json"
$script:DefaultConfigPath = Join-Path $script:MakeAppRoot "config" "defaults.json"
$script:SandboxConfigPath = Join-Path $script:MakeAppRoot "config" "sandbox_defaults.json"
$script:SandboxMode = $false

#region Configuration Management

function Get-MakeAppConfig {
    <#
    .SYNOPSIS
        Gets the current MakeApp configuration
    .PARAMETER UseSandbox
        Use sandbox configuration instead of default
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [switch]$UseSandbox
    )
    
    # Determine which config to use
    $usesSandbox = $UseSandbox -or $script:SandboxMode
    
    # Start with defaults (sandbox or regular)
    $config = $null
    $configFile = if ($usesSandbox) { $script:SandboxConfigPath } else { $script:DefaultConfigPath }
    
    if (Test-Path $configFile) {
        $configContent = Get-Content $configFile -Raw
        
        # Replace placeholders for sandbox config
        if ($usesSandbox) {
            $configContent = $configContent -replace '\$\{MakeAppRoot\}', ($script:MakeAppRoot -replace '\\', '\\\\')
        }
        
        $config = $configContent | ConvertFrom-Json -AsHashtable
    }
    else {
        Write-Warning "Configuration not found at $configFile"
        $config = @{}
    }
    
    # Override with user config (only for non-sandbox mode)
    if (-not $usesSandbox -and (Test-Path $script:ConfigPath)) {
        $userConfig = Get-Content $script:ConfigPath -Raw | ConvertFrom-Json -AsHashtable
        $config = Merge-Hashtable -Base $config -Override $userConfig
    }
    
    # Override with environment variables
    $envOverrides = @{
        "GITHUB_TOKEN" = { param($c) $c.github.token = $env:GITHUB_TOKEN }
        "MAKEAPP_REPOS" = { param($c) if (-not $usesSandbox) { $c.folders.repos = $env:MAKEAPP_REPOS } }
        "MAKEAPP_VERBOSE" = { param($c) $c.ui.verboseOutput = $true }
    }
    
    foreach ($envVar in $envOverrides.Keys) {
        if ([System.Environment]::GetEnvironmentVariable($envVar)) {
            & $envOverrides[$envVar] $config
        }
    }
    
    # Add sandbox indicator
    if (-not $config.ContainsKey('_sandbox')) {
        $config['_sandbox'] = $usesSandbox
    }
    
    return $config
}

function Set-MakeAppConfig {
    <#
    .SYNOPSIS
        Saves configuration to user config file
    .PARAMETER Config
        Configuration hashtable to save
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config
    )
    
    $configDir = Split-Path $script:ConfigPath -Parent
    if (-not (Test-Path $configDir)) {
        New-Item -ItemType Directory -Path $configDir -Force | Out-Null
    }
    
    $Config | ConvertTo-Json -Depth 10 | Set-Content $script:ConfigPath -Encoding UTF8
    Write-Host "Configuration saved to: $script:ConfigPath" -ForegroundColor Green
}

function Merge-Hashtable {
    <#
    .SYNOPSIS
        Deep merges two hashtables
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Base,
        
        [Parameter(Mandatory)]
        [hashtable]$Override
    )
    
    $result = $Base.Clone()
    
    foreach ($key in $Override.Keys) {
        if ($result.ContainsKey($key) -and $result[$key] -is [hashtable] -and $Override[$key] -is [hashtable]) {
            $result[$key] = Merge-Hashtable -Base $result[$key] -Override $Override[$key]
        }
        else {
            $result[$key] = $Override[$key]
        }
    }
    
    return $result
}

#endregion

#region Module Loading

function Initialize-Modules {
    <#
    .SYNOPSIS
        Loads all MakeApp modules
    #>
    [CmdletBinding()]
    param()
    
    $modulesPath = Join-Path $script:MakeAppRoot "modules"
    
    $modules = @(
        "Sandbox.ps1",
        "BranchManager.ps1",
        "FeaturePrompt.ps1",
        "CopilotConfig.ps1",
        "Executor.ps1",
        "GitAutomation.ps1"
    )
    
    foreach ($module in $modules) {
        $modulePath = Join-Path $modulesPath $module
        if (Test-Path $modulePath) {
            . $modulePath
            Write-Verbose "Loaded module: $module"
        }
        else {
            Write-Warning "Module not found: $modulePath"
        }
    }
}

#endregion

#region Actions

# Dot-source modules at script level so functions are available globally
$modulesPath = Join-Path $script:MakeAppRoot "modules"
$modules = @(
    "Sandbox.ps1",
    "BranchManager.ps1",
    "FeaturePrompt.ps1",
    "CopilotConfig.ps1",
    "Executor.ps1",
    "GitAutomation.ps1"
)

foreach ($module in $modules) {
    $modulePath = Join-Path $modulesPath $module
    if (Test-Path $modulePath) {
        . $modulePath
    }
}

function Start-FeatureWorkflow {
    <#
    .SYNOPSIS
        Main workflow for developing a new feature
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$RepoPath,
        
        [Parameter()]
        [string]$FeatureFile,
        
        [Parameter()]
        [string]$BranchName,
        
        [Parameter()]
        [switch]$AutoApprove,
        
        [Parameter()]
        [switch]$NoPush,
        
        [Parameter()]
        [switch]$NoPR
    )
    
    $config = Get-MakeAppConfig
    
    Show-Banner
    
    Write-Host "`n=== Step 1: Select Repository & Branch ===" -ForegroundColor Cyan
    
    # Determine repository path
    if (-not $RepoPath) {
        $RepoPath = Get-Location
    }
    
    # Verify it's a git repo
    if (-not (Test-Path (Join-Path $RepoPath ".git"))) {
        # Offer to select from repos folder
        $selection = Select-RepoBranch -ReposPath $config.folders.repos -AllowCreate
        if (-not $selection) {
            Write-Error "No repository selected"
            return
        }
        $RepoPath = $selection.RepoPath
        $BranchName = $selection.BranchName
    }
    else {
        # Already in a repo, handle branch
        if ($BranchName) {
            # Check if branch exists
            $branches = Get-RepoBranches -RepoPath $RepoPath
            $existingBranch = $branches | Where-Object { $_.Name -eq $BranchName }
            
            if ($existingBranch) {
                Switch-ToBranch -RepoPath $RepoPath -BranchName $BranchName | Out-Null
            }
            else {
                # Create new branch
                New-FeatureBranch -RepoPath $RepoPath -BranchName $BranchName -SwitchToBranch | Out-Null
            }
        }
        else {
            # Interactive branch selection
            $currentBranch = Get-CurrentBranch -RepoPath $RepoPath
            Write-Host "Current branch: $currentBranch" -ForegroundColor White
            
            $changeBranch = Read-Host "Change branch? (y/N)"
            if ($changeBranch -eq 'y' -or $changeBranch -eq 'Y') {
                $selection = Select-RepoBranch -ReposPath (Split-Path $RepoPath -Parent) -AllowCreate
                if ($selection) {
                    $BranchName = $selection.BranchName
                }
            }
            else {
                $BranchName = $currentBranch
            }
        }
    }
    
    Write-Host "Repository: $RepoPath" -ForegroundColor Green
    Write-Host "Branch: $BranchName" -ForegroundColor Green
    
    Write-Host "`n=== Step 2: Feature Requirements ===" -ForegroundColor Cyan
    
    # Get feature requirements
    $feature = $null
    
    if ($FeatureFile -and (Test-Path $FeatureFile)) {
        Write-Host "Loading feature from file: $FeatureFile" -ForegroundColor White
        $feature = Import-FeatureFromFile -FilePath $FeatureFile
    }
    else {
        $feature = Get-FeatureRequirements
    }
    
    if (-not $feature) {
        Write-Error "No feature requirements provided"
        return
    }
    
    # Save feature to temp
    $featurePath = Join-Path $config.folders.temp "current-feature.json"
    Save-FeatureInstructions -Feature $feature -OutputPath $featurePath -Format "json" | Out-Null
    
    Write-Host "`n=== Step 3: Verify Configurations ===" -ForegroundColor Cyan
    
    # Check all configurations
    $configResults = Test-AllConfigurations -RepoPath $RepoPath -AutoFix
    
    if (-not $configResults.AllValid) {
        $continue = Read-Host "`nSome configurations are missing. Continue anyway? (y/N)"
        if ($continue -ne 'y' -and $continue -ne 'Y') {
            return
        }
    }
    
    # Set agent permissions
    if ($AutoApprove) {
        Set-AgentPermissions -RepoPath $RepoPath -AllowAll | Out-Null
    }
    else {
        Set-AgentPermissions -RepoPath $RepoPath | Out-Null
    }
    
    Write-Host "`n=== Step 4: Execute Implementation ===" -ForegroundColor Cyan
    
    $confirm = Read-Host "Start agent orchestration? (Y/n)"
    if ($confirm -eq 'n' -or $confirm -eq 'N') {
        Write-Host "Orchestration cancelled" -ForegroundColor Yellow
        return
    }
    
    # Start orchestration
    $orchestrationResult = Start-AgentOrchestration -Feature $feature -RepoPath $RepoPath -MaxIterations $config.agent.maxIterations
    
    Write-Host "`n=== Step 5: Complete Workflow ===" -ForegroundColor Cyan
    
    if ($orchestrationResult) {
        # Modify config based on flags
        if ($NoPush) {
            $config.git.autoPush = $false
        }
        if ($NoPR) {
            $config.git.autoCreatePr = $false
        }
        
        Complete-FeatureWorkflow -RepoPath $RepoPath -Feature $feature -CreatePR:(-not $NoPR)
    }
    else {
        Write-Host "Orchestration did not complete successfully" -ForegroundColor Yellow
        
        $saveChanges = Read-Host "Save partial changes anyway? (y/N)"
        if ($saveChanges -eq 'y' -or $saveChanges -eq 'Y') {
            Complete-FeatureWorkflow -RepoPath $RepoPath -Feature $feature -CreatePR:$false
        }
    }
    
    Write-Host "`n=== Workflow Complete ===" -ForegroundColor Green
}

function Initialize-Repository {
    <#
    .SYNOPSIS
        Initializes MakeApp configuration in a repository
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$RepoPath
    )
    
    if (-not $RepoPath) {
        $RepoPath = Get-Location
    }
    
    Write-Host "`n=== Initializing MakeApp in Repository ===" -ForegroundColor Cyan
    Write-Host "Repository: $RepoPath" -ForegroundColor White
    
    # Create .github/copilot-instructions.md
    $projectType = Read-Host "Project type (powershell/node/dotnet/python/generic) [generic]"
    if (-not $projectType) { $projectType = "generic" }
    
    New-CopilotInstructions -RepoPath $RepoPath -ProjectType $projectType -Force:$false
    
    # Create .vscode/mcp.json
    New-McpConfig -RepoPath $RepoPath -Force:$false
    
    Write-Host "`n✓ Repository initialized for MakeApp" -ForegroundColor Green
}

function Show-Status {
    <#
    .SYNOPSIS
        Shows the current status of MakeApp and repository
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$RepoPath
    )
    
    if (-not $RepoPath) {
        $RepoPath = Get-Location
    }
    
    $config = Get-MakeAppConfig
    
    Write-Host "`n=== MakeApp Status ===" -ForegroundColor Cyan
    
    # Configuration status
    Write-Host "`nConfiguration:" -ForegroundColor Yellow
    Write-Host "  User config: $(if (Test-Path $script:ConfigPath) { '✓ Found' } else { '✗ Not found' })" -ForegroundColor White
    Write-Host "  Default config: $(if (Test-Path $script:DefaultConfigPath) { '✓ Found' } else { '✗ Not found' })" -ForegroundColor White
    Write-Host "  Repos folder: $($config.folders.repos)" -ForegroundColor White
    
    # Repository status
    Write-Host "`nRepository ($RepoPath):" -ForegroundColor Yellow
    if (Test-Path (Join-Path $RepoPath ".git")) {
        $gitState = Test-GitState -RepoPath $RepoPath
        Write-Host "  Branch: $($gitState.CurrentBranch)" -ForegroundColor White
        Write-Host "  Uncommitted changes: $(if ($gitState.HasUncommittedChanges) { 'Yes' } else { 'No' })" -ForegroundColor White
        Write-Host "  Untracked files: $(if ($gitState.HasUntrackedFiles) { 'Yes' } else { 'No' })" -ForegroundColor White
        
        # Check configurations
        Test-AllConfigurations -RepoPath $RepoPath | Out-Null
    }
    else {
        Write-Host "  Not a git repository" -ForegroundColor Red
    }
}

function New-FeatureTemplateFile {
    <#
    .SYNOPSIS
        Creates a new feature template file
    #>
    [CmdletBinding()]
    param()
    
    $outputPath = Read-Host "Output file path [./feature.md]"
    if (-not $outputPath) { $outputPath = "./feature.md" }
    
    $format = if ($outputPath -match "\.json$") { "json" } else { "md" }
    
    New-FeatureTemplate -OutputPath $outputPath -Format $format
}

function Show-Configuration {
    <#
    .SYNOPSIS
        Shows and allows editing of configuration
    #>
    [CmdletBinding()]
    param()
    
    $config = Get-MakeAppConfig
    
    Write-Host "`n=== MakeApp Configuration ===" -ForegroundColor Cyan
    Write-Host ($config | ConvertTo-Json -Depth 5) -ForegroundColor Gray
    
    $edit = Read-Host "`nEdit configuration? (y/N)"
    if ($edit -eq 'y' -or $edit -eq 'Y') {
        # Interactive configuration editing
        Write-Host "`nEnter new values (leave empty to keep current):" -ForegroundColor Yellow
        
        $newOwner = Read-Host "GitHub owner [$($config.github.owner)]"
        if ($newOwner) { $config.github.owner = $newOwner }
        
        $newRepos = Read-Host "Repos folder [$($config.folders.repos)]"
        if ($newRepos) { $config.folders.repos = $newRepos }
        
        $newAutoStage = Read-Host "Auto-stage changes (true/false) [$($config.git.autoStage)]"
        if ($newAutoStage) { $config.git.autoStage = $newAutoStage -eq 'true' }
        
        $newAutoCommit = Read-Host "Auto-commit changes (true/false) [$($config.git.autoCommit)]"
        if ($newAutoCommit) { $config.git.autoCommit = $newAutoCommit -eq 'true' }
        
        $newAutoPush = Read-Host "Auto-push changes (true/false) [$($config.git.autoPush)]"
        if ($newAutoPush) { $config.git.autoPush = $newAutoPush -eq 'true' }
        
        $newAutoCreatePr = Read-Host "Auto-create PR (true/false) [$($config.git.autoCreatePr)]"
        if ($newAutoCreatePr) { $config.git.autoCreatePr = $newAutoCreatePr -eq 'true' }
        
        Set-MakeAppConfig -Config $config
    }
}

function Show-Help {
    <#
    .SYNOPSIS
        Shows help information
    #>
    [CmdletBinding()]
    param()
    
    Show-Banner
    
    Write-Host @"

USAGE:
    makeapp.ps1 <action> [subaction] [options]

ACTIONS:
    start       Start the interactive feature development workflow (default)
    init        Initialize MakeApp configuration in current repository
    config      View and edit MakeApp configuration
    status      Show current status of MakeApp and repository
    template    Create a new feature template file
    sandbox     Manage sandbox environment (create, delete, status, reset, enter)
    help        Show this help message

SANDBOX SUBACTIONS:
    create      Create a new sandbox environment
    delete      Remove the sandbox environment
    status      Show sandbox status
    reset       Reset sandbox to initial state
    enter       Change directory to sandbox repo

OPTIONS:
    -RepoPath       Path to the repository
    -FeatureFile    Path to a feature definition file (.md or .json)
    -BranchName     Name of branch to create or switch to
    -AutoApprove    Auto-approve all commands (use with caution)
    -NoPush         Don't push changes to remote
    -NoPR           Don't create a pull request
    -Sandbox        Run workflow in sandbox mode
    -ProjectType    Project type for sandbox (powershell, node, python, generic)
    -Force          Force operation (overwrite existing)
    -Verbose        Enable verbose output

EXAMPLES:
    # Start interactive workflow
    ./makeapp.ps1 start

    # Start in sandbox mode (isolated environment)
    ./makeapp.ps1 start -Sandbox

    # Create sandbox with Node.js project
    ./makeapp.ps1 sandbox create -ProjectType node

    # Delete sandbox
    ./makeapp.ps1 sandbox delete

    # Check sandbox status
    ./makeapp.ps1 sandbox status

    # Start with a feature file
    ./makeapp.ps1 start -FeatureFile ./features/add-auth.md

    # Initialize current repo
    ./makeapp.ps1 init

CONFIGURATION:
    User config:    ~/.makeapp/config.json
    Default config: ./config/defaults.json
    Sandbox config: ./config/sandbox_defaults.json

For more information, see: https://github.com/your-org/makeapp

"@ -ForegroundColor White
}

function Invoke-SandboxAction {
    <#
    .SYNOPSIS
        Handles sandbox subactions
    .PARAMETER SubAction
        The sandbox subaction to perform
    .PARAMETER ProjectType
        Type of project for sandbox creation
    .PARAMETER Force
        Force the operation
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$SubAction,
        
        [Parameter()]
        [string]$ProjectType = "powershell",
        
        [Parameter()]
        [switch]$Force
    )
    
    # If no subaction, show sandbox menu
    if (-not $SubAction) {
        Show-SandboxMenu
        return
    }
    
    switch ($SubAction.ToLower()) {
        "create" {
            New-Sandbox -MakeAppRoot $script:MakeAppRoot -ProjectType $ProjectType -Force:$Force
        }
        "delete" {
            Remove-Sandbox -MakeAppRoot $script:MakeAppRoot -Confirm:(-not $Force)
        }
        "status" {
            Show-SandboxStatus -MakeAppRoot $script:MakeAppRoot
        }
        "reset" {
            Reset-Sandbox -MakeAppRoot $script:MakeAppRoot -ProjectType $ProjectType
        }
        "enter" {
            Enter-Sandbox -MakeAppRoot $script:MakeAppRoot
        }
        "info" {
            $info = Get-SandboxInfo -MakeAppRoot $script:MakeAppRoot
            $info | ConvertTo-Json -Depth 5 | Write-Host -ForegroundColor Gray
        }
        default {
            Write-Host "Unknown sandbox action: $SubAction" -ForegroundColor Red
            Write-Host "Valid actions: create, delete, status, reset, enter" -ForegroundColor Yellow
        }
    }
}

function Show-SandboxMenu {
    <#
    .SYNOPSIS
        Shows interactive sandbox menu
    #>
    [CmdletBinding()]
    param()
    
    Show-Banner
    
    $sandboxTest = Test-Sandbox -MakeAppRoot $script:MakeAppRoot
    
    Write-Host "`n╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║                    SANDBOX MANAGER                        ║" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    
    if ($sandboxTest.IsValid) {
        Write-Host "`nSandbox Status: " -NoNewline -ForegroundColor White
        Write-Host "ACTIVE" -ForegroundColor Green
        Write-Host "Path: $($sandboxTest.RepoPath)" -ForegroundColor Gray
    }
    else {
        Write-Host "`nSandbox Status: " -NoNewline -ForegroundColor White
        Write-Host "NOT CREATED" -ForegroundColor Yellow
    }
    
    Write-Host "`nOptions:" -ForegroundColor Yellow
    
    if ($sandboxTest.IsValid) {
        Write-Host "  [1] Show status" -ForegroundColor White
        Write-Host "  [2] Enter sandbox" -ForegroundColor White
        Write-Host "  [3] Reset sandbox" -ForegroundColor White
        Write-Host "  [4] Delete sandbox" -ForegroundColor Red
        Write-Host "  [5] Start workflow in sandbox" -ForegroundColor Green
    }
    else {
        Write-Host "  [1] Create sandbox (PowerShell)" -ForegroundColor White
        Write-Host "  [2] Create sandbox (Node.js)" -ForegroundColor White
        Write-Host "  [3] Create sandbox (Python)" -ForegroundColor White
        Write-Host "  [4] Create sandbox (Generic)" -ForegroundColor White
    }
    Write-Host "  [Q] Quit" -ForegroundColor Gray
    
    $choice = Read-Host "`nSelect option"
    
    if ($sandboxTest.IsValid) {
        switch ($choice) {
            "1" { Show-SandboxStatus -MakeAppRoot $script:MakeAppRoot }
            "2" { Enter-Sandbox -MakeAppRoot $script:MakeAppRoot }
            "3" { Reset-Sandbox -MakeAppRoot $script:MakeAppRoot }
            "4" { Remove-Sandbox -MakeAppRoot $script:MakeAppRoot }
            "5" { 
                $script:SandboxMode = $true
                $sandboxInfo = Get-SandboxInfo -MakeAppRoot $script:MakeAppRoot
                Start-FeatureWorkflow -RepoPath $sandboxInfo.RepoPath -AutoApprove -NoPush -NoPR
            }
            "Q" { return }
            "q" { return }
            default { Write-Host "Invalid option" -ForegroundColor Red }
        }
    }
    else {
        switch ($choice) {
            "1" { New-Sandbox -MakeAppRoot $script:MakeAppRoot -ProjectType "powershell" }
            "2" { New-Sandbox -MakeAppRoot $script:MakeAppRoot -ProjectType "node" }
            "3" { New-Sandbox -MakeAppRoot $script:MakeAppRoot -ProjectType "python" }
            "4" { New-Sandbox -MakeAppRoot $script:MakeAppRoot -ProjectType "generic" }
            "Q" { return }
            "q" { return }
            default { Write-Host "Invalid option" -ForegroundColor Red }
        }
    }
}

function Show-Banner {
    <#
    .SYNOPSIS
        Shows the MakeApp banner
    #>
    [CmdletBinding()]
    param()
    
    Write-Host @"

  __  __       _           _                
 |  \/  | __ _| | _____   / \   _ __  _ __  
 | |\/| |/ _`  | |/ / _ \ / _ \ | '_ \| '_ \ 
 | |  | | (_| |   <  __// ___ \| |_) | |_) |
 |_|  |_|\__,_|_|\_\___/_/   \_\ .__/| .__/ 
                               |_|   |_|    
  AI-Powered Feature Development Workflow

"@ -ForegroundColor Magenta
}

#endregion

#region Main

# Handle sandbox mode
if ($Sandbox -and $Action -eq "start") {
    $script:SandboxMode = $true
    
    # Ensure sandbox exists
    $sandboxTest = Test-Sandbox -MakeAppRoot $script:MakeAppRoot
    if (-not $sandboxTest.IsValid) {
        Write-Host "Sandbox not found. Creating..." -ForegroundColor Yellow
        New-Sandbox -MakeAppRoot $script:MakeAppRoot -ProjectType $ProjectType | Out-Null
    }
    
    # Set repo path to sandbox
    $sandboxInfo = Get-SandboxInfo -MakeAppRoot $script:MakeAppRoot
    $RepoPath = $sandboxInfo.RepoPath
}

# Execute action
switch ($Action) {
    "start" {
        Start-FeatureWorkflow -RepoPath $RepoPath -FeatureFile $FeatureFile -BranchName $BranchName -AutoApprove:$AutoApprove -NoPush:$NoPush -NoPR:$NoPR
    }
    "init" {
        Initialize-Repository -RepoPath $RepoPath
    }
    "config" {
        Show-Configuration
    }
    "status" {
        Show-Status -RepoPath $RepoPath
    }
    "template" {
        New-FeatureTemplateFile
    }
    "sandbox" {
        Invoke-SandboxAction -SubAction $SubAction -ProjectType $ProjectType -Force:$Force
    }
    "help" {
        Show-Help
    }
    default {
        Show-Help
    }
}

#endregion
