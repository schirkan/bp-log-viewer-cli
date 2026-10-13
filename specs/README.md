# Specs — Übersicht (BP Log Viewer CLI)

**Stand:** 2026-10-13
**Quelle der Konventionen:** `projects/PROJECT-RULES.md` § „File Management"

## Konventionen

- **Eine Spec-Datei pro Feature:** `specs/feature-<ID>-<slug>.md`
- **ID:** fortlaufende Nummer (`F-001`, `F-002`, …), unveränderlich nach Vergabe.
- **Slug:** kurze Beschreibung in Kleinschreibung, Bindestrich-getrennt, ASCII.
- **Template:** `specs/_TEMPLATE.md` als Vorlage für jede neue Feature-Spec.
- **Reihenfolge der Bearbeitung:** Spec **vor** Implementierung (Martin-Vorgabe 2026-10-02).
- **Cross-References:** Feature-IDs in Notizen/Akzeptanzkriterien verwenden, nicht Dateinamen.
- **Out-of-Scope** ist Pflichtfeld — was bewusst weggelassen wird, wird mit Begründung erfasst.

## Status-Indikatoren

| Symbol | Status | Voraussetzung für Wechsel |
|:-------|:-------|:--------------------------|
| 📋 | Entwurf | — |
| 🗔 | In Review | Spec vollständig ausgefüllt, an Martin übergeben |
| ✅ | Freigegeben | Martin-Abnahme |
| 🚧 | In Umsetzung | Workboard-Karte aktiv |
| ✔️ | Fertig | Implementierung verifiziert |
| ❌ | Verworfen | (immer mit Begründung) |

## Zusatz-Dokumente

| Datei | Inhalt |
|:------|:-------|
| `architecture.md` | Tech-Stack, Befehls-Struktur, Pfad-Resolution, Error-Model, Self-Test |
| `blue-prism-installation.md` (in `context/`) | Befund der BP-Installation (Version, Pfade, Service, Registry) |
| `blue-prism-database-schema.md` (in `context/`) | Befund des DB-Schemas (213 Objekte, Schlüsseltabellen) |

## Status-Übersicht

| ID | Titel | Status | Priorität | Spec | Abhängigkeiten |
|:---|:------|:-------|:----------|:-----|:---------------|
| F-001 | Database Connection Management | 📋 Entwurf | Hoch | [spec](feature-F-001-db-connection.md) | (Fundament) |
| F-002 | Session List | 📋 Entwurf | Hoch | [spec](feature-F-002-list-sessions.md) | F-001, F-011, F-012, F-013 |
| F-003 | Session Details | 📋 Entwurf | Hoch | [spec](feature-F-003-session-details.md) | F-001, F-011, F-012, F-013 |
| F-004 | Session Log | 📋 Entwurf | Hoch | [spec](feature-F-004-session-log.md) | F-001, F-011, F-012, F-013 |
| F-005 | Search Errors | 📋 Entwurf | Hoch | [spec](feature-F-005-search-errors.md) | F-001, F-002, F-011, F-012, F-013 |
| F-006 | Resources | 📋 Entwurf | Mittel | [spec](feature-F-006-resources.md) | F-001, F-011, F-012, F-013 |
| F-007 | Processes | 📋 Entwurf | Mittel | [spec](feature-F-007-processes.md) | F-001, F-011, F-012, F-013 |
| F-008 | Work Queues | 📋 Entwurf | Mittel | [spec](feature-F-008-work-queues.md) | F-001, F-011, F-012, F-013 |
| F-009 | Work Queue Items | 📋 Entwurf | Mittel | [spec](feature-F-009-queue-items.md) | F-001, F-008, F-011, F-012, F-013 |
| F-010 | Audit Events | 📋 Entwurf | Niedrig | [spec](feature-F-010-audit-events.md) | F-001, F-011, F-012, F-013 |
| F-011 | Common Filters | 📋 Entwurf | Hoch | [spec](feature-F-011-common-filters.md) | architecture § 5 |
| F-012 | Output to File | 📋 Entwurf | **Hoch** (Martin-Vorgabe) | [spec](feature-F-012-output-to-file.md) | F-011, F-013, Skill `dotnet-config-path-anchoring` |
| F-013 | Output Format (jsonl / Table) | 📋 Entwurf | Hoch | [spec](feature-F-013-output-format.md) | Skill `dotnet-trimmed-binary-json-fix` |
| F-014 | Self-Test & Diagnostics | 📋 Entwurf | Mittel | [spec](feature-F-014-self-test.md) | F-001, architecture § 9 |

**Zusammenfassung:** 14 Specs, 8 Hoch / 5 Mittel / 1 Niedrig. Fundament-Reihenfolge: **F-013 → F-011 → F-001 → F-002 → F-003 → F-004 → F-005 → F-012 → F-014 → F-006 → F-007 → F-008 → F-009 → F-010**.

## Spec-Datei-Verzeichnis

| Datei | Inhalt |
|:------|:-------|
| `README.md` | Diese Übersicht |
| `_TEMPLATE.md` | Vorlage für eine Feature-Spec |
| `architecture.md` | Architektur-Spec (Tech-Stack, Layout, Konfiguration, Fehlerbehandlung) |
| `feature-F-001-…md` … `feature-F-014-…md` | Feature-Specs |

## Verlauf

| Datum | Änderung |
|:------|:---------|
| 2026-10-02 | Übersicht + Template angelegt. Status-Konvention etabliert. |
| 2026-10-13 | Architektur-Spec + 14 Feature-Specs angelegt. Status-Tabelle gefüllt. Fundament-Reihenfolge dokumentiert. |
