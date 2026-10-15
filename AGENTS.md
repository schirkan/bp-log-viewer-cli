# AGENTS.md — BP Log Viewer CLI

## Project

- **Name:** BP Log Viewer CLI
- **Stack:** C# 12 / .NET 8 (self-contained single-file trimmed, win-x64)
- **DB target:** Blue Prism 7.5.x (LocalDB or remote SQL Server, configurable)
- **Owner:** Martin
- **License:** MIT (see `LICENSE`)

## Project-specific rules (Martin directive 2026-10-13)

> **No workboard for this project.**
>
> - Status and todo tracking run exclusively through the repo's docs (this file and the test project).
> - The `PROJECT-RULES.md` section "Workboard" is deliberately ignored here.
> - On first contact / sub-agent start in this project: do **not** use workboard tools.
> - Rationale: small project, direct spec implementation, no multi-session tracking needed.

## Current Status

- All planned commands implemented and verified against a real Blue Prism 7.5.x database.
- Code review pass 1-4 completed; all findings fixed.
- Release workflow (`.github/workflows/release-single-exe.yml`) ships a self-contained single-file EXE + SHA256 as GitHub Release assets on tag `v*`.
- Latest release: see [GitHub Releases](https://github.com/schirkan/bp-log-viewer-cli/releases).

## Git

- **Repo type:** GitHub, public
- **Path / URL:** https://github.com/schirkan/bp-log-viewer-cli
- **Remote(s):** `origin` → https://github.com/schirkan/bp-log-viewer-cli.git
- **Default branch:** `main`
- **Release workflow:** `.github/workflows/release-single-exe.yml` (tag `v*` → single EXE + ZIP + SHA256 + Release assets)

## Architecture highlights

| Aspect | Choice |
|:-------|:-------|
| Runtime | .NET 8 self-contained single-file trimmed, win-x64 |
| DB driver | `Microsoft.Data.SqlClient` 5.x |
| CLI parser | manual dispatcher (System.CommandLine workaround) |
| JSON | `System.Text.Json` + `JsonSerializerContext` (source generator) |
| Path resolution | central `PathResolver`, anchored at `AppContext.BaseDirectory` (env override `BP_LOG_VIEWER_OUTPUT_ROOT`) |
| Auth | Windows Integrated (no password in config) |
| Output default | **jsonl** (newline-delimited JSON, identical to NDJSON) |
| Output size guard | `--output <path>` + threshold + hard ceiling `MaxRowsAbsoluteCeiling` |
| Status lookup | runtime from `BPAStatus` (no hardcoding) |
| Timezone policy | DB = UTC, display = `Europe/Berlin` (env override) |
| External spawn | absolute path expected (callers should not rely on `PATH` lookup) |

## Known limitations

- Blue Prism stores `DateTime` columns in `BPVSessionInfo`, `BPASession`, `BPAResource`, `BPVWorkQueueItem` as `Unspecified` in the local server timezone. We interpret them as UTC (the typical BP server runs in UTC). If your production DB uses a different server timezone, expect a silent ±1h drift on displayed timestamps.

## Implementation notes (from code review, non-blocking)

These points came up during code review and live as ongoing refinements (no blocker for the release):

| # | Topic |
|---|:------|
| IN-1 | `BPAExceptionType` has only 2 columns (`id`, `type`); several places assumed `description`/`detail`. DB column validation per query. |
| IN-2 | `processxml` from `BPAProcess` and `sessionstatexml` from `BPASession` are opt-in via flag (no "never" / no default emission). |
| IN-3 | Tests live in `tests/BpLogViewer.Cli.IntegrationTests/`; they require a real BP database and do not mock the DB. |
| IN-4 | License is MIT, no contributor setup yet (CONTRIBUTING.md, issue templates) — future work. |
| IN-5 | `appsettings.template.json` is the versioned template; local `appsettings.json` (with the real connection string) is gitignored. |
| IN-6 | `audit summary` (aggregated audit events) is a possible future command. |
| IN-7 | Cross-process correlation in error analysis is a possible future command. |
| IN-8 | `process list --avg-duration` is included by default but is only meaningful when ≥ 1 session has completed. |
| IN-9 | `last_session_number` is per process — for very many processes the window function is expensive. EXPLAIN plan verified during implementation. |

## Context documents

| File | Content |
|:------|:-------|
| `context/blue-prism-installation.md` | BP version, install paths, service, registry, LocalDB, auth mode |
| `context/blue-prism-database-schema.md` | DB schema findings: 213 objects, key tables with columns, views, fill state |

## Project files

- `AGENTS.md` (this file)
- `LICENSE` (MIT, versioned)
- `README.md` (usage + license, versioned)
- `.gitignore` (versioned)
- `.gitattributes` (versioned, line endings)
- `appsettings.json` (not versioned, local config with connection string)
- `appsettings.template.json` (versioned, template)
- `context/` (installation and schema findings)
- `src/BpLogViewer.Cli/` (the CLI project)
- `tests/BpLogViewer.Cli.IntegrationTests/` (integration tests, not part of the release build)
