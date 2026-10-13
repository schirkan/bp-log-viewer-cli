# Architektur — BP Log Viewer CLI

**Stand:** 2026-10-13 (Review-Nachzug: 2026-10-13)
**Quelle:** Martin-Vorgabe 2026-10-13 + DB-Befund + geladene .NET-Skills + code-review (2026-10-13)

Diese Datei ist **kein** Feature-Spec, sondern die Architektur, an der sich alle
Feature-Specs (`F-001` … `F-014`) orientieren. Architektur-Änderungen werden hier
in `## Verlauf` dokumentiert; einzelne Features referenzieren die relevanten
Abschnitte.

## 1. Zweck und Aufrufer

| Aspekt | Wert |
|:-------|:-----|
| Zweck | Direkter DB-Zugriff auf die Blue Prism-Datenbank, um Logs und Session-Informationen **strukturiert und kompakt** für die Fehleranalyse und Ursachenforschung zu liefern |
| Aufrufer (primär) | KI-Agent (OpenClaw / MCP-Host), der das CLI als Skill aufruft |
| Aufrufer (sekundär) | Operator (interaktiv oder im Skript) |
| Nicht-Aufrufer | Blue Prism selbst, andere Anwendungen mit BP-Schnittstelle |

## 2. Technologie-Stack

| Schicht | Wahl | Begründung |
|:--------|:-----|:-----------|
| Sprache | C# 12 / .NET 8 | Martins Vorgabe; LTS bis 2026-11, aktuelle Features (Source-Generator, AOT) |
| Runtime | self-contained, single-file, trimmed | Verteilung als einzelne `.exe` ohne .NET-Installations-Voraussetzung |
| Build | `dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true /p:IncludeNativeLibrariesForSelfExtract=true` | siehe `dotnet-self-contained-redeploy` |
| DB-Zugriff | `Microsoft.Data.SqlClient` 5.x | kompatibel zur BP-eigenen Version 2.x; SqlBulkCopy für große Exporte |
| CLI-Parser | `System.CommandLine` (Microsoft) | deklarativ, gut dokumentiert, Roslyn-Generator statt Reflection |
| JSON | `System.Text.Json` mit `JsonSerializerContext` (Source-Generator) | Trim-fest, kein `Reflection-based serialization` zur Laufzeit |
| Logging (eigenes) | `Microsoft.Extensions.Logging` → **stderr** | NIEMALS nach stdout (würde die KI-Ausgabe zerstören) |
| Config | `Microsoft.Extensions.Configuration` mit `appsettings.json` neben der Binary | DB-Connection-String später überschreibbar via ENV-Vars (`BP_DB_CONNECTION`) |
| Path-Resolution | zentraler `PathResolver` (s. u.) | Pflicht wegen KI-Spawn mit fremdem CWD |

## 3. Projekt-Layout

```
bp-log-viewer-cli/
├── AGENTS.md
├── LICENSE                          # MIT
├── README.md                        # Bedienung Kurzfassung (für Operator)
├── appsettings.json                 # Connection-String (nicht versionieren)
├── appsettings.template.json        # Vorlage (versioniert)
├── .gitignore
├── .gitattributes                   # Line-Endings
├── specs/
│   ├── README.md
│   ├── _TEMPLATE.md
│   ├── architecture.md              # diese Datei
│   ├── feature-F-001-…md
│   └── …
├── context/
│   ├── blue-prism-installation.md
│   └── blue-prism-database-schema.md
├── src/
│   └── BpLogViewer.Cli/
│       ├── BpLogViewer.Cli.csproj
│       ├── Program.cs               # Einstieg, DI-Bootstrap
│       ├── Commands/                # ein File pro Sub-Command
│       ├── Data/                    # DbContext, Repositories
│       ├── Models/                  # POCO/Records
│       ├── Output/                  # Formatter, jsonl, Table
│       ├── Configuration/           # AppSettings + PathResolver
│       └── Infrastructure/          # Logging, File-Writer
├── tests/
│   └── BpLogViewer.Cli.Tests/
└── dist/                            # Publish-Output (nicht versionieren)
```

## 4. Konfiguration

### 4.1 `appsettings.json` (Nicht versionieren)

