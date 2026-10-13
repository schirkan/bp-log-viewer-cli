# Feature-Spec — Resources

> Listet Blue-Prism-Roboter-/Runtime-Instanzen mit aktuellem Zustand.

## Identifikation

- **ID:** F-006
- **Titel:** Resources
- **Slug:** resources
- **Status:** 📋 Entwurf
- **Priorität:** Mittel
- **Zugewiesen:** —
- **Erstellt am:** 2026-10-13
- **Letzte Aktualisierung:** 2026-10-13

## Ziel / Anwendungsfall

Welche Roboter gibt es, welchen Status haben sie, was läuft gerade?
Notwendig für „warum lief Prozess X auf Resource Y nicht?".

## Funktionale Anforderungen

1. Befehl: `bp-log-viewer resource list`.
2. Quelle: `BPAResource` (alle Spalten außer `processesrunning`/`actionsrunning` werden joined mit aktueller Session aus `BPASession` wo `runningresourceid = BPAResource.resourceid AND enddatetime IS NULL`).
3. Filter:
   - `--status <available|running|disconnected|…>` (Mapping auf `DisplayStatus`)
   - `--name <substring>` (LIKE auf `name`)
4. Sortierung: `name ASC`.
5. NDJSON-Felder: `resource_id, name, display_status, status_id, processes_running, actions_running, last_updated, last_logged_in, current_session_number, current_session_started_at, fqdn`.

## Nicht-funktionale Anforderungen

- Antwort < 200 ms.
- Kein Schreibzugriff.

## Akzeptanzkriterien

1. Liste enthält alle registrierten Resources.
2. `current_session_number` ist `null`, wenn keine Session aktiv läuft.
3. `--status disconnected` filtert auf `DisplayStatus = 'Disconnected'`.

## Out of Scope

- Resource starten / stoppen.
- Konfiguration der Resource-Pools / Controller.

## Abhängigkeiten

- F-001, F-011, F-012, F-013.

## Offene Fragen

1. Soll `--status` auch numerische ID akzeptieren? (Vorschlag: ja, identisch zu F-002.)

## Verlauf

| Datum | Status | Änderung |
|:------|:-------|:---------|
| 2026-10-13 | 📋 Entwurf | Initial angelegt |
