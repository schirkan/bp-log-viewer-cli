# AGENTS.md — BP Log Viewer CLI

## Project

- **Name:** BP Log Viewer CLI
- **Topic-ID:** 7122
- **Channel:** Telegram (Group)
- **Stack:** C# 12 / .NET 8 (self-contained single-file trimmed, win-x64)
- **DB-Zielsystem:** Blue Prism 7.5.1.18099 (LocalDB `BluePrismLocalDB`, Datenbank `BluePrism`)
- **Owner:** Martin
- **License:** MIT (siehe `LICENSE`)

## ⚠️ Projektspezifische Anweisung (Martin-Vorgabe 2026-10-13)

> **Kein Workboard für dieses Projekt.**
>
> - Status- und Todo-Tracking laufen ausschließlich über `specs/README.md` (Status-Übersicht + Verlauf).
> - Die `PROJECT-RULES.md`-Sektion „Workboard" wird für dieses Projekt bewusst ignoriert.
> - Beim ersten Nachfragen / Sub-Agent-Start in diesem Projekt: Workboard-Tools **nicht** einsetzen, `specs/README.md` als Single-Source-of-Truth nutzen.
> - Begründung: kleines Projekt, direkte Spec-Implementierung, kein Multi-Session-Tracking nötig.

## Current Status

- Spezifikations-Phase abgeschlossen (Martin-Vorgabe 2026-10-02).
- Architektur dokumentiert in `specs/architecture.md` (Tech-Stack, Layout, Konfiguration, Fehlerbehandlung, Deployment, Timezone-Policy § 4.4, Status-Convention § 4.5).
- 14 Feature-Specs angelegt (F-001 bis F-014), alle von Martin freigegeben (✅).
- DB- und Installations-Befunde dokumentiert in `context/`.
- **code-review 2026-10-13 abgeschlossen** (Skill `code-review`, 2 CRITICAL + 8 MAJOR + 7 MINOR + 5 NIT). Alle CRITICAL + MAJOR-Blocker in derselben Session gefixt. MINOR-Items in `## Blocker → Implementation-Notes` überführt.
- Lizenz: **MIT** (`LICENSE`, README-Badge).
- **Alle 14 Specs ✅ Freigegeben** (Martin-Abnahme 2026-10-13 21:47).
- Implementierungsstand: F-013 ✔️, F-011 ✔️, F-001/F-002/F-003/F-004/F-005/F-012/F-014 🚧.
- Git-Repo: **eingerichtet** (siehe `## Git`).
- Workboard: **bewusst nicht verwendet** (siehe ⚠️ oben).

## Git

- **Repo-Typ:** GitHub, öffentlich
- **Pfad / URL:** https://github.com/schirkan/bp-log-viewer-cli
- **Remote(s):** `origin` → https://github.com/schirkan/bp-log-viewer-cli.git
- **Eingerichtet am:** 2026-10-13 (lokal + GitHub-Remote)
- **`.gitignore`-Status:** vorhanden
- **Standard-Branch:** `main`
- **GitHub Release Workflow:** `.github/workflows/release-single-exe.yml` (Tag `v*` → single EXE + ZIP + SHA256 + Release-Assets)

## Spec-Übersicht (Kurzfassung)

| ID | Titel | Priorität | Status |
|:---|:------|:----------|:-------|
| F-001 | Database Connection Management | Hoch | 🚧 |
| F-002 | Session List | Hoch | 🚧 |
| F-003 | Session Details | Hoch | 🚧 |
| F-004 | Session Log | Hoch | 🚧 |
| F-005 | Search Errors | Hoch | 🚧 |
| F-006 | Resources | Mittel | ✅ |
| F-007 | Processes | Mittel | ✅ |
| F-008 | Work Queues | Mittel | ✅ |
| F-009 | Work Queue Items | Mittel | ✅ |
| F-010 | Audit Events | Niedrig | ✅ |
| F-011 | Common Filters | Hoch | ✔️ |
| F-012 | Output to File | **Hoch** (Martin-Vorgabe) | 🚧 |
| F-013 | Output Format (jsonl / Table) | Hoch | ✔️ |
| F-014 | Self-Test & Diagnostics | Mittel | 🚧 |

Vollständige Tabelle mit Abhängigkeiten: `specs/README.md` § Status-Übersicht.

## Architektur-Highlights

