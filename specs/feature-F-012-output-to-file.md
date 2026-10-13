# Feature-Spec — Output to File

> Schreibt das Ergebnis in eine Datei statt nach stdout, wenn die Datenmenge zu groß wird.

## Identifikation

- **ID:** F-012
- **Titel:** Output to File
- **Slug:** output-to-file
- **Status:** 📋 Entwurf
- **Priorität:** Hoch (Martin-Vorgabe 2026-10-13)
- **Zugewiesen:** —
- **Erstellt am:** 2026-10-13
- **Letzte Aktualisierung:** 2026-10-13

## Ziel / Anwendungsfall

Wenn das Log einer Session mehrere tausend Zeilen umfasst, sprengt das den
Kontext eines KI-Aufrufs. Die CLI muss dann automatisch (Schwellwert aus
Config) oder explizit (`--output <path>`) in eine Datei schreiben und dem
Aufrufer den Pfad zurückgeben.

## Funktionale Anforderungen

1. Globale Option `--output <path>` (oder `--out-file <path>`).
2. Pfad-Auflösung über `PathResolver` (architecture § 4.2) gegen `AppContext.BaseDirectory`, **nicht** gegen `Environment.CurrentDirectory`. Grund: KI-Agent spawnt Binary mit fremdem CWD (siehe `dotnet-config-path-anchoring`-Skill).
   - Absolute Pfade: erlaubt.
   - Relative Pfade: gegen `AppContext.BaseDirectory` aufgelöst.
   - Optionaler ENV-Override `BP_LOG_VIEWER_OUTPUT_ROOT` (architecture § 4.3): wenn gesetzt, werden relative Pfade gegen diesen Root aufgelöst, **nicht** gegen `AppContext.BaseDirectory`. Pfad-Traversal (`..`) wird in beiden Fällen normalisiert und gegen den Anker geprüft.
3. Ausgabe-Format identisch zu stdout (jsonl oder Table).
4. Statt der Daten wird auf stdout ein einzeiliger Marker geschrieben: `{"output_written": "<absoluter Pfad>", "row_count": <n>, "bytes": <n>}`. (Marker ist immer jsonl, unabhängig vom `--format`.)
5. Datei wird mit `FileMode.Create`, `FileAccess.Write`, `FileShare.Read` geöffnet. UTF-8, kein BOM.
6. Wenn `--output` gesetzt ist, wird die `MaxRowsBeforeFile`-Schwelle ignoriert (alle Zeilen).
7. Ohne `--output`, aber mehr Zeilen als `MaxRowsBeforeFile`:
   - Die ersten `MaxRows` Zeilen werden **trotzdem** ausgegeben.
   - Auf stderr: Warning `output_truncated: <n> of <total> rows. Use --output to write full result.`
   - Exit-Code 2.
8. Existiert der Zielpfad als Verzeichnis → Fehler `output_is_directory` + Exit 6.
9. Zielpfad nicht beschreibbar (Permissions, fehlende Parent-Dirs) → Fehler + Exit 6.

## Nicht-funktionale Anforderungen

- Atomic-Write optional: nicht in der ersten Version, nur Plain-Write.
- Streaming: `StreamWriter` mit `leaveOpen=false`, Buffer-Größe Default.
- Pfad mit `..` wird vom `PathResolver` normalisiert; außerhalb des Ankers (`AppContext.BaseDirectory` oder `BP_LOG_VIEWER_OUTPUT_ROOT`) ist nicht erlaubt.

## Akzeptanzkriterien

1. `bp-log-viewer session log 4711 --output logs/s4711.jsonl` schreibt in `<binary-dir>/logs/s4711.jsonl`, auch wenn CWD `C:\Windows` ist.
2. Marker auf stdout enthält absoluten Pfad und Zeilen-Anzahl.
3. Negative-Probe: `bp-log-viewer session log 4711 --output /etc/passwd` (Linux-Pfad) → Exit 6.
4. Negative-Probe: `bp-log-viewer session log 4711 --output existing-dir/` → Exit 6 (`output_is_directory`).
5. Negative-Probe: `bp-log-viewer session log 4711 --output ../../etc/passwd` (Path-Traversal) → Exit 6.
6. Bei `--output` und gleichzeitigem Schwellwert-Überschreitung: voller Output in Datei, kein Warning.

## Out of Scope

- Komprimierte Outputs (gzip) — Vorschlag F-019.
- Verschlüsselte Outputs.
- Multi-File-Splits.

## Abhängigkeiten

- F-013 (Output-Format identisch).
- F-011 (`--max-rows`).
- Skill `dotnet-config-path-anchoring` (Pfad-Resolution).
- architecture § 4.2, § 4.3, § 6.1, § 8.

## Offene Fragen

_(keine — im Review gelöst: Vorschlag C umgesetzt, beide Anker dokumentiert)_

## Verlauf

| Datum | Status | Änderung |
|:------|:-------|:---------|
| 2026-10-13 | 📋 Entwurf | Initial angelegt |
| 2026-10-13 | 📋 Entwurf | NDJSON → jsonl; m3: Exit-Code 6 für Pfad-Fehler vereinheitlicht; ENV-Override `BP_LOG_VIEWER_OUTPUT_ROOT` ergänzt |
