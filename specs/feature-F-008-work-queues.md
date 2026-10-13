# Feature-Spec — Work Queues (Liste)

> Listet Work Queues mit aggregierten Item-Counts nach Status.

## Identifikation

- **ID:** F-008
- **Titel:** Work Queues
- **Slug:** work-queues
- **Status:** 📋 Entwurf
- **Priorität:** Mittel
- **Zugewiesen:** —
- **Erstellt am:** 2026-10-13
- **Letzte Aktualisierung:** 2026-10-13

## Ziel / Anwendungsfall

„Welche Queues gibt es, wie viele Items hängen, wie viele sind in Exception?"
Notwendig für Engpass-Diagnose.

## Funktionale Anforderungen

1. Befehl: `bp-log-viewer queue list`.
2. Quelle: `BPAWorkQueue` + aggregierte Counts aus `BPAWorkQueueItem` (GROUP BY `queueid`, `status`).
3. Filter:
   - `--name <substring>` (Suffix-Match; Leading-Wildcard nur mit `--name-contains`)
   - `--only-exceptions` (nur Queues mit `exception > 0`)
   - `--only-running` (`BPAWorkQueue.running = 1`)
4. Sortierung: `name ASC`.
5. Hard-Ceiling auf `--max-rows` wie F-002.
6. jsonl-Felder: `queue_id, name, key_field, running, max_attempts, process_id, item_count_total, item_count_pending, item_count_locked, item_count_completed, item_count_exception, item_count_deferred, oldest_pending_loaded_at, newest_pending_loaded_at, stale (bool, true wenn oldest_pending > 24h)`.

## Nicht-funktionale Anforderungen

- Aggregat-Queries: 1× `COUNT(*) FILTER (WHERE …)` pro Status (oder `CASE WHEN`-Aggregation).
- Single-Query-LEFT-JOIN (architecture § 7.2).
- Antwort < 500 ms bei 100 Queues.

## Akzeptanzkriterien

1. Liste zeigt alle Queues mit `name`, `item_count_*`-Feldern.
2. `--only-exceptions` filtert auf `item_count_exception > 0`.
3. Leere Queue (`item_count_total = 0`) erscheint weiterhin in der Liste.
4. `stale: true` wird automatisch gesetzt bei `oldest_pending_loaded_at` > 24h.
5. Hard-Ceiling greift (Exit 8 bei Überschreitung).

## Out of Scope

- Items herunterladen (siehe F-009).
- Items hinzufügen / statusändern.

## Abhängigkeiten

- F-001, F-011, F-012, F-013.
- architecture § 7.2, § 8.

## Offene Fragen

_(keine — im Review gelöst: `stale: true` als Default-Feld aufgenommen)_

## Verlauf

| Datum | Status | Änderung |
|:------|:-------|:---------|
| 2026-10-13 | 📋 Entwurf | Initial angelegt |
| 2026-10-13 | 📋 Entwurf | C2: Hard-Ceiling; `stale`-Feld als Default aufgenommen |
