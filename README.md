# BP Log Viewer CLI

Structured, AI-optimized console interface for the Blue Prism database (7.5.x).
Direct DB access for error analysis and root cause research — without the slow
official BP CLI.

> **Status:** ✔️ Ready for use. All planned commands are implemented and tested against
> a real Blue Prism 7.5.x database (LocalDB or remote SQL Server).

## License

[MIT](LICENSE) — Copyright (c) 2026 Martin.

## What this is

A single `.exe` (`.NET 8 framework-dependent single-file`, win-x64) that
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
jsonl on stderr. Printable Unicode characters (such as `ü`) are emitted directly
rather than as `\uXXXX` escapes; JSON control characters remain escaped.

## Session log output

`session log` emits compact JSONL arrays instead of objects. The first line is
always the column header, followed by one array per log entry in the same order:

```jsonl
["log_id","started_at","stage_name","stage_type","result_type","result_type_name","result_preview","attribute_size_bytes"]
[18398968,"2026-10-26T08:55:25.263+00:00","SuchTab filled?",4,8,"Unknown(8)","False",0]
```

Missing values remain `null` to preserve column positions. `source_table`,
`ended_at`, and `duration_ms` are not included. The header is also emitted for
empty results and written into the file with `--output`; `--max-rows` and the
`row_count` marker count only data rows. `--full-result` still controls the text
in the `result_preview` column. Warnings remain JSON objects on stderr.
Other commands retain their existing JSON object output.

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

## Agent skill and full CLI reference

The companion agent and skill live in a separate repository:
[**schirkan/BP-Agents**](https://github.com/schirkan/BP-Agents).

- [BP Log Viewer CLI skill](https://github.com/schirkan/BP-Agents/blob/main/.github/skills/bp-log-viewer-cli/SKILL.md) — investigation workflow for agents
- [Complete parameter reference (German)](https://github.com/schirkan/BP-Agents/blob/main/.github/skills/bp-log-viewer-cli/references/cli-reference.md) — every implemented command, filter scope, examples, configuration, output handling, exit codes, and current limitations
- [Blue Prism Operator agent](https://github.com/schirkan/BP-Agents/blob/main/.github/agents/Blue%20Prism%20Operator.agent.md) — uses this skill for error analysis

## Architecture (short)

- **Runtime:** .NET 8 framework-dependent single-file, win-x64; requires .NET 8 Runtime x64
- **DB driver:** `Microsoft.Data.SqlClient` 5.x (`BPA*`, `BPV*`)
- **JSON:** `System.Text.Json` + Source Generator
- **CLI parsing:** manual dispatcher (System.CommandLine workaround)
- **Path resolution:** `PathResolver` anchored at `AppContext.BaseDirectory`

## Publish (single EXE)

Local publish (Windows, framework-dependent, single-file):
The target system must have the .NET 8 Runtime (x64) installed; .NET Framework 4.x
is not sufficient. The runtime is not bundled, reducing the executable size.
Globalization support remains enabled because `Microsoft.Data.SqlClient` requires
named cultures such as `en-US`; globalization-invariant mode is not supported.
If the target system reports `Only the invariant culture is supported`, replace
older executables with a build published from the current project settings (do
not override `InvariantGlobalization` to `true`). Check the target environment
for `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT`; it must not enable invariant mode.
This error is unrelated to the database credentials or the process filters.
For example, `process list --type process --name-contains Health --max-rows 50`
filters process names containing `Health`; these are supported command options.
Trimming is disabled because the current SqlClient dependency causes a
`TypeLoadException` during TLS login in the trimmed build.

```bash
dotnet publish src/BpLogViewer.Cli/BpLogViewer.Cli.csproj \
  -c Release \
  -r win-x64 \
  --self-contained false \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=false \
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
2. Publish as single-file EXE (`win-x64`, framework-dependent, untrimmed)
3. ZIP + SHA256
4. GitHub Release: create/update + attach both files

## Session timestamps

Session datetimes are interpreted as **UTC** and converted to the Blue Prism offset
in **seconds** (`7200` → `+02:00`, `3600` → `+01:00`); the offset is not merely
attached to the stored clock time. For example, stored `11:13 UTC` with `7200`
is displayed as `13:13+02:00`. Session logs remain in UTC (`+00:00`); their instants
and UTC list filters match the localized session timestamps. Invalid offsets are
reported as errors, not silently clamped to `±14:00`.

## Configuration

Copy `appsettings.template.json` to `appsettings.json` next to the binary and
fill in `Database.ConnectionString`. The connection string accepts any
standard ADO.NET value (LocalDB, remote SQL Server, integrated auth, SQL
auth — whatever your Blue Prism environment uses).
The connection target is determined solely by `Database.ConnectionString`.
The CLI does not invoke `sqllocaldb start`; `self-test` checks the connection directly.

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
