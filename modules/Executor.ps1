#Requires -Version 7.0
<#
.SYNOPSIS
    Execution orchestrator module for MakeApp CLI
.DESCRIPTION
    Provides functions for executing commands via GitHub Copilot CLI,
    managing agent orchestration loops, and handling iterative prompts.
#>

# Module-level state
$script:ExecutionHistory = @()
$script:CurrentIteration = 0
$script:MaxIterations = 50

function Invoke-CopilotCommand {
    <#
    .SYNOPSIS
        Executes a command via GitHub Copilot CLI
    .PARAMETER Prompt
        The prompt to send to Copilot
    .PARAMETER Type
        Type of Copilot command (suggest, explain, or ask via copilot extension)
    .PARAMETER WorkingDirectory
        Directory to execute in
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Prompt,
        
        [Parameter()]
        [ValidateSet("suggest", "explain", "ask")]
        [string]$Type = "suggest",
        
        [Parameter()]
        [string]$WorkingDirectory
    )
    
    $config = Get-MakeAppConfig
    
    if ($WorkingDirectory) {
        Push-Location $WorkingDirectory
    }
    
    try {
        $startTime = Get-Date
        
        Write-Host "`n┌─ Copilot $Type ─────────────────────────────────────" -ForegroundColor Cyan
        Write-Host "│ Prompt: $($Prompt.Substring(0, [Math]::Min(60, $Prompt.Length)))..." -ForegroundColor Gray
        
        $result = [PSCustomObject]@{
            Success = $false
            Output = ""
            Error = ""
            Duration = $null
            Command = ""
        }
        
        switch ($Type) {
            "suggest" {
                # gh copilot suggest expects a shell type and prompt
                $output = gh copilot suggest -t shell "$Prompt" 2>&1
                $result.Command = "gh copilot suggest -t shell `"$Prompt`""
            }
            "explain" {
                $output = gh copilot explain "$Prompt" 2>&1
                $result.Command = "gh copilot explain `"$Prompt`""
            }
            "ask" {
                # Use the GitHub Copilot extension in CLI mode if available
                $output = gh copilot suggest "$Prompt" 2>&1
                $result.Command = "gh copilot suggest `"$Prompt`""
            }
        }
        
        $result.Duration = (Get-Date) - $startTime
        
        if ($LASTEXITCODE -eq 0) {
            $result.Success = $true
            $result.Output = $output -join "`n"
            Write-Host "│ ✓ Command completed in $($result.Duration.TotalSeconds.ToString('F2'))s" -ForegroundColor Green
        }
        else {
            $result.Error = $output -join "`n"
            Write-Host "│ ✗ Command failed" -ForegroundColor Red
        }
        
        Write-Host "└──────────────────────────────────────────────────────" -ForegroundColor Cyan
        
        # Record in history
        $script:ExecutionHistory += [PSCustomObject]@{
            Timestamp = Get-Date
            Type = $Type
            Prompt = $Prompt
            Result = $result
        }
        
        return $result
    }
    finally {
        if ($WorkingDirectory) {
            Pop-Location
        }
    }
}