| Aspekt | Wahl | Quelle |
|:-------|:-----|:-------|
| Laufzeit | .NET 8 self-contained single-file trimmed, win-x64 | architecture § 2 |
| DB-Treiber | `Microsoft.Data.SqlClient` 5.x | architecture § 2 |
| CLI-Parser | `System.CommandLine` | architecture § 2 |
| JSON | `System.Text.Json` + `JsonSerializerContext` (Source-Generator) | Skill `dotnet-trimmed-binary-json-fix` |
| Path-Resolution | zentraler `PathResolver`, Anker `AppContext.BaseDirectory` (ENV-Override `BP_LOG_VIEWER_OUTPUT_ROOT`) | Skill `dotnet-config-path-anchoring`, architecture § 4.2/4.3 |
| Auth | Windows Integrated (kein Passwort in Config) | architecture § 4.1 |
| Output-Default | **jsonl** (Newline-Delimited JSON, identisch zu NDJSON) | F-013 |
| Output bei Größenüberschreitung | `--output <path>` (F-012) + Schwellwert + **Hard-Ceiling** `MaxRowsAbsoluteCeiling` | F-011, F-012, architecture § 4.1 |
| Status-Lookup | Runtime aus `BPAStatus` (kein Hardcoding) | architecture § 4.5 |
| Timezone-Policy | DB = UTC, Anzeige = `Europe/Berlin` (ENV-Override) | architecture § 4.4 |
| KI-Spawn | absoluter Pfad (nicht `PATH`-Suche) | F-014 § KI-Integration |

## Blocker

_(keine — alle Specs freigegeben, Implementierung freigegeben)_

## Implementation-Notes (aus code-review, nicht-blockierend)

Diese Punkte sind im Review als MINOR markiert und laufen als Tasks während der Implementierung mit (kein Blocker für Spec-Abnahme):

| # | Thema | Spec |
|---|:------|:-----|
| IN-1 | `BPAExceptionType` enthält nur 2 Spalten (`id`, `type`); in mehreren Specs war `description`/`detail` angenommen. DB-Spalten-Validierung pro Spec-Query. | F-004, F-005 |
| IN-2 | `processxml` aus `BPAProcess` und `sessionstatexml` aus `BPASession` — F-007 sagt „niemals", F-003 sagt „opt-in via Flag". Konsolidiert: opt-in via Flag (F-007-Annahme korrigiert). | F-003, F-007 |
| IN-3 | Test-Strategie (Unit/Integration/E2E) in architecture § 10, aber keine Spec verlinkt Tests direkt. Erstellen: `tests/BpLogViewer.Cli.Tests/`-Skeleton + erste Tests bei Implementation-Start. | architecture § 10 |
| IN-4 | License bereits MIT gesetzt, aber kein Contributor-Setup (CONTRIBUTING.md, Issue-Templates). F-022+. | README.md |
| IN-5 | `appsettings.template.json` als versionierte Vorlage, aber lokale `appsettings.json` (mit Connection-String) wird **nicht** versioniert. Implementation-Hinweis in `appsettings.template.json` selbst. | `appsettings.template.json` |
| IN-6 | Aggregation `audit summary` (F-010 offene Frage) → F-017 für später. | F-010 |
| IN-7 | Cross-Prozess-Correlation in F-005 (Out of Scope) → F-016 für später. | F-005 |
| IN-8 | `process list --avg-duration` ist jetzt Default-Feld, aber nur sinnvoll bei ≥ 1 abgeschlossener Session. Test mit leerem Prozess. | F-007 |
| IN-9 | `last_session_number` ist pro Prozess — bei sehr vielen Prozessen teure Window-Function. EXPLAIN-Plan in Implementation-Phase verifizieren. | F-007 |

Detail-Begründungen in den jeweiligen Spec-Dateien (`## Implementation Notes`).

## Kontext-Dokumente

| Datei | Inhalt |
|:------|:-------|
| `context/blue-prism-installation.md` | BP-Version 7.5.1.18099, Install-Pfade, Service, Registry, LocalDB, Auth-Modus |
| `context/blue-prism-database-schema.md` | DB-Schema-Befund: 213 Objekte, Schlüsseltabellen mit Spalten, Views, Befüllungs-Stand |

## Project Files

