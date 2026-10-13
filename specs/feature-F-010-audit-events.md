# Feature-Spec — Audit Events

> Liefert die letzten Audit-Ereignisse aus `BPAAuditEvents`.

## Identifikation

- **ID:** F-010
- **Titel:** Audit Events
- **Slug:** audit-events
- **Status:** 📋 Entwurf
- **Priorität:** Niedrig
- **Zugewiesen:** —
- **Erstellt am:** 2026-10-13
- **Letzte Aktualisierung:** 2026-10-13

## Ziel / Anwendungsfall

„Wer hat wann Prozess X deployed, Resource Y deaktiviert, User Z die Rolle
entzogen?" Ergänzend zu den Laufzeit-Logs (F-002 … F-009) für die vollständige
Ursachenforschung.

## Funktionale Anforderungen

1. Befehl: `bp-log-viewer audit list`.
2. Quelle: `BPAAuditEvents` (oder `vw_Audit` View).
3. Filter:
   - `--since <iso>` / `--until <iso>` (Default: letzte 7 Tage)
   - `--code <sCode>` (z. B. `RESSTART`, `SESSCOMP`, wiederholbar)
   - `--user <name>` (LIKE auf Username — Join mit `BPAUser`)
   - `--target-process <name>`
   - `--target-resource <name>`
4. Sortierung: `eventdatetime DESC`.
5. Limit per `--max-rows` (Default 500).
6. NDJSON-Felder: `event_id, event_at, code, narrative, src_user, tgt_user, tgt_process, tgt_resource, comments_preview`. (XML-Spalten `oldXML`/`newXML` per `--include-xml` opt-in, sonst weggelassen.)

## Nicht-funktionale Anforderungen

- Antwort < 1 s für 10.000 Audit-Zeilen.
- `comments` auf 500 Zeichen gekürzt (`comments_preview`).

## Akzeptanzkriterien

1. `bp-log-viewer audit list --code RESSTART` liefert nur Resource-Start-Events.
2. `--target-process "Rechnungslauf"` filtert auf Audit-Events, die diesen Prozess betreffen.
3. `--since 2026-10-01` reicht als Default-Filter.

## Out of Scope

- Schreibende Audit-Reports erstellen.
- `oldXML` / `newXML` standardmäßig ausgeben (zu groß).

## Abhängigkeiten

- F-001, F-011, F-012, F-013.

## Offene Fragen

1. Soll ein `event_count_per_code`-Aggregat als Sub-Command `audit summary` angeboten werden? (Vorschlag: ja, als F-017 notieren.)

## Verlauf

| Datum | Status | Änderung |
|:------|:-------|:---------|
| 2026-10-13 | 📋 Entwurf | Initial angelegt |
