# Feature-Spec — Session List

> Listet Sessions aus `BPVSessionInfo` mit Standard-Filtern (Zeit, Prozess, Resource, Status, Exception).

## Identifikation

- **ID:** F-002
- **Titel:** Session List
- **Slug:** session-list
- **Status:** 📋 Entwurf
- **Priorität:** Hoch
- **Zugewiesen:** —
- **Erstellt am:** 2026-10-13
- **Letzte Aktualisierung:** 2026-10-13

## Ziel / Anwendungsfall

Schneller Überblick über jüngste oder gefilterte Sessions. Primärer Use-Case
für die KI: „Welche Sessions sind in den letzten 24h gelaufen / haben Exceptions
geworfen?". Eine Zeile pro Session, jsonl-Ausgabe (F-013).

## Funktionale Anforderungen

1. Befehl: `bp-log-viewer session list`.
2. Quelle: `BPVSessionInfo` (spart LEFT-JOINs, siehe architecture § 7.2).
3. Filter (alle optional, kombinierbar):
   - `--since <iso>` / `--until <iso>` (Default: `--since = jetzt - 24h` UTC, `--until = jetzt` UTC)
   - `--process <name>` (exact match auf `processname`)
   - `--resource <name>` (exact match auf `runningresourcename`)
   - `--status <code-or-name>` (wiederholbar; Code **oder** Name, siehe F-001 § `--status`-Convention und architecture § 4.5)
   - `--has-exception` (nur Sessions mit `exceptiontype IS NOT NULL`)
4. Sortierung: `startdatetime DESC`.
5. Limit per `--max-rows <n>` (Default aus `appsettings.json`, Default-Wert `1000`).
   **Hard-Ceiling:** `--max-rows` darf `BpLogViewer.Output.MaxRowsAbsoluteCeiling` (Default 100 000, siehe architecture § 4.1) nicht überschreiten. Bei Überschreitung → Exit 8 (`error: max_rows_exceeds_ceiling: <n> > <ceiling>`).
6. Ausgabe-Felder (jsonl, snake_case): `session_number, session_id, process, process_id, resource, resource_id, status, status_name, started_at, ended_at, duration_sec, last_stage, exception_type, exception_message`.
7. Zeitstempel: ISO-8601 mit Offset (Default `Europe/Berlin` per § 4.4), generiert aus `startdatetime` + `starttimezoneoffset`.

## Nicht-funktionale Anforderungen

- Query darf nicht `SELECT *` enthalten (architecture § 7.2).
- **Parametrisierte Queries** (CWE-89-Schutz, architecture § 7.2).
- Indexnutzung: BP-Default-Indizes auf `BPASession.startdatetime` werden angenommen; falls nicht, in `DECISIONS.md` dokumentieren.
- Antwort < 200 ms bei Standard-DB-Größe (1 Mio. Sessions).

## Filter-Conflict-Erkennung

| Konflikt | Verhalten |
|:---------|:----------|
| `--since > --until` | Exit 11 (`filter_invalid: since > until`) |
| `--has-exception` + `--status <code>` das Completed ohne Exception impliziert (z. B. `3`) | Warning auf stderr (`filter_warning: contradictory`), Ausführung erfolgt |
| `--status <unknown>` (weder Code noch Name in `BPAStatus`) | Exit 11 (`filter_invalid: unknown_status_value`) |
| `--since` ohne Offset | UTC-Annahme + Warning `filter_timezone_assumed: utc` (architecture § 4.4) |
| `--text` aktiv | Warning `text_filter_perf_warning: full table scan` auf stderr (architecture § 7.2) |

## Akzeptanzkriterien

1. Ohne Filter werden Sessions der letzten 24h zurückgegeben.
2. `--has-exception` filtert auf nicht-leeren `exceptiontype`.
3. `--status 4` liefert nur `Completed with Exceptions`.
4. `--status "Completed with Exceptions"` (Name-Variante) liefert dieselbe Ergebnismenge.
5. Leere Ergebnismenge → leere jsonl-Ausgabe + Exit 0.
6. Ergebnis > `MaxRowsBeforeFile` (ohne `--output`) → die ersten `MaxRows` + Warning + Exit 2.
7. `--max-rows 500000` → Exit 8 (überschreitet Ceiling 100 000).

## Out of Scope

- Pagination per Cursor (nur Offset/Limit via `--max-rows`).
- Aggregation (Anzahl pro Status etc.) — gehört zu einem eigenen Feature.
- Volltext-Suche im `laststage` — Vorschlag in F-011.
- SQL Server Full-Text Search (`--text-fulltext`) — v2.

## Abhängigkeiten

- F-001 DB Connection.
- F-011 Common Filters (Filteroptionen sind geteilt).
- F-012 Output to File.
- F-013 Output Format (jsonl).
- architecture § 4.4 (Timezone), § 4.5 (Status-Convention), § 7.3 (Pflicht-Query), § 8 (Exit-Codes).

## Offene Fragen

_(keine — im Review gelöst)_

## Verlauf

| Datum | Status | Änderung |
|:------|:-------|:---------|
| 2026-10-13 | 📋 Entwurf | Initial angelegt |
| 2026-10-13 | 📋 Entwurf | NDJSON → jsonl |
| 2026-10-13 | 📋 Entwurf | C2: Hard-Ceiling + Exit 8; m5: striktere Filter-Conflict-Erkennung; M2/M3: Status-Convention per architecture § 4.5 |
