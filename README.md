# BP Log Viewer CLI

Structured, AI-optimized console interface for the Blue Prism database (7.5.x).
Direct DB access for error analysis and root cause research — without the slow
official BP CLI.

> **Status:** ✔️ Ready for use. All planned commands are implemented and tested against
> a real Blue Prism 7.5.x database (LocalDB or remote SQL Server).

## License

[MIT](LICENSE) — Copyright (c) 2026 Martin.

## What this is

A single `.exe` (`.NET 8 self-contained single-file trimmed`, win-x64) that
queries the Blue Prism SQL Server / LocalDB directly and emits results as
[JSON Lines](https://jsonlines.org/) (jsonl) on stdout — one JSON object per
line, ready for AI agents or `jq` to consume.

## Quick start

```bash
bp-log-viewer --version
bp-log-viewer self-test
bp-log-viewer session list --max-rows 50
bp-log-viewer session get 4711
bp-log-viewer session log 4711 --level error --max-rows 200
bp-log-viewer session errors --aggregate --since 2026-10-01T00:00:00Z
bp-log-viewer resource list
bp-log-viewer process list --has-errors-since 2026-10-01T00:00:00Z
bp-log-viewer queue list
bp-log-viewer queue items "MyQueue" --include-tags
bp-log-viewer audit list --code RESSTART
```

All commands emit **jsonl** on stdout by default; warnings and errors go as
jsonl on stderr.

## Command overview

- `version` / `--version`
- `self-test`
- `session list|get|log|errors`
- `resource list`
- `process list`
- `queue list|items`
- `audit list`

Global options (excerpt):
- `--connection-string`
- `--output` / `--out-file`
- `--max-rows`
- `--since` / `--until`
- `--process` / `--resource` / `--status`
- `--text`

## Architecture (short)

- **Runtime:** .NET 8 self-contained single-file trimmed, win-x64
- **DB driver:** `Microsoft.Data.SqlClient` 5.x (`BPA*`, `BPV*`)
- **JSON:** `System.Text.Json` + Source Generator
- **CLI parsing:** manual dispatcher (System.CommandLine workaround)
- **Path resolution:** `PathResolver` anchored at `AppContext.BaseDirectory`

## Publish (single EXE)

Local publish (Windows, self-contained, single-file, trimmed):

```bash
dotnet publish src/BpLogViewer.Cli/BpLogViewer.Cli.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o out/win-x64
```

Artifact: `out/win-x64/bp-log-viewer.exe`

## GitHub Release Build Flow

Workflow: `.github/workflows/release-single-exe.yml`

Trigger:
- Tag push `v*` (e.g. `v0.1.0`)
- optional manual via `workflow_dispatch`

Steps:
1. Restore + Build (`Release`)
2. Publish as single-file EXE (`win-x64`, self-contained, trimmed)
3. ZIP + SHA256
4. GitHub Release: create/update + attach both files

## Configuration

Copy `appsettings.template.json` to `appsettings.json` next to the binary and
fill in `Database.ConnectionString`. The connection string accepts any
standard ADO.NET value (LocalDB, remote SQL Server, integrated auth, SQL
auth — whatever your Blue Prism environment uses).

| File | Purpose |
|------|---------|
| [`AGENTS.md`](AGENTS.md) | Project status and notes for sub-agents |
| [`context/blue-prism-installation.md`](context/blue-prism-installation.md) | Local Blue Prism installation findings |
| [`context/blue-prism-database-schema.md`](context/blue-prism-database-schema.md) | DB schema findings (213 objects) |
| [`appsettings.template.json`](appsettings.template.json) | Configuration template (copy to `appsettings.json`) |

## Testing

A separate test project lives under `tests/BpLogViewer.Cli.IntegrationTests/`.
It requires a real Blue Prism database (LocalDB or remote). The tests do **not**
mock the DB; they connect to a live instance and exercise the repositories and
commands end-to-end.

Setup:

1. Copy `appsettings.template.json` to `src/BpLogViewer.Cli/appsettings.json`
   and point it at your BP database.
2. Build and run the test project:

   ```bash
   dotnet test tests/BpLogViewer.Cli.IntegrationTests/BpLogViewer.Cli.IntegrationTests.csproj
   ```

   By default the tests target the LocalDB instance configured in
   `appsettings.json`. To target a different server, set
   `BP_LOG_VIEWER_TEST_CONNECTION_STRING` in the environment, e.g.:

   ```bash
   export BP_LOG_VIEWER_TEST_CONNECTION_STRING="Server=myserver;Database=BluePrism;Integrated Security=true;TrustServerCertificate=true;Encrypt=false;"
   dotnet test tests/BpLogViewer.Cli.IntegrationTests/BpLogViewer.Cli.IntegrationTests.csproj
   ```

The test project is **not** part of the release build. The release workflow
only builds `src/BpLogViewer.Cli/`, so tests never run during a release.
