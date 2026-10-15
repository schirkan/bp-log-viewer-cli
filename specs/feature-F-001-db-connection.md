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
4. Verbindungsziel ausschließlich über `ConnectionString`; kein separater LocalDB-Instanzname und kein expliziter `sqllocaldb start`-Aufruf.
5. Bei SQL-Verbindungsfehler: 1× Retry mit 500 ms Backoff, danach strukturierte Fehlermeldung auf stderr.
6. Authentifizierung: Windows Integrated (keine User/Passwort-Pflicht).
7. Verbindung wird einmal pro CLI-Aufruf geöffnet; Pooling aktiv.
8. `appsettings.json` darf **fehlen**, wenn `--connection-string` über CLI oder `BP_DB_CONNECTION` übergeben werden (siehe § 4.3 Override-Reihenfolge).

## Nicht-funktionale Anforderungen

- `Connect Timeout=5` als Default im Connection-String.
- `TrustServerCertificate=true` (LocalDB nutzt selbstsigniertes Zert).
- DB-Permissions: Lesezugriff auf `BPA*` und `BPV*`; **kein** Schreibrecht erforderlich oder zulässig.
- Keine User/Passwort-Speicherung — Windows Integrated Auth.
- Connection-String wird **nicht** über `PathResolver` geleitet (kein Pfad) — aber das Auffinden der `appsettings.json` schon.

## Globale Konvention: `--status` (Code + Name)

Alle Sub-Commands, die `--status` akzeptieren (F-002, F-005, F-006), folgen derselben Konvention (siehe architecture § 4.5):

1. **Code zuerst versuchen:** numerische ID aus `BPAStatus.statusid` (z. B. `--status 4`).
2. **Fallback Name:** exakter Vergleich gegen `BPAStatus.description` (case-insensitive, z. B. `--status "Completed with Exceptions"`).
3. **Wiederholbar:** `--status 2 --status 4` (OR-Verknüpfung).
4. **Unbekannt:** Exit 11 (`error: unknown_status_value: <input>`).
5. **Runtime-Lookup:** `BPAStatus` wird beim Start 1× geladen und gecacht; **kein** Hardcoding der Code-Werte im CLI-Code.

## Akzeptanzkriterien

1. `bp-log-viewer self-test` (F-014) meldet `db_status: ok` nach erfolgreichem Aufbau.
2. Fehlende `appsettings.json` ohne CLI/ENV-Override → Exit 3, Meldung `config_missing: appsettings.json not found at <path>`.
3. Leerer `ConnectionString` ohne Override → Exit 3, Meldung `config_invalid: BpLogViewer.Database.ConnectionString is empty`.
4. LocalDB nicht erreichbar → SQL-Verbindungsfehler, Exit 5.
5. SQL-Fehler nach Retry → Exit 5, Meldung `sql_error: <nr>: <msg>`.
6. CLI-Aufruf mit `--connection-string "..."` ohne `appsettings.json` läuft erfolgreich.
7. `BPAStatus` wird 1× beim Start geladen und im Prozess gecacht (`StatusCache`-Service).

## Out of Scope

- Schreibende Operationen (INSERT/UPDATE/DELETE).
- Connection-String-Verschlüsselung (z. B. via DPAPI / Azure KeyVault).
- Mandanten-/Multi-Tenant-Wechsel.
- User/Passwort-Authentifizierung gegen SQL-Login (nur Windows Integrated).

## Abhängigkeiten

- `specs/architecture.md` § 4 (Konfiguration), § 7 (DB-Zugriff), § 8 (Fehlerbehandlung).
- F-014 Self-Test.
- Skill `dotnet-config-path-anchoring` (Pfad-Resolution für `appsettings.json`).

## Offene Fragen

_(keine — alle im Review gelöst)_

## Verlauf

| Datum | Status | Änderung |
|:------|:-------|:---------|
| 2026-10-13 | 📋 Entwurf | Initial angelegt |
| 2026-10-13 | 📋 Entwurf | m2: Globale `--status`-Convention (Code + Name) ergänzt, § 4.5 verlinkt |
| 2026-10-13 | 📋 Entwurf | m6: ENV `BP_DB_CONNECTION` explizit in der Spec verankert |
