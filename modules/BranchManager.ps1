#Requires -Version 7.0
<#
.SYNOPSIS
    Branch management module for MakeApp CLI
.DESCRIPTION
    Provides functions for managing Git branches including listing, creating,
    switching, and validating branch states.
#>

# Module-level variables
$script:CurrentRepo = $null
$script:CurrentBranch = $null

function Get-AvailableRepos {
    <#
    .SYNOPSIS
        Lists available repositories in the configured repos folder
    .PARAMETER ReposPath
        Path to the repositories folder. Uses config default if not specified.
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$ReposPath
    )
    
    $config = Get-MakeAppConfig
    if (-not $ReposPath) {
        $ReposPath = $config.folders.repos
    }
    
    if (-not (Test-Path $ReposPath)) {
        Write-Error "Repos path does not exist: $ReposPath"
        return @()
    }
    
    $repos = Get-ChildItem -Path $ReposPath -Directory | Where-Object {
        Test-Path (Join-Path $_.FullName ".git")
    } | ForEach-Object {
        [PSCustomObject]@{
            Name = $_.Name
            Path = $_.FullName
            LastModified = $_.LastWriteTime
        }
    }
    
    return $repos
}

function Get-RepoBranches {
    <#
    .SYNOPSIS
        Lists all branches for a repository
    .PARAMETER RepoPath
        Path to the repository
    .PARAMETER IncludeRemote
        Include remote branches in the list
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$RepoPath,
        
        [Parameter()]
        [switch]$IncludeRemote
    )
    
    if (-not (Test-Path (Join-Path $RepoPath ".git"))) {
        Write-Error "Not a git repository: $RepoPath"
        return @()
    }
    
    Push-Location $RepoPath
    try {
        # Fetch latest from remote
        git fetch --prune 2>$null
        
        $branches = @()
        
        # Get local branches
        $localBranches = git branch --format="%(refname:short)|%(objectname:short)|%(committerdate:iso8601)" 2>$null
        foreach ($line in $localBranches) {
            if ($line) {
                $parts = $line -split '\|'
                $branches += [PSCustomObject]@{
                    Name = $parts[0].Trim()
                    CommitHash = $parts[1]
                    LastCommit = if ($parts[2]) { [datetime]$parts[2] } else { $null }
                    IsRemote = $false
                    IsCurrent = $parts[0].Trim() -eq (git branch --show-current)
                }
            }
        }
        
        # Get remote branches if requested
        if ($IncludeRemote) {
            $remoteBranches = git branch -r --format="%(refname:short)|%(objectname:short)|%(committerdate:iso8601)" 2>$null
            foreach ($line in $remoteBranches) {
                if ($line -and $line -notmatch "HEAD") {
                    $parts = $line -split '\|'
                    $branches += [PSCustomObject]@{
                        Name = $parts[0].Trim()
                        CommitHash = $parts[1]
                        LastCommit = if ($parts[2]) { [datetime]$parts[2] } else { $null }
                        IsRemote = $true
                        IsCurrent = $false
                    }
                }
            }
        }
        
        return $branches
    }
    finally {
        Pop-Location
    }
}

