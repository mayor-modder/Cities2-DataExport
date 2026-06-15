# Install (Windows)

## 1. Build the mod

```bat
cd %USERPROFILE%\Downloads\CS2-DataExport
rmdir /s /q obj 2>nul
rmdir /s /q bin 2>nul
dotnet build -c Release -p:LangVersion=latest
```

## 2. Copy build output into CS2 Mods

```bat
robocopy "%CD%\bin\Release\net48" "%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\CS2DataExport" /MIR
```

`robocopy` exit codes `0` through `7` are success.

## 3. Enable in game

Enable `CS2DataExport` from the mod list.

## 4. Verify JSON export output

Expected output root:

`C:\Users\<YourUser>\AppData\LocalLow\Colossal Order\Cities Skylines II\ModsData\CS2DataExport\`

Expected files:

- `snapshots\<yyyyMMdd-HHmmss>.json`
- `latest.json`

Validate `latest.json` contains:

- `schema_version` set to `2.7.0`
- `exported_at_utc` with a fresh timestamp
- `city`, `population`, `education`, `workforce`, `workplaces`, `mobility`, `economy_signals`, `external_connections`, `labor_market_detail`, `official_city_statistics`, `facility_identity`, `company_service_semantics`, `housing_pressure_semantics`, `household_pressure_context`, `labor_pressure_context`, `transit_performance_semantics`, and `transit_line_detail_semantics` groups

Optional quick checks:

```bat
set "MODDATA=%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\ModsData\CS2DataExport"
powershell -NoProfile -Command "$p=Join-Path $env:USERPROFILE 'AppData\LocalLow\Colossal Order\Cities Skylines II\ModsData\CS2DataExport\latest.json'; $j=Get-Content -Raw -Path $p | ConvertFrom-Json; 'schema_version=' + $j.schema_version; 'mobility.status=' + $j.mobility.status"
powershell -NoProfile -Command "Get-Content -Tail 120 -Path (Join-Path $env:USERPROFILE 'AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\CS2DataExport.log')"
```

In logs, verify `loaded:` shows `interval_min=10` unless intentionally overridden.
