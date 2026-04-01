<#
.SYNOPSIS
    Updates BonniePDF version numbers across all projects.

.DESCRIPTION
    This script manages version numbers in the [yyyy].[mm].[patch] format.
    It automatically increments the patch number for releases in the same month,
    or resets to .0 when moving to a new month.

.PARAMETER Version
    Optional. Specify a specific version to use instead of auto-calculating.
    Format: yyyy.mm.patch (e.g., 2026.04.0)

.PARAMETER WhatIf
    Preview the changes without modifying any files.

.PARAMETER Commit
    Create a git commit with the version changes.

.PARAMETER Tag
    Create a git tag for the new version (requires -Commit).

.PARAMETER Quiet
    Suppress non-error output.

.EXAMPLE
    .\Update-Versions.ps1
    Auto-increment version based on current date.

.EXAMPLE
    .\Update-Versions.ps1 -Version "2026.04.5"
    Set a specific version.

.EXAMPLE
    .\Update-Versions.ps1 -WhatIf
    Preview changes without applying them.

.EXAMPLE
    .\Update-Versions.ps1 -Commit -Tag
    Update version, commit changes, and create a git tag.
#>

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$Version,
    [switch]$Commit,
    [switch]$Tag,
    [switch]$Quiet
)

# Error handling
$ErrorActionPreference = "Stop"

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    if (-not $Quiet) {
        Write-Host $Message -ForegroundColor $Color
    }
}

function Get-ScriptDirectory {
    $scriptPath = $MyInvocation.MyCommand.Path
    $scriptDir = Split-Path -Parent $scriptPath
    return $scriptDir
}

function Get-VersionsFilePath {
    $scriptDir = Get-ScriptDirectory
    return Join-Path $scriptDir "..\versions.json"
}

function Get-RootDirectory {
    $scriptDir = Get-ScriptDirectory
    return Split-Path -Parent $scriptDir
}

function Read-VersionsFile {
    param([string]$Path)
    
    if (-not (Test-Path $Path)) {
        throw "Versions file not found at: $Path"
    }
    
    $json = Get-Content $Path -Raw | ConvertFrom-Json
    return $json
}

function Write-VersionsFile {
    param(
        [string]$Path,
        [object]$Data
    )
    
    $json = $Data | ConvertTo-Json -Depth 10
    Set-Content -Path $Path -Value $json -Encoding UTF8
}

function Calculate-NextVersion {
    param([object]$VersionsData)
    
    $currentDate = Get-Date
    $currentYear = $currentDate.Year
    $currentMonth = $currentDate.Month
    
    $lastVersion = $VersionsData.currentVersion
    $lastUpdate = [DateTime]::Parse($VersionsData.lastUpdate)
    
    $lastYear = $lastUpdate.Year
    $lastMonth = $lastUpdate.Month
    
    $newPatch = 0
    $newVersion = ""
    
    if ($currentYear -eq $lastYear -and $currentMonth -eq $lastMonth) {
        # Same month - increment patch
        $lastPatch = $VersionsData.history[0].patch
        $newPatch = $lastPatch + 1
        $newVersion = "$currentYear.$currentMonth.$newPatch"
        Write-ColorOutput "Same month detected. Incrementing patch to $newPatch" "Cyan"
    } else {
        # New month - reset patch to 0
        $newPatch = 0
        $newVersion = "$currentYear.$currentMonth.$newPatch"
        Write-ColorOutput "New month detected. Resetting patch to 0" "Cyan"
    }
    
    return @{
        Version = $newVersion
        Patch = $newPatch
        Year = $currentYear
        Month = $currentMonth
    }
}