```json
{
  "BpLogViewer": {
    "Database": {
      "ConnectionString": "REPLACE_WITH_YOUR_CONNECTION_STRING",
      "LocalDbInstance": "BluePrismLocalDB"
    },
    "Output": {
      "DefaultFormat": "jsonl",
      "MaxRowsBeforeFile": 1000,
      "MaxRowsAbsoluteCeiling": 100000
    },
    "Timezone": {
      "DisplayTimezone": "Europe/Berlin"
    },
    "Logging": {
      "LogLevel": {
        "Default": "Information",
        "Microsoft.Data.SqlClient": "Warning"
      }
    }
  }
}
```

| Feld | Bedeutung | Default |
|:-----|:----------|:--------|
| `BpLogViewer.Database.ConnectionString` | vollständiger ADO.NET-Connection-String | — (Pflicht) |
| `BpLogViewer.Database.LocalDbInstance` | optionaler LocalDB-Instanzname; CLI versucht `sqllocaldb start <name>`, falls nicht erreichbar | _(leer)_ |
| `BpLogViewer.Output.DefaultFormat` | `jsonl` \| `table` | `jsonl` |
| `BpLogViewer.Output.MaxRowsBeforeFile` | Schwellwert für Hinweis „Output zu groß, in Datei auslagern" | `1000` |
| `BpLogViewer.Output.MaxRowsAbsoluteCeiling` | **Hard-Cap** für `--max-rows` (OOM-Schutz). `--max-rows` > Ceiling → Exit 8. | `100000` |
| `BpLogViewer.Timezone.DisplayTimezone` | IANA-TZ für Anzeige | `Europe/Berlin` |
| `BpLogViewer.Logging.LogLevel.*` | Microsoft.Extensions.Logging-Level pro Kategorie | siehe oben |

### 4.2 Pfad-Auflösung (PathResolver)

```csharp
public static class PathResolver
{
    public static string Resolve(string? configured, string? baseDir = null)
    {
        baseDir ??= AppContext.BaseDirectory;  // NIEMALS Environment.CurrentDirectory
        if (string.IsNullOrWhiteSpace(configured)) return baseDir;
        return Path.IsPathRooted(configured)
            ? Path.GetFullPath(configured)
            : Path.GetFullPath(Path.Combine(baseDir, configured));
    }
}
```

Wird in **allen** Stellen verwendet, an denen ein Pfad aus der Config oder einem CLI-Argument in einen absoluten Pfad gewandelt wird. Begründung: der KI-Spawn-Prozess hat CWD ≠ Binary-Pfad (siehe `dotnet-config-path-anchoring`-Skill).

### 4.3 Override-Reihenfolge

1. CLI-Argument (z. B. `--connection-string "..."`) — gewinnt immer
2. ENV-Var `BP_DB_CONNECTION` — überschreibt Config
3. `appsettings.json` (Section `BpLogViewer.Database.ConnectionString`)

ENV-Override für `DisplayTimezone`: `BP_LOG_VIEWER_TIMEZONE` (IANA, z. B. `UTC`, `Europe/Berlin`, `America/New_York`).

### 4.4 Timezone-Policy

| Aspekt | Verhalten |
|:-------|:----------|
| Speicherung in DB | UTC (BP-Default) |
| CLI-Default Anzeige | `Europe/Berlin` (`+01:00` Winter / `+02:00` Sommer) |
| ENV-Override | `BP_LOG_VIEWER_TIMEZONE` (IANA) |
| `--since` / `--until` ohne Offset | als **UTC** interpretiert + Warning auf stderr (`filter_timezone_assumed: utc`) |
| `--since` / `--until` mit Offset | wie angegeben (z. B. `2026-10-13T14:00:00+02:00`) |
| Zeitstempel im Output | ISO-8601 mit Offset (`2026-10-13T08:42:11+01:00`) |
| `--since > --until` | Exit 11 (`filter_invalid: since > until`) |
| DB-Spalten `starttimezoneoffset` / `endtimezoneoffset` | zur Anzeige verwendet (BP-eigene TZ-Info) |

### 4.5 Status-Convention (`--status` Code + Name)

Alle Sub-Commands, die `--status` akzeptieren (F-002, F-005, F-006), folgen derselben Konvention:

