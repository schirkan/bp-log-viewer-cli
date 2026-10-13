# Feature-Spec — Processes

> Listet Prozesse mit Verweis auf letzte/aktuelle Session.

## Identifikation

- **ID:** F-007
- **Titel:** Processes
- **Slug:** processes
- **Status:** 📋 Entwurf
- **Priorität:** Mittel
- **Zugewiesen:** —
- **Erstellt am:** 2026-10-13
- **Letzte Aktualisierung:** 2026-10-13

## Ziel / Anwendungsfall

Welche Prozesse sind deployed, wann liefen sie zuletzt, wie oft sind sie
fehlgeschlagen? Hilft der KI bei der Frage „ist Prozess X überhaupt aktiv?".

## Funktionale Anforderungen

1. Befehl: `bp-log-viewer process list`.
2. Quelle: `BPAProcess` + aggregierte Session-Counts aus `BPASession` (LEFT JOIN, GROUP BY).
3. Filter:
   - `--name <substring>` (Suffix-Match, Index-nutzend; Leading-Wildcard nur mit `--name-contains`)
   - `--type <process|object>` (Mapping auf `ProcessType`: `P` / `O`)
   - `--has-errors-since <iso>` (nur Prozesse mit ≥ 1 Exception in dem Zeitraum)
4. Sortierung: `name ASC`.
5. Hard-Ceiling auf `--max-rows` wie F-002.
6. jsonl-Felder: `process_id, name, type, version, last_modified, last_session_number, last_session_started_at, session_count_total, session_count_failed_since, avg_duration_sec`.

## Nicht-funktionale Anforderungen

- **Single-Query-LEFT-JOIN** für „letzte Session pro Prozess" (siehe Implementation Notes).
- `processxml` und `compressedxml` werden **nie** ausgegeben (Größe + Binärformat).
- Aggregat-Queries: 1× Hauptliste, 1× Subquery für Counts. Bei großen Datenmengen Window-Functions bevorzugen.

## Implementation Notes (zwingend)

- **„Letzte Session pro Prozess"** wird als **Single-Query** mit LEFT JOIN auf Window-Function umgesetzt:

  ```sql
  SELECT p.*, last_s.sessionnumber AS last_session_number, last_s.startdatetime AS last_session_started_at,
         (SELECT COUNT(*) FROM BPASession s WHERE s.processid = p.processid) AS session_count_total,
         (SELECT COUNT(*) FROM BPASession s WHERE s.processid = p.processid AND s.exceptiontype IS NOT NULL AND s.startdatetime >= @since) AS session_count_failed_since,
         (SELECT AVG(DATEDIFF(SECOND, s.startdatetime, s.enddatetime)) FROM BPASession s WHERE s.processid = p.processid AND s.enddatetime IS NOT NULL) AS avg_duration_sec
  FROM BPAProcess p
  LEFT JOIN (
      SELECT processid, sessionnumber, startdatetime,
             ROW_NUMBER() OVER (PARTITION BY processid ORDER BY startdatetime DESC) AS rn
      FROM BPASession
  ) last_s ON last_s.processid = p.processid AND last_s.rn = 1
  ```

- Subqueries in SELECT-Liste sind hier **akzeptabel** (Aggregationen, kein N+1 über Rows).
- Implementation muss per `EXPLAIN` verifiziert werden.

## Akzeptanzkriterien

1. Liste enthält alle Prozesse mit `name`, `version`, `last_session_number`.
2. `--has-errors-since 2026-10-12T00:00:00Z` filtert auf Prozesse mit Exceptions in dem Zeitraum.
3. `avg_duration_sec` ist nicht `null` für Prozesse mit abgeschlossenen Sessions.
4. Leere DB (kein Prozess) → leere jsonl + Exit 0.
5. Hard-Ceiling greift (Exit 8 bei Überschreitung).
6. EXPLAIN-Output zeigt **eine** Abfrage, keinen Loop.

## Out of Scope

- Prozess-XML herunterladen / anzeigen (für Studio-Use-Case).
- Prozess deployen / versionieren.

## Abhängigkeiten

- F-001, F-011, F-012, F-013.
- architecture § 7.2 (Single-Query), § 8 (Exit-Codes).

## Offene Fragen

_(keine — im Review gelöst: `avg_duration_sec` als Default-Feld aufgenommen)_

## Verlauf

| Datum | Status | Änderung |
|:------|:-------|:---------|
| 2026-10-13 | 📋 Entwurf | Initial angelegt |
| 2026-10-13 | 📋 Entwurf | C2: Hard-Ceiling; M5: Single-Query-LEFT-JOIN-Pattern; avg_duration_sec als Default-Feld |
