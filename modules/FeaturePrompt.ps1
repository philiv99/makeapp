#Requires -Version 7.0
<#
.SYNOPSIS
    Feature prompt module for MakeApp CLI
.DESCRIPTION
    Provides functions for capturing, storing, and formatting feature
    requirements for use with Copilot CLI and agent orchestration.
#>

function Get-FeatureRequirements {
    <#
    .SYNOPSIS
        Interactive prompt for gathering feature requirements
    .PARAMETER Title
        Optional pre-filled title
    .PARAMETER Description
        Optional pre-filled description
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$Title,
        
        [Parameter()]
        [string]$Description
    )
    
    Write-Host "`n=== Feature Requirements ===" -ForegroundColor Cyan
    
    # Get title
    if (-not $Title) {
        $Title = Read-Host "Feature title"
    } else {
        Write-Host "Title: $Title" -ForegroundColor White
    }
    
    # Get description
    if (-not $Description) {
        Write-Host "Enter feature description (press Enter twice to finish):" -ForegroundColor Yellow
        $descLines = @()
        while ($true) {
            $line = Read-Host
            if ([string]::IsNullOrEmpty($line) -and $descLines.Count -gt 0) {
                break
            }
            $descLines += $line
        }
        $Description = $descLines -join "`n"
    } else {
        Write-Host "Description: $Description" -ForegroundColor White
    }
    
    # Get acceptance criteria
    Write-Host "`nEnter acceptance criteria (one per line, empty line to finish):" -ForegroundColor Yellow
    $criteria = @()
    while ($true) {
        $criterion = Read-Host "  - "
        if ([string]::IsNullOrEmpty($criterion)) {
            break
        }
        $criteria += $criterion
    }
    
    # Get technical notes
    Write-Host "`nAny technical notes/constraints? (one per line, empty line to finish):" -ForegroundColor Yellow
    $notes = @()
    while ($true) {
        $note = Read-Host "  - "
        if ([string]::IsNullOrEmpty($note)) {
            break
        }
        $notes += $note
    }
    
    # Get affected files/areas (optional)
    Write-Host "`nKnown affected files/areas? (one per line, empty line to finish):" -ForegroundColor Yellow
    $affectedAreas = @()
    while ($true) {
        $area = Read-Host "  - "
        if ([string]::IsNullOrEmpty($area)) {
            break
        }
        $affectedAreas += $area
    }
    
    # Get priority
    $priorityInput = Read-Host "`nPriority (1=Low, 2=Medium, 3=High) [2]"
    $priority = switch ($priorityInput) {
        "1" { "Low" }
        "3" { "High" }
        default { "Medium" }
    }
    
    $feature = [PSCustomObject]@{
        Id = [guid]::NewGuid().ToString("N").Substring(0, 8)
        Title = $Title
        Description = $Description
        AcceptanceCriteria = $criteria
        TechnicalNotes = $notes
        AffectedAreas = $affectedAreas
        Priority = $priority
        CreatedAt = Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ"
        Status = "Draft"
    }
    
    # Display summary
    Write-Host "`n=== Feature Summary ===" -ForegroundColor Green
    Write-Host "ID: $($feature.Id)" -ForegroundColor White
    Write-Host "Title: $($feature.Title)" -ForegroundColor White
    Write-Host "Priority: $($feature.Priority)" -ForegroundColor White
    Write-Host "Acceptance Criteria: $($feature.AcceptanceCriteria.Count) items" -ForegroundColor White
    
    $confirm = Read-Host "`nSave this feature? (Y/n)"
    if ($confirm -eq 'n' -or $confirm -eq 'N') {
        Write-Host "Feature discarded" -ForegroundColor Yellow
        return $null
    }
    
    return $feature
}

