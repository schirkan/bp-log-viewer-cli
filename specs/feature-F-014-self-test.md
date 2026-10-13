# Feature-Spec — Self-Test & Diagnostics

> Führt einen Sanity-Check der Installation durch (Config, DB-Verbindung, Tabellen, Schreibzugriff).

## Identifikation

- **ID:** F-014
- **Titel:** Self-Test & Diagnostics
- **Slug:** self-test
- **Status:** 📋 Entwurf
- **Priorität:** Mittel
- **Zugewiesen:** —
- **Erstellt am:** 2026-10-13
- **Letzte Aktualisierung:** 2026-10-13

## Ziel / Anwendungsfall

Vor dem ersten produktiven Einsatz (und nach jeder Konfig-Änderung) muss die
KI / der Operator prüfen können, ob alle Voraussetzungen erfüllt sind: Config
auffindbar, DB erreichbar, BP-Version erkannt, erwartete Tabellen vorhanden.

## Funktionale Anforderungen

1. Befehl: `bp-log-viewer self-test` (Sub-Command, kein Resource).
2. Schritte (alle in Reihenfolge, Abbruch bei Fehler):
   1. **Config:** `appsettings.json` laden → bei Fehler `config_status: error`.
   2. **LocalDB:** Instanz `BpLogViewer.Database.LocalDbInstance` aus Config vorhanden? → `localdb_status: ok | not_configured | not_running | start_failed`.
   3. **DB-Connect:** `SELECT @@VERSION` → `db_status: ok`, `db_version: <…>`.
   4. **DB-Identität:** `SELECT COUNT(*) FROM BPADBVersion` (oder vergleichbar) → `db_schema_status: ok | unexpected_version`.
   5. **BP-Version:** aus DB-Version oder Registry (`HKLM\SOFTWARE\Blue Prism Limited\Automate`) → `bp_version: <…>`.
   6. **Tabellen-Check:** Existenz von `BPASession`, `BPASessionLog_NonUnicode`, `BPASessionLog_Unicode`, `BPAProcess`, `BPAResource`, `BPAWorkQueue`, `BPAAuditEvents`. Pro Tabelle `tables.<name>: ok | missing`.
   7. **Aktive Log-Tabelle:** welche `BPASessionLog_*` ist befüllt → `active_log_table: NonUnicode | Unicode | none`.
   8. **Output-Pfad-Test:** wenn `appsettings.json` einen Pfad für `--output` enthält (z. B. `Logs/`), prüfen ob beschreibbar.
3. Ausgabe: ein NDJSON-Objekt pro Schritt + ein abschließendes Summary-Objekt `{"self_test": "ok" | "degraded" | "failed", "duration_ms": <n>}`.
4. Exit-Code:
   - alle Schritte ok → 0
   - nicht-kritische Probleme (z. B. `BPAWorkQueue` missing) → 0 mit `degraded: true`
   - kritische Probleme (Config, DB-Connect) → ≠ 0

## Nicht-funktionale Anforderungen

- Antwort < 2 s.
- Kein Schreibzugriff auf die DB (rein lesend).

## Akzeptanzkriterien

1. `bp-log-viewer self-test` auf diesem System meldet alle Tabellen als `ok` und `active_log_table: NonUnicode`.
2. `bp_version: 7.5.1.18099` (aus Registry).
3. Bei falscher `appsettings.json` (z. B. gelöscht) → Exit ≠ 0 und `config_status: error`.
4. Bei gestoppter LocalDB und erfolgreichem Start-Versuch → `localdb_status: ok` (statt `not_running`).

## Out of Scope

- Performance-Benchmarks.
- Schreibtest gegen die DB (gehört zu F-001, dort dokumentiert).
- Update-Check für die CLI selbst.

## Abhängigkeiten

- F-001 DB Connection.
- architecture.md § 9 (Deployment & Self-Test).

## Offene Fragen

1. Soll `self-test` auch die Version des Binarys (`--version` Wert) gegen ein GitHub-Release prüfen? (Vorschlag: nein, gehört in einen separaten `update-check`-Befehl.)

## Verlauf

| Datum | Status | Änderung |
|:------|:-------|:---------|
| 2026-10-13 | 📋 Entwurf | Initial angelegt |
