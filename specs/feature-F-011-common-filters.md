# Feature-Spec — Common Filters

> Gemeinsame Filteroptionen für alle Lese-Kommandos.

## Identifikation

- **ID:** F-011
- **Titel:** Common Filters
- **Slug:** common-filters
- **Status:** 📋 Entwurf
- **Priorität:** Hoch
- **Zugewiesen:** —
- **Erstellt am:** 2026-10-13
- **Letzte Aktualisierung:** 2026-10-13

## Ziel / Anwendungsfall

Einheitliches Filter-Verhalten über alle Sub-Commands (`session`, `resource`,
`process`, `queue`, `audit`). KI muss nicht pro Sub-Command neue Filter-Syntax
lernen.

## Funktionale Anforderungen

| Option | Typ | Default | Bedeutung |
|:-------|:----|:--------|:----------|
| `--since <iso>` | ISO-8601 (UTC oder mit Offset) | `now-24h` (für `session`) / `now-7d` (für `audit`) | Start-Zeitpunkt |
| `--until <iso>` | ISO-8601 | `now` | End-Zeitpunkt |
| `--process <name>` | string | — | exakter Prozess-Name |
| `--resource <name>` | string | — | exakter Resource-Name |
| `--status <code-or-name>` | int oder string | — | Session/Resource-Status (Code **oder** Name), wiederholbar — siehe architecture § 4.5 / F-001 |
| `--has-exception` | flag | false | nur Sessions mit `exceptiontype IS NOT NULL` |
| `--level <info\|warn\|error\|debug>` | enum | — | `resulttype` im Log (Runtime-Mapping, F-004) |
| `--text <substring>` | string | — | LIKE-Filter (siehe sub-command-spezifische Spalten); **Performance-Warning** (architecture § 7.2) |
| `--max-rows <n>` | int | aus Config | Cutoff, **Hard-Ceiling** `MaxRowsAbsoluteCeiling` (Exit 8 bei Überschreitung) |
| `--output <path>` | path | — | schreibt in Datei statt stdout (F-012) |
| `--format <jsonl\|table>` | enum | `jsonl` | Output-Format (F-013) |
| `--connection-string <cs>` | string | aus `appsettings.json` | überschreibt DB-Connection (architecture § 4.3) |
| `--verbose` | flag | false | erweiterte Fehlerausgabe (Stacktrace) |

## Override-Reihenfolge (Cross-Cutting)

1. CLI-Argument
2. ENV-Var (`BP_DB_CONNECTION` für Connection-String; `BP_LOG_VIEWER_TIMEZONE` für Display-TZ; `BP_LOG_VIEWER_OUTPUT_ROOT` für `--output`-Roots; `BP_LOG_VIEWER_LOG_LEVEL` für Log-Level)
3. `appsettings.json`

## Nicht-funktionale Anforderungen

- ISO-8601-Eingaben akzeptieren sowohl `2026-10-13T12:00:00Z` (UTC) als auch `2026-10-13T14:00:00+02:00` (mit Offset). Ohne Offset → UTC-Annahme + Warning (architecture § 4.4).
- Filter-Konflikte werden erkannt:
  - **Harte Fehler** (Exit 11): `--since > --until`, ISO-Parse-Fehler, unbekannter Status.
  - **Warnings** (nur stderr): widersprüchliche Kombinationen (z. B. `--has-exception + --status 3`).
- Alle Filter-Werte werden vor der SQL-Übergabe validiert (CWE-89-Schutz via parametrisierte Queries).

## Akzeptanzkriterien

1. Alle Lese-Kommandos akzeptieren mindestens `--since`, `--until`, `--max-rows`.
2. `--has-exception --status 3` erzeugt eine Warning, bricht aber nicht ab.
3. `--since > --until` → Exit 11 (`filter_invalid`).
4. Ungültiges ISO-Datum in `--since` → Exit 11 (`filter_invalid: parse error`).
5. `--max-rows 500000` → Exit 8 (`max_rows_exceeds_ceiling`).
6. `--text` aktiv → Warning `text_filter_perf_warning` auf stderr.

## Out of Scope

- Reguläre Ausdrücke (Vorschlag F-018).
- Saved filters / Filter-Presets.
- SQL Server Full-Text Search (`--text-fulltext`) — v2.

## Abhängigkeiten

- Wird von F-002 … F-010 referenziert.
- architecture § 4.3 (Override-Reihenfolge), § 4.4 (Timezone-Policy), § 4.5 (Status-Convention), § 5 (Globale Optionen), § 8 (Exit-Codes).

## Offene Fragen

_(keine — im Review gelöst: ENV-Override-Tabelle ergänzt)_

## Verlauf

| Datum | Status | Änderung |
|:------|:-------|:---------|
| 2026-10-13 | 📋 Entwurf | Initial angelegt |
| 2026-10-13 | 📋 Entwurf | C2: Hard-Ceiling in Tabelle; m6: ENV-Override-Tabelle ergänzt; m5: striktere Filter-Conflict-Erkennung |
