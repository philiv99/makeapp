#Requires -Version 7.0
<#
.SYNOPSIS
    Sandbox management module for MakeApp CLI
.DESCRIPTION
    Provides functions for creating, managing, and cleaning up sandbox
    environments for testing MakeApp workflows locally.
#>

# Module-level variables
$script:SandboxActive = $false
$script:SandboxPath = $null

function Get-SandboxConfig {
    <#
    .SYNOPSIS
        Gets the sandbox configuration with resolved paths
    .PARAMETER MakeAppRoot
        Root path of MakeApp installation
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$MakeAppRoot
    )
    
    $sandboxConfigPath = Join-Path $MakeAppRoot "config" "sandbox_defaults.json"
    
    if (-not (Test-Path $sandboxConfigPath)) {
        Write-Error "Sandbox configuration not found: $sandboxConfigPath"
        return $null
    }
    
    $configContent = Get-Content $sandboxConfigPath -Raw
    
    # Replace ${MakeAppRoot} placeholder with actual path
    $configContent = $configContent -replace '\$\{MakeAppRoot\}', ($MakeAppRoot -replace '\\', '\\\\')
    
    $config = $configContent | ConvertFrom-Json -AsHashtable
    
    return $config
}

function New-Sandbox {
    <#
    .SYNOPSIS
        Creates a new sandbox environment for testing
    .PARAMETER MakeAppRoot
        Root path of MakeApp installation
    .PARAMETER ProjectType
        Type of sample project to create (powershell, node, python, generic)
    .PARAMETER Force
        Overwrite existing sandbox
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$MakeAppRoot,
        
        [Parameter()]
        [ValidateSet("powershell", "node", "python", "generic")]
        [string]$ProjectType = "powershell",
        
        [Parameter()]
        [switch]$Force
    )
    
    $config = Get-SandboxConfig -MakeAppRoot $MakeAppRoot
    
    if (-not $config) {
        return $null
    }
    
    $sandboxPath = $config.folders.repos
    $repoPath = Join-Path $sandboxPath $config.sandbox.repoName
    
    Write-Host "`n╔══════════════════════════════════════════════════════════╗" -ForegroundColor Yellow
    Write-Host "║              CREATING SANDBOX ENVIRONMENT                 ║" -ForegroundColor Yellow
    Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Yellow
    
    # Check if sandbox already exists
    if (Test-Path $sandboxPath) {
        if ($Force) {
            Write-Host "`nRemoving existing sandbox..." -ForegroundColor Yellow
            Remove-Sandbox -MakeAppRoot $MakeAppRoot -Confirm:$false
        }
        else {
            Write-Host "`nSandbox already exists at: $sandboxPath" -ForegroundColor Yellow
            $overwrite = Read-Host "Overwrite? (y/N)"
            if ($overwrite -ne 'y' -and $overwrite -ne 'Y') {
                Write-Host "Using existing sandbox" -ForegroundColor Cyan
                $script:SandboxActive = $true
                $script:SandboxPath = $sandboxPath
                return Get-SandboxInfo -MakeAppRoot $MakeAppRoot
            }
            Remove-Sandbox -MakeAppRoot $MakeAppRoot -Confirm:$false
        }
    }
    
    # Create directory structure
    Write-Host "`nCreating sandbox directories..." -ForegroundColor Cyan
    
    $directories = @(
        $sandboxPath,
        $config.folders.workspace,
        $config.folders.temp,
        $config.folders.logs,
        $config.folders.cache,
        $repoPath
    )
    
    foreach ($dir in $directories) {
        if (-not (Test-Path $dir)) {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
            Write-Host "  ✓ Created: $dir" -ForegroundColor Gray
        }
    }
    
    # Initialize git repository
    Write-Host "`nInitializing git repository..." -ForegroundColor Cyan
    Push-Location $repoPath
    try {
        git init 2>$null | Out-Null
        git config user.email "sandbox@makeapp.local"
        git config user.name "MakeApp Sandbox"
        
        Write-Host "  ✓ Git repository initialized" -ForegroundColor Gray
        
        # Create sample files based on project type
        if ($config.sandbox.initWithSampleFiles) {
            Write-Host "`nCreating sample project files ($ProjectType)..." -ForegroundColor Cyan
            New-SandboxSampleFiles -RepoPath $repoPath -ProjectType $ProjectType
        }
        
        # Create initial commit
        git add -A 2>$null
        git commit -m "Initial sandbox commit" 2>$null | Out-Null
        
        Write-Host "  ✓ Initial commit created" -ForegroundColor Gray
        
        # Create .github folder with copilot instructions
        $githubDir = Join-Path $repoPath ".github"
        New-Item -ItemType Directory -Path $githubDir -Force | Out-Null
        
        $instructionsContent = Get-CopilotInstructionsTemplate -ProjectType $ProjectType
        $instructionsContent | Set-Content (Join-Path $githubDir "copilot-instructions.md") -Encoding UTF8
        
        Write-Host "  ✓ Copilot instructions created" -ForegroundColor Gray
        
        # Commit the .github folder
        git add -A 2>$null
        git commit -m "Add Copilot instructions" 2>$null | Out-Null
    }
    finally {
        Pop-Location
    }
    
    $script:SandboxActive = $true
    $script:SandboxPath = $sandboxPath
    
    Write-Host "`n╔══════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║              SANDBOX CREATED SUCCESSFULLY                 ║" -ForegroundColor Green
    Write-Host "╠══════════════════════════════════════════════════════════╣" -ForegroundColor Green
    Write-Host "║ Path: $($sandboxPath.PadRight(49)) ║" -ForegroundColor White
    Write-Host "║ Repo: $($repoPath.PadRight(49)) ║" -ForegroundColor White
    Write-Host "║ Type: $($ProjectType.PadRight(49)) ║" -ForegroundColor White
    Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Green
    
    return Get-SandboxInfo -MakeAppRoot $MakeAppRoot
}