function Validate-VersionFormat {
    param([string]$Version)
    
    if ($Version -notmatch '^\d{4}\.\d{1,2}\.\d+$') {
        throw "Invalid version format: $Version. Expected format: yyyy.mm.patch (e.g., 2026.04.0)"
    }
    
    $parts = $Version -split '\.'
    $year = [int]$parts[0]
    $month = [int]$parts[1]
    $patch = [int]$parts[2]
    
    if ($year -lt 2020 -or $year -gt 2100) {
        throw "Invalid year: $year. Must be between 2020 and 2100."
    }
    
    if ($month -lt 1 -or $month -gt 12) {
        throw "Invalid month: $month. Must be between 1 and 12."
    }
    
    if ($patch -lt 0) {
        throw "Invalid patch: $patch. Must be 0 or greater."
    }
    
    return @{
        Version = $Version
        Year = $year
        Month = $month
        Patch = $patch
    }
}

function Update-VersionsData {
    param(
        [object]$VersionsData,
        [hashtable]$NewVersionInfo
    )
    
    $currentDate = Get-Date
    $isoDate = $currentDate.ToString("o")
    
    # Create new history entry
    $historyEntry = @{
        version = $NewVersionInfo.Version
        date = $isoDate
        patch = $NewVersionInfo.Patch
        year = $NewVersionInfo.Year
        month = $NewVersionInfo.Month
    }
    
    # Update current version
    $VersionsData.currentVersion = $NewVersionInfo.Version
    $VersionsData.lastUpdate = $isoDate
    
    # Add to history (at the beginning)
    $VersionsData.history = @($historyEntry) + $VersionsData.history
    
    return $VersionsData
}

function Find-ProjectFiles {
    param([string]$RootPath)
    
    $csprojFiles = Get-ChildItem -Path $RootPath -Filter "*.csproj" -Recurse -File
    return $csprojFiles
}

function Update-ProjectFile {
    param(
        [string]$FilePath,
        [string]$NewVersion
    )
    
    $content = Get-Content $FilePath -Raw -Encoding UTF8
    
    # Check if Version element exists
    if ($content -match '<Version>([^<]+)</Version>') {
        $oldVersion = $matches[1]
        if ($oldVersion -ne $NewVersion) {
            $newContent = $content -replace "<Version>$([regex]::Escape($oldVersion))</Version>", "<Version>$NewVersion</Version>"
            Set-Content -Path $FilePath -Value $newContent -Encoding UTF8 -NoNewline
            return $true, $oldVersion
        }
        return $false, $oldVersion
    } else {
        # Version element doesn't exist, add it after TargetFramework or in first PropertyGroup
        if ($content -match '<PropertyGroup>(.*?)</PropertyGroup>') {
            $propertyGroup = $matches[1]
            $newPropertyGroup = $propertyGroup -replace "(`r?n\s*)$", "`r`n    <Version>$NewVersion</Version>$1"
            $newContent = $content -replace "<PropertyGroup>(.*?)</PropertyGroup>", "<PropertyGroup>$newPropertyGroup</PropertyGroup>"
            Set-Content -Path $FilePath -Value $newContent -Encoding UTF8 -NoNewline
            return $true, ""
        }
    }
    
    return $false, ""
}

function Update-DirectoryBuildProps {
    param(
        [string]$FilePath,
        [string]$NewVersion
    )
    
    if (-not (Test-Path $FilePath)) {
        Write-ColorOutput "Warning: Directory.Build.props not found at $FilePath" "Yellow"
        return $false
    }
    
    $content = Get-Content $FilePath -Raw -Encoding UTF8
    
    # Check if Version property exists
    if ($content -match '<Version>([^<]+)</Version>') {
        $oldVersion = $matches[1]
        if ($oldVersion -ne $NewVersion) {
            $newContent = $content -replace "<Version>$([regex]::Escape($oldVersion))</Version>", "<Version>$NewVersion</Version>"
            Set-Content -Path $FilePath -Value $newContent -Encoding UTF8 -NoNewline
            return $true, $oldVersion
        }
        return $false, $oldVersion
    } else {
        # Version property doesn't exist, add it in first PropertyGroup
        if ($content -match '<PropertyGroup>(.*?)</PropertyGroup>') {
            $propertyGroup = $matches[1]
            $newPropertyGroup = $propertyGroup -replace "(`r?n\s*)$", "`r`n    <Version>$NewVersion</Version>$1"
            $newContent = $content -replace "<PropertyGroup>(.*?)</PropertyGroup>", "<PropertyGroup>$newPropertyGroup</PropertyGroup>"
            Set-Content -Path $FilePath -Value $newContent -Encoding UTF8 -NoNewline
            return $true, ""
        }
    }
    
    return $false, ""
}

