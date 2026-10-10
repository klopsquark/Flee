<#
.SYNOPSIS
Builds the GitHub wiki of the fork from wiki/ and doc/, and optionally pushes it.

.DESCRIPTION
The wiki has two kinds of pages:
- hand-written pages in wiki/*.md (Home, Getting Started, Examples, ...), copied as they are;
- pages generated from repository documents, listed in wiki/generated-pages.txt
  ("<path> = <page name>").

For every page the leading "# Title" line is dropped (the wiki shows the page name as title).
In generated pages, links to other mapped documents become wiki links, and links to any other
repository file point to it on GitHub. Each generated page starts with a note naming its source.

Without -Push the pages are written to -OutDir (default: build/wiki) for review.
With -Push the wiki repository is cloned, its pages are replaced, and the result is committed and
pushed. GitHub creates the wiki repository only when the first page is created on the website, so
create any page there once before the first push.

.EXAMPLE
pwsh tools/Publish-Wiki.ps1
pwsh tools/Publish-Wiki.ps1 -Push
#>
[CmdletBinding()]
param(
    [string]$OutDir,
    [switch]$Push,
    [string]$WikiUrl = 'https://github.com/klopsquark/Flee.wiki.git',
    [string]$BlobBase = 'https://github.com/klopsquark/Flee/blob/develop'
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$commit = (git -C $repo rev-parse --short HEAD).Trim()

if (-not $OutDir) { $OutDir = Join-Path $repo 'build/wiki' }

# --- prepare the output folder ---------------------------------------------------------------
if ($Push) {
    if (Test-Path $OutDir) { Remove-Item $OutDir -Recurse -Force }
    git clone --quiet $WikiUrl $OutDir
    if ($LASTEXITCODE -ne 0) {
        throw "Could not clone $WikiUrl. If the wiki has no page yet, create one on GitHub first."
    }
    Get-ChildItem $OutDir -Filter *.md | Remove-Item
}
else {
    if (Test-Path $OutDir) { Remove-Item $OutDir -Recurse -Force }
    New-Item -ItemType Directory $OutDir | Out-Null
}

# --- helpers ---------------------------------------------------------------------------------
function Remove-Title([string]$text) {
    # Drops the first line if it is a level-1 heading, and the blank lines after it.
    return [regex]::Replace($text, '\A#\s[^\n]*\n(\s*\n)*', '')
}

function Write-Page([string]$name, [string]$text) {
    $text = $text.Replace("`r`n", "`n")
    [IO.File]::WriteAllText((Join-Path $OutDir "$name.md"), $text, [Text.UTF8Encoding]::new($false))
}

# --- the page map ----------------------------------------------------------------------------
$map = @{}
foreach ($line in Get-Content (Join-Path $repo 'wiki/generated-pages.txt')) {
    if ($line -match '^\s*(#|$)') { continue }
    $parts = $line -split '=', 2
    $source = $parts[0].Trim()
    $page = $parts[1].Trim()
    if (-not (Test-Path (Join-Path $repo $source))) { throw "wiki/generated-pages.txt: $source does not exist" }
    $map[$source] = $page
}

function Convert-Links([string]$text, [string]$source) {
    $sourceDir = Split-Path $source -Parent
    return [regex]::Replace($text, '\]\(([^)\s]+)\)', {
        param($m)
        $target = $m.Groups[1].Value
        if ($target -match '^(https?:|mailto:|#)') { return $m.Value }

        $anchor = ''
        $hash = $target.IndexOf('#')
        if ($hash -ge 0) { $anchor = $target.Substring($hash); $target = $target.Substring(0, $hash) }

        $decoded = [Uri]::UnescapeDataString($target)
        $full = [IO.Path]::GetFullPath((Join-Path (Join-Path $repo $sourceDir) $decoded))
        $relative = [IO.Path]::GetRelativePath($repo, $full).Replace('\', '/')

        if ($map.ContainsKey($relative)) { return "]($($map[$relative])$anchor)" }
        if (-not (Test-Path $full)) { throw "$source links to $target, which does not exist" }
        $escaped = ($relative -split '/' | ForEach-Object { [Uri]::EscapeDataString($_) }) -join '/'
        return "]($BlobBase/$escaped$anchor)"
    })
}

# --- hand-written pages ----------------------------------------------------------------------
foreach ($file in Get-ChildItem (Join-Path $repo 'wiki') -Filter *.md) {
    $text = Get-Content $file.FullName -Raw
    if (-not $file.BaseName.StartsWith('_')) { $text = Remove-Title $text }
    Write-Page $file.BaseName $text
}

# --- generated pages -------------------------------------------------------------------------
foreach ($source in $map.Keys) {
    $text = Get-Content (Join-Path $repo $source) -Raw
    $text = Convert-Links (Remove-Title $text) $source
    $note = "> Generated from [``$source``]($BlobBase/$(($source -split '/' | ForEach-Object { [Uri]::EscapeDataString($_) }) -join '/')) at commit ``$commit``. Edit the source in the repository.`n`n"
    Write-Page $map[$source] ($note + $text)
}

$count = (Get-ChildItem $OutDir -Filter *.md).Count
Write-Host "Wrote $count pages to $OutDir"

# --- publish ---------------------------------------------------------------------------------
if ($Push) {
    git -C $OutDir add -A
    git -C $OutDir diff --cached --quiet
    if ($LASTEXITCODE -eq 0) {
        Write-Host 'The wiki is already up to date.'
        return
    }
    git -C $OutDir commit --quiet -m "Publish from klopsquark/Flee $commit"
    git -C $OutDir push --quiet
    if ($LASTEXITCODE -ne 0) { throw 'git push to the wiki failed.' }
    Write-Host "Published the wiki from commit $commit."
}
