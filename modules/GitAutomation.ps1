#Requires -Version 7.0
<#
.SYNOPSIS
    Git automation module for MakeApp CLI
.DESCRIPTION
    Provides functions for Git operations including staging, committing,
    pushing, and creating pull requests.
#>

function Add-AllChanges {
    <#
    .SYNOPSIS
        Stages all changes in the repository
    .PARAMETER RepoPath
        Path to the repository
    .PARAMETER PathSpec
        Optional path specification to limit staging
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$RepoPath,
        
        [Parameter()]
        [string]$PathSpec = "."
    )
    
    Push-Location $RepoPath
    try {
        Write-Host "Staging changes..." -ForegroundColor Cyan
        
        $result = git add $PathSpec 2>&1
        
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to stage changes: $result"
            return $false
        }
        
        # Show what was staged
        $staged = git diff --cached --stat 2>$null
        if ($staged) {
            Write-Host "Staged changes:" -ForegroundColor Green
            Write-Host $staged -ForegroundColor Gray
        }
        else {
            Write-Host "No changes to stage" -ForegroundColor Yellow
        }
        
        return $true
    }
    finally {
        Pop-Location
    }
}

function New-FeatureCommit {
    <#
    .SYNOPSIS
        Creates a commit with the feature description
    .PARAMETER RepoPath
        Path to the repository
    .PARAMETER Message
        Commit message
    .PARAMETER Feature
        Optional feature object to generate message from
    .PARAMETER UseConventionalCommit
        Use conventional commit format
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$RepoPath,
        
        [Parameter()]
        [string]$Message,
        
        [Parameter()]
        [PSCustomObject]$Feature,
        
        [Parameter()]
        [switch]$UseConventionalCommit
    )
    
    $config = Get-MakeAppConfig
    
    # Generate message from feature if not provided
    if (-not $Message -and $Feature) {
        if ($UseConventionalCommit -or $config.git.commitMessagePrefix) {
            $prefix = $config.git.commitMessagePrefix
            $Message = "$prefix$($Feature.Title)"
        }
        else {
            $Message = $Feature.Title
        }
        
        # Add body with description
        if ($Feature.Description) {
            $body = "`n`n$($Feature.Description)"
            if ($body.Length -gt 500) {
                $body = $body.Substring(0, 497) + "..."
            }
            $Message += $body
        }
    }
    
    if (-not $Message) {
        Write-Error "No commit message provided"
        return $false
    }
    
    Push-Location $RepoPath
    try {
        Write-Host "Creating commit..." -ForegroundColor Cyan
        
        # Check if there are staged changes
        $staged = git diff --cached --name-only 2>$null
        if (-not $staged) {
            Write-Warning "No staged changes to commit"
            return $false
        }
        
        # Build commit command
        $commitArgs = @("commit", "-m", $Message)
        
        if ($config.git.signCommits) {
            $commitArgs += "-S"
        }
        
        $result = & git @commitArgs 2>&1
        
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to create commit: $result"
            return $false
        }
        
        Write-Host "✓ Commit created" -ForegroundColor Green
        
        # Show commit info
        $commitInfo = git log -1 --oneline 2>$null
        Write-Host "  $commitInfo" -ForegroundColor Gray
        
        return $true
    }
    finally {
        Pop-Location
    }
}

function Push-FeatureBranch {
    <#
    .SYNOPSIS
        Pushes the current branch to remote
    .PARAMETER RepoPath
        Path to the repository
    .PARAMETER SetUpstream
        Set upstream for new branches
    .PARAMETER Force
        Force push (use with caution)
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$RepoPath,
        
        [Parameter()]
        [switch]$SetUpstream,
        
        [Parameter()]
        [switch]$Force
    )
    
    Push-Location $RepoPath
    try {
        $branch = git branch --show-current 2>$null
        
        if (-not $branch) {
            Write-Error "Could not determine current branch"
            return $false
        }
        
        Write-Host "Pushing branch '$branch' to remote..." -ForegroundColor Cyan
        
        $pushArgs = @("push")
        
        if ($SetUpstream) {
            $pushArgs += "-u"
            $pushArgs += "origin"
            $pushArgs += $branch
        }
        
        if ($Force) {
            $pushArgs += "--force"
        }
        
        $result = & git @pushArgs 2>&1
        
        if ($LASTEXITCODE -ne 0) {
            # Check if it's a new branch that needs upstream
            if ($result -match "no upstream branch") {
                Write-Host "Setting upstream for new branch..." -ForegroundColor Yellow
                $result = git push -u origin $branch 2>&1
                
                if ($LASTEXITCODE -ne 0) {
                    Write-Error "Failed to push: $result"
                    return $false
                }
            }
            else {
                Write-Error "Failed to push: $result"
                return $false
            }
        }
        
        Write-Host "✓ Branch pushed successfully" -ForegroundColor Green
        return $true
    }
    finally {
        Pop-Location
    }
}

