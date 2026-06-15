# Install (Windows)

These instructions assume you downloaded the GitHub source zip and extracted it under your Downloads folder.

PowerShell and Command Prompt use different environment variable syntax:

- PowerShell: `$HOME` or `$env:USERPROFILE`
- Command Prompt: `%USERPROFILE%`

## 1. Build the mod with PowerShell

Open PowerShell, then run:

```powershell
$sourceRoot = Join-Path $HOME 'Downloads\Cities2-DataExport-main'

if (-not (Test-Path -LiteralPath (Join-Path $sourceRoot 'CS2DataExport.csproj'))) {
    $projectFile = Get-ChildItem -LiteralPath $sourceRoot -Recurse -Filter CS2DataExport.csproj |
        Select-Object -First 1

    if ($null -eq $projectFile) {
        throw "Could not find CS2DataExport.csproj under $sourceRoot. Check where the zip was extracted."
    }

    $sourceRoot = $projectFile.DirectoryName
}

Set-Location -LiteralPath $sourceRoot

$env:DOTNET_ROLL_FORWARD = 'Major'
Remove-Item -LiteralPath .\obj -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath .\bin -Recurse -Force -ErrorAction SilentlyContinue
dotnet build .\CS2DataExport.csproj -c Release -p:LangVersion=latest
```

If you extracted the zip somewhere else, change the first line to that folder.

## 2. Copy build output into CS2 Mods

Still in PowerShell, run:

```powershell
$modInstallPath = Join-Path $env:USERPROFILE 'AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\CS2DataExport'
robocopy (Join-Path $PWD 'bin\Release\net48') $modInstallPath /MIR

if ($LASTEXITCODE -gt 7) {
    throw "robocopy failed with exit code $LASTEXITCODE"
}
```

`robocopy` exit codes `0` through `7` are success.

`robocopy /MIR` mirrors the source folder to the destination and can delete destination files that are not present in the build output. Use it only when you intentionally want the installed mod folder to match the fresh build.

## 3. Verify JSON export output

Expected output root:

```text
%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\ModsData\CS2DataExport
```

Expected files:

- `snapshots\<yyyyMMdd-HHmmss>.json`
- `latest.json`

Validate `latest.json` contains:

- `schema_version` set to `2.7.0`
- `exported_at_utc` with a fresh timestamp
- `city`, `population`, `education`, `workforce`, `workplaces`, `mobility`, `economy_signals`, `external_connections`, `labor_market_detail`, `official_city_statistics`, `facility_identity`, `company_service_semantics`, `housing_pressure_semantics`, `household_pressure_context`, `labor_pressure_context`, `transit_performance_semantics`, and `transit_line_detail_semantics` groups

Optional quick checks from PowerShell:

```powershell
$latest = Join-Path $env:USERPROFILE 'AppData\LocalLow\Colossal Order\Cities Skylines II\ModsData\CS2DataExport\latest.json'
$snapshot = Get-Content -Raw -LiteralPath $latest | ConvertFrom-Json
"schema_version=$($snapshot.schema_version)"
"mobility.status=$($snapshot.mobility.status)"

$log = Join-Path $env:USERPROFILE 'AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\CS2DataExport.log'
Get-Content -Tail 120 -LiteralPath $log
```

In logs, verify `loaded:` shows `interval_min=10` unless intentionally overridden.

## Command Prompt alternative

If you are using Command Prompt (`cmd.exe`), run the project commands from the folder that contains `CS2DataExport.csproj`:

```bat
cd /d "%USERPROFILE%\Downloads\Cities2-DataExport-main\Cities2-DataExport-main"
set DOTNET_ROLL_FORWARD=Major
rmdir /s /q obj 2>nul
rmdir /s /q bin 2>nul
dotnet build CS2DataExport.csproj -c Release -p:LangVersion=latest
robocopy "%CD%\bin\Release\net48" "%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\CS2DataExport" /MIR
```