1. **Code zuerst versuchen:** numerische ID aus `BPAStatus.statusid` (z. B. `--status 4`).
2. **Fallback Name:** exakter Vergleich gegen `BPAStatus.description` (case-insensitive, z. B. `--status "Completed with Exceptions"`).
3. **Wiederholbar:** `--status 2 --status 4` (OR-Verknüpfung).
4. **Unbekannt:** Exit 11 (`error: unknown_status_value: <input>`).
5. **Runtime-Lookup:** `BPAStatus` wird beim Start 1× geladen und gecacht; **kein** Hardcoding der Code-Werte im CLI-Code.

## 5. Befehls-Struktur

Kanonisches Pattern (analog zu `git`, `kubectl`, `dotnet`):

```
bp-log-viewer <resource> <action> [options]
bp-log-viewer version
bp-log-viewer self-test
```

| Resource | Aktionen | Spec |
|:---------|:---------|:-----|
| _(global)_ | `version`, `self-test` | F-014 |
| `session` | `list`, `get`, `log`, `errors` | F-002, F-003, F-004, F-005 |
| `resource` | `list` | F-006 |
| `process` | `list` | F-007 |
| `queue` | `list`, `items` | F-008, F-009 |
| `audit` | `list` | F-010 |

Globale Optionen (jedes Sub-Command akzeptiert sie):

| Option | Bedeutung |
|:-------|:----------|
| `--connection-string <cs>` | überschreibt `appsettings.json` (Override-Reihenfolge § 4.3) |
| `--output <path>` | schreibt Ergebnis in Datei (statt stdout), siehe F-012 |
| `--format jsonl\|table` | überschreibt `DefaultFormat` (F-013) |
| `--max-rows <n>` | Cutoff; Default aus `appsettings.json`; **Hard-Cap** aus `MaxRowsAbsoluteCeiling` |
| `--since <iso>` / `--until <iso>` | Zeit-Filter (F-011), siehe § 4.4 für TZ-Policy |
| `--process <name>` | Filter auf Prozessname |
| `--resource <name>` | Filter auf Resource-Name |
| `--status <code-or-name>` | Status-Filter (Code **oder** Name, siehe § 4.5); wiederholbar |
| `--has-exception` | nur Sessions mit `exceptiontype IS NOT NULL` |
| `--level <info\|warn\|error\|debug>` | für `session log` (F-011) |
| `--text <substring>` | Volltext-Suche in `result` / `exceptionmessage` (F-011) — **Performance-Warning**, siehe § 7.2 |
| `--verbose` | erweiterte Fehlerausgabe (Stacktrace) |

## 6. Output-Format

### 6.1 jsonl (Default)

> **jsonl** = Newline-Delimited JSON, identisch zu NDJSON. Eine JSON-Zeile pro
> Datensatz, UTF-8, kein Whitespace-Padding. Felder immer kleingeschrieben
> (`snake_case`) für KI-Konsumierung.

Beispiel `bp-log-viewer session list --since 2025-06-15T00:00:00Z`:

```json
{"session_number":4711,"session_id":"…","process":"Rechnungslauf","process_id":"…","resource":"robot-01","status":2,"status_name":"Running","started_at":"2025-06-15T08:42:11+01:00","ended_at":null,"duration_sec":null,"exception_type":null,"exception_message":null,"last_stage":"Validation"}
{"session_number":4710,…}
```

### 6.2 Table (für Operator)

Menschlich lesbare Tabelle auf stdout; `--max-rows` greift, danach Abbruch
mit Exit-Code `2` und Hinweis auf `--output`. Spaltenbreite via
`StringInfo.LengthInTextElements` (UTF-8-aware, korrekt für Em-Dash, CJK,
Emoji).

### 6.3 Fehler / Warnings

- **Fehler:** ein JSON-Objekt `{"error":{…}}` auf **stderr** + Exit-Code ≠ 0.
  Die KI kann `--format jsonl` mit `2>/dev/null` parsen und Fehler separat
  lesen.