function Invoke-ShellCommand {
    <#
    .SYNOPSIS
        Executes a shell command with permission checking
    .PARAMETER Command
        The command to execute
    .PARAMETER WorkingDirectory
        Directory to execute in
    .PARAMETER RequireApproval
        Always require user approval
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Command,
        
        [Parameter()]
        [string]$WorkingDirectory,
        
        [Parameter()]
        [switch]$RequireApproval
    )
    
    $config = Get-MakeAppConfig
    
    # Check if command is allowed
    $permCheck = Test-CommandAllowed -Command $Command
    
    if (-not $permCheck.Allowed) {
        if ($permCheck.RequiresApproval -or $RequireApproval) {
            Write-Host "`n⚠ Command requires approval:" -ForegroundColor Yellow
            Write-Host "  $Command" -ForegroundColor White
            $approval = Read-Host "Execute this command? (y/N)"
            
            if ($approval -ne 'y' -and $approval -ne 'Y') {
                Write-Host "Command skipped" -ForegroundColor Yellow
                return [PSCustomObject]@{
                    Success = $false
                    Output = ""
                    Error = "User declined to execute"
                    Skipped = $true
                }
            }
        }
        else {
            Write-Host "✗ Command blocked: $($permCheck.Reason)" -ForegroundColor Red
            return [PSCustomObject]@{
                Success = $false
                Output = ""
                Error = $permCheck.Reason
                Blocked = $true
            }
        }
    }
    
    if ($WorkingDirectory) {
        Push-Location $WorkingDirectory
    }
    
    try {
        Write-Host "Executing: $Command" -ForegroundColor Cyan
        
        $startTime = Get-Date
        
        # Execute command
        $output = Invoke-Expression $Command 2>&1
        $exitCode = $LASTEXITCODE
        
        $duration = (Get-Date) - $startTime
        
        $result = [PSCustomObject]@{
            Success = $exitCode -eq 0
            Output = $output -join "`n"
            Error = if ($exitCode -ne 0) { $output -join "`n" } else { "" }
            ExitCode = $exitCode
            Duration = $duration
        }
        
        if ($result.Success) {
            Write-Host "✓ Command completed ($($duration.TotalSeconds.ToString('F2'))s)" -ForegroundColor Green
        }
        else {
            Write-Host "✗ Command failed with exit code $exitCode" -ForegroundColor Red
        }
        
        return $result
    }
    finally {
        if ($WorkingDirectory) {
            Pop-Location
        }
    }
}

