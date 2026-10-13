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
2. Quelle: `BPVWorkQueueItem` (View, spart LEFT-JOINs).
3. Filter:
   - `--status <pending|locked|completed|exception|deferred>` (wiederholbar; **Name** statt Code, da String-Status)
   - `--since <iso>` / `--until <iso>` (auf `lastupdated`)
   - `--text <substring>` (LIKE auf `keyvalue` und `exceptionreason` — **Performance-Warning**)
4. Sortierung: `priority DESC, lastupdated DESC`.
5. Hard-Ceiling auf `--max-rows` wie F-002.
6. jsonl-Felder: `item_id, ident, key_value, status, attempt, loaded_at, locked_at, completed_at, exception_at, exception_reason, exception_tag, priority, work_time_sec, session_id, last_updated, data_preview (data auf 1000 Zeichen gekürzt)`.

## Nicht-funktionale Anforderungen

- `data` (Item-Daten, oft JSON) wird per Default auf 1000 Zeichen gekürzt (`data_preview`); Volltext per `--full-data` (kann groß sein).
- Single-Query (architecture § 7.2).
- Parametrisierte Queries (CWE-89-Schutz).

## Akzeptanzkriterien

1. `bp-log-viewer queue items "Rechnungen" --status exception` liefert nur Items im Status `Exception`.
2. `--text "DE-2024"` findet Items mit passender `keyvalue` + Performance-Warning.
3. `exceptionreason` wird im Output nicht abgeschnitten, `data` aber schon (auf 1000 Zeichen).
4. Hard-Ceiling greift (Exit 8 bei Überschreitung).
5. `--include-tags` fügt `tags: [...]` aus `BPAWorkQueueItemTag` hinzu.

## Out of Scope

- Items modifizieren.
- Items hinzufügen.

## Abhängigkeiten

- F-001, F-008, F-011, F-012, F-013.
- architecture § 7.2, § 8.

## Offene Fragen

_(keine — im Review gelöst: `--include-tags` als Default-Flag aufgenommen)_

## Verlauf

| Datum | Status | Änderung |
|:------|:-------|:---------|
| 2026-10-13 | 📋 Entwurf | Initial angelegt |
| 2026-10-13 | 📋 Entwurf | C2: Hard-Ceiling; `--include-tags` als Default-Flag |