function New-SandboxSampleFiles {
    <#
    .SYNOPSIS
        Creates sample files for the sandbox project
    .PARAMETER RepoPath
        Path to the repository
    .PARAMETER ProjectType
        Type of project
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$RepoPath,
        
        [Parameter()]
        [string]$ProjectType = "powershell"
    )
    
    switch ($ProjectType) {
        "powershell" {
            # Create a sample PowerShell module
            $srcDir = Join-Path $RepoPath "src"
            New-Item -ItemType Directory -Path $srcDir -Force | Out-Null
            
            @"
#Requires -Version 7.0
<#
.SYNOPSIS
    Sample module for MakeApp sandbox testing
.DESCRIPTION
    This is a sample module created by MakeApp sandbox.
#>

function Get-SampleData {
    <#
    .SYNOPSIS
        Gets sample data
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]`$Name = "World"
    )
    
    return [PSCustomObject]@{
        Message = "Hello, `$Name!"
        Timestamp = Get-Date
    }
}

Export-ModuleMember -Function 'Get-SampleData'
"@ | Set-Content (Join-Path $srcDir "SampleModule.ps1") -Encoding UTF8
            
            # Create README
            @"
# Sandbox Sample Project

This is a sample PowerShell project for testing MakeApp workflows.

## Usage

``````powershell
. ./src/SampleModule.ps1
Get-SampleData -Name "MakeApp"
``````

## Features

- [ ] Add more functions
- [ ] Add error handling
- [ ] Add tests
"@ | Set-Content (Join-Path $RepoPath "README.md") -Encoding UTF8
            
            Write-Host "  ✓ Created PowerShell sample files" -ForegroundColor Gray
        }
        
        "node" {
            # Create package.json
            @{
                name = "sandbox-project"
                version = "1.0.0"
                description = "Sample Node.js project for MakeApp sandbox"
                main = "src/index.js"
                scripts = @{
                    start = "node src/index.js"
                    test = "echo 'No tests yet'"
                }
            } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $RepoPath "package.json") -Encoding UTF8
            
            # Create src/index.js
            $srcDir = Join-Path $RepoPath "src"
            New-Item -ItemType Directory -Path $srcDir -Force | Out-Null
            
            @"
/**
 * Sample Node.js application for MakeApp sandbox testing
 */

