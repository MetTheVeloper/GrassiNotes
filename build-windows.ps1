$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

[xml]$project = Get-Content .\GrassiNotes.csproj
$versionNode = $project.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1
$version = [string]$versionNode.Version
if ([string]::IsNullOrWhiteSpace($version)) { throw 'Version not found in GrassiNotes.csproj' }
$env:APP_VERSION = $version

dotnet restore .\GrassiNotes.csproj
dotnet publish .\GrassiNotes.csproj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:DebugType=None -p:DebugSymbols=false -o .\dist\portable

Copy-Item .\dist\portable\GrassiNotes.exe ".\dist\GrassiNotes-$version-x64.exe" -Force
Copy-Item .\README-FA.md .\dist\portable\README-FA.md -Force

$zip = ".\dist\GrassiNotes-Portable-$version.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path .\dist\portable\* -DestinationPath $zip -CompressionLevel Optimal

$iscc = Get-Command ISCC.exe -ErrorAction SilentlyContinue
if ($iscc) {
  & $iscc.Source .\installer\GrassiNotes.iss
  Write-Host "Installer built: $root\dist\GrassiNotes-Setup-$version.exe" -ForegroundColor Green
}
else {
  Write-Warning 'ISCC.exe was not found. Portable files were built, but the installer was skipped.'
}

Write-Host "Portable EXE: $root\dist\GrassiNotes-$version-x64.exe" -ForegroundColor Green
Write-Host "Portable ZIP: $root\dist\GrassiNotes-Portable-$version.zip" -ForegroundColor Green
