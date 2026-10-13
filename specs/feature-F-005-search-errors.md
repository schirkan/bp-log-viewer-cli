# Feature-Spec — Search Errors (Ursachenforschung)

> Findet Sessions mit Exceptions, gruppiert nach Exception-Typ für die Ursachenforschung.

## Identifikation

- **ID:** F-005
- **Titel:** Search Errors
- **Slug:** search-errors
- **Status:** 📋 Entwurf
- **Priorität:** Hoch
- **Zugewiesen:** —
- **Erstellt am:** 2026-10-13
- **Letzte Aktualisierung:** 2026-10-13

## Ziel / Anwendungsfall

Direkter Use-Case aus Martins Briefing: **Ursachenforschung**. Aggregiert
fehlerhafte Sessions nach Exception-Typ, damit die KI Muster erkennen kann
(z. B. „AppModelException tritt seit gestern 14× in Prozess X auf Resource Y auf").

## Funktionale Anforderungen

1. Befehl: `bp-log-viewer session errors [--since <iso>] [--until <iso>] [--process <name>] [--resource <name>]`.
2. Zwei Modi:
   - **Liste-Modus** (Default): eine Zeile pro Session mit Exception (gleiches Schema wie F-002, nur gefiltert auf `exceptiontype IS NOT NULL`).
   - **Aggregat-Modus** (`--aggregate`): ein Objekt pro Exception-Typ mit `exception_type, count, first_seen, last_seen, processes[], resources[], sample_session_numbers[]`.
3. Filter wie F-002 + zusätzlich:
   - `--exception-type <name>` (exact match; wiederholbar)
   - `--text <substring>` (LIKE auf `exceptionmessage`)
4. Sortierung Liste: `startdatetime DESC`. Sortierung Aggregat: `count DESC`.

## Nicht-funktionale Anforderungen

- Aggregat-Queries: zwei separate SQLs (1× GROUP BY exceptiontype, 1× TOP-N Sample-Listen).
- `sample_session_numbers` auf max. 5 Werte pro Aggregat-Zeile beschränken.

## Akzeptanzkriterien

1. `bp-log-viewer session errors` liefert die Exceptions der letzten 24h.
2. `--aggregate --since 2026-10-12T00:00:00Z` liefert Aggregat-Objekte.
3. `--exception-type AppModelException` filtert auf exakten Typ.
4. Leere Ergebnisliste (keine Exceptions im Zeitraum) → leere NDJSON + Exit 0.

## Out of Scope

- Cross-Prozess-Correlation (Suche über Sessions, die denselben Work-Queue-Item-Verlauf haben) — eigenes Feature F-016+.
- Root-Cause-Hypothesen generieren (LLM-Aufgabe) — CLI liefert nur Daten.

## Abhängigkeiten

- F-001, F-002 (Session-Listen-Query als Basis), F-011, F-012, F-013.

## Offene Fragen

1. Soll im Aggregat auch `exception_message` (gekürzt) mitgeliefert werden? (Vorschlag: ja, ein repräsentativer Text je Exception-Typ.)
2. Soll `--text` regex-fähig sein? (Vorschlag: nein, LIKE reicht; Regex kommt später.)

## Verlauf

| Datum | Status | Änderung |
|:------|:-------|:---------|
| 2026-10-13 | 📋 Entwurf | Initial angelegt |
