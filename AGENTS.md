# AGENTS.md — BP Log Viewer CLI

## Project

- **Name:** BP Log Viewer CLI
- **Topic-ID:** 7122
- **Channel:** Telegram (Group)
- **Stack:** C# 12 / .NET 8 (self-contained single-file trimmed, win-x64)
- **DB-Zielsystem:** Blue Prism 7.5.1.18099 (LocalDB `BluePrismLocalDB`, Datenbank `BluePrism`)
- **Owner:** Martin

## ⚠️ Projektspezifische Anweisung (Martin-Vorgabe 2026-10-13)

> **Kein Workboard für dieses Projekt.**
>
> - Status- und Todo-Tracking laufen ausschließlich über `specs/README.md` (Status-Übersicht + Verlauf).
> - Die `PROJECT-RULES.md`-Sektion „Workboard" wird für dieses Projekt bewusst ignoriert.
> - Beim ersten Nachfragen / Sub-Agent-Start in diesem Projekt: Workboard-Tools **nicht** einsetzen, `specs/README.md` als Single-Source-of-Truth nutzen.
> - Begründung: kleines Projekt, direkte Spec-Implementierung, kein Multi-Session-Tracking nötig.

## Current Status

- Spezifikations-Phase abgeschlossen (Martin-Vorgabe 2026-10-02).
- Architektur dokumentiert in `specs/architecture.md` (Tech-Stack, Layout, Konfiguration, Fehlerbehandlung, Deployment).
- 14 Feature-Specs angelegt (F-001 bis F-014), alle im Status 📋 Entwurf.
- DB- und Installations-Befunde dokumentiert in `context/`.
- **Nächste Phase wartet auf Martin-Abnahme der Specs** (Wechsel 📋 → 🗔 → ✅).
- Git-Repo: **eingerichtet** (siehe `## Git`). Public: `https://github.com/schirkan/bp-log-viewer-cli`.
- Workboard: **bewusst nicht verwendet** (siehe ⚠️ oben).

## Git

- **Repo-Typ:** GitHub, öffentlich
- **Pfad / URL:** https://github.com/schirkan/bp-log-viewer-cli
- **Remote(s):** `origin` → https://github.com/schirkan/bp-log-viewer-cli.git
- **Eingerichtet am:** 2026-10-13 (lokal + GitHub-Remote)
- **`.gitignore`-Status:** vorhanden (siehe Projekt-Root)
- **Standard-Branch:** `main`

## CI/CD

_(Sektion entfällt, solange kein GitHub-Actions-Workflow existiert — laut `PROJECT-RULES.md` § CI/CD nichts erzwingen.)_

## Spec-Übersicht (Kurzfassung)

| ID | Titel | Priorität | Status |
|:---|:------|:----------|:-------|
| F-001 | Database Connection Management | Hoch | 📋 |
| F-002 | Session List | Hoch | 📋 |
| F-003 | Session Details | Hoch | 📋 |
| F-004 | Session Log | Hoch | 📋 |
| F-005 | Search Errors | Hoch | 📋 |
| F-006 | Resources | Mittel | 📋 |
| F-007 | Processes | Mittel | 📋 |
| F-008 | Work Queues | Mittel | 📋 |
| F-009 | Work Queue Items | Mittel | 📋 |
| F-010 | Audit Events | Niedrig | 📋 |
| F-011 | Common Filters | Hoch | 📋 |
| F-012 | Output to File | **Hoch** (Martin-Vorgabe) | 📋 |
| F-013 | Output Format (jsonl / Table) | Hoch | 📋 |
| F-014 | Self-Test & Diagnostics | Mittel | 📋 |

Vollständige Tabelle mit Abhängigkeiten: `specs/README.md` § Status-Übersicht.

## Architektur-Highlights

| Aspekt | Wahl | Quelle |
|:-------|:-----|:-------|
| Laufzeit | .NET 8 self-contained single-file trimmed, win-x64 | architecture § 2 |
| DB-Treiber | `Microsoft.Data.SqlClient` 5.x | architecture § 2 |
| CLI-Parser | `System.CommandLine` | architecture § 2 |
| JSON | `System.Text.Json` + `JsonSerializerContext` (Source-Generator) | Skill `dotnet-trimmed-binary-json-fix` |
| Path-Resolution | zentraler `PathResolver`, Anker `AppContext.BaseDirectory` | Skill `dotnet-config-path-anchoring` |
| Auth | Windows Integrated (kein Passwort in Config) | architecture § 4.1 |
| Output-Default | **jsonl** (Newline-Delimited JSON, identisch zu NDJSON) | F-013 |
| Output bei Größenüberschreitung | `--output <path>` (F-012) + Schwellwert aus `appsettings.json` | F-011, F-012 |

## Blocker

| # | Blocker | Auswirkung |
|---|:--------|:-----------|
| 1 | **Spec-Review** durch Martin | Wechsel 📋 → 🗔 → ✅ nötig, bevor Implementierung startet |
| 2 | ~~**GitHub-Repo-Erstellung**~~ | erledigt — siehe `## Git` |
| 3 | **`appsettings.template.json`** Inhalt (DB-Connection-String-Form, Schwellwert) | Wird in Implementierungs-Phase festgelegt; Connection-String wird **nicht** versioniert |

## Offene Architektur-Entscheidungen

| Frage | Vorschlag | Spec-Referenz |
|:------|:----------|:--------------|
| Relative `--output`-Pfade: nur `AppContext.BaseDirectory` oder ENV-Allowlist? | ENV-Override `BP_LOG_VIEWER_OUTPUT_ROOT`, Default `AppContext.BaseDirectory` | F-012 offene Frage 1 |
| `--status` numerisch oder Name? | beides | F-002 offene Frage 1 |
| Soll `self-test` die Binary-Version gegen GitHub prüfen? | nein, eigener Befehl | F-014 offene Frage 1 |

Detail-Begründungen in den jeweiligen Spec-Dateien.

## Kontext-Dokumente

| Datei | Inhalt |
|:------|:-------|
| `context/blue-prism-installation.md` | BP-Version 7.5.1.18099, Install-Pfade, Service, Registry, LocalDB, Auth-Modus |
| `context/blue-prism-database-schema.md` | DB-Schema-Befund: 213 Objekte, Schlüsseltabellen mit Spalten, Views, Befüllungs-Stand |

## Project Files

- `AGENTS.md` (dieses Dokument)
- `.gitignore` (versioniert)
- `appsettings.json` (nicht versioniert, wird in Implementierungs-Phase angelegt)
- `appsettings.template.json` (Vorlage, wird in Implementierungs-Phase angelegt)
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
