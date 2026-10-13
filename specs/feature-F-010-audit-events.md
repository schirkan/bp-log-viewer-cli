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
2. Quelle: `BPAAuditEvents` (oder `vw_Audit_improved` View).
3. Filter:
   - `--since <iso>` / `--until <iso>` (Default: letzte 7 Tage)
   - `--code <sCode>` (z. B. `RESSTART`, `SESSCOMP`, wiederholbar)
   - `--user <name>` (Suffix-Match auf Username — Join mit `BPAUser`)
   - `--target-process <name>`
   - `--target-resource <name>`
4. Sortierung: `eventdatetime DESC`.
5. Hard-Ceiling auf `--max-rows` (Default 500, wie F-002).
6. jsonl-Felder: `event_id, event_at, code, narrative, src_user, tgt_user, tgt_process, tgt_resource, comments_preview (comments auf 500 Zeichen gekürzt)`. XML-Spalten `oldXML`/`newXML` per `--include-xml` opt-in, sonst weggelassen.

## Nicht-funktionale Anforderungen

- Antwort < 1 s für 10.000 Audit-Zeilen.
- Single-Query mit LEFT-JOIN auf `BPAUser` (architecture § 7.2).
- `oldXML` / `newXML` **nie** standardmäßig ausgeben (zu groß, sensible Daten).

## Akzeptanzkriterien

1. `bp-log-viewer audit list --code RESSTART` liefert nur Resource-Start-Events.
2. `--target-process "Rechnungslauf"` filtert auf Audit-Events, die diesen Prozess betreffen.
3. `--since 2026-10-01` reicht als Default-Filter.
4. `comments_preview` ist auf 500 Zeichen gekürzt; Volltext per `--include-xml` (oder neuer Flag `--full-comments`).
5. Hard-Ceiling greift (Exit 8 bei Überschreitung).

## Out of Scope

- Schreibende Audit-Reports erstellen.
- `oldXML` / `newXML` standardmäßig ausgeben.

## Abhängigkeiten

- F-001, F-011, F-012, F-013.
- architecture § 7.2, § 8.

## Offene Fragen

_(keine — `audit summary` als F-017 für später notiert, kein Blocker)_

## Verlauf

| Datum | Status | Änderung |
|:------|:-------|:---------|
| 2026-10-13 | 📋 Entwurf | Initial angelegt |
| 2026-10-13 | 📋 Entwurf | C2: Hard-Ceiling |
