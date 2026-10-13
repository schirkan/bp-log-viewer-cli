# Feature-Spec — Common Filters

> Gemeinsame Filteroptionen für alle Lese-Kommandos.

## Identifikation

- **ID:** F-011
- **Titel:** Common Filters
- **Slug:** common-filters
- **Status:** 📋 Entwurf
- **Priorität:** Hoch
- **Zugewiesen:** —
- **Erstellt am:** 2026-10-13
- **Letzte Aktualisierung:** 2026-10-13

## Ziel / Anwendungsfall

Einheitliches Filter-Verhalten über alle Sub-Commands (`session`, `resource`,
`process`, `queue`, `audit`). KI muss nicht pro Sub-Command neue Filter-Syntax
lernen.

## Funktionale Anforderungen

| Option | Typ | Default | Bedeutung |
|:-------|:----|:--------|:----------|
| `--since <iso>` | ISO-8601 (UTC oder mit Offset) | `now-24h` (für `session`) / `now-7d` (für `audit`) | Start-Zeitpunkt |
| `--until <iso>` | ISO-8601 | `now` | End-Zeitpunkt |
| `--process <name>` | string | — | exakter Prozess-Name |
| `--resource <name>` | string | — | exakter Resource-Name |
| `--status <code>` | int oder Name | — | Session/Resource-Status, wiederholbar |
| `--has-exception` | flag | false | nur Sessions mit `exceptiontype IS NOT NULL` |
| `--level <info\|warn\|error\|debug>` | enum | — | `resulttype` im Log |
| `--text <substring>` | string | — | LIKE-Filter (siehe sub-command-spezifische Spalten) |
| `--max-rows <n>` | int | aus Config | Cutoff, Exit 2 wenn überschritten ohne `--output` |

## Nicht-funktionale Anforderungen

- Filter-Konflikte (z. B. `--has-exception` + `--status 3` = Completed) müssen erkannt und als Warnung auf stderr gemeldet werden (kein Fehler).
- ISO-8601-Eingaben akzeptieren sowohl `2026-10-13T12:00:00Z` als auch `2026-10-13T14:00:00+02:00`.

## Akzeptanzkriterien

1. Alle Lese-Kommandos akzeptieren mindestens die Filter `--since`, `--until`, `--max-rows`.
2. `--has-exception --status 3` erzeugt eine Warnung, brechen aber nicht ab.
3. Ungültiges ISO-Datum in `--since` → Exit 3, Fehlertext `filter_invalid: --since expects ISO-8601`.

## Out of Scope

- Reguläre Ausdrücke (Vorschlag: F-018).
- Saved filters / Filter-Presets.

## Abhängigkeiten

- Wird von F-002 … F-010 referenziert.
- architecture.md § 5 (Globale Optionen).

## Offene Fragen

1. Soll es einen gemeinsamen `--query` String geben (Mini-DSL)? (Vorschlag: nein, explizite Optionen sind robuster.)

## Verlauf

| Datum | Status | Änderung |
|:------|:-------|:---------|
| 2026-10-13 | 📋 Entwurf | Initial angelegt |
