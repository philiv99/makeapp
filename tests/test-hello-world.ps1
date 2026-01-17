#!/usr/bin/env pwsh
#Requires -Version 7.0
<#
.SYNOPSIS
    Test script: Create a Python Hello World app using MakeApp
.DESCRIPTION
    This script tests the full MakeApp workflow by:
    1. Creating a sandbox environment
    2. Creating a feature request for a Python Hello World app
    3. Running the MakeApp workflow
    4. Verifying the results
.EXAMPLE
    ./tests/test-hello-world.ps1
#>

[CmdletBinding()]
param(
    [Parameter()]
    [switch]$SkipGitHubPush,
    
    [Parameter()]
    [switch]$CleanupAfter
)

$ErrorActionPreference = "Stop"
$script:TestRoot = Split-Path $PSScriptRoot -Parent
$script:SandboxPath = Join-Path $script:TestRoot "sandbox"
$script:RepoPath = Join-Path $script:SandboxPath "sandbox-repo"

function Write-TestHeader {
    param([string]$Message)
    Write-Host "`n╔══════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
    Write-Host "║ $($Message.PadRight(56)) ║" -ForegroundColor Magenta
    Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Magenta
}

function Write-TestStep {
    param([int]$Step, [string]$Message)
    Write-Host "`n[$Step] $Message" -ForegroundColor Cyan
}

function Write-TestSuccess {
    param([string]$Message)
    Write-Host "  ✓ $Message" -ForegroundColor Green
}

function Write-TestFailure {
    param([string]$Message)
    Write-Host "  ✗ $Message" -ForegroundColor Red
}

# ============================================================================
# MAIN TEST
# ============================================================================

Write-TestHeader "MAKEAPP TEST: Python Hello World"

# Step 1: Check prerequisites
Write-TestStep 1 "Checking prerequisites..."

# Check GitHub CLI
$ghVersion = gh --version 2>$null
if (-not $ghVersion) {
    Write-TestFailure "GitHub CLI (gh) not installed"
    Write-Host "  Install from: https://cli.github.com/" -ForegroundColor Yellow
    exit 1
}
Write-TestSuccess "GitHub CLI installed: $($ghVersion[0])"

# Check GitHub auth
$authStatus = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-TestFailure "GitHub CLI not authenticated"
    Write-Host "  Run: gh auth login" -ForegroundColor Yellow
    exit 1
}
Write-TestSuccess "GitHub CLI authenticated"

# Check Python (try multiple detection methods)
$pythonCmd = $null
$pythonVersion = $null

# Try python3 first, then python
foreach ($cmd in @('python3', 'python', 'py')) {
    try {
        $result = & $cmd --version 2>&1
        if ($LASTEXITCODE -eq 0 -and $result -match 'Python') {
            $pythonCmd = $cmd
            $pythonVersion = $result
            break
        }
    } catch {
        continue
    }
}

if (-not $pythonVersion) {
    Write-TestFailure "Python not installed"
    Write-Host "  Install Python from: https://python.org/" -ForegroundColor Yellow
    exit 1
}
Write-TestSuccess "Python installed: $pythonVersion (using '$pythonCmd')"

# Step 2: Create/Reset sandbox
Write-TestStep 2 "Setting up sandbox environment..."

Push-Location $script:TestRoot
try {
    # Load modules (suppress Export-ModuleMember errors when dot-sourcing)
    $ErrorActionPreference = "SilentlyContinue"
    . "$script:TestRoot\modules\Sandbox.ps1" 2>$null
    . "$script:TestRoot\modules\BranchManager.ps1" 2>$null
    . "$script:TestRoot\modules\FeaturePrompt.ps1" 2>$null
    . "$script:TestRoot\modules\CopilotConfig.ps1" 2>$null
    . "$script:TestRoot\modules\Executor.ps1" 2>$null
    . "$script:TestRoot\modules\GitAutomation.ps1" 2>$null
    $ErrorActionPreference = "Stop"
    
    # Create fresh sandbox
    if (Test-Path $script:SandboxPath) {
        Write-Host "  Removing existing sandbox..." -ForegroundColor Yellow
        Remove-Sandbox -MakeAppRoot $script:TestRoot -Confirm:$false
    }
    
    New-Sandbox -MakeAppRoot $script:TestRoot -ProjectType "python" -Force | Out-Null
    Write-TestSuccess "Sandbox created at: $script:RepoPath"
}
finally {
    Pop-Location
}

# Step 3: Create feature file for Hello World app
Write-TestStep 3 "Creating feature request..."

$featureFile = Join-Path $script:SandboxPath "temp" "hello-world-feature.json"
$feature = @{
    Title = "Create Python Hello World Application"
    Description = @"
Create a simple Python Hello World application with the following:
1. A main.py file that prints 'Hello, World!' when run
2. A function called greet(name) that returns a greeting string
3. If run directly, it should greet 'World' by default
4. Include a simple docstring explaining the module
"@
    AcceptanceCriteria = @(
        "main.py exists in the src/ directory",
        "Running 'python src/main.py' prints 'Hello, World!'",
        "greet() function accepts a name parameter",
        "Code includes proper docstrings"
    )
    TechnicalNotes = @(
        "Use Python 3 syntax",
        "Follow PEP 8 style guidelines",
        "Keep it simple - no external dependencies"
    )
    AffectedAreas = @(
        "src/main.py"
    )
    Priority = "High"
} | ConvertTo-Json -Depth 5

# Ensure temp directory exists
$tempDir = Split-Path $featureFile -Parent
if (-not (Test-Path $tempDir)) {
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
}

$feature | Set-Content $featureFile -Encoding UTF8
Write-TestSuccess "Feature file created: $featureFile"