function Create-GitCommit {
    param(
        [string]$Version,
        [string]$RootPath
    )
    
    Push-Location $RootPath
    
    try {
        # Check if we're in a git repository
        $gitDir = git rev-parse --git-dir 2>$null
        if (-not $gitDir) {
            Write-ColorOutput "Not in a git repository. Skipping commit." "Yellow"
            return
        }
        
        # Check for changes
        $status = git status --porcelain
        if (-not $status) {
            Write-ColorOutput "No changes to commit." "Yellow"
            return
        }
        
        # Stage changes
        git add versions.json
        git add Directory.Build.props
        
        $csprojFiles = Get-ChildItem -Path "." -Filter "*.csproj" -Recurse -File
        foreach ($file in $csprojFiles) {
            git add $file.FullName
        }
        
        # Commit
        $commitMessage = "chore: bump version to $Version"
        git commit -m $commitMessage
        
        Write-ColorOutput "Created git commit: $commitMessage" "Green"
    }
    finally {
        Pop-Location
    }
}

function Create-GitTag {
    param(
        [string]$Version,
        [string]$RootPath
    )
    
    Push-Location $RootPath
    
    try {
        # Check if we're in a git repository
        $gitDir = git rev-parse --git-dir 2>$null
        if (-not $gitDir) {
            Write-ColorOutput "Not in a git repository. Skipping tag." "Yellow"
            return
        }
        
        $tagName = "v$Version"
        
        # Check if tag already exists
        $existingTag = git tag -l $tagName
        if ($existingTag) {
            Write-ColorOutput "Tag $tagName already exists. Skipping tag creation." "Yellow"
            return
        }
        
        # Create tag
        git tag -a $tagName -m "Version $Version"
        
        Write-ColorOutput "Created git tag: $tagName" "Green"
    }
    finally {
        Pop-Location
    }
}

