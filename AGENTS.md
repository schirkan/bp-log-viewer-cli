# AGENTS.md — BP Log Viewer CLI

## Project

- **Name:** BP Log Viewer CLI
- **Stack:** C# 12 / .NET 8 (framework-dependent single-file, untrimmed, win-x64; requires .NET 8 Runtime x64)
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
- The CLI option vocabulary lives in one place (`src/BpLogViewer.Cli/Configuration/OptionRegistry.cs`); the dispatcher, the filter parser and every command read it from there. New options must be declared there only.
- `session log` emits compact JSONL arrays (header line with the column names, then one array per entry).
- `environment vars` and `environment dependencies` expose the Environment Variables and their process assignment; the option vocabulary lives in `Configuration/OptionRegistry.cs`.
- Environment validation: six variables created with the official Blue Prism CLI `/setev`; value/description round-trip (Unicode, quotes, backslashes and tabs), filters, truncation, file export and the existing process dependency verified. Integration suite: 241 tests passed.
- Console output is UTF-8 without BOM, including redirected JSONL. For `environment vars`, the last name-filter option determines both the filter value and prefix/contains mode.
- Release workflow (`.github/workflows/release-single-exe.yml`) ships a framework-dependent single-file EXE + SHA256 as GitHub Release assets on tag `v*`.
- Latest release: see [GitHub Releases](https://github.com/schirkan/bp-log-viewer-cli/releases).

## Git

- **Repo type:** GitHub, public
- **Path / URL:** https://github.com/schirkan/bp-log-viewer-cli
- **Remote(s):** `origin` → https://github.com/schirkan/bp-log-viewer-cli.git
- **Default branch:** `main`
- **Release workflow:** `.github/workflows/release-single-exe.yml` (tag `v*` → single EXE + ZIP + SHA256 + Release assets)

## CI/CD

- **Platform:** GitHub Actions.
- **Workflow:** `.github/workflows/release-single-exe.yml`.
- **Trigger:** tag push `v*`; optional `workflow_dispatch`.
- **Build:** .NET 8 framework-dependent, untrimmed, single-file Windows x64 CLI.
- **Artifacts:** executable and configuration in a ZIP, plus ZIP SHA256 checksum, attached to GitHub Releases.
- **Last verified run:** 16.11.2026, release `v0.5.0`, source commit `5cd0830`, [run 43959908732](https://github.com/schirkan/bp-log-viewer-cli/actions/runs/43959908732), success.
- **Artifact verification:** downloaded ZIP checksum matched; its executable passed all 11 environment acceptance checks against the real BP database. The source test suite passed 241/241 tests in three consecutive runs.

## Architecture highlights

| Aspect | Choice |
|:-------|:-------|
| Runtime | .NET 8 framework-dependent single-file, untrimmed, win-x64; .NET 8 Runtime x64 required |
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
| IN-3 | Tests live in `tests/BpLogViewer.Cli.IntegrationTests/`; they require a real BP database and do not mock the DB. They run serially (`AssemblyInfo.cs`) because `Console` and environment variables are process-global; parallel runs let one test write into another test's captured buffer. |
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
