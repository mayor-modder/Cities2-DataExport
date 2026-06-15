# CS2 Data Export

CS2 Data Export is a local data-export mod for Cities: Skylines II.

It writes a readable JSON snapshot of your current city so external tools can analyze what is happening in the simulation: population, education, jobs, workplaces, transit lines, service pressure, housing pressure, economy signals, and other live city context.

The original use case is AI-assisted city analysis, but the output is plain JSON. You can use it with scripts, dashboards, spreadsheets, local tools, or anything else that can read a file.

## What It Does

When the mod is enabled, it periodically exports city data to your local `ModsData` folder.

By default it:

- exports every 10 minutes
- keeps a rolling history of snapshots
- writes a `latest.json` file for tools that only need the newest city state
- leaves unavailable or unproven metrics in the file as `null` with status notes instead of guessing

The mod does not upload data anywhere. It only writes files on your machine.

## Output Location

Default output folder:

```text
%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\ModsData\CS2DataExport
```

Expected files:

```text
latest.json
snapshots\<timestamp>.json
```

`latest.json` is the easiest place to start. It is overwritten whenever a new export succeeds. The `snapshots` folder keeps timestamped historical exports.

## What Is In The Export

Current schema version: `2.7.0`

Top-level data groups include:

- city basics
- population and household signals
- official aggregate city statistics
- education and workforce
- workplaces and labor demand
- mobility and transit line records
- economy signals
- external connection signals
- facility and company/service summaries
- housing and labor pressure context
- transit performance and line-detail summaries
- optional transit access-gap capture results
- export metadata and status notes

Some fields are intentionally marked `partial` or `unavailable`. Cities: Skylines II exposes different kinds of data through different runtime systems, and this mod tries to be honest about what it can prove from the current game build.

For the full machine-readable contract, see [SCHEMA.md](SCHEMA.md).

## Who This Is For

This mod is useful if you want to:

- inspect your city outside the in-game UI
- build a local dashboard
- compare city snapshots over time
- feed city state into an analysis script or LLM workflow
- research how CS2 simulation data changes during play

It is mostly a data bridge. It does not change city simulation behavior.

## Installation

If you are installing a prebuilt copy, place the built mod files in:

```text
%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\CS2DataExport
```

Then launch or reload the game.

After launching or loading a city, check for:

```text
%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\ModsData\CS2DataExport\latest.json
```

If that file exists and has a recent timestamp, the mod is exporting.

## Building From Source

Requirements:

- Windows
- .NET SDK. The CS2 post-processor may also need .NET 6 runtime compatibility; see the roll-forward note below if you only have a newer runtime installed.
- Cities: Skylines II installed
- CS2 modding toolchain initialized by the game. Before building, launch Cities: Skylines II at least once and make sure this folder exists:

```text
%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\.cache\Modding
```

If that folder is missing, initialize or update the CS2 modding tools from the game or launcher first.

Warning: depending on your CS2 modding toolchain, `dotnet build` may automatically copy the built mod into your local `Mods\CS2DataExport` folder. If you already have an installed copy, back it up before building.

### PowerShell

These commands are for PowerShell. If you downloaded the GitHub source zip, Windows may extract it as `Downloads\Cities2-DataExport-main\Cities2-DataExport-main`; the commands below find the project file automatically.

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

$modInstallPath = Join-Path $env:USERPROFILE 'AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\CS2DataExport'
robocopy (Join-Path $PWD 'bin\Release\net48') $modInstallPath /MIR

if ($LASTEXITCODE -gt 7) {
    throw "robocopy failed with exit code $LASTEXITCODE"
}
```

`robocopy` exit codes `0` through `7` are considered success.

`robocopy /MIR` mirrors the source folder to the destination and can delete destination files that are not present in the build output. Use it only when you intentionally want the installed mod folder to match the fresh build.

### Command Prompt

These commands are for Windows Command Prompt (`cmd.exe`).

```bat
cd /d "%USERPROFILE%\Downloads\Cities2-DataExport-main\Cities2-DataExport-main"
set DOTNET_ROLL_FORWARD=Major
rmdir /s /q obj 2>nul
rmdir /s /q bin 2>nul
dotnet build CS2DataExport.csproj -c Release -p:LangVersion=latest
robocopy "%CD%\bin\Release\net48" "%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\CS2DataExport" /MIR
```

`robocopy` exit codes `0` through `7` are considered success.

`robocopy /MIR` mirrors the source folder to the destination and can delete destination files that are not present in the build output. Use it only when you intentionally want the installed mod folder to match the fresh build.

If the CS2 mod post-processor fails with a message about `Microsoft.NETCore.App 6.0.0`, make sure `DOTNET_ROLL_FORWARD` is set in the same terminal before running `dotnet build`:

```bat
set DOTNET_ROLL_FORWARD=Major
dotnet build CS2DataExport.csproj -c Release -p:LangVersion=latest
```

More detailed Windows install notes are in [INSTALL.md](INSTALL.md).

## Project Layout

- `Mod.cs`: mod entry point and lifecycle wiring
- `DataExportSystem.cs`: export scheduling and orchestration
- `MetricsCollector.cs`: snapshot assembly and status rollup
- `RuntimeEcsMetricProbe.cs`: live ECS-backed metric collection
- `SnapshotWriter.cs`: JSON writing, `latest.json`, and snapshot retention
- `ExportSettings.cs`: export cadence, retention, and output paths
- `CitySnapshotV1.cs`: JSON DTO contract
- `SCHEMA.md`: schema notes for downstream parsers
- `INSTALL.md`: Windows build and install notes

## Compatibility

Cities: Skylines II patches can change internal ECS components, systems, or modding toolchain behavior. After a game update, rebuild the mod and check that `latest.json` is still being written before relying on the export.

The current public schema target is `2.7.0`. Additive fields may appear over time. Breaking schema changes should be reflected by a schema version update.

## Status

This project is still evolving. The most stable parts are the local file output, snapshot structure, and core city/workforce/transit summaries. Some deeper semantic groups are best treated as research-grade signals with explicit status notes.
