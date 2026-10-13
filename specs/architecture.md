# Architektur — BP Log Viewer CLI

**Stand:** 2026-10-13
**Quelle:** Martin-Vorgabe 2026-10-13 + DB-Befund + geladene .NET-Skills

Diese Datei ist **kein** Feature-Spec, sondern die Architektur, an der sich alle
Feature-Specs (`F-001` … `F-014`) orientieren. Architektur-Änderungen werden hier
in `## Verlauf` dokumentiert; einzelne Features referenzieren die relevanten
Abschnitte.

## 1. Zweck und Aufrufer

| Aspekt | Wert |
|:-------|:-----|
| Zweck | Direkter DB-Zugriff auf die Blue-Prism-Datenbank, um Logs und Session-Informationen **strukturiert und kompakt** für die Fehleranalyse und Ursachenforschung zu liefern |
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
├── README.md                        # Bedienung Kurzfassung (für Operator)
├── DECISIONS.md                     # Architektur-Entscheidungen
├── appsettings.json                 # Connection-String (nicht versionieren)
├── appsettings.template.json        # Vorlage (versioniert)
├── .gitignore
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
      "ConnectionString": "Server=(localdb)\\BluePrismLocalDB;Database=BluePrism;Integrated Security=true;TrustServerCertificate=true;Connect Timeout=5",
      "LocalDbInstance": "BluePrismLocalDB"
    },
    "Output": {
      "DefaultFormat": "jsonl",
      "MaxRowsBeforeFile": 1000
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

## 5. Befehls-Struktur

Kanonisches Pattern (analog zu `git`, `kubectl`, `dotnet`):

```
bp-log-viewer <resource> <action> [options]
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
| `--connection-string <cs>` | überschreibt `appsettings.json` |
| `--output <path>` | schreibt Ergebnis in Datei (statt stdout), siehe F-012 |
| `--format jsonl\|table` | überschreibt `DefaultFormat` (F-013) |
| `--max-rows <n>` | harter Cutoff; Default aus `appsettings.json` |
| `--since <iso>` / `--until <iso>` | Zeit-Filter (F-011) |
| `--process <name>` | Filter auf Prozessname |
| `--resource <name>` | Filter auf Resource-Name |
| `--status <code>` | Filter auf Session-Status (z. B. `4` = Completed with Exceptions) |
| `--has-exception` | nur Sessions mit `exceptiontype IS NOT NULL` |
| `--level <info\|warn\|error\|debug>` | für `session log` (F-011) |
| `--text <substring>` | Volltext-Suche in `result` / `exceptionmessage` (F-011) |

## 6. Output-Format

### 6.1 jsonl (Default)

> **jsonl** = Newline-Delimited JSON, identisch zu NDJSON. Eine JSON-Zeile pro
> Datensatz, UTF-8, kein Whitespace-Padding. Felder immer kleingeschrieben
> (`snake_case`) für KI-Konsumierung.

Beispiel `bp-log-viewer session list --since 2026-10-13T00:00:00Z`:

```json
{"session_number":4711,"session_id":"…","process":"Rechnungslauf","process_id":"…","resource":"robot-01","status":2,"status_name":"Running","started_at":"2026-10-13T08:42:11+01:00","ended_at":null,"duration_sec":null,"exception_type":null,"exception_message":null,"last_stage":"Validation"}
{"session_number":4710,…}
```

### 6.2 Table (für Operator)

Menschlich lesbare Tabelle auf stdout; `--max-rows` greift, danach Abbruch
mit Exit-Code `2` und Hinweis auf `--output`.

### 6.3 Fehler / Warnings

- **Fehler:** ein JSON-Objekt `{"error":{…}}` auf **stderr** + Exit-Code ≠ 0.
  Die KI kann `--format jsonl` mit `2>/dev/null` parsen und Fehler separat
  lesen.
- **Warnings** (z. B. „mehr als N Zeilen, erwäge `--output`") gehen ebenfalls
  nach stderr.

## 7. Datenbank-Zugriff

### 7.1 Connection-Lifecycle

- Pro Aufruf: ein Connection, ein Pool-Roundtrip.
- Pooling aktiv (Default in `Microsoft.Data.SqlClient`).
- Bei Verbindungsfehler: einmaliger Retry, dann klare Fehlermeldung.

### 7.2 Abfrage-Stack

- Für **Listen** → Views (`BPVSessionInfo`, `BPVWorkQueueItem`).
- Für **Details** → gezielt Tabellen + Lookups (`BPAStatus`, `BPAExceptionType`).
- Für **Logs** → `BPASessionLog_NonUnicode` (bevorzugt) bzw. `BPASessionLog_Unicode` (Fallback), je nach Befüllung.
- Niemals `SELECT *` — explizite Spaltenliste, damit JSON-Output stabil bleibt.

### 7.3 Pflicht-Queries (Skizzen)

```sql
-- session list (Beispiel)
SELECT s.sessionnumber, s.sessionid, s.processid, p.name AS processname,
       s.runningresourceid, r.name AS resourcename, s.statusid,
       st.description AS statusname, s.startdatetime, s.enddatetime,
       s.exceptiontype, s.exceptionmessage, s.laststage
FROM BPASession s
LEFT JOIN BPAProcess p      ON p.processid = s.processid
LEFT JOIN BPAResource r     ON r.resourceid = s.runningresourceid
LEFT JOIN BPAStatus   st    ON st.statusid  = s.statusid
WHERE s.startdatetime >= @since
  AND s.enddatetime   <  @until
  AND (@process IS NULL OR p.name = @process)
  AND (@resource IS NULL OR r.name = @resource)
ORDER BY s.startdatetime DESC
OFFSET 0 ROWS FETCH NEXT @max ROWS ONLY;

-- session log
SELECT logid, stagename, stagetype, resulttype, startdatetime, enddatetime,
       LEFT(result, 4000) AS result_preview, attributesize
FROM BPASessionLog_NonUnicode
WHERE sessionnumber = @sessionnumber
  AND (@level IS NULL OR resulttype = @level)
  AND (@text  IS NULL OR result LIKE '%' + @text + '%')
ORDER BY logid;
```

## 8. Fehlerbehandlung

| Szenario | Verhalten |
|:---------|:----------|
| Config-Datei fehlt | Fehler auf stderr, Exit 3 |
| Connection-String leer | Fehler auf stderr, Exit 3 |
| LocalDB läuft nicht | `sqllocaldb start <instanz>` versuchen; falls fehlschlägt → Exit 4 |
| SQL-Verbindung schlägt fehl | Retry 1× mit 500 ms Backoff, dann Exit 5 |
| Query liefert 0 Zeilen | leere jsonl-Ausgabe + Exit 0 |
| Query liefert > `MaxRows` (ohne `--output`) | die ersten `MaxRows` + Warning auf stderr + Exit 2 |
| Output-Pfad nicht beschreibbar | Fehler auf stderr, Exit 6 |
| Unbekannter Fehler | Stacktrace auf stderr (nur wenn `--verbose` gesetzt) + Exit 1 |

## 9. Deployment & Self-Test

- Publish-Befehl siehe § 2.
- `bp-log-viewer self-test` (F-014) führt aus:
  1. Konfig laden
  2. LocalDB starten (falls konfiguriert)
  3. Verbindung aufbauen
  4. `SELECT @@VERSION` ausführen
  5. `SELECT COUNT(*) FROM BPASession` (Erwartung: >= 0, nicht Fehler)
  6. Aktive Log-Tabelle ermitteln (`NonUnicode` vs. `Unicode`)
  7. JSON-Output mit Status pro Schritt
- Standard-Deploy-Pfad: `C:\Program Files\Blue Prism Limited\bp-log-viewer-cli\BpLogViewer.Cli.exe`.
- Eine zentrale `version`-Datei / `--version` reflektiert das Build-Commit (über
  `AssemblyInformationalVersion`).

## 10. Tests

| Ebene | Tooling | Fokus |
|:------|:--------|:------|
| Unit | xUnit + FluentAssertions | PathResolver, Filter-Parsing, Output-Format |
| Integration | xUnit + Testcontainers (LocalDB-Container) | SQL-Queries gegen Schema-Stand |
| End-to-End | PowerShell-Skript | `bp-log-viewer self-test` + `session list` gegen Dev-DB |

## 11. Verlauf

| Datum | Änderung |
|:------|:---------|
| 2026-10-13 | Initialer Entwurf auf Basis von Martins Vorgaben und DB-Befund |
