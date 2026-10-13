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
geworfen?". Eine Zeile pro Session, NDJSON-Ausgabe (F-013).

## Funktionale Anforderungen

1. Befehl: `bp-log-viewer session list`.
2. Quelle: `BPVSessionInfo` (spart LEFT-JOINs).
3. Filter (alle optional, kombinierbar):
   - `--since <iso>` / `--until <iso>` (Default: `--since = jetzt - 24h`, `--until = jetzt`)
   - `--process <name>` (exact match auf `processname`)
   - `--resource <name>` (exact match auf `runningresourcename`)
   - `--status <code>` (Session-Status-ID, wiederholtbar; z. B. `--status 2 --status 4`)
   - `--has-exception` (nur Sessions mit `exceptiontype IS NOT NULL`)
4. Sortierung: `startdatetime DESC`.
5. Limit per `--max-rows <n>` (Default aus `appsettings.json`, Default-Wert `1000`).
6. Ausgabe-Felder (NDJSON, snake_case): `session_number, session_id, process, process_id, resource, resource_id, status, status_name, started_at, ended_at, duration_sec, last_stage, exception_type, exception_message`.
7. Zeitstempel in lokaler TZ (Europe/Berlin) mit Offset, generiert aus `startdatetime` + `starttimezoneoffset`.

## Nicht-funktionale Anforderungen

- Query darf nicht `SELECT *` enthalten.
- Indexnutzung: BP-Default-Indizes auf `BPASession.startdatetime` werden angenommen; falls nicht, dokumentieren in `DECISIONS.md`.
- Antwort < 200 ms bei Standard-DB-Größe.

## Akzeptanzkriterien

1. Ohne Filter werden Sessions der letzten 24h zurückgegeben.
2. `--has-exception` filtert auf nicht-leeren `exceptiontype`.
3. `--status 4` liefert nur `Completed with Exceptions`.
4. Leere Ergebnismenge → leere NDJSON-Ausgabe + Exit 0.
5. Ergebnis > `MaxRows` (ohne `--output`) → die ersten `MaxRows` + Warning auf stderr + Exit 2.

## Out of Scope

- Pagination per Cursor (nur Offset/Limit via `--max-rows`).
- Aggregation (Anzahl pro Status etc.) — gehört zu einem eigenen Feature.
- Volltext-Suche im `laststage` — Vorschlag in F-011.

## Abhängigkeiten

- F-001 DB Connection.
- F-011 Common Filters (Filteroptionen sind geteilt).
- F-012 Output to File.
- F-013 Output Format (NDJSON).

## Offene Fragen

1. Soll `--status` auch den menschenlesbaren Namen akzeptieren (`--status running`) oder nur die numerische ID? (Vorschlag: beides, mit Fehler bei Unklarheit.)
2. Soll `--since` auch relative Werte wie `24h`, `7d` akzeptieren? (Vorschlag: nein, ISO-8601 + klare `now-Nh`-Konstanten später.)

## Verlauf

| Datum | Status | Änderung |
|:------|:-------|:---------|
| 2026-10-13 | 📋 Entwurf | Initial angelegt |