function greet(name = 'World') {
    return `Hello, `${name}!`;
}

function main() {
    console.log(greet('MakeApp'));
    console.log('Sandbox project running...');
}

main();

module.exports = { greet };
"@ | Set-Content (Join-Path $srcDir "index.js") -Encoding UTF8
            
            # Create README
            @"
# Sandbox Sample Project

This is a sample Node.js project for testing MakeApp workflows.

## Usage

``````bash
npm start
``````

## Features

- [ ] Add API endpoints
- [ ] Add database connection
- [ ] Add tests
"@ | Set-Content (Join-Path $RepoPath "README.md") -Encoding UTF8
            
            Write-Host "  ✓ Created Node.js sample files" -ForegroundColor Gray
        }
        
        "python" {
            # Create src folder
            $srcDir = Join-Path $RepoPath "src"
            New-Item -ItemType Directory -Path $srcDir -Force | Out-Null
            
            # Create main.py
            @"
"""
Sample Python application for MakeApp sandbox testing
"""

def greet(name: str = "World") -> str:
    """Return a greeting message."""
    return f"Hello, {name}!"

def main():
    print(greet("MakeApp"))
    print("Sandbox project running...")

if __name__ == "__main__":
    main()
"@ | Set-Content (Join-Path $srcDir "main.py") -Encoding UTF8
            
            # Create requirements.txt
            @"
# Add dependencies here
"@ | Set-Content (Join-Path $RepoPath "requirements.txt") -Encoding UTF8
            
            # Create README
            @"
# Sandbox Sample Project

This is a sample Python project for testing MakeApp workflows.

## Usage

``````bash
python src/main.py
``````

## Features

- [ ] Add CLI arguments
- [ ] Add configuration
- [ ] Add tests
"@ | Set-Content (Join-Path $RepoPath "README.md") -Encoding UTF8
            
            Write-Host "  ✓ Created Python sample files" -ForegroundColor Gray
        }
        
        default {
            # Generic project
            @"
# Sandbox Sample Project

This is a generic sample project for testing MakeApp workflows.

## Getting Started

Add your code and documentation here.

## Features

- [ ] Feature 1
- [ ] Feature 2
- [ ] Feature 3
"@ | Set-Content (Join-Path $RepoPath "README.md") -Encoding UTF8
            
            Write-Host "  ✓ Created generic sample files" -ForegroundColor Gray
        }
    }
    
    # Create .gitignore
    @"
# MakeApp sandbox ignore patterns
*.log
*.tmp
node_modules/
__pycache__/
.env
.venv/
"@ | Set-Content (Join-Path $RepoPath ".gitignore") -Encoding UTF8
}

function Remove-Sandbox {
    <#
    .SYNOPSIS
        Removes the sandbox environment
    .PARAMETER MakeAppRoot
        Root path of MakeApp installation
    .PARAMETER Confirm
        Require confirmation before deletion
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$MakeAppRoot,
        
        [Parameter()]
        [bool]$Confirm = $true
    )
    
    $config = Get-SandboxConfig -MakeAppRoot $MakeAppRoot
    
    if (-not $config) {
        return $false
    }
    
    $sandboxPath = $config.folders.repos
    
    if (-not (Test-Path $sandboxPath)) {
        Write-Host "Sandbox does not exist: $sandboxPath" -ForegroundColor Yellow
        return $true
    }
    
    if ($Confirm) {
        Write-Host "`n⚠ WARNING: This will permanently delete the sandbox!" -ForegroundColor Red
        Write-Host "Path: $sandboxPath" -ForegroundColor Yellow
        
        # Show what will be deleted
        $itemCount = (Get-ChildItem $sandboxPath -Recurse -File -ErrorAction SilentlyContinue).Count
        Write-Host "Files to delete: $itemCount" -ForegroundColor Yellow
        
        $confirmation = Read-Host "`nType 'DELETE' to confirm"
        if ($confirmation -ne 'DELETE') {
            Write-Host "Deletion cancelled" -ForegroundColor Cyan
            return $false
        }
    }
    
    Write-Host "`nRemoving sandbox..." -ForegroundColor Cyan
    
    try {
        # Remove read-only attributes (git objects often have this)
        Get-ChildItem $sandboxPath -Recurse -Force -ErrorAction SilentlyContinue | ForEach-Object {
            $_.Attributes = 'Normal'
        }
        
        Remove-Item $sandboxPath -Recurse -Force -ErrorAction Stop
        
        $script:SandboxActive = $false
        $script:SandboxPath = $null
        
        Write-Host "✓ Sandbox removed successfully" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Error "Failed to remove sandbox: $_"
        return $false
    }
}

function Test-Sandbox {
    <#
    .SYNOPSIS
        Checks if a sandbox exists and is valid
    .PARAMETER MakeAppRoot
        Root path of MakeApp installation
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$MakeAppRoot
    )
    
    $config = Get-SandboxConfig -MakeAppRoot $MakeAppRoot
    
    if (-not $config) {
        return $false
    }
    
    $sandboxPath = $config.folders.repos
    $repoPath = Join-Path $sandboxPath $config.sandbox.repoName
    
    $result = [PSCustomObject]@{
        Exists = Test-Path $sandboxPath
        HasRepo = Test-Path (Join-Path $repoPath ".git")
        Path = $sandboxPath
        RepoPath = $repoPath
        IsValid = $false
    }
    
    $result.IsValid = $result.Exists -and $result.HasRepo
    
    return $result
}