- `AGENTS.md` (dieses Dokument)
- `LICENSE` (MIT, versioniert)
- `README.md` (Bedienung + Lizenz, versioniert)
- `.gitignore` (versioniert)
- `.gitattributes` (versioniert, Line-Endings)
- `appsettings.json` (nicht versioniert, lokale Config mit Connection-String)
- `appsettings.template.json` (versioniert, Vorlage)
- `specs/` (14 Feature-Specs + Architektur + README + Template)
- `context/` (Installations- und Schema-Befunde)
- `temp/` (temporäre Arbeitsdateien, nicht versionieren)
- `src/` (wird in Implementierungs-Phase angelegt)
- `tests/` (wird in Implementierungs-Phase angelegt)

## Verlauf

| Datum | Änderung |
|:------|:---------|
| 2026-10-02 | Projektordner + zentrale `AGENTS.md` initial angelegt |
| 2026-10-02 | Spec-Struktur etabliert (`specs/README.md` + `_TEMPLATE.md`) |
| 2026-10-13 | Skills geladen (dotnet-config-path-anchoring, dotnet-self-contained-redeploy, dotnet-trimmed-binary-json-fix, github-actions-dotnet-pipeline) |
| 2026-10-13 | DB- und Installations-Befunde dokumentiert (`context/`) |
| 2026-10-13 | Architektur-Spec + 14 Feature-Specs angelegt, alle 📋 Entwurf |
| 2026-10-13 | `specs/README.md` Status-Tabelle gefüllt, Fundament-Reihenfolge dokumentiert |
| 2026-10-13 | **No-Workboard-Entscheidung** in `AGENTS.md` prominent vermerkt (Martin-Vorgabe) |
| 2026-10-13 | **NDJSON → jsonl** Umbenennung in architecture.md, F-012, F-013, specs/README.md, AGENTS.md (Martin-Vorgabe) |
| 2026-10-13 | `.gitignore` angelegt, lokales Git-Repo initialisiert (Standard-Branch `main`) |
| 2026-10-13 | **Initial Commit** auf `main` (24 Dateien, 1918 Zeilen) + **Public Repo** `https://github.com/schirkan/bp-log-viewer-cli` angelegt via `gh repo create --public --source=. --push` |
| 2026-10-13 | **code-review** durchgeführt (Skill `code-review`, 2 CRITICAL + 8 MAJOR + 7 MINOR + 5 NIT) |
| 2026-10-13 | **C1** MIT-Lizenz (`LICENSE`) + README-Badge |
| 2026-10-13 | **C2** `MaxRowsAbsoluteCeiling` (Default 100 000) + Exit 8 |
| 2026-10-13 | **M1–M8** + **m1–m7** + **n1, n4** in architecture.md und allen 14 Specs nachgezogen (siehe specs/README.md § Verlauf für Detail) |
| 2026-10-13 | `.gitattributes` + `appsettings.template.json` neu angelegt |
| 2026-10-13 | **Blocker #2** (GitHub-Repo) + **#3** (`appsettings.template.json`) erledigt. |
| 2026-10-13 | **Blocker #1** (Spec-Review) erledigt — Martin-Abnahme „Specs ok" 2026-10-13 21:47. Alle 14 Specs auf ✅ Freigegeben. |
| 2026-10-13 | **Implementierung gestartet** — Fundament-Reihenfolge: F-013 → F-011 → F-001 → F-002 → F-003 → F-004 → F-005 → F-012 → F-014 → F-006 → F-007 → F-008 → F-009 → F-010. |
| 2026-10-14 | Weiterbau: F-011 Parser/Validierung stabilisiert, `self-test` (F-001/F-014-Teil) aktiv mit DB-Connect/StatusCache, `session list` (F-002) gegen `BPVSessionInfo` lauffähig mit jsonl-Output. |
| 2026-10-14 | F-003 gestartet: `session get` implementiert (Suche per Session-Nr./UUID, optional `--include-state-xml`, Queue-Items + truncation-Flag, Exit 10 bei nicht gefunden). |
| 2026-10-14 | F-004/F-005 gestartet: `session log` (aktive Log-Tabelle, Filter + truncation) und `session errors` (List/Aggregate) implementiert. |
| 2026-10-14 | F-012/F-014 erweitert: Output-to-File (`--output`) mit Marker-Response + Pfadschutz (Traversal-Block), `self-test` um BP-Version, Tabellen-Check, aktive Log-Tabelle und Write-Test ergänzt. |
| 2026-10-14 | Release-Pipeline ergänzt: `.github/workflows/release-single-exe.yml` baut bei Tag `v*` ein self-contained single-file `win-x64`-Artefakt, packt ZIP + SHA256 und veröffentlicht als GitHub Release Asset. |
