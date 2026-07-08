<#
    Deterministic NuGet pack for this package.

    Why this script exists:
      * The compiled assembly is guaranteed fresh — bin/obj are wiped first, so a stale
        DLL can never end up inside the package (the bug behind several "republish"
        releases in this repo's history).
      * The freshly built version is evicted from the local NuGet global-packages cache.
        NuGet never re-extracts a version it has already cached, so without this step a
        re-packed same-version package would be ignored by consumers on this machine.

    Usage:  pwsh ./pack.ps1        (from the repo root)
#>
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

# Locate the single packable project (the one that declares <PackageId>).
$proj = Get-ChildItem -Path (Join-Path $root 'src') -Recurse -Filter *.csproj |
        Where-Object { (Get-Content $_.FullName -Raw) -match '<PackageId>' } |
        Select-Object -First 1
if (-not $proj) { throw "No packable .csproj (with <PackageId>) found under src/." }

$xml = [xml](Get-Content $proj.FullName)
$id  = ("" + (($xml.Project.PropertyGroup.PackageId | Where-Object { $_ }) | Select-Object -First 1)).Trim()
$ver = ("" + (($xml.Project.PropertyGroup.Version   | Where-Object { $_ }) | Select-Object -First 1)).Trim()

Write-Host "Packing $id $ver" -ForegroundColor Cyan

# 1) Wipe build outputs -> forces a full, fresh compile (no stale DLL can be packed).
foreach ($dir in 'bin', 'obj') {
    $p = Join-Path $proj.Directory $dir
    if (Test-Path $p) { Remove-Item -Recurse -Force $p }
}

# 2) Clean Release build + pack into the repo's Build/ output folder.
dotnet pack $proj.FullName -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw "dotnet pack failed." }

# 3) Evict this exact version from the local cache so consumers re-extract the new bits.
if ($id -and $ver) {
    $cache = Join-Path $env:USERPROFILE (".nuget\packages\{0}\{1}" -f $id.ToLowerInvariant(), $ver)
    if (Test-Path $cache) {
        Remove-Item -Recurse -Force $cache
        Write-Host "Evicted $id $ver from the global-packages cache." -ForegroundColor Yellow
    }
}

Write-Host "Done -> Build\$id.$ver.nupkg" -ForegroundColor Green