function Import-FeatureFromFile {
    <#
    .SYNOPSIS
        Loads feature requirements from a file
    .PARAMETER FilePath
        Path to the feature file (JSON or Markdown)
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$FilePath
    )
    
    if (-not (Test-Path $FilePath)) {
        Write-Error "File not found: $FilePath"
        return $null
    }
    
    $extension = [System.IO.Path]::GetExtension($FilePath).ToLower()
    
    switch ($extension) {
        ".json" {
            try {
                $content = Get-Content $FilePath -Raw | ConvertFrom-Json
                # Validate required fields
                if (-not $content.Title) {
                    Write-Error "Feature file missing required field: Title"
                    return $null
                }
                
                # Add defaults for missing fields
                if (-not $content.Id) {
                    $content | Add-Member -NotePropertyName "Id" -NotePropertyValue ([guid]::NewGuid().ToString("N").Substring(0, 8))
                }
                if (-not $content.CreatedAt) {
                    $content | Add-Member -NotePropertyName "CreatedAt" -NotePropertyValue (Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ")
                }
                if (-not $content.Status) {
                    $content | Add-Member -NotePropertyName "Status" -NotePropertyValue "Imported"
                }
                
                return $content
            }
            catch {
                Write-Error "Failed to parse JSON file: $_"
                return $null
            }
        }
        ".md" {
            return Import-FeatureFromMarkdown -FilePath $FilePath
        }
        default {
            # Try to parse as plain text prompt
            $content = Get-Content $FilePath -Raw
            return [PSCustomObject]@{
                Id = [guid]::NewGuid().ToString("N").Substring(0, 8)
                Title = [System.IO.Path]::GetFileNameWithoutExtension($FilePath)
                Description = $content
                AcceptanceCriteria = @()
                TechnicalNotes = @()
                AffectedAreas = @()
                Priority = "Medium"
                CreatedAt = Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ"
                Status = "Imported"
            }
        }
    }
}

function Import-FeatureFromMarkdown {
    <#
    .SYNOPSIS
        Parses a markdown file into a feature object
    .PARAMETER FilePath
        Path to the markdown file
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$FilePath
    )
    
    $content = Get-Content $FilePath -Raw
    $lines = Get-Content $FilePath
    
    $feature = [PSCustomObject]@{
        Id = [guid]::NewGuid().ToString("N").Substring(0, 8)
        Title = ""
        Description = ""
        AcceptanceCriteria = @()
        TechnicalNotes = @()
        AffectedAreas = @()
        Priority = "Medium"
        CreatedAt = Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ"
        Status = "Imported"
    }
    
    $currentSection = "description"
    $descriptionLines = @()
    
    foreach ($line in $lines) {
        # Check for title (# heading)
        if ($line -match "^#\s+(.+)$" -and -not $feature.Title) {
            $feature.Title = $Matches[1]
            continue
        }
        
        # Check for section headers
        if ($line -match "^##\s*Acceptance\s*Criteria" -or $line -match "^##\s*Requirements") {
            $currentSection = "criteria"
            continue
        }
        if ($line -match "^##\s*Technical\s*Notes" -or $line -match "^##\s*Notes") {
            $currentSection = "notes"
            continue
        }
        if ($line -match "^##\s*Affected\s*(Files|Areas)") {
            $currentSection = "areas"
            continue
        }
        if ($line -match "^##\s*Description") {
            $currentSection = "description"
            continue
        }
        
        # Skip other headers
        if ($line -match "^##") {
            $currentSection = "other"
            continue
        }
        
        # Process list items
        if ($line -match "^\s*[-*]\s+(.+)$") {
            $item = $Matches[1]
            switch ($currentSection) {
                "criteria" { $feature.AcceptanceCriteria += $item }
                "notes" { $feature.TechnicalNotes += $item }
                "areas" { $feature.AffectedAreas += $item }
            }
            continue
        }
        
        # Collect description lines
        if ($currentSection -eq "description" -and $line.Trim()) {
            $descriptionLines += $line
        }
    }
    
    $feature.Description = $descriptionLines -join "`n"
    
    # If no title found, use filename
    if (-not $feature.Title) {
        $feature.Title = [System.IO.Path]::GetFileNameWithoutExtension($FilePath)
    }
    
    return $feature
}