function New-PullRequest {
    <#
    .SYNOPSIS
        Creates a pull request to the base branch
    .PARAMETER RepoPath
        Path to the repository
    .PARAMETER Title
        PR title
    .PARAMETER Body
        PR body/description
    .PARAMETER BaseBranch
        Target branch for the PR (default: main)
    .PARAMETER Feature
        Optional feature object to generate PR from
    .PARAMETER Draft
        Create as draft PR
    .PARAMETER Labels
        Labels to add to the PR
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$RepoPath,
        
        [Parameter()]
        [string]$Title,
        
        [Parameter()]
        [string]$Body,
        
        [Parameter()]
        [string]$BaseBranch = "main",
        
        [Parameter()]
        [PSCustomObject]$Feature,
        
        [Parameter()]
        [switch]$Draft,
        
        [Parameter()]
        [string[]]$Labels
    )
    
    $config = Get-MakeAppConfig
    
    # Generate from feature if not provided
    if (-not $Title -and $Feature) {
        $Title = $Feature.Title
    }
    
    if (-not $Body -and $Feature) {
        $Body = Format-PullRequestBody -Feature $Feature
    }
    
    if (-not $Title) {
        Write-Error "No PR title provided"
        return $null
    }
    
    Push-Location $RepoPath
    try {
        $branch = git branch --show-current 2>$null
        
        Write-Host "Creating pull request..." -ForegroundColor Cyan
        Write-Host "  From: $branch" -ForegroundColor Gray
        Write-Host "  To:   $BaseBranch" -ForegroundColor Gray
        Write-Host "  Title: $Title" -ForegroundColor Gray
        
        # Build gh pr create command
        $prArgs = @(
            "pr", "create",
            "--title", $Title,
            "--base", $BaseBranch
        )
        
        if ($Body) {
            $prArgs += "--body"
            $prArgs += $Body
        }
        
        if ($Draft) {
            $prArgs += "--draft"
        }
        
        if ($Labels) {
            foreach ($label in $Labels) {
                $prArgs += "--label"
                $prArgs += $label
            }
        }
        
        $result = & gh @prArgs 2>&1
        
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to create PR: $result"
            return $null
        }
        
        # Parse PR URL from result
        $prUrl = $result | Where-Object { $_ -match "https://github.com" } | Select-Object -First 1
        
        Write-Host "✓ Pull request created" -ForegroundColor Green
        if ($prUrl) {
            Write-Host "  URL: $prUrl" -ForegroundColor Cyan
        }
        
        return [PSCustomObject]@{
            Success = $true
            Url = $prUrl
            Title = $Title
            BaseBranch = $BaseBranch
            HeadBranch = $branch
        }
    }
    finally {
        Pop-Location
    }
}

function Format-PullRequestBody {
    <#
    .SYNOPSIS
        Formats a PR body from a feature object
    .PARAMETER Feature
        The feature object
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [PSCustomObject]$Feature
    )
    
    $body = @"
## Description

$($Feature.Description)

## Changes

<!-- Describe the changes made -->

## Acceptance Criteria

$($Feature.AcceptanceCriteria | ForEach-Object { "- [ ] $_" } | Out-String)

## Technical Notes

$($Feature.TechnicalNotes | ForEach-Object { "- $_" } | Out-String)

---
*Generated by MakeApp CLI*
"@
    
    return $body
}

function Send-UserNotification {
    <#
    .SYNOPSIS
        Sends a notification to the user
    .PARAMETER Title
        Notification title
    .PARAMETER Message
        Notification message
    .PARAMETER Type
        Notification type (Success, Warning, Error, Info)
    .PARAMETER Url
        Optional URL to include
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Title,
        
        [Parameter(Mandatory)]
        [string]$Message,
        
        [Parameter()]
        [ValidateSet("Success", "Warning", "Error", "Info")]
        [string]$Type = "Info",
        
        [Parameter()]
        [string]$Url
    )
    
    $config = Get-MakeAppConfig
    
    # Terminal notification (always)
    $color = switch ($Type) {
        "Success" { "Green" }
        "Warning" { "Yellow" }
        "Error" { "Red" }
        default { "Cyan" }
    }
    
    $symbol = switch ($Type) {
        "Success" { "✓" }
        "Warning" { "⚠" }
        "Error" { "✗" }
        default { "ℹ" }
    }
    
    Write-Host "`n$symbol $Title" -ForegroundColor $color
    Write-Host "  $Message" -ForegroundColor White
    if ($Url) {
        Write-Host "  URL: $Url" -ForegroundColor Cyan
    }
    
    # Toast notification (if enabled and BurntToast is available)
    if ($config.notifications.toastEnabled) {
        try {
            if (Get-Module -ListAvailable -Name BurntToast -ErrorAction SilentlyContinue) {
                Import-Module BurntToast
                
                $toastParams = @{
                    Text = $Title, $Message
                    AppLogo = $null
                }
                
                if ($config.notifications.soundEnabled) {
                    $toastParams.Sound = "Default"
                }
                
                New-BurntToastNotification @toastParams
            }
        }
        catch {
            # Silently ignore toast errors
        }
    }
    
    # Webhook notification
    if ($config.notifications.method -eq "webhook" -and $config.notifications.webhookUrl) {
        try {
            $payload = @{
                title = $Title
                message = $Message
                type = $Type
                url = $Url
                timestamp = Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ"
            } | ConvertTo-Json
            
            Invoke-RestMethod -Uri $config.notifications.webhookUrl -Method Post -Body $payload -ContentType "application/json" -TimeoutSec 5
        }
        catch {
            Write-Warning "Failed to send webhook notification: $_"
        }
    }
    
    # Teams notification
    if ($config.notifications.teamsWebhook) {
        try {
            $teamsPayload = @{
                "@type" = "MessageCard"
                "@context" = "http://schema.org/extensions"
                "themeColor" = switch ($Type) {
                    "Success" { "00FF00" }
                    "Warning" { "FFFF00" }
                    "Error" { "FF0000" }
                    default { "0076D7" }
                }
                "summary" = $Title
                "sections" = @(
                    @{
                        "activityTitle" = $Title
                        "activitySubtitle" = $Message
                        "facts" = @()
                        "markdown" = $true
                    }
                )
            }
            
            if ($Url) {
                $teamsPayload["potentialAction"] = @(
                    @{
                        "@type" = "OpenUri"
                        "name" = "View"
                        "targets" = @(
                            @{ "os" = "default"; "uri" = $Url }
                        )
                    }
                )
            }
            
            Invoke-RestMethod -Uri $config.notifications.teamsWebhook -Method Post -Body ($teamsPayload | ConvertTo-Json -Depth 10) -ContentType "application/json" -TimeoutSec 5
        }
        catch {
            Write-Warning "Failed to send Teams notification: $_"
        }
    }
}