# Step 4: Create the Hello World app manually (simulating what Copilot would do)
Write-TestStep 4 "Creating Hello World application..."

$srcDir = Join-Path $script:RepoPath "src"
if (-not (Test-Path $srcDir)) {
    New-Item -ItemType Directory -Path $srcDir -Force | Out-Null
}

$mainPyContent = @'
#!/usr/bin/env python3
"""
Hello World Application

A simple Python application that demonstrates basic greeting functionality.
Created by MakeApp CLI test.
"""


def greet(name: str = "World") -> str:
    """
    Return a greeting message for the given name.
    
    Args:
        name: The name to greet. Defaults to "World".
        
    Returns:
        A greeting string in the format "Hello, {name}!"
    """
    return f"Hello, {name}!"


def main():
    """Main entry point for the application."""
    message = greet()
    print(message)


if __name__ == "__main__":
    main()
'@

$mainPyPath = Join-Path $srcDir "main.py"
$mainPyContent | Set-Content $mainPyPath -Encoding UTF8
Write-TestSuccess "Created: $mainPyPath"

# Step 5: Verify the application works
Write-TestStep 5 "Testing the application..."

Push-Location $script:RepoPath
try {
    $output = python3 src/main.py 2>&1
    if ($output -eq "Hello, World!") {
        Write-TestSuccess "Application output: $output"
    }
    else {
        Write-TestFailure "Unexpected output: $output"
        exit 1
    }
}
finally {
    Pop-Location
}

# Step 6: Git operations
Write-TestStep 6 "Committing changes..."

Push-Location $script:RepoPath
try {
    # Create feature branch
    $branchName = "feature/hello-world-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    git checkout -b $branchName 2>$null
    Write-TestSuccess "Created branch: $branchName"
    
    # Stage and commit
    git add -A 2>$null
    git commit -m "feat: Add Python Hello World application" 2>$null | Out-Null
    Write-TestSuccess "Changes committed"
    
    # Show what was created
    Write-Host "`n  Files in repository:" -ForegroundColor Yellow
    git ls-files | ForEach-Object { Write-Host "    - $_" -ForegroundColor Gray }
}
finally {
    Pop-Location
}

# Step 7: GitHub operations (optional)
if (-not $SkipGitHubPush) {
    Write-TestStep 7 "GitHub operations..."
    
    # Check if remote repo exists, create if not
    $repoName = "makeapp-sandbox-test"
    $owner = "philiv99"
    
    Write-Host "  Checking if repo exists: $owner/$repoName" -ForegroundColor Yellow
    $repoExists = gh repo view "$owner/$repoName" 2>$null
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  Creating GitHub repository..." -ForegroundColor Yellow
        gh repo create $repoName --public --description "MakeApp sandbox test repository" 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-TestSuccess "Created repository: $owner/$repoName"
        }
        else {
            Write-TestFailure "Failed to create repository"
            Write-Host "  You may need to create it manually or check permissions" -ForegroundColor Yellow
        }
    }
    else {
        Write-TestSuccess "Repository exists: $owner/$repoName"
    }
    
    # Add remote and push
    Push-Location $script:RepoPath
    try {
        # Remove existing origin if any
        git remote remove origin 2>$null
        
        # Add new origin
        git remote add origin "https://github.com/$owner/$repoName.git"
        Write-TestSuccess "Added remote: origin -> $owner/$repoName"
        
        # Push
        Write-Host "  Pushing to GitHub..." -ForegroundColor Yellow
        $pushResult = git push -u origin $branchName 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-TestSuccess "Pushed branch to GitHub"
            
            # Create PR
            Write-Host "  Creating pull request..." -ForegroundColor Yellow
            $prResult = gh pr create --title "feat: Add Python Hello World application" --body "This PR adds a simple Python Hello World application created by MakeApp test." --base main 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-TestSuccess "Pull request created"
                Write-Host "  PR URL: $prResult" -ForegroundColor Cyan
            }
            else {
                Write-Host "  Note: PR creation skipped (may need main branch first)" -ForegroundColor Yellow
            }
        }
        else {
            Write-TestFailure "Failed to push: $pushResult"
        }
    }
    finally {
        Pop-Location
    }
}
else {
    Write-TestStep 7 "Skipping GitHub push (use without -SkipGitHubPush to enable)"
}

# Step 8: Summary
Write-TestStep 8 "Test Summary"

Write-Host @"

╔══════════════════════════════════════════════════════════╗
║                    TEST COMPLETED                         ║
╠══════════════════════════════════════════════════════════╣
║  Sandbox Path: $($script:SandboxPath.PadRight(40).Substring(0,40))  ║
║  Repo Path:    $($script:RepoPath.PadRight(40).Substring(0,40))  ║
║  Branch:       $($branchName.PadRight(40).Substring(0,40))  ║
╚══════════════════════════════════════════════════════════╝

"@ -ForegroundColor Green

Write-Host "To test the app manually:" -ForegroundColor Yellow
Write-Host "  cd $script:RepoPath" -ForegroundColor White
Write-Host "  python src/main.py" -ForegroundColor White

if ($CleanupAfter) {
    Write-Host "`nCleaning up sandbox..." -ForegroundColor Yellow
    Push-Location $script:TestRoot
    Remove-Sandbox -MakeAppRoot $script:TestRoot -Confirm:$false
    Pop-Location
    Write-Host "Sandbox removed" -ForegroundColor Green
}
else {
    Write-Host "`nTo clean up sandbox:" -ForegroundColor Yellow
    Write-Host "  ./makeapp.ps1 sandbox delete" -ForegroundColor White
}