function Save-FeatureInstructions {
    <#
    .SYNOPSIS
        Saves feature instructions to a file
    .PARAMETER Feature
        The feature object to save
    .PARAMETER OutputPath
        Path to save the file
    .PARAMETER Format
        Output format (json or md)
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [PSCustomObject]$Feature,
        
        [Parameter(Mandatory)]
        [string]$OutputPath,
        
        [Parameter()]
        [ValidateSet("json", "md")]
        [string]$Format = "json"
    )
    
    # Ensure directory exists
    $directory = [System.IO.Path]::GetDirectoryName($OutputPath)
    if ($directory -and -not (Test-Path $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }
    
    switch ($Format) {
        "json" {
            $Feature | ConvertTo-Json -Depth 10 | Set-Content $OutputPath -Encoding UTF8
        }
        "md" {
            $md = @"
# $($Feature.Title)

## Description

$($Feature.Description)

## Acceptance Criteria

$($Feature.AcceptanceCriteria | ForEach-Object { "- $_" } | Out-String)

## Technical Notes

$($Feature.TechnicalNotes | ForEach-Object { "- $_" } | Out-String)

## Affected Areas

$($Feature.AffectedAreas | ForEach-Object { "- $_" } | Out-String)

---
- **ID:** $($Feature.Id)
- **Priority:** $($Feature.Priority)
- **Created:** $($Feature.CreatedAt)
- **Status:** $($Feature.Status)
"@
            $md | Set-Content $OutputPath -Encoding UTF8
        }
    }
    
    Write-Host "Feature saved to: $OutputPath" -ForegroundColor Green
    return $OutputPath
}

function Format-CopilotPrompt {
    <#
    .SYNOPSIS
        Formats feature requirements into a prompt for Copilot CLI
    .PARAMETER Feature
        The feature object
    .PARAMETER Style
        Prompt style (concise, detailed, structured)
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [PSCustomObject]$Feature,
        
        [Parameter()]
        [ValidateSet("concise", "detailed", "structured")]
        [string]$Style = "structured"
    )
    
    switch ($Style) {
        "concise" {
            return "Implement: $($Feature.Title). $($Feature.Description)"
        }
        "detailed" {
            $prompt = @"
Please implement the following feature:

**$($Feature.Title)**

$($Feature.Description)

Requirements:
$($Feature.AcceptanceCriteria | ForEach-Object { "- $_" } | Out-String)

$($Feature.TechnicalNotes | ForEach-Object { "Note: $_" } | Out-String)
"@
            return $prompt
        }
        "structured" {
            $prompt = @"
## Task
Implement: $($Feature.Title)

## Description
$($Feature.Description)

## Acceptance Criteria
$($Feature.AcceptanceCriteria | ForEach-Object { "- [ ] $_" } | Out-String)

## Technical Constraints
$($Feature.TechnicalNotes | ForEach-Object { "- $_" } | Out-String)

## Known Affected Areas
$($Feature.AffectedAreas | ForEach-Object { "- $_" } | Out-String)

## Instructions
1. Analyze the requirements and create a plan
2. Implement the necessary changes
3. Ensure all acceptance criteria are met
4. Follow existing code patterns and conventions
"@
            return $prompt
        }
    }
}

function New-FeatureTemplate {
    <#
    .SYNOPSIS
        Creates a new feature template file
    .PARAMETER OutputPath
        Path to create the template
    .PARAMETER Format
        Template format (json or md)
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$OutputPath,
        
        [Parameter()]
        [ValidateSet("json", "md")]
        [string]$Format = "md"
    )
    
    if ($Format -eq "json") {
        $template = @{
            Title = "Feature Title"
            Description = "Describe what this feature should do..."
            AcceptanceCriteria = @(
                "User can...",
                "System should...",
                "When X happens, Y should occur"
            )
            TechnicalNotes = @(
                "Consider using existing pattern from...",
                "Must be compatible with..."
            )
            AffectedAreas = @(
                "src/components/",
                "src/services/"
            )
            Priority = "Medium"
        }
        $template | ConvertTo-Json -Depth 10 | Set-Content $OutputPath -Encoding UTF8
    }
    else {
        $template = @"
# Feature Title

## Description

Describe what this feature should do. Be specific about the expected behavior and user experience.

## Acceptance Criteria

- [ ] User can...
- [ ] System should...
- [ ] When X happens, Y should occur
- [ ] Error handling for edge cases

## Technical Notes

- Consider using existing pattern from...
- Must be compatible with...
- Performance considerations...

## Affected Areas

- src/components/
- src/services/
- tests/

---
**Priority:** Medium
"@
        $template | Set-Content $OutputPath -Encoding UTF8
    }
    
    Write-Host "Template created: $OutputPath" -ForegroundColor Green
    return $OutputPath
}