function New-FeatureBranch {
    <#
    .SYNOPSIS
        Creates a new feature branch from the base branch
    .PARAMETER RepoPath
        Path to the repository
    .PARAMETER BranchName
        Name for the new branch
    .PARAMETER BaseBranch
        Base branch to create from (default: main)
    .PARAMETER SwitchToBranch
        Switch to the new branch after creation
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$RepoPath,
        
        [Parameter(Mandatory)]
        [string]$BranchName,
        
        [Parameter()]
        [string]$BaseBranch = "main",
        
        [Parameter()]
        [switch]$SwitchToBranch
    )
    
    if (-not (Test-Path (Join-Path $RepoPath ".git"))) {
        Write-Error "Not a git repository: $RepoPath"
        return $false
    }
    
    Push-Location $RepoPath
    try {
        # Ensure we have latest from remote
        Write-Host "Fetching latest from remote..." -ForegroundColor Cyan
        git fetch origin 2>$null
        
        # Check if branch already exists
        $existingBranch = git branch --list $BranchName 2>$null
        if ($existingBranch) {
            Write-Warning "Branch '$BranchName' already exists locally"
            if ($SwitchToBranch) {
                return Switch-ToBranch -RepoPath $RepoPath -BranchName $BranchName
            }
            return $false
        }
        
        # Create branch from base
        Write-Host "Creating branch '$BranchName' from '$BaseBranch'..." -ForegroundColor Cyan
        $result = git checkout -b $BranchName "origin/$BaseBranch" 2>&1
        
        if ($LASTEXITCODE -ne 0) {
            # Try without origin prefix
            $result = git checkout -b $BranchName $BaseBranch 2>&1
            if ($LASTEXITCODE -ne 0) {
                Write-Error "Failed to create branch: $result"
                return $false
            }
        }
        
        Write-Host "Branch '$BranchName' created successfully" -ForegroundColor Green
        $script:CurrentBranch = $BranchName
        
        if (-not $SwitchToBranch) {
            # Switch back to previous branch
            git checkout - 2>$null
        }
        
        return $true
    }
    finally {
        Pop-Location
    }
}

function Switch-ToBranch {
    <#
    .SYNOPSIS
        Switches to an existing branch
    .PARAMETER RepoPath
        Path to the repository
    .PARAMETER BranchName
        Name of the branch to switch to
    .PARAMETER Force
        Force switch even with uncommitted changes
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$RepoPath,
        
        [Parameter(Mandatory)]
        [string]$BranchName,
        
        [Parameter()]
        [switch]$Force
    )
    
    if (-not (Test-Path (Join-Path $RepoPath ".git"))) {
        Write-Error "Not a git repository: $RepoPath"
        return $false
    }
    
    Push-Location $RepoPath
    try {
        # Check for uncommitted changes
        if (-not $Force) {
            $status = git status --porcelain 2>$null
            if ($status) {
                Write-Warning "You have uncommitted changes. Use -Force to override or commit/stash changes first."
                return $false
            }
        }
        
        $forceFlag = if ($Force) { "-f" } else { "" }
        
        Write-Host "Switching to branch '$BranchName'..." -ForegroundColor Cyan
        $result = git checkout $forceFlag $BranchName 2>&1
        
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to switch branch: $result"
            return $false
        }
        
        Write-Host "Switched to branch '$BranchName'" -ForegroundColor Green
        $script:CurrentBranch = $BranchName
        return $true
    }
    finally {
        Pop-Location
    }
}

function Test-GitState {
    <#
    .SYNOPSIS
        Validates the current git state
    .PARAMETER RepoPath
        Path to the repository
    .PARAMETER RequireClean
        Require no uncommitted changes
    .PARAMETER RequireBranch
        Require being on a specific branch
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$RepoPath,
        
        [Parameter()]
        [switch]$RequireClean,
        
        [Parameter()]
        [string]$RequireBranch
    )
    
    if (-not (Test-Path (Join-Path $RepoPath ".git"))) {
        return [PSCustomObject]@{
            IsValid = $false
            IsGitRepo = $false
            CurrentBranch = $null
            HasUncommittedChanges = $null
            HasUntrackedFiles = $null
            Errors = @("Not a git repository")
        }
    }
    
    Push-Location $RepoPath
    try {
        $errors = @()
        
        # Get current branch
        $currentBranch = git branch --show-current 2>$null
        
        # Check for uncommitted changes
        $status = git status --porcelain 2>$null
        $hasChanges = ($status | Where-Object { $_ -match "^[MADRC]" }).Count -gt 0
        $hasUntracked = ($status | Where-Object { $_ -match "^\?\?" }).Count -gt 0
        
        if ($RequireClean -and ($hasChanges -or $hasUntracked)) {
            $errors += "Repository has uncommitted changes"
        }
        
        if ($RequireBranch -and $currentBranch -ne $RequireBranch) {
            $errors += "Expected branch '$RequireBranch' but on '$currentBranch'"
        }
        
        return [PSCustomObject]@{
            IsValid = $errors.Count -eq 0
            IsGitRepo = $true
            CurrentBranch = $currentBranch
            HasUncommittedChanges = $hasChanges
            HasUntrackedFiles = $hasUntracked
            Errors = $errors
        }
    }
    finally {
        Pop-Location
    }
}