function Get-SandboxInfo {
    <#
    .SYNOPSIS
        Gets detailed information about the current sandbox
    .PARAMETER MakeAppRoot
        Root path of MakeApp installation
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$MakeAppRoot
    )
    
    $config = Get-SandboxConfig -MakeAppRoot $MakeAppRoot
    
    if (-not $config) {
        return $null
    }
    
    $sandboxPath = $config.folders.repos
    $repoPath = Join-Path $sandboxPath $config.sandbox.repoName
    
    $info = [PSCustomObject]@{
        SandboxPath = $sandboxPath
        RepoPath = $repoPath
        RepoName = $config.sandbox.repoName
        Exists = Test-Path $sandboxPath
        Config = $config
        Stats = $null
    }
    
    if ($info.Exists) {
        # Get stats
        $files = Get-ChildItem $sandboxPath -Recurse -File -ErrorAction SilentlyContinue
        $totalSize = ($files | Measure-Object -Property Length -Sum).Sum
        
        $info.Stats = [PSCustomObject]@{
            FileCount = $files.Count
            TotalSizeBytes = $totalSize
            TotalSizeMB = [math]::Round($totalSize / 1MB, 2)
            Created = (Get-Item $sandboxPath).CreationTime
        }
        
        # Get git info if repo exists
        if (Test-Path (Join-Path $repoPath ".git")) {
            Push-Location $repoPath
            try {
                $branch = git branch --show-current 2>$null
                $commitCount = git rev-list --count HEAD 2>$null
                $lastCommit = git log -1 --format="%s (%cr)" 2>$null
                
                $info.Stats | Add-Member -NotePropertyName "CurrentBranch" -NotePropertyValue $branch
                $info.Stats | Add-Member -NotePropertyName "CommitCount" -NotePropertyValue $commitCount
                $info.Stats | Add-Member -NotePropertyName "LastCommit" -NotePropertyValue $lastCommit
            }
            finally {
                Pop-Location
            }
        }
    }
    
    return $info
}

function Show-SandboxStatus {
    <#
    .SYNOPSIS
        Displays the current sandbox status
    .PARAMETER MakeAppRoot
        Root path of MakeApp installation
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$MakeAppRoot
    )
    
    $info = Get-SandboxInfo -MakeAppRoot $MakeAppRoot
    
    Write-Host "`n╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║                   SANDBOX STATUS                          ║" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    
    if ($info.Exists) {
        Write-Host "`n  Status: " -NoNewline -ForegroundColor White
        Write-Host "ACTIVE" -ForegroundColor Green
        Write-Host "  Path: $($info.SandboxPath)" -ForegroundColor White
        Write-Host "  Repo: $($info.RepoPath)" -ForegroundColor White
        
        if ($info.Stats) {
            Write-Host "`n  Statistics:" -ForegroundColor Yellow
            Write-Host "    Files: $($info.Stats.FileCount)" -ForegroundColor Gray
            Write-Host "    Size: $($info.Stats.TotalSizeMB) MB" -ForegroundColor Gray
            Write-Host "    Created: $($info.Stats.Created)" -ForegroundColor Gray
            
            if ($info.Stats.CurrentBranch) {
                Write-Host "`n  Git:" -ForegroundColor Yellow
                Write-Host "    Branch: $($info.Stats.CurrentBranch)" -ForegroundColor Gray
                Write-Host "    Commits: $($info.Stats.CommitCount)" -ForegroundColor Gray
                Write-Host "    Last: $($info.Stats.LastCommit)" -ForegroundColor Gray
            }
        }
    }
    else {
        Write-Host "`n  Status: " -NoNewline -ForegroundColor White
        Write-Host "NOT CREATED" -ForegroundColor Yellow
        Write-Host "  Expected path: $($info.SandboxPath)" -ForegroundColor Gray
    }
    
    Write-Host ""
}

function Reset-Sandbox {
    <#
    .SYNOPSIS
        Resets the sandbox to initial state
    .PARAMETER MakeAppRoot
        Root path of MakeApp installation
    .PARAMETER ProjectType
        Type of sample project
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$MakeAppRoot,
        
        [Parameter()]
        [string]$ProjectType = "powershell"
    )
    
    Write-Host "`nResetting sandbox..." -ForegroundColor Cyan
    
    # Remove and recreate
    Remove-Sandbox -MakeAppRoot $MakeAppRoot -Confirm:$false
    New-Sandbox -MakeAppRoot $MakeAppRoot -ProjectType $ProjectType
}

function Enter-Sandbox {
    <#
    .SYNOPSIS
        Enters the sandbox environment (changes to sandbox repo directory)
    .PARAMETER MakeAppRoot
        Root path of MakeApp installation
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$MakeAppRoot
    )
    
    $info = Get-SandboxInfo -MakeAppRoot $MakeAppRoot
    
    if (-not $info.Exists) {
        Write-Error "Sandbox does not exist. Run 'makeapp.ps1 sandbox create' first."
        return $false
    }
    
    Set-Location $info.RepoPath
    Write-Host "Entered sandbox: $($info.RepoPath)" -ForegroundColor Green
    
    return $true
}
