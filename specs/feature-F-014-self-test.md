# Feature-Spec — Self-Test & Diagnostics

> Führt einen Sanity-Check der Installation durch (Config, DB-Verbindung, Tabellen, Schreibzugriff) und gibt die Binary-Version aus.

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
Zusätzlich: Binary-Version für Diagnose und CI/CD-Logging.

## Sub-Command 1: `version`

1. Befehl: `bp-log-viewer version` (auch als `--version`-Flag global).
2. Ausgabe als jsonl-Objekt:
   ```json
   {"cli_version":"0.1.0","build_commit":"87d3887","build_date":"2026-10-13T19:24:00Z","target_framework":"net8.0","runtime":"win-x64"}
   ```
3. Quelle: `AssemblyInformationalVersion` (vom Build-Prozess via `git rev-parse --short HEAD` + `dotnet-version`-Action injiziert). Fallback: Hardcoded in `AssemblyInfo.cs` mit manuellem Bump.
4. Exit 0.

## Sub-Command 2: `self-test`

1. Befehl: `bp-log-viewer self-test`.
2. Schritte (alle in Reihenfolge, Abbruch bei Fehler in **Schritt 1–3**):
   1. **Config:** `appsettings.json` laden → bei Fehler `config_status: error`.
   2. **LocalDB:** Instanz `BpLogViewer.Database.LocalDbInstance` aus Config vorhanden? → `localdb_status: ok | not_configured | not_running | start_failed`.
   3. **DB-Connect:** `SELECT @@VERSION` → `db_status: ok`, `db_version: <…>`.
   4. **BP-Version (DB bevorzugt, Registry als Fallback):**
      - **DB-Vorrang:** `SELECT TOP 1 versionno FROM BPADBVersion ORDER BY installeddate DESC` (oder vergleichbar; DB ist Single Source of Truth).
      - **Registry-Fallback** nur wenn DB-Query fehlschlägt: `HKLM\SOFTWARE\Blue Prism Limited\Automate\Version`.
      - Bei beiden Quellen OK: `bp_version: <db_value>`, `bp_version_source: "db"`.
      - Nur Registry: `bp_version_source: "registry"`.
   5. **Tabellen-Check:** Existenz von `BPASession`, `BPASessionLog_NonUnicode`, `BPASessionLog_Unicode`, `BPAProcess`, `BPAResource`, `BPAWorkQueue`, `BPAAuditEvents`. Pro Tabelle `tables.<name>: ok | missing`.
   6. **Aktive Log-Tabelle:** welche `BPASessionLog_*` ist befüllt (2-stufige Kette aus F-004) → `active_log_table: NonUnicode | Unicode | none`.
   7. **Write-Test:** Schreibtest gegen `<binary-dir>/temp/self-test-write-<timestamp>.json`. Bei Erfolg: `write_test: ok`, Datei wird sofort wieder gelöscht. Bei Fehler: `write_test: failed` + Reason.
3. Ausgabe: ein jsonl-Objekt pro Schritt + ein abschließendes Summary-Objekt `{"self_test": "ok" | "degraded" | "failed", "duration_ms": <n>}`.
4. Exit-Code:
   - alle Schritte ok → 0
   - nicht-kritische Probleme (z. B. `BPAWorkQueue` missing) → 0 mit `degraded: true`
   - kritische Probleme (Config, DB-Connect) → Exit 3 / 5 (architecture § 8)

## Nicht-funktionale Anforderungen

- Antwort < 2 s.
- Kein Schreibzugriff auf die DB (rein lesend).
- Write-Test (Schritt 7) löscht die Test-Datei sofort wieder.

## Akzeptanzkriterien

1. `bp-log-viewer self-test` auf diesem System meldet alle Tabellen als `ok` und `active_log_table: NonUnicode` (8 Zeilen in der Tabelle).
2. `bp_version: 7.5.1.18099` aus DB.
3. `bp-log-viewer version` liefert gültiges jsonl mit `cli_version`, `build_commit`, `build_date`.
4. Bei falscher `appsettings.json` (z. B. gelöscht) → Exit 3 und `config_status: error`.
5. Bei gestoppter LocalDB und erfolgreichem Start-Versuch → `localdb_status: ok` (statt `not_running`).
6. Bei fehlender `BPAWorkQueue`-Tabelle → Exit 0 mit `degraded: true`.

## KI-Integration

- KI-Agent (OpenClaw / MCP-Host) ruft die Binary per **absolutem Pfad** auf, nicht per `PATH`-Suche:

  ```
  "C:\\Program Files\\Blue Prism Limited\\bp-log-viewer-cli\\BpLogViewer.Cli.exe" self-test
  ```

- Empfohlene Reihenfolge beim ersten Spawn: `version` → `self-test` → eigentliches Sub-Command. Damit hat der Agent Versions- und Konfig-Kontext vor der ersten Abfrage.

## Out of Scope

- Performance-Benchmarks.
- Schreibtest gegen die DB (gehört zu F-001).
- Update-Check für die CLI selbst (separater `update-check`-Befehl, F-022+).

## Abhängigkeiten

- F-001 DB Connection.
- architecture § 9 (Deployment & Self-Test).
- `AssemblyInformationalVersion` (Build-Pipeline, `github-actions-dotnet-pipeline`-Skill).

## Offene Fragen

_(keine — im Review gelöst: DB-Version-Vorrang, `version`-Sub-Command ergänzt, Write-Test repariert)_

## Verlauf

| Datum | Status | Änderung |
|:------|:-------|:---------|
| 2026-10-13 | 📋 Entwurf | Initial angelegt |
| 2026-10-13 | 📋 Entwurf | M7: Output-Pfad-Test repariert (Schritt 7 schreibt gegen `PathResolver.Root`); M8: `version`-Sub-Command ergänzt; m7: DB-Vorrang vor Registry bei BP-Version |
| 2026-10-13 | 📋 Entwurf | Schritt 6 `active_log_table` 2-stufig (`NonUnicode \| Unicode \| none`); pre-65-Varianten entfernt (Martin-Vorgabe 2026-10-13) |