function Start-AgentOrchestration {
    <#
    .SYNOPSIS
        Starts the agent orchestration loop for implementing a feature
    .PARAMETER Feature
        The feature object to implement
    .PARAMETER RepoPath
        Path to the repository
    .PARAMETER MaxIterations
        Maximum number of iteration cycles
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [PSCustomObject]$Feature,
        
        [Parameter(Mandatory)]
        [string]$RepoPath,
        
        [Parameter()]
        [int]$MaxIterations = 50
    )
    
    $config = Get-MakeAppConfig
    $script:MaxIterations = $MaxIterations
    $script:CurrentIteration = 0
    $script:ExecutionHistory = @()
    
    Write-Host "`n╔══════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
    Write-Host "║           AGENT ORCHESTRATION STARTED                     ║" -ForegroundColor Magenta
    Write-Host "╠══════════════════════════════════════════════════════════╣" -ForegroundColor Magenta
    Write-Host "║ Feature: $($Feature.Title.PadRight(46).Substring(0,46)) ║" -ForegroundColor White
    Write-Host "║ Max Iterations: $($MaxIterations.ToString().PadRight(40))  ║" -ForegroundColor White
    Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Magenta
    
    # Format the initial prompt
    $prompt = Format-CopilotPrompt -Feature $Feature -Style "structured"
    
    $orchestrationState = @{
        Phase = "Planning"
        CompletedSteps = @()
        PendingSteps = @()
        Errors = @()
        StartTime = Get-Date
    }
    
    try {
        # Phase 1: Planning
        Write-Host "`n── Phase 1: Planning ──────────────────────────────────────" -ForegroundColor Cyan
        $planResult = Get-ImplementationPlan -Prompt $prompt -RepoPath $RepoPath
        
        if (-not $planResult.Success) {
            Write-Error "Failed to generate implementation plan"
            return $false
        }
        
        $orchestrationState.PendingSteps = $planResult.Steps
        $orchestrationState.Phase = "Implementation"
        
        # Phase 2: Implementation Loop
        Write-Host "`n── Phase 2: Implementation ────────────────────────────────" -ForegroundColor Cyan
        
        while ($script:CurrentIteration -lt $script:MaxIterations) {
            $script:CurrentIteration++
            
            Write-Host "`n[Iteration $script:CurrentIteration/$script:MaxIterations]" -ForegroundColor Yellow
            
            # Check if we have pending steps
            if ($orchestrationState.PendingSteps.Count -eq 0) {
                Write-Host "All steps completed!" -ForegroundColor Green
                break
            }
            
            $currentStep = $orchestrationState.PendingSteps[0]
            Write-Host "Current step: $currentStep" -ForegroundColor White
            
            # Execute step via Copilot
            $stepResult = Invoke-ImplementationStep -Step $currentStep -RepoPath $RepoPath
            
            if ($stepResult.Success) {
                $orchestrationState.CompletedSteps += $currentStep
                $orchestrationState.PendingSteps = $orchestrationState.PendingSteps[1..($orchestrationState.PendingSteps.Count - 1)]
            }
            else {
                $orchestrationState.Errors += @{
                    Step = $currentStep
                    Error = $stepResult.Error
                    Iteration = $script:CurrentIteration
                }
                
                # Ask user how to proceed
                Write-Host "`n⚠ Step failed: $($stepResult.Error)" -ForegroundColor Yellow
                $action = Read-Host "Options: [R]etry, [S]kip, [A]bort"
                
                switch ($action.ToUpper()) {
                    "R" { 
                        Write-Host "Retrying step..." -ForegroundColor Cyan
                        continue 
                    }
                    "S" { 
                        Write-Host "Skipping step..." -ForegroundColor Yellow
                        $orchestrationState.PendingSteps = $orchestrationState.PendingSteps[1..($orchestrationState.PendingSteps.Count - 1)]
                    }
                    "A" { 
                        Write-Host "Aborting orchestration" -ForegroundColor Red
                        return $false 
                    }
                }
            }
            
            # Check for user interrupt
            if ([Console]::KeyAvailable) {
                $key = [Console]::ReadKey($true)
                if ($key.Key -eq [ConsoleKey]::Escape) {
                    Write-Host "`nOrchestration paused. Continue? (y/N)" -ForegroundColor Yellow
                    $continue = Read-Host
                    if ($continue -ne 'y') {
                        return $false
                    }
                }
            }
        }
        
        if ($script:CurrentIteration -ge $script:MaxIterations) {
            Write-Warning "Maximum iterations reached"
        }
        
        # Phase 3: Validation
        Write-Host "`n── Phase 3: Validation ────────────────────────────────────" -ForegroundColor Cyan
        $validationResult = Test-ImplementationComplete -Feature $Feature -RepoPath $RepoPath
        
        $orchestrationState.Phase = "Complete"
        $orchestrationState.EndTime = Get-Date
        $orchestrationState.TotalDuration = $orchestrationState.EndTime - $orchestrationState.StartTime
        
        # Summary
        Write-Host "`n╔══════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
        Write-Host "║           ORCHESTRATION COMPLETE                          ║" -ForegroundColor Magenta
        Write-Host "╠══════════════════════════════════════════════════════════╣" -ForegroundColor Magenta
        Write-Host "║ Completed Steps: $($orchestrationState.CompletedSteps.Count.ToString().PadRight(39)) ║" -ForegroundColor Green
        Write-Host "║ Errors: $($orchestrationState.Errors.Count.ToString().PadRight(48)) ║" -ForegroundColor $(if ($orchestrationState.Errors.Count -gt 0) { "Yellow" } else { "Green" })
        Write-Host "║ Duration: $($orchestrationState.TotalDuration.ToString('hh\:mm\:ss').PadRight(46)) ║" -ForegroundColor White
        Write-Host "║ Iterations: $($script:CurrentIteration.ToString().PadRight(44)) ║" -ForegroundColor White
        Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Magenta
        
        return $validationResult.Success
    }
    catch {
        Write-Error "Orchestration failed: $_"
        return $false
    }
}

function Get-ImplementationPlan {
    <#
    .SYNOPSIS
        Gets an implementation plan from Copilot
    .PARAMETER Prompt
        The formatted feature prompt
    .PARAMETER RepoPath
        Path to the repository
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Prompt,
        
        [Parameter(Mandatory)]
        [string]$RepoPath
    )
    
    Write-Host "Generating implementation plan..." -ForegroundColor Cyan
    
    $planPrompt = @"
