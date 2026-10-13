# Feature-Spec — Database Connection Management

> Verbindungs-Aufbau, -Test und -Lifecycle gegen die BluePrism-Datenbank.

## Identifikation

- **ID:** F-001
- **Titel:** Database Connection Management
- **Slug:** db-connection
- **Status:** 📋 Entwurf
- **Priorität:** Hoch
- **Zugewiesen:** —
- **Erstellt am:** 2026-10-13
- **Letzte Aktualisierung:** 2026-10-13

## Ziel / Anwendungsfall

Stellt die einzige Stelle bereit, an der die CLI eine SQL-Verbindung öffnet.
Fundament für alle Lese-Kommandos (F-002 … F-010, F-014). Konfiguration über
`appsettings.json` (siehe `specs/architecture.md` § 4.1), überschreibbar per
CLI-Argument oder ENV-Var.

## Funktionale Anforderungen

1. `ConnectionString` aus `appsettings.json`, Section `BpLogViewer.Database.ConnectionString`.
2. Override per CLI `--connection-string <cs>` (gewinnt vor allen anderen Quellen).
3. Override per ENV `BP_DB_CONNECTION` (gewinnt vor Config, aber nach CLI).
4. Optional `LocalDbInstance` aus Config; bei nicht erreichbarer Instanz: einmalig `sqllocaldb start <instanz>` versuchen, Exit 4 wenn das fehlschlägt.
5. Bei SQL-Verbindungsfehler: 1× Retry mit 500 ms Backoff, danach strukturierte Fehlermeldung auf stderr.
6. Authentifizierung: Windows Integrated (keine User/Passwort-Pflicht).
7. Verbindung wird einmal pro CLI-Aufruf geöffnet; Pooling aktiv.

## Nicht-funktionale Anforderungen

- `Connect Timeout=5` als Default im Connection-String.
- `TrustServerCertificate=true` (LocalDB nutzt selbstsigniertes Zert).
- Keine Schreibrechte; nur `SELECT`-Operationen.
- Connection-String wird **nicht** über `PathResolver` geleitet (kein Pfad) — aber das Auffinden der `appsettings.json` schon.

## Akzeptanzkriterien

1. `bp-log-viewer self-test` (F-014) meldet `db_status: ok` nach erfolgreichem Aufbau.
2. Fehlende `appsettings.json` ohne CLI-Override → Exit 3, Meldung `config_missing: appsettings.json not found at <path>`.
3. Leerer `ConnectionString` ohne Override → Exit 3, Meldung `config_invalid: BpLogViewer.Database.ConnectionString is empty`.
4. LocalDB nicht erreichbar und Start schlägt fehl → Exit 4.
5. SQL-Fehler nach Retry → Exit 5, Meldung `sql_error: <nr>: <msg>`.

## Out of Scope

- Schreibende Operationen (INSERT/UPDATE/DELETE).
- Connection-String-Verschlüsselung.
- Mandanten-/Multi-Tenant-Wechsel.

## Abhängigkeiten

- `specs/architecture.md` § 4 (Konfiguration), § 7 (DB-Zugriff), § 8 (Fehlerbehandlung).
- F-014 Self-Test.
- Skill `dotnet-config-path-anchoring` (Pfad-Resolution für `appsettings.json`).

## Offene Fragen

1. Soll die CLI auch ohne `appsettings.json` laufen, wenn `--connection-string` übergeben wird? (Vorschlag: ja.)
2. Soll `--connection-string` zusätzlich als ENV `BP_LOG_VIEWER_CONNECTION` exponiert werden? (Vorschlag: nein, ein Name reicht.)

## Verlauf

| Datum | Status | Änderung |
|:------|:-------|:---------|
| 2026-10-13 | 📋 Entwurf | Initial angelegt |
