# BP Log Viewer CLI

Structured, AI-optimized console interface for the Blue Prism database (7.5.x).
Direct DB access for error analysis and root cause research — without the slow
official BP CLI.

> **Status:** 📋 Specification phase. 14 feature specs tracked in
> [`specs/README.md`](specs/README.md). Implementation starts after Martin signs
> off the specs.

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

## Documentation

| File | Purpose |
|------|---------|
| [`AGENTS.md`](AGENTS.md) | Project status, blockers, workboard-override decision |
| [`specs/README.md`](specs/README.md) | Feature overview + status (single source of truth) |
| [`specs/architecture.md`](specs/architecture.md) | Architecture spec |
| [`specs/feature-F-*.md`](specs/) | Feature specs (F-001 … F-014) |
| [`context/blue-prism-installation.md`](context/blue-prism-installation.md) | Local Blue Prism installation findings |
| [`context/blue-prism-database-schema.md`](context/blue-prism-database-schema.md) | DB schema findings (213 objects) |

## Project context

This is a small, focused tool. Per Martin (project owner), the OpenClaw
workboard is **deliberately not used** here; status and todo tracking run
exclusively through `specs/README.md`.

## License

_(to be added — currently a personal project, license to be decided before any
external distribution.)_