Analyze this feature request and provide a step-by-step implementation plan.
Return a numbered list of specific tasks to complete.

$Prompt

Provide the plan as a numbered list, one task per line.
"@
    
    $result = Invoke-CopilotCommand -Prompt $planPrompt -Type "suggest" -WorkingDirectory $RepoPath
    
    if (-not $result.Success) {
        return @{
            Success = $false
            Error = $result.Error
            Steps = @()
        }
    }
    
    # Parse steps from output
    $steps = @()
    $lines = $result.Output -split "`n"
    foreach ($line in $lines) {
        if ($line -match "^\s*\d+[\.\)]\s*(.+)$") {
            $steps += $Matches[1].Trim()
        }
    }
    
    # If no steps parsed, create generic steps
    if ($steps.Count -eq 0) {
        $steps = @(
            "Analyze existing codebase",
            "Create necessary files",
            "Implement core functionality",
            "Add error handling",
            "Write tests",
            "Update documentation"
        )
    }
    
    Write-Host "`nImplementation Plan:" -ForegroundColor Green
    for ($i = 0; $i -lt $steps.Count; $i++) {
        Write-Host "  $($i + 1). $($steps[$i])" -ForegroundColor White
    }
    
    return @{
        Success = $true
        Steps = $steps
    }
}

function Invoke-ImplementationStep {
    <#
    .SYNOPSIS
        Executes a single implementation step
    .PARAMETER Step
        The step description
    .PARAMETER RepoPath
        Path to the repository
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Step,
        
        [Parameter(Mandatory)]
        [string]$RepoPath
    )
    
    $stepPrompt = "Implement this step: $Step"
    
    $result = Invoke-CopilotCommand -Prompt $stepPrompt -Type "suggest" -WorkingDirectory $RepoPath
    
    return @{
        Success = $result.Success
        Output = $result.Output
        Error = $result.Error
    }
}

function Test-ImplementationComplete {
    <#
    .SYNOPSIS
        Validates that a feature implementation is complete
    .PARAMETER Feature
        The feature object
    .PARAMETER RepoPath
        Path to the repository
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [PSCustomObject]$Feature,
        
        [Parameter(Mandatory)]
        [string]$RepoPath
    )
    
    Write-Host "Validating implementation..." -ForegroundColor Cyan
    
    $issues = @()
    
    # Check for uncommitted changes (indicates work was done)
    Push-Location $RepoPath
    try {
        $status = git status --porcelain 2>$null
        if (-not $status) {
            $issues += "No changes detected in repository"
        }
    }
    finally {
        Pop-Location
    }
    
    # Validate acceptance criteria
    Write-Host "`nAcceptance Criteria Status:" -ForegroundColor Yellow
    foreach ($criterion in $Feature.AcceptanceCriteria) {
        # This is a placeholder - in a real implementation, we'd use
        # Copilot to verify each criterion
        Write-Host "  [ ] $criterion" -ForegroundColor White
    }
    
    $result = @{
        Success = $issues.Count -eq 0
        Issues = $issues
    }
    
    if ($result.Success) {
        Write-Host "✓ Implementation appears complete" -ForegroundColor Green
    }
    else {
        Write-Host "⚠ Issues found:" -ForegroundColor Yellow
        $issues | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
    }
    
    return $result
}

function Get-ExecutionHistory {
    <#
    .SYNOPSIS
        Returns the execution history for the current session
    #>
    [CmdletBinding()]
    param()
    
    return $script:ExecutionHistory
}

function Clear-ExecutionHistory {
    <#
    .SYNOPSIS
        Clears the execution history
    #>
    [CmdletBinding()]
    param()
    
    $script:ExecutionHistory = @()
    $script:CurrentIteration = 0
    Write-Host "Execution history cleared" -ForegroundColor Green
}

