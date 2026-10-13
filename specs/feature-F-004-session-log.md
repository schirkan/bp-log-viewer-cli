# Feature-Spec — Session Log

> Liefert die Log-Zeilen einer Session aus `BPASessionLog_NonUnicode` / `BPASessionLog_Unicode`.

## Identifikation

- **ID:** F-004
- **Titel:** Session Log
- **Slug:** session-log
- **Status:** 📋 Entwurf
- **Priorität:** Hoch
- **Zugewiesen:** —
- **Erstellt am:** 2026-10-13
- **Letzte Aktualisierung:** 2026-10-13

## Ziel / Anwendungsfall

Tatsächlicher Log-Zugriff pro Session. Wichtigstes Werkzeug für
Ursachenforschung. **Output kann groß sein** — daher zwingend mit F-012
(Output to File) zusammenarbeiten.

## Funktionale Anforderungen

1. Befehl: `bp-log-viewer session log <session-number>`.
2. Tabellenwahl zur **Laufzeit** ermitteln (2-stufige Kette, **erste** befüllte Tabelle gewinnt):
   1. `SELECT COUNT(*) FROM BPASessionLog_NonUnicode` → wenn > 0, Quelle = `BPASessionLog_NonUnicode`.
   2. sonst `BPASessionLog_Unicode` → wenn > 0, Quelle = `BPASessionLog_Unicode`.
   3. beide leer → leere Antwort + Warning `no_log_entries: <session-number>` + Exit 0.
   4. Ausgabe **jedes** Datensatzes enthält `source_table: <tablename>`, damit KI weiß, ob Daten aus NonUnicode oder Unicode stammen.
3. Filter (alle optional):
   - `--level <info|warn|error|debug>` (Mapping auf `resulttype`, siehe Implementation Notes unten)
   - `--text <substring>` (`LIKE '%text%'` auf `result` — **Performance-Warning** per architecture § 7.2)
   - `--stage <name>` (`stagename = @stage`)
4. Sortierung: `logid ASC` (chronologisch).
5. Limit per `--max-rows <n>` (Default 1000; **Hard-Ceiling** wie F-002). Für vollständige Logs → `--output` empfehlen.
6. jsonl-Felder (snake_case): `log_id, source_table, stage_name, stage_type, result_type, result_type_name, started_at, ended_at, duration_ms, result_preview, attribute_size_bytes`.
7. `result` wird auf 4000 Zeichen gekürzt (`result_preview`); Volltext per Flag `--full-result` (Achtung: Zeilen können MBs groß sein).
8. `resulttype_name` wird aus dem **Runtime-Lookup** abgeleitet (siehe Implementation Notes).

## Nicht-funktionale Anforderungen

- Antwort < 1 s für 10.000 Zeilen.
- Speicher-Streaming: `Microsoft.Data.SqlClient.SqlDataReader` mit `CommandBehavior.SequentialAccess` (verhindert OOM bei großen `result`/`attributexml`).
- Bei `--output` wird die `MaxRowsBeforeFile`-Schwelle ignoriert (explizit in Datei).
- Parametrisierte Queries (CWE-89-Schutz).

## Implementation Notes (zwingend)

- **`resulttype` → Name Mapping** wird **nicht hartkodiert**. Beim Start 1× Mapping aus der BP-DB laden (z. B. aus `BPAStatus` falls dort geführt, oder eigene Tabelle `BPALogResultType` falls vorhanden) und gecacht für die Prozess-Dauer.
- Falls BP-Version das Mapping ändert: **Daten-Update**, kein Code-Deploy.
- Default-Mapping (für Self-Test bis Lookup greift): `0=Information, 1=Warning, 2=Error, 3=Debug, 4=Exception` — hartkodiert als **letzter Fallback**, mit Kommentar im Code, dass dies überschrieben werden soll.

## Akzeptanzkriterien

1. `bp-log-viewer session log 4711 --level error` liefert nur `resulttype=2` (Error).
2. `--text "timeout"` findet alle Zeilen mit `timeout` in `result` + Warning `text_filter_perf_warning`.
3. `result` mit 50 KB wird in `result_preview` auf 4000 Zeichen gekürzt; `--full-result` liefert vollständigen String.
4. Bei > `MaxRows` ohne `--output` → die ersten `MaxRows` + Warning + Exit 2.
5. Tabelle automatisch erkannt: bei 0 Zeilen in `NonUnicode` automatisch Fallback auf `Unicode`.
6. `result_type_name` ist nicht `null` (Runtime-Lookup erfolgreich).
7. Bei beiden Tabellen = 0 Zeilen → Warning `no_log_entries` + Exit 0.
8. `source_table` in jedem Datensatz befüllt.

## Out of Scope

- Live-Tail (laufende Session beobachten) — Vorschlag F-021.
- Log-Rotation / Archivierung.
- Parsen der `attributexml` (zu spezifisch, kann opt-in später kommen).
- SQL Server Full-Text Search — v2 (`--text-fulltext`).

## Abhängigkeiten

- F-001, F-011, F-012, F-013.
- architecture § 7.2 (LIKE-Performance, Streaming), § 7.3 (Pflicht-Query).
- `dotnet-trimmed-binary-json-fix`-Skill für Streaming-Pfad.

## Offene Fragen

_(keine — Mapping-Lookup im Review entschieden)_

## Verlauf

| Datum | Status | Änderung |
|:------|:-------|:---------|
| 2026-10-13 | 📋 Entwurf | Initial angelegt |
| 2026-10-13 | 📋 Entwurf | NDJSON → jsonl; M3: resulttype → Name via Runtime-Lookup, nicht hartkodiert |
| 2026-10-13 | 📋 Entwurf | **2-stufige Tabellen-Prioritätskette** (NonUnicode → Unicode); `source_table`-Feld im Output für KI-Transparenz |
| 2026-10-13 | 📋 Entwurf | pre-65-Tabellen (`*_pre65`) wieder rausgenommen (Martin-Vorgabe 2026-10-13) |