function Complete-FeatureWorkflow {
    <#
    .SYNOPSIS
        Completes the full git workflow for a feature
    .PARAMETER RepoPath
        Path to the repository
    .PARAMETER Feature
        The feature object
    .PARAMETER CommitMessage
        Optional custom commit message
    .PARAMETER CreatePR
        Create a pull request
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$RepoPath,
        
        [Parameter(Mandatory)]
        [PSCustomObject]$Feature,
        
        [Parameter()]
        [string]$CommitMessage,
        
        [Parameter()]
        [switch]$CreatePR
    )
    
    $config = Get-MakeAppConfig
    
    Write-Host "`n=== Completing Feature Workflow ===" -ForegroundColor Cyan
    
    # Step 1: Stage changes
    if ($config.git.autoStage) {
        $stageResult = Add-AllChanges -RepoPath $RepoPath
        if (-not $stageResult) {
            Send-UserNotification -Title "Workflow Failed" -Message "Failed to stage changes" -Type "Error"
            return $false
        }
    }
    
    # Step 2: Commit
    if ($config.git.autoCommit -or $CommitMessage) {
        $commitResult = New-FeatureCommit -RepoPath $RepoPath -Feature $Feature -Message $CommitMessage -UseConventionalCommit
        if (-not $commitResult) {
            Send-UserNotification -Title "Workflow Failed" -Message "Failed to create commit" -Type "Error"
            return $false
        }
    }
    else {
        $confirm = Read-Host "Create commit? (Y/n)"
        if ($confirm -ne 'n' -and $confirm -ne 'N') {
            $commitResult = New-FeatureCommit -RepoPath $RepoPath -Feature $Feature -UseConventionalCommit
            if (-not $commitResult) {
                Send-UserNotification -Title "Workflow Failed" -Message "Failed to create commit" -Type "Error"
                return $false
            }
        }
    }
    
    # Step 3: Push
    if ($config.git.autoPush) {
        $pushResult = Push-FeatureBranch -RepoPath $RepoPath -SetUpstream
        if (-not $pushResult) {
            Send-UserNotification -Title "Workflow Failed" -Message "Failed to push changes" -Type "Error"
            return $false
        }
    }
    else {
        $confirm = Read-Host "Push to remote? (Y/n)"
        if ($confirm -ne 'n' -and $confirm -ne 'N') {
            $pushResult = Push-FeatureBranch -RepoPath $RepoPath -SetUpstream
            if (-not $pushResult) {
                Send-UserNotification -Title "Workflow Failed" -Message "Failed to push changes" -Type "Error"
                return $false
            }
        }
    }
    
    # Step 4: Create PR
    $prUrl = $null
    if ($CreatePR -or $config.git.autoCreatePr) {
        $prResult = New-PullRequest -RepoPath $RepoPath -Feature $Feature
        if ($prResult) {
            $prUrl = $prResult.Url
        }
    }
    else {
        $confirm = Read-Host "Create pull request? (Y/n)"
        if ($confirm -ne 'n' -and $confirm -ne 'N') {
            $prResult = New-PullRequest -RepoPath $RepoPath -Feature $Feature
            if ($prResult) {
                $prUrl = $prResult.Url
            }
        }
    }
    
    # Send success notification
    Send-UserNotification -Title "Feature Complete" -Message "Feature '$($Feature.Title)' workflow completed successfully" -Type "Success" -Url $prUrl
    
    return $true
}
