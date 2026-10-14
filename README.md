# BP Log Viewer CLI

Structured, AI-optimized console interface for the Blue Prism database (7.5.x).
Direct DB access for error analysis and root cause research — without the slow
official BP CLI.

> **Status:** 📋 Specification phase. 14 feature specs tracked in
> [`specs/README.md`](specs/README.md). Implementation starts after Martin signs
> off the specs.

## License

[MIT](LICENSE) — Copyright (c) 2026 Martin.

## What this is

A single `.exe` (`.NET 8 self-contained single-file trimmed`, win-x64) that
queries the Blue Prism SQL Server / LocalDB directly and emits results as
[JSON Lines](https://jsonlines.org/) (jsonl) on stdout — one JSON object per
line, ready for AI agents or `jq` to consume.

## Quick start (post-implementation)

```bash
bp-log-viewer self-test                          # sanity check (config, DB, tables)
bp-log-viewer session list --since 2026-10-01    # recent sessions
bp-log-viewer session log 4711                   # log of a session
bp-log-viewer session log 4711 --output big.json # large logs → file
bp-log-viewer session errors --aggregate         # root-cause research
```

All sub-commands emit jsonl on stdout, errors and warnings on stderr.

## Architecture

- **Runtime:** .NET 8 self-contained single-file trimmed, win-x64
- **DB driver:** `Microsoft.Data.SqlClient` 5.x against `BPASession*` tables and `BPV*` views
- **JSON:** `System.Text.Json` with source generator (trim-safe)
- **CLI parser:** `System.CommandLine`
- **Path resolution:** central `PathResolver` anchored on `AppContext.BaseDirectory` (the AI agent spawns the binary with a foreign CWD)

See [`specs/architecture.md`](specs/architecture.md) for the full picture.

## Publish (Single EXE)

Lokaler Publish (Windows, self-contained, single-file, trimmed):

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

Artefakt: `out/win-x64/bp-log-viewer.exe`

## GitHub Release Build Flow

Workflow: `.github/workflows/release-single-exe.yml`

Trigger:
- Tag-Push `v*` (z. B. `v0.2.0`)
- optional manuell via `workflow_dispatch`

Ablauf:
1. Restore + Build (`Release`)
2. Publish als single-file EXE (`win-x64`, self-contained, trimmed)
3. ZIP + SHA256 erzeugen
4. GitHub Release erstellen/aktualisieren und beide Dateien anhängen

## Documentation

| File | Purpose |
|------|---------|
| [`AGENTS.md`](AGENTS.md) | Project status, blockers, workboard-override decision |
| [`specs/README.md`](specs/README.md) | Feature overview + status (single source of truth) |
| [`specs/architecture.md`](specs/architecture.md) | Architecture spec |
| [`specs/feature-F-*.md`](specs/) | Feature specs (F-001 … F-014) |
| [`context/blue-prism-installation.md`](context/blue-prism-installation.md) | Local Blue Prism installation findings |
| [`context/blue-prism-database-schema.md`](context/blue-prism-database-schema.md) | DB schema findings (213 objects) |
| [`appsettings.template.json`](appsettings.template.json) | Configuration template (copy to `appsettings.json`) |

## Project context

This is a small, focused tool. Per Martin (project owner), the OpenClaw
workboard is **deliberately not used** here; status and todo tracking run
exclusively through `specs/README.md`.
