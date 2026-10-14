# Specs — Übersicht (BP Log Viewer CLI)

**Stand:** 2026-10-13 (Code-Review-Nachzug)
**Quelle der Konventionen:** `projects/PROJECT-RULES.md` § „File Management"

## Konventionen

- **Eine Spec-Datei pro Feature:** `specs/feature-<ID>-<slug>.md`
- **ID:** fortlaufende Nummer (`F-001`, `F-002`, …), unveränderlich nach Vergabe.
- **Slug:** kurze Beschreibung in Kleinschreibung, Bindestrich-getrennt, ASCII.
- **Template:** `specs/_TEMPLATE.md` als Vorlage für jede neue Feature-Spec.
- **Reihenfolge der Bearbeitung:** Spec **vor** Implementierung (Martin-Vorgabe 2026-10-02).
- **Cross-References:** Feature-IDs in Notizen/Akzeptanzkriterien verwenden, nicht Dateinamen.
- **Out-of-Scope** ist Pflichtfeld — was bewusst weggelassen wird, wird mit Begründung erfasst.
- **Implementation Notes** ist Pflichtfeld, falls das Feature Runtime-Lookups, Single-Query-Constraints oder ähnliches hat.

## Status-Indikatoren

| Symbol | Status | Voraussetzung für Wechsel |
|:-------|:-------|:--------------------------|
| 📋 | Entwurf | — |
| 🗔 | In Review | Spec vollständig ausgefüllt, an Martin übergeben |
| ✅ | Freigegeben | Martin-Abnahme |
| 🚧 | In Umsetzung | Workboard-Karte aktiv (nicht verwendet in diesem Projekt — siehe AGENTS.md) |
| ✔️ | Fertig | Implementierung verifiziert |
| ❌ | Verworfen | (immer mit Begründung) |

## Zusatz-Dokumente

| Datei | Inhalt |
|:------|:-------|
| `architecture.md` | Tech-Stack, Befehls-Struktur, Pfad-Resolution, Timezone-Policy, Status-Convention, Error-Model |
| `../context/blue-prism-installation.md` | Befund der BP-Installation (Version, Pfade, Service, Registry) |
| `../context/blue-prism-database-schema.md` | Befund des DB-Schemas (213 Objekte, Schlüsseltabellen) |
| `../appsettings.template.json` | Konfigurations-Template (versioniert; kopieren nach `appsettings.json` und anpassen) |
| `../LICENSE` | MIT-Lizenztext (Copyright 2026 Martin) |

## Status-Übersicht

| ID | Titel | Status | Priorität | Spec | Abhängigkeiten |
|:---|:------|:-------|:----------|:-----|:---------------|
| F-001 | Database Connection Management | 🚧 In Umsetzung | Hoch | [spec](feature-F-001-db-connection.md) | (Fundament) |
| F-002 | Session List | 🚧 In Umsetzung | Hoch | [spec](feature-F-002-list-sessions.md) | F-001, F-011, F-012, F-013 |
| F-003 | Session Details | ✅ Freigegeben | Hoch | [spec](feature-F-003-session-details.md) | F-001, F-011, F-012, F-013 |
| F-004 | Session Log | ✅ Freigegeben | Hoch | [spec](feature-F-004-session-log.md) | F-001, F-011, F-012, F-013 |
| F-005 | Search Errors | ✅ Freigegeben | Hoch | [spec](feature-F-005-search-errors.md) | F-001, F-002, F-011, F-012, F-013 |
| F-006 | Resources | ✅ Freigegeben | Mittel | [spec](feature-F-006-resources.md) | F-001, F-011, F-012, F-013 |
| F-007 | Processes | ✅ Freigegeben | Mittel | [spec](feature-F-007-processes.md) | F-001, F-011, F-012, F-013 |
| F-008 | Work Queues | ✅ Freigegeben | Mittel | [spec](feature-F-008-work-queues.md) | F-001, F-011, F-012, F-013 |
| F-009 | Work Queue Items | ✅ Freigegeben | Mittel | [spec](feature-F-009-queue-items.md) | F-001, F-008, F-011, F-012, F-013 |
| F-010 | Audit Events | ✅ Freigegeben | Niedrig | [spec](feature-F-010-audit-events.md) | F-001, F-011, F-012, F-013 |
| F-011 | Common Filters | ✔️ Fertig | Hoch | [spec](feature-F-011-common-filters.md) | architecture § 4.3, § 4.4, § 4.5, § 5, § 8 |
| F-012 | Output to File | ✅ Freigegeben | **Hoch** (Martin-Vorgabe) | [spec](feature-F-012-output-to-file.md) | F-011, F-013, Skill `dotnet-config-path-anchoring` |
| F-013 | Output Format (jsonl / Table) | ✔️ Fertig | Hoch | [spec](feature-F-013-output-format.md) | Skill `dotnet-trimmed-binary-json-fix` |
| F-014 | Self-Test & Diagnostics | 🚧 In Umsetzung | Mittel | [spec](feature-F-014-self-test.md) | F-001, architecture § 9 |

