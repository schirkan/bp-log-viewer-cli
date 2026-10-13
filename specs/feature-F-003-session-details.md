# Feature-Spec — Session Details

> Liefert die vollständigen Metadaten einer einzelnen Session.

## Identifikation

- **ID:** F-003
- **Titel:** Session Details
- **Slug:** session-details
- **Status:** 📋 Entwurf
- **Priorität:** Hoch
- **Zugewiesen:** —
- **Erstellt am:** 2026-10-13
- **Letzte Aktualisierung:** 2026-10-13

## Ziel / Anwendungsfall

Drill-Down von F-002 (Session List) auf eine konkrete Session. Liefert alle
Felder, die für die Fehleranalyse relevant sind, inklusive Work-Queue-Verknüpfung
und Exception-Details.

## Funktionale Anforderungen

1. Befehl: `bp-log-viewer session get <session-number>` (menschenlesbare ID; `--session-id <uuid>` als Alternative).
2. Quelle: `BPASession` (alle Spalten) + Lookups (`BPAStatus`, `BPAExceptionType` falls vorhanden, `BPAProcess`, `BPAResource`, `BPAUser` falls referenziert).
3. Verknüpfte Work-Queue-Items: `BPAWorkQueueItem` mit `sessionid = @sessionid` → `keyvalue`, `status`, `attempt`, `exceptionreason`, `exceptionreasontag`, `loaded_at`, `finished_at` (max. 100 Items; bei mehr → `truncated: true` Flag).
4. jsonl-Ausgabe: **ein** Objekt mit verschachtelten Feldern (nicht eine Liste).
5. Lokale Zeitstempel (per `BpLogViewer.Timezone.DisplayTimezone`, Default `Europe/Berlin`) wie in F-002.
6. `duration_sec` als `ended_at - started_at` in Sekunden (oder `null` wenn `ended_at IS NULL`).
7. `is_running: true` wenn `ended_at IS NULL` (laufende Session).

## Nicht-funktionale Anforderungen

- Antwort < 100 ms.
- `sessionstatexml` und `startparamsxml` werden **nicht** standardmäßig ausgegeben (zu groß). Per Flag `--include-state-xml` opt-in (Konsistenz mit F-013: opt-in statt „niemals", um F-007 zu harmonisieren — XML-Größe bleibt explizit dokumentiert).
- `BPAProcess.compressedxml` wird niemals ausgegeben.

## Akzeptanzkriterien

1. `bp-log-viewer session get 4711` liefert genau ein jsonl-Objekt.
2. Existiert die Session nicht → leerer Stream + Warning `session_not_found: 4711` auf stderr + **Exit 10** (`error: not_found`) — siehe architecture § 8.
3. `--include-state-xml` fügt `start_params_xml` und `session_state_xml` als String-Felder hinzu (Größe-Hinweis in stderr).
4. Verknüpfte Queue-Items erscheinen unter `work_queue_items: [...]` (auch wenn leer: `[]`).
5. > 100 verknüpfte Queue-Items → `truncated: true` in der gleichen Antwort.
6. Aktive Session (`ended_at IS NULL`) → Feld `is_running: true`.

## Out of Scope

- Verknüpfte Log-Zeilen — siehe F-004.
- Verknüpfte Audit-Events — siehe F-010.

## Abhängigkeiten

- F-001, F-011, F-012, F-013.
- architecture § 4.4 (Timezone), § 8 (Exit-Codes).

## Offene Fragen

_(keine — im Review gelöst: max. 100 Items + `truncated: true` entschieden)_

## Verlauf

| Datum | Status | Änderung |
|:------|:-------|:---------|
| 2026-10-13 | 📋 Entwurf | Initial angelegt |
| 2026-10-13 | 📋 Entwurf | m1: Exit 10 für „not found" statt 0; F-007-Inkonsistenz `--include-state-xml` opt-in harmonisiert; `is_running: true` ergänzt |