# Main script execution
try {
    Write-ColorOutput "=== BonniePDF Version Update ===" "Cyan"
    Write-ColorOutput ""
    
    # Get paths
    $versionsPath = Get-VersionsFilePath
    $rootPath = Get-RootDirectory
    
    Write-ColorOutput "Versions file: $versionsPath" "Gray"
    Write-ColorOutput "Root directory: $rootPath" "Gray"
    Write-ColorOutput ""
    
    # Read current versions data
    Write-ColorOutput "Reading versions file..." "Gray"
    $versionsData = Read-VersionsFile -Path $versionsPath
    $oldVersion = $versionsData.currentVersion
    Write-ColorOutput "Current version: $oldVersion" "Gray"
    Write-ColorOutput ""
    
    # Determine new version
    if ($Version) {
        Write-ColorOutput "Using specified version: $Version" "Cyan"
        $versionInfo = Validate-VersionFormat -Version $Version
        $newVersion = $Version
    } else {
        Write-ColorOutput "Auto-calculating next version..." "Cyan"
        $versionInfo = Calculate-NextVersion -VersionsData $versionsData
        $newVersion = $versionInfo.Version
    }
    
    Write-ColorOutput "New version: $newVersion" "Green"
    Write-ColorOutput ""
    
    # WhatIf mode
    if ($WhatIfPreference) {
        Write-ColorOutput "WHAT IF MODE: No changes will be made." "Yellow"
        Write-ColorOutput ""
        Write-ColorOutput "Would update:" "Cyan"
        Write-ColorOutput "  - versions.json: $oldVersion -> $newVersion" "Gray"
        
        $csprojFiles = Find-ProjectFiles -RootPath $rootPath
        foreach ($file in $csprojFiles) {
            $relativePath = $file.FullName.Substring($rootPath.Length + 1)
            Write-ColorOutput "  - $relativePath" "Gray"
        }
        
        $buildPropsPath = Join-Path $rootPath "Directory.Build.props"
        if (Test-Path $buildPropsPath) {
            Write-ColorOutput "  - Directory.Build.props" "Gray"
        }
        
        if ($Commit) {
            Write-ColorOutput "  - Would create git commit" "Gray"
        }
        if ($Tag) {
            Write-ColorOutput "  - Would create git tag v$newVersion" "Gray"
        }
        
        exit 0
    }
    
    # Update versions.json
    Write-ColorOutput "Updating versions.json..." "Gray"
    $versionsData = Update-VersionsData -VersionsData $versionsData -NewVersionInfo $versionInfo
    Write-VersionsFile -Path $versionsPath -Data $versionsData
    Write-ColorOutput "✓ Updated versions.json: $oldVersion -> $newVersion" "Green"
    
    # Update all .csproj files
    Write-ColorOutput ""
    Write-ColorOutput "Updating .csproj files..." "Gray"
    $csprojFiles = Find-ProjectFiles -RootPath $rootPath
    $updatedCount = 0
    
    foreach ($file in $csprojFiles) {
        $relativePath = $file.FullName.Substring($rootPath.Length + 1)
        $updated, $oldProjVersion = Update-ProjectFile -FilePath $file.FullName -NewVersion $newVersion
        
        if ($updated) {
            if ($oldProjVersion) {
                Write-ColorOutput "✓ Updated $relativePath: $oldProjVersion -> $newVersion" "Green"
            } else {
                Write-ColorOutput "✓ Added version to $relativePath: $newVersion" "Green"
            }
            $updatedCount++
        }
    }
    
    if ($updatedCount -eq 0) {
        Write-ColorOutput "No .csproj files needed updating." "Yellow"
    }
    
    # Update Directory.Build.props
    Write-ColorOutput ""
    Write-ColorOutput "Updating Directory.Build.props..." "Gray"
    $buildPropsPath = Join-Path $rootPath "Directory.Build.props"
    $updated, $oldPropsVersion = Update-DirectoryBuildProps -FilePath $buildPropsPath -NewVersion $newVersion
    
    if ($updated) {
        if ($oldPropsVersion) {
            Write-ColorOutput "✓ Updated Directory.Build.props: $oldPropsVersion -> $newVersion" "Green"
        } else {
            Write-ColorOutput "✓ Added version to Directory.Build.props: $newVersion" "Green"
        }
    } else {
        Write-ColorOutput "Directory.Build.props already up to date." "Yellow"
    }
    
    # Create git commit if requested
    if ($Commit) {
        Write-ColorOutput ""
        Write-ColorOutput "Creating git commit..." "Gray"
        Create-GitCommit -Version $newVersion -RootPath $rootPath
    }
    
    # Create git tag if requested
    if ($Tag) {
        if (-not $Commit) {
            Write-ColorOutput "Warning: -Tag requires -Commit. Skipping tag creation." "Yellow"
        } else {
            Write-ColorOutput ""
            Write-ColorOutput "Creating git tag..." "Gray"
            Create-GitTag -Version $newVersion -RootPath $rootPath
        }
    }
    
    Write-ColorOutput ""
    Write-ColorOutput "=== Version update complete ===" "Green"
    Write-ColorOutput "New version: $newVersion" "Cyan"
    
    exit 0
}
catch {
    Write-ColorOutput ""
    Write-ColorOutput "ERROR: $($_.Exception.Message)" "Red"
    Write-ColorOutput $_.ScriptStackTrace "Red"
    exit 1
}
