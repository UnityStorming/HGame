param(
    [string]$ProjectRoot = (Join-Path $PSScriptRoot '../..'),
    [switch]$Check
)

$ErrorActionPreference = 'Stop'

try {
    $root = [System.IO.Path]::GetFullPath($ProjectRoot).TrimEnd('\', '/')
    if (-not (Test-Path -LiteralPath $root -PathType Container)) {
        throw "Project root does not exist: $root"
    }
    $prefix = $root + [System.IO.Path]::DirectorySeparatorChar

    function Get-SafePath([string]$Relative) {
        $path = [System.IO.Path]::GetFullPath((Join-Path $root $Relative))
        if (-not $path.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Path escapes project root: $path"
        }
        return $path
    }

    function Assert-SafeParents([string]$Path) {
        $parent = Split-Path -Parent $Path
        while ($parent -ne $root) {
            $item = Get-Item -LiteralPath $parent -Force -ErrorAction SilentlyContinue
            if ($null -ne $item) {
                if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                    throw "Parent must be a real directory, not a link: $parent"
                }
                if (-not $item.PSIsContainer) { throw "Parent is not a directory: $parent" }
            }
            $parent = Split-Path -Parent $parent
        }
    }

    $skills = Get-SafePath '.codex/skills'
    $mcp = Get-SafePath '.codex/mcp.json'
    foreach ($source in @($skills, $mcp)) {
        Assert-SafeParents $source
        $item = Get-Item -LiteralPath $source -Force -ErrorAction SilentlyContinue
        if ($null -eq $item) { throw "Canonical source is missing: $source" }
        if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Canonical source must not be a link: $source"
        }
    }
    if (-not (Test-Path -LiteralPath $skills -PathType Container)) { throw "Canonical skills must be a directory: $skills" }
    if (-not (Test-Path -LiteralPath $mcp -PathType Leaf)) { throw "Canonical MCP must be a file: $mcp" }
    $config = Get-SafePath '.codex/config.toml'
    Assert-SafeParents $config
    $configItem = Get-Item -LiteralPath $config -Force -ErrorAction SilentlyContinue
    if ($null -ne $configItem -and $configItem.PSIsContainer) {
        throw "Codex config must be a file: $config"
    }
    if ($null -ne $configItem -and (($configItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0)) {
        throw "Codex config must not be a link: $config"
    }

    $specs = @(
        @{ Name = '.claude/skills'; Target = '../.codex/skills'; Directory = $true },
        @{ Name = '.cursor/skills'; Target = '../.codex/skills'; Directory = $true },
        @{ Name = '.agents/skills'; Target = '../.codex/skills'; Directory = $true },
        @{ Name = '.mcp.json'; Target = '.codex/mcp.json'; Directory = $false },
        @{ Name = '.cursor/mcp.json'; Target = '../.codex/mcp.json'; Directory = $false }
    )
    $plan = @()
    # Validate every entry before creating parents, deleting placeholders, or syncing config.
    foreach ($spec in $specs) {
        $path = Get-SafePath $spec.Name
        Assert-SafeParents $path
        $targetPath = [System.IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $path) $spec.Target))
        if (-not $targetPath.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Link target escapes project root: $path"
        }
        $item = Get-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
        $replace = $false
        if ($null -ne $item) {
            if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                $targets = @($item.Target)
                if ($item.LinkType -ne 'SymbolicLink' -or $targets.Count -ne 1 -or
                    $targets[0].Replace('\', '/') -cne $spec.Target -or
                    -not (Test-Path -LiteralPath $path)) {
                    throw "Wrong or stale link: $path (expected $($spec.Target))"
                }
            } elseif ($item.PSIsContainer) {
                if (-not $spec.Directory -or @(Get-ChildItem -LiteralPath $path -Force).Count -gt 0) {
                    throw "Refusing existing nonempty or incompatible directory: $path. Back it up and move it manually."
                }
                $replace = $true
            } else {
                $text = [System.IO.File]::ReadAllText($path)
                $serialized = $text -ceq $spec.Target
                $sameJson = -not $spec.Directory -and
                    [Convert]::ToBase64String([System.IO.File]::ReadAllBytes($path)) -ceq
                    [Convert]::ToBase64String([System.IO.File]::ReadAllBytes($mcp))
                if (-not $serialized -and -not $sameJson) {
                    throw "Refusing existing file with different content: $path. Back it up and move it manually."
                }
                $replace = $true
            }
        }
        $needsLink = $null -eq $item -or $replace
        if ($Check -and $needsLink) { throw "Missing symbolic link or unconverted placeholder: $path" }
        $plan += @{ Path = $path; Target = $spec.Target; Replace = $replace; NeedsLink = $needsLink }
    }

    $syncScript = Join-Path $PSScriptRoot 'sync_mcp.py'
    if (-not (Test-Path -LiteralPath $syncScript -PathType Leaf)) { throw "Missing MCP sync script: $syncScript" }
    $python = Get-Command python -ErrorAction Stop
    if (-not $Check) {
        foreach ($entry in $plan) {
            if (-not $entry.NeedsLink) { continue }
            $parent = Split-Path -Parent $entry.Path
            if (-not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Path $parent | Out-Null }
            if ($entry.Replace) {
                # Empty real directories only; never recursively remove a directory.
                $existing = Get-Item -LiteralPath $entry.Path -Force
                if ($existing.PSIsContainer) { [System.IO.Directory]::Delete($entry.Path, $false) }
                else { Remove-Item -LiteralPath $entry.Path -Force }
            }
            Push-Location -LiteralPath $parent
            try {
                New-Item -ItemType SymbolicLink -Path (Split-Path -Leaf $entry.Path) -Target $entry.Target | Out-Null
            } finally { Pop-Location }
        }
    }
    $syncArgs = @($syncScript, '--root', $root)
    if ($Check) { $syncArgs += '--check' }
    & $python.Source @syncArgs
    if ($LASTEXITCODE -ne 0) { throw "MCP synchronization failed (exit $LASTEXITCODE)." }
    Write-Output 'Agent links and MCP configuration are consistent.'
    exit 0
} catch {
    Write-Error $_ -ErrorAction Continue
    exit 1
}
