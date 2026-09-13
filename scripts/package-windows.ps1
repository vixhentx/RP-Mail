$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $RepoRoot

$PackageDir = "artifacts/packages"
$Version = if ($env:VERSION) { $env:VERSION } else { (git describe --tags --always --dirty 2>$null) -replace '^v', '' }
$Version = $Version -replace '^v', ''
$Rid = if ($env:RID) { $env:RID } else { "win-x64" }

function Publish-App([string]$Name, [string]$Project, [string]$Executable) {
  $PublishDir = "artifacts/publish/$Name-$Rid"
  Remove-Item -Recurse -Force $PublishDir -ErrorAction SilentlyContinue
  New-Item -ItemType Directory -Force $PublishDir, $PackageDir | Out-Null

  dotnet publish $Project `
    --configuration Release `
    --runtime $Rid `
    --self-contained true `
    --output $PublishDir `
    -p:PublishAot=true `
    -p:NuGetAudit=false

  $Archive = "$PackageDir/$Name-$Version-$Rid.zip"
  Remove-Item -Force $Archive -ErrorAction SilentlyContinue
  Compress-Archive -Path "$PublishDir/*" -DestinationPath $Archive
}

Publish-App "rpmail-console" "RPMailConsole/RPMailConsole.csproj" "RPMailConsole"
Publish-App "rpmail-ui" "RPMailUI/RPMailUI.csproj" "RPMailUI"