function Get-CurrentBranch {
    <#
    .SYNOPSIS
        Gets the current branch name
    .PARAMETER RepoPath
        Path to the repository
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$RepoPath
    )
    
    Push-Location $RepoPath
    try {
        return git branch --show-current 2>$null
    }
    finally {
        Pop-Location
    }
}

function Select-RepoBranch {
    <#
    .SYNOPSIS
        Interactive selection of repository and branch
    .PARAMETER ReposPath
        Path to repositories folder
    .PARAMETER AllowCreate
        Allow creating new branches
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$ReposPath,
        
        [Parameter()]
        [switch]$AllowCreate
    )
    
    # Get available repos
    $repos = Get-AvailableRepos -ReposPath $ReposPath
    
    if ($repos.Count -eq 0) {
        Write-Error "No git repositories found"
        return $null
    }
    
    # Display repos
    Write-Host "`nAvailable Repositories:" -ForegroundColor Cyan
    for ($i = 0; $i -lt $repos.Count; $i++) {
        Write-Host "  [$($i + 1)] $($repos[$i].Name)" -ForegroundColor White
    }
    
    # Select repo
    $repoSelection = Read-Host "`nSelect repository (1-$($repos.Count))"
    $repoIndex = [int]$repoSelection - 1
    
    if ($repoIndex -lt 0 -or $repoIndex -ge $repos.Count) {
        Write-Error "Invalid selection"
        return $null
    }
    
    $selectedRepo = $repos[$repoIndex]
    $script:CurrentRepo = $selectedRepo.Path
    
    # Get branches
    $branches = Get-RepoBranches -RepoPath $selectedRepo.Path
    
    Write-Host "`nAvailable Branches:" -ForegroundColor Cyan
    for ($i = 0; $i -lt $branches.Count; $i++) {
        $current = if ($branches[$i].IsCurrent) { " (current)" } else { "" }
        Write-Host "  [$($i + 1)] $($branches[$i].Name)$current" -ForegroundColor White
    }
    
    if ($AllowCreate) {
        Write-Host "  [N] Create new branch" -ForegroundColor Yellow
    }
    
    # Select branch
    $branchSelection = Read-Host "`nSelect branch (1-$($branches.Count)$(if($AllowCreate){' or N'}))"
    
    if ($AllowCreate -and $branchSelection -eq 'N') {
        $newBranchName = Read-Host "Enter new branch name"
        $baseBranch = Read-Host "Base branch (default: main)"
        if (-not $baseBranch) { $baseBranch = "main" }
        
        $created = New-FeatureBranch -RepoPath $selectedRepo.Path -BranchName $newBranchName -BaseBranch $baseBranch -SwitchToBranch
        if ($created) {
            return [PSCustomObject]@{
                RepoPath = $selectedRepo.Path
                RepoName = $selectedRepo.Name
                BranchName = $newBranchName
                IsNew = $true
            }
        }
        return $null
    }
    
    $branchIndex = [int]$branchSelection - 1
    
    if ($branchIndex -lt 0 -or $branchIndex -ge $branches.Count) {
        Write-Error "Invalid selection"
        return $null
    }
    
    $selectedBranch = $branches[$branchIndex]
    
    # Switch to branch if not current
    if (-not $selectedBranch.IsCurrent) {
        $switched = Switch-ToBranch -RepoPath $selectedRepo.Path -BranchName $selectedBranch.Name
        if (-not $switched) {
            return $null
        }
    }
    
    return [PSCustomObject]@{
        RepoPath = $selectedRepo.Path
        RepoName = $selectedRepo.Name
        BranchName = $selectedBranch.Name
        IsNew = $false
    }
}