**Zusammenfassung:** Spec-Abnahme ist komplett ✅. Implementierungsstand: **F-013 ✔️, F-011 ✔️, F-001/F-002/F-014 🚧**. Nächste Reihenfolge: **F-003 → F-004 → F-005 → F-012 → F-014 fertigziehen → F-006 → F-007 → F-008 → F-009 → F-010**.

## Code-Review-Status (2026-10-13)

Erste code-review-Runde (Skill `code-review`) identifizierte **2 CRITICAL + 8 MAJOR + 7 MINOR + 5 NIT**. Nachzug in derselben Session erfolgt (siehe `## Verlauf` und § „Verlauf" jeder Spec). **Stand 2026-10-13: alle CRITICAL + MAJOR-Blocker behoben, MINOR in der Implementierungs-Phase als Tasks zu tracken.**

## Spec-Datei-Verzeichnis

| Datei | Inhalt |
|:------|:-------|
| `README.md` | Diese Übersicht |
| `_TEMPLATE.md` | Vorlage für eine Feature-Spec |
| `architecture.md` | Architektur-Spec (Tech-Stack, Layout, Konfiguration, Fehlerbehandlung, Timezone, Status-Convention) |
| `feature-F-001-…md` … `feature-F-014-…md` | Feature-Specs (jeweils mit Implementation Notes bei Bedarf) |

## Verlauf

| Datum | Änderung |
|:------|:---------|
| 2026-10-02 | Übersicht + Template angelegt. Status-Konvention etabliert. |
| 2026-10-13 | Architektur-Spec + 14 Feature-Specs angelegt. Status-Tabelle gefüllt. Fundament-Reihenfolge dokumentiert. |
| 2026-10-13 | jsonl statt ndjson in allen relevanten Docs. |
| 2026-10-13 | Public GitHub-Repo `https://github.com/schirkan/bp-log-viewer-cli` angelegt. AGENTS.md § „Git" und § „⚠️ Projektspezifische Anweisung" (kein Workboard) ergänzt. |
| 2026-10-13 | **code-review** durchgeführt (Skill `code-review`, 2 CRITICAL + 8 MAJOR + 7 MINOR + 5 NIT identifiziert). |
| 2026-10-13 | **C1** MIT-Lizenz (`LICENSE`) + README-Badge. |
| 2026-10-13 | **C2** `MaxRowsAbsoluteCeiling` (Default 100 000) + Exit 8 in architecture § 8 + alle relevanten Specs. |
| 2026-10-13 | **M1** architecture § 7.3 SQL-Beispiel auf `BPVSessionInfo` umgestellt. |
| 2026-10-13 | **M2/M3** Status-Convention in architecture § 4.5 + F-001: Runtime-Lookup von `BPAStatus`, kein Hardcoding. |
| 2026-10-13 | **M4** Timezone-Policy in architecture § 4.4 + ENV `BP_LOG_VIEWER_TIMEZONE`. |
| 2026-10-13 | **M5** Single-Query-LEFT-JOIN-Constraint in architecture § 7.2 + Implementation Notes in F-006, F-007. |
| 2026-10-13 | **M6** LIKE-Performance-Warning in architecture § 7.2 + alle Specs mit `--text`. |
| 2026-10-13 | **M7** F-014 Output-Pfad-Test repariert (Schritt 7 schreibt gegen `PathResolver.Root`). |
| 2026-10-13 | **M8** F-014 um `version`-Sub-Command erweitert. |
| 2026-10-13 | **m1** Exit 10 für „not found" (statt 0) in architecture § 8 + F-003. |
| 2026-10-13 | **m3** Exit-Code-Tabelle in architecture § 8 vereinheitlicht (1–11). |
| 2026-10-13 | **m4** UTF-8-aware Spaltenbreite via `StringInfo.LengthInTextElements` in F-013. |
| 2026-10-13 | **m5** strengere Filter-Conflict-Erkennung in F-002 + F-011. |
| 2026-10-13 | **m6** ENV-Override-Tabelle in F-011 ergänzt. |
| 2026-10-13 | **m7** F-014 DB-Version-Vorrang vor Registry. |
| 2026-10-13 | **n1** Beispiel-Datum in architecture § 6.1 neutralisiert (`2025-06-15`). |
| 2026-10-13 | **n4** `.gitattributes` für konsistente Line-Endings. |
| 2026-10-13 | **Blocker #3** `appsettings.template.json` angelegt (versioniert; in Implementation kopieren und Connection-String eintragen). |
| 2026-10-13 | **Martin-Abnahme** aller 14 Specs („Specs ok" 2026-10-13 21:47) — alle Status von 📋 auf ✅. Implementierung startet. |
| 2026-10-14 | Implementierung fortgeführt: F-011 Common Filters stabilisiert (Validation + Exit-Codes), F-001-Teile für `self-test` (Config/LocalDB/DB/StatusCache) aktiv, F-002 `session list` gegen `BPVSessionInfo` mit jsonl-Output und Statusauflösung umgesetzt. |