- **Warnings** (z. B. „mehr als N Zeilen, erwäge `--output`") gehen ebenfalls
  nach stderr, eine pro Zeile als jsonl-Objekt: `{"warning": "<code>", "detail": "<…>"}`.

## 7. Datenbank-Zugriff

### 7.1 Connection-Lifecycle

- Pro Aufruf: ein Connection, ein Pool-Roundtrip.
- Pooling aktiv (Default in `Microsoft.Data.SqlClient`).
- Bei Verbindungsfehler: einmaliger Retry, dann klare Fehlermeldung.
- DB-Permissions: Lesezugriff auf `BPA*` und `BPV*`-Objekte; keine Schreibrechte.

### 7.2 Abfrage-Stack

- Für **Listen** → Views (`BPVSessionInfo`, `BPVWorkQueueItem`).
- Für **Details** → gezielt Tabellen + Lookups (`BPAStatus`, `BPAExceptionType`).
- Für **Logs** → **2-stufige Kette** (siehe F-004 § Tabellenwahl): `BPASessionLog_NonUnicode` (bevorzugt) → `BPASessionLog_Unicode` (Fallback). Erste befüllte Tabelle gewinnt. pre-65-Tabellen (`*_pre65`) werden **nicht** abgefragt (Martin-Vorgabe 2026-10-13).
- Niemals `SELECT *` — explizite Spaltenliste, damit JSON-Output stabil bleibt.
- **Single-Query-LEFT-JOIN** für „aktuelle Session pro Resource" (F-006) und „letzte Session pro Prozess" (F-007): kein Loop mit Einzel-Queries, sondern ein LEFT JOIN auf aggregiertes Subquery. Implementation muss per `EXPLAIN` verifiziert werden.
- **Volltext-Suche** (`--text`) verwendet `LIKE '%text%'` = Full Table Scan. Auf großen Tabellen (`BPASessionLog_NonUnicode` mit Millionen Zeilen) unbrauchbar. v1: harte Warning auf stderr (`text_filter_perf_warning: full table scan, consider v2 fulltext`). v2 (geplant): SQL Server Full-Text Search (`CONTAINS`) als Alternative hinter Flag `--text-fulltext`.
- **Parametrisierte Queries:** alle User-Inputs gehen als SQL-Parameter (`@since`, `@process`, …) rein, **keine** String-Interpolation. Schutz vor SQL-Injection (CWE-89).

### 7.3 Pflicht-Queries (Skizzen)

```sql
-- session list (Default über View, spart LEFT-JOINs in eigenem Code)
SELECT sessionnumber, sessionid, processid, processname,
       runningresourceid, runningresourcename,
       statusid, statusname,
       startdatetime, starttimezoneoffset,
       enddatetime, endtimezoneoffset,
       exceptiontype, exceptionmessage, laststage
FROM BPVSessionInfo
WHERE startdatetime >= @since
  AND startdatetime <  @until
  AND (@process  IS NULL OR processname       = @process)
  AND (@resource IS NULL OR runningresourcename = @resource)
  AND (@status_filter_active = 0 OR statusid IN (SELECT value FROM STRING_SPLIT(@status_csv, ',')))
  AND (@has_exception = 0 OR exceptiontype IS NOT NULL)
ORDER BY startdatetime DESC
OFFSET 0 ROWS FETCH NEXT @max ROWS ONLY;

-- session log (Streaming mit CommandBehavior.SequentialAccess für große result-Spalten)
-- Tabellenwahl via 2-stufiger Kette (siehe F-004); im Code zur Laufzeit ermittelt
SELECT logid, stagename, stagetype, resulttype,
       startdatetime, enddatetime,
       LEFT(result, 4000) AS result_preview, attributesize
FROM @SessionLogTable   -- eine von: BPASessionLog_NonUnicode, BPASessionLog_Unicode
WHERE sessionnumber = @sessionnumber
  AND (@level IS NULL OR resulttype = @level)
  AND (@text  IS NULL OR result LIKE '%' + @text + '%')   -- Performance: siehe § 7.2
ORDER BY logid;
```

## 8. Fehlerbehandlung

| Szenario | Verhalten | Exit-Code |
|:---------|:----------|:---------:|
| Erfolg | OK | 0 |
| Hilfe / `--help` | Output | 0 |
| Output truncated (>`MaxRowsBeforeFile` ohne `--output`) | erste `MaxRows` + Warning | 2 |
| Config-Datei fehlt / invalid | Fehler | 3 |
| LocalDB nicht erreichbar (Start fehlgeschlagen) | Fehler | 4 |
| SQL-Verbindung schlägt fehl (nach Retry) | Fehler | 5 |
| Output-Pfad nicht beschreibbar | Fehler | 6 |
| Output-Pfad ist Verzeichnis | Fehler | 6 |
| MaxRows > `AbsoluteCeiling` | Fehler | 8 |
| Session/Item/Resource nicht gefunden | Fehler + leerer Stream | 10 |
| Filter invalid (z. B. `--since > --until`, ISO-Parse-Fehler, unbekannter Status) | Fehler | 11 |
| Unbekannter Fehler | Stacktrace (nur `--verbose`) | 1 |

Exit-Codes `1`–`11` sind reserviert. Sub-Commands dürfen keinen Code aus diesem Bereich doppelt belegen.

## 9. Deployment & Self-Test

- Publish-Befehl siehe § 2.
- Standard-Deploy-Pfad: `C:\Program Files\Blue Prism Limited\bp-log-viewer-cli\BpLogViewer.Cli.exe`.
- KI-Agent ruft die Binary per absolutem Pfad auf (nicht per `PATH`-Suche), siehe F-014 § KI-Integration.
- `bp-log-viewer self-test` (F-014) führt 8 Diagnose-Schritte aus.
- `bp-log-viewer version` (F-014) gibt Version, Build-Commit, Build-Datum aus — `AssemblyInformationalVersion` wird zur Build-Zeit via `git rev-parse --short HEAD` + `dotnet-version`-GitHub-Action injiziert. Fallback: Hardcoded in `AssemblyInfo.cs` mit manuellem Bump.

## 10. Tests

| Ebene | Tooling | Fokus |
|:------|:--------|:------|
| Unit | xUnit + FluentAssertions | PathResolver, Filter-Parsing, Output-Format, Status-Lookup-Cache |
| Integration | xUnit + Testcontainers (LocalDB-Container) | SQL-Queries gegen Schema-Stand, EXPLAIN-Verifikation |
| End-to-End | PowerShell-Skript | `bp-log-viewer self-test` + `session list` gegen Dev-DB |

## 11. Verlauf

| Datum | Änderung |
|:------|:---------|
| 2026-10-13 | Initialer Entwurf auf Basis von Martins Vorgaben und DB-Befund |
| 2026-10-13 | code-review-Durchgang: 2 CRITICAL + 8 MAJOR + 7 MINOR + 5 NIT identifiziert |
| 2026-10-13 | **C1** MIT-Lizenz + `LICENSE`-Datei + README-Badge |
| 2026-10-13 | **C2** `MaxRowsAbsoluteCeiling` (Default 100 000) in § 4.1 + Exit-Code 8 in § 8 |
| 2026-10-13 | **M1** § 7.3 SQL-Beispiel auf `BPVSessionInfo` umgestellt |
| 2026-10-13 | **M2/M3** Status-Convention § 4.5: Runtime-Lookup von `BPAStatus`, kein Hardcoding |
| 2026-10-13 | **M4** Timezone-Policy § 4.4 + ENV `BP_LOG_VIEWER_TIMEZONE` |
| 2026-10-13 | **M5** Single-Query-LEFT-JOIN-Constraint in § 7.2 + Hinweis in F-006/F-007 |
| 2026-10-13 | **M6** LIKE-Performance-Warning in § 7.2 + Hard-Warning für `--text` |
| 2026-10-13 | **m1** Exit 10 für „not found" (statt 0) in § 8 + F-003 |
| 2026-10-13 | **m3** Exit-Code-Tabelle in § 8 vereinheitlicht (1–11) |
| 2026-10-13 | **m4** UTF-8-aware Spaltenbreite in § 6.2 |
| 2026-10-13 | **n1** Beispiel-Datum in § 6.1 neutralisiert (`2025-06-15`) |
| 2026-10-13 | **n4** `.gitattributes` für konsistente Line-Endings (silence „LF/CRLF"-Warnungen) |
| 2026-10-13 | **Blocker #3** `appsettings.template.json` angelegt (versioniert) |
| 2026-10-13 | **Session-Log: 2-stufige Tabellen-Prioritätskette** (architecture § 7.2 + F-004) — pre-65-Tabellen bewusst weggelassen (Martin-Vorgabe 2026-10-13) |
