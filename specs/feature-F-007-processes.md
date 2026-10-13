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
   - `--name <substring>` (LIKE auf `name`)
   - `--type <process|object>` (Mapping auf `ProcessType`: `P` / `O`)
   - `--has-errors-since <iso>` (nur Prozesse mit ≥ 1 Exception in dem Zeitraum)
4. Sortierung: `name ASC`.
5. NDJSON-Felder: `process_id, name, type, version, last_modified, last_session_number, last_session_started_at, session_count_total, session_count_failed_since`.

## Nicht-funktionale Anforderungen

- Aggregat-Queries: 1× Hauptliste, 1× Subquery für Counts. Bei großen Datenmengen Window-Functions bevorzugen.
- `processxml` und `compressedxml` werden **nie** ausgegeben.

## Akzeptanzkriterien

1. Liste enthält alle Prozesse mit `name`, `version`, `last_session_number`.
2. `--has-errors-since 2026-10-12T00:00:00Z` filtert auf Prozesse mit Exceptions in dem Zeitraum.
3. Leere DB (kein Prozess) → leere NDJSON + Exit 0.

## Out of Scope

- Prozess-XML herunterladen / anzeigen (für Studio-Use-Case).
- Prozess deployen / versionieren.

## Abhängigkeiten

- F-001, F-011, F-012, F-013.

## Offene Fragen

1. Soll auch die durchschnittliche Laufzeit pro Prozess ausgewiesen werden? (Vorschlag: ja, Feld `avg_duration_sec`.)

## Verlauf

| Datum | Status | Änderung |
|:------|:-------|:---------|
| 2026-10-13 | 📋 Entwurf | Initial angelegt |
