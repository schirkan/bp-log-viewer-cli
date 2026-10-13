# Feature-Spec — Output Format (jsonl / Table)

> Definiert die beiden unterstützten Ausgabe-Formate und deren Eignung für KI vs. Operator.

## Identifikation

- **ID:** F-013
- **Titel:** Output Format
- **Slug:** output-format
- **Status:** 📋 Entwurf
- **Priorität:** Hoch
- **Zugewiesen:** —
- **Erstellt am:** 2026-10-13
- **Letzte Aktualisierung:** 2026-10-13

## Ziel / Anwendungsfall

Die KI ist primärer Konsument. **jsonl** (Newline-Delimited JSON, identisch zu
NDJSON) ist Stream-freundlich und gut parsenbar. Operator (sekundär) bevorzugt
eine menschenlesbare Tabelle.

## Funktionale Anforderungen

1. Globale Option `--format <jsonl|table>` (Default: `BpLogViewer.Output.DefaultFormat` aus `appsettings.json`, Default-Wert `jsonl`).
2. **jsonl** (Default):
   - Eine Zeile pro Datensatz, UTF-8, kein Padding.
   - Felder snake_case.
   - Zeitstempel ISO-8601 mit Offset (`2026-10-13T08:42:11+01:00`).
   - Null-Werte als JSON-`null` (nicht weggelassen).
3. **Table** (Operator):
   - Spaltenbreite via **`StringInfo.LengthInTextElements`** (UTF-8-aware) — korrekt für Em-Dash, CJK, Emoji, zusammengesetzte Glyphen.
   - Header-Zeile mit Spaltennamen.
   - Lange Felder (z. B. `exception_message`) werden auf 80 Zeichen gekürzt + `…`. Truncation erfolgt an **Grapheme-Cluster-Grenzen** (nicht mitten im Emoji).
   - `--max-rows` greift (Default 100), Warnung bei Überschreitung.
4. Fehler werden **immer** als JSON auf stderr ausgegeben, unabhängig vom `--format`.
5. Warnings ebenfalls auf stderr, eine pro Zeile: `{"warning": "<code>", "detail": "<…>"}`.

## Nicht-funktionale Anforderungen

- Trim-fest: `System.Text.Json` mit `JsonSerializerContext` (Source-Generator) für **alle** DTOs, siehe `dotnet-trimmed-binary-json-fix`-Skill. Pro DTO: `[JsonSerializable(typeof(T))]` in einer `JsonSerializerContext`-partial-class.
- Memory: jsonl-Streaming über `Utf8JsonWriter` direkt auf `stdout` (kein Zwischen-Buffer).
- Table-Renderer allokiert nur die berechnete Maximalbreite pro Spalte (kein vollständiger Materialize-in-Memory).

## Akzeptanzkriterien

1. `bp-log-viewer session list --format jsonl` erzeugt gültiges jsonl (jede Zeile `parse(JSON) = success`).
2. `bp-log-viewer session list --format table` erzeugt eine Spaltenüberschrift mit UTF-8-korrekter Breite.
3. Tabelle mit Emoji-Spalten (z. B. `last_stage: "Validation ✓"`) zeigt korrekte Ausrichtung (nicht verschoben).
4. Wechsel des Formats ändert die Felder nicht (gleiches Schema, anderes Rendering).
5. `appsettings.json` mit `BpLogViewer.Output.DefaultFormat=table` macht Table zum Default.

## Out of Scope

- CSV (Vorschlag F-020).
- Farbige Terminal-Ausgabe (kein ANSI bei KI-Spawn sinnvoll).
- Markdown-Tabellen.
- Interaktive Tabellen (Pager) — non-TTY-Output, KI-Spawn.

## Abhängigkeiten

- Wird von F-002 … F-010 referenziert.
- architecture § 6 (Output-Format), § 2 (Technologie-Stack).
- Skill `dotnet-trimmed-binary-json-fix`.

## Offene Fragen

_(keine — im Review gelöst: UTF-8-Breite via `StringInfo.LengthInTextElements`)_

## Verlauf

| Datum | Status | Änderung |
|:------|:-------|:---------|
| 2026-10-13 | 📋 Entwurf | Initial angelegt |
| 2026-10-13 | 📋 Entwurf | NDJSON → jsonl; m4: UTF-8-aware Spaltenbreite via `StringInfo.LengthInTextElements` |
