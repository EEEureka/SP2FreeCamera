[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepositoryRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$excludedDirectoryNames = @('.git', '.cache', '.build', 'bin')
$textExtensions = @(
    '.cs', '.ps1', '.md', '.txt', '.json', '.yml', '.yaml', '.xml', '.ini',
    '.gitignore', '.gitattributes', '.sha256'
)

$privateValues = @(
    [Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile),
    [Environment]::MachineName,
    [Environment]::UserName
) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

$failures = New-Object 'System.Collections.Generic.List[string]'
$files = Get-ChildItem -LiteralPath $RepositoryRoot -Recurse -File -Force | Where-Object {
    $relative = $_.FullName.Substring($RepositoryRoot.Length).TrimStart('\')
    $segments = $relative.Split('\')
    -not ($segments | Where-Object { $excludedDirectoryNames -contains $_ }) -and
        $_.Extension.ToLowerInvariant() -ne '.zip'
}

foreach ($file in $files) {
    $extension = $file.Extension.ToLowerInvariant()
    if ([string]::IsNullOrEmpty($extension)) {
        $extension = $file.Name.ToLowerInvariant()
    }
    if ($textExtensions -notcontains $extension) {
        continue
    }

    $text = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($privateValue in $privateValues) {
        if ($text.IndexOf($privateValue, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            $failures.Add($file.FullName.Substring($RepositoryRoot.Length + 1))
            break
        }
    }

    if ($text -match '(?i)(?<!\d)(?:10(?:\.\d{1,3}){3}|192\.168(?:\.\d{1,3}){2}|172\.(?:1[6-9]|2[0-9]|3[01])(?:\.\d{1,3}){2})(?!\d)' -or
        $text -match '(?i)https?://[^/\s:@]+:[^/\s@]+@') {
        $failures.Add($file.FullName.Substring($RepositoryRoot.Length + 1))
    }
}

if ($failures.Count -gt 0) {
    $uniqueFailures = $failures | Sort-Object -Unique
    throw "Repository privacy verification failed in: $($uniqueFailures -join ', ')"
}

Write-Host "Repository privacy verification passed."
