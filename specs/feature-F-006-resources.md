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
   - `--status <available|running|disconnected|…>` (Name **oder** Code via `BPAStatus`, siehe F-001 § `--status`-Convention und architecture § 4.5)
   - `--name <substring>` (LIKE auf `name`; ohne Leading-Wildcard wenn möglich, siehe Implementation Notes)
4. Sortierung: `name ASC`.
5. Hard-Ceiling auf `--max-rows` wie F-002.
6. jsonl-Felder: `resource_id, name, display_status, status_id, processes_running, actions_running, last_updated, last_logged_in, current_session_number, current_session_started_at, fqdn`.

## Nicht-funktionale Anforderungen

- Antwort < 200 ms.
- Kein Schreibzugriff.
- **Single-Query-LEFT-JOIN** für „aktuelle Session pro Resource" (siehe Implementation Notes).

## Implementation Notes (zwingend)

- **„Aktuelle Session pro Resource"** wird als **Single-Query** umgesetzt:

  ```sql
  SELECT r.*, cur.sessionnumber AS current_session_number, cur.startdatetime AS current_session_started_at
  FROM BPAResource r
  LEFT JOIN (
      SELECT runningresourceid, sessionnumber, startdatetime
      FROM (
          SELECT runningresourceid, sessionnumber, startdatetime,
                 ROW_NUMBER() OVER (PARTITION BY runningresourceid ORDER BY startdatetime DESC) AS rn
          FROM BPASession
          WHERE enddatetime IS NULL
      ) ranked
      WHERE rn = 1
  ) cur ON cur.runningresourceid = r.resourceid
  ```

- **Kein Loop** mit Einzel-Queries pro Resource (N+1-Schutz).
- Implementation muss per `EXPLAIN` verifiziert werden.
- `--name <substring>` wird als `name LIKE @name + '%'` (Suffix-Match) umgesetzt für Index-Nutzung; Leading-Wildcard (`'%' + @name + '%'`) **nur** auf expliziten Flag `--name-contains` (Suffix-Match ist Default).

## Akzeptanzkriterien

1. Liste enthält alle registrierten Resources.
2. `current_session_number` ist `null`, wenn keine Session aktiv läuft.
3. `--status disconnected` filtert auf `DisplayStatus = 'Disconnected'` (oder `BPAStatus.description` match, je nach BP-Version).
4. `--status 1` (numerischer Code) liefert dieselbe Ergebnismenge.
5. EXPLAIN-Output zeigt **eine** Abfrage, keinen Loop.
6. Hard-Ceiling greift (Exit 8 bei Überschreitung).

## Out of Scope

- Resource starten / stoppen.
- Konfiguration der Resource-Pools / Controller.

## Abhängigkeiten

- F-001, F-011, F-012, F-013.
- architecture § 4.5 (Status-Convention), § 7.2 (Single-Query), § 8 (Exit-Codes).

## Offene Fragen

_(keine — im Review gelöst)_

## Verlauf

| Datum | Status | Änderung |
|:------|:-------|:---------|
| 2026-10-13 | 📋 Entwurf | Initial angelegt |
| 2026-10-13 | 📋 Entwurf | C2: Hard-Ceiling; M5: Single-Query-LEFT-JOIN-Pattern dokumentiert; m2: Status-Convention konsistent zu F-001 |
