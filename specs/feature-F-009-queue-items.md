# Feature-Spec — Work Queue Items

> Liefert die Items einer Work Queue, filterbar nach Status.

## Identifikation

- **ID:** F-009
- **Titel:** Work Queue Items
- **Slug:** queue-items
- **Status:** 📋 Entwurf
- **Priorität:** Mittel
- **Zugewiesen:** —
- **Erstellt am:** 2026-10-13
- **Letzte Aktualisierung:** 2026-10-13

## Ziel / Anwendungsfall

Welche Items sind pending / exception / completed? „Zeig mir alle Items, die
seit gestern in Exception gelaufen sind."

## Funktionale Anforderungen

1. Befehl: `bp-log-viewer queue items <queue-name>` (oder `--queue-id <uuid>`).
2. Quelle: `BPAWorkQueueItem` (bevorzugt) oder `BPVWorkQueueItem` (View).
3. Filter:
   - `--status <pending|locked|completed|exception|deferred>` (wiederholbar)
   - `--since <iso>` / `--until <iso>` (auf `lastupdated`)
   - `--text <substring>` (LIKE auf `keyvalue` und `exceptionreason`)
4. Sortierung: `priority DESC, lastupdated DESC`.
5. Limit per `--max-rows` (Default 1000).
6. NDJSON-Felder: `item_id, ident, key_value, status, attempt, loaded_at, locked_at, completed_at, exception_at, exception_reason, exception_tag, priority, work_time_sec, session_id, last_updated`.

## Nicht-funktionale Anforderungen

- `data` (Item-Daten, oft JSON) wird per Default auf 1000 Zeichen gekürzt (`data_preview`); Volltext per `--full-data` (kann groß sein).

## Akzeptanzkriterien

1. `bp-log-viewer queue items "Rechnungen" --status exception` liefert nur Items im Status `Exception`.
2. `--text "DE-2024"` findet Items mit passender `keyvalue`.
3. `exceptionreason` > 1 KB wird im Output nicht abgeschnitten (Volltext in NDJSON), `data` aber schon.

## Out of Scope

- Items modifizieren.
- Items hinzufügen.

## Abhängigkeiten

- F-001, F-008, F-011, F-012, F-013.

## Offene Fragen

1. Soll zusätzlich `BPAWorkQueueItemTag` mit ausgegeben werden? (Vorschlag: opt-in per `--include-tags`.)

## Verlauf

| Datum | Status | Änderung |
|:------|:-------|:---------|
| 2026-10-13 | 📋 Entwurf | Initial angelegt |
