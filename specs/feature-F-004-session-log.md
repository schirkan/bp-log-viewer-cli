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
2. Tabellenwahl zur **Laufzeit** ermitteln:
   - `SELECT COUNT(*) FROM BPASessionLog_NonUnicode` → wenn > 0, NonUnicode als Quelle.
   - sonst `BPASessionLog_Unicode`.
   - Beide leer → leere Antwort + Warning `no_log_entries: <session-number>`.
3. Filter (alle optional):
   - `--level <info|warn|error|debug>` (Mapping auf `resulttype`: 0=Info, 1=Warn, 2=Error, 3=Debug)
   - `--text <substring>` (`LIKE '%text%'` auf `result`)
   - `--stage <name>` (`stagename = @stage`)
4. Sortierung: `logid ASC` (chronologisch).
5. Limit per `--max-rows <n>` (Default 1000; für vollständige Logs empfohlen `--output` zu nutzen).
6. NDJSON-Felder (snake_case): `log_id, stage_name, stage_type, result_type, result_type_name, started_at, ended_at, duration_ms, result_preview, attribute_size_bytes`.
7. `result` wird auf 4000 Zeichen gekürzt (`result_preview`); Volltext per Flag `--full-result` (Achtung: Zeilen können MBs groß sein).

## Nicht-funktionale Anforderungen

- Antwort < 1 s für 10.000 Zeilen.
- Speicher-Streaming: nicht alle Zeilen in den Speicher laden — `Microsoft.Data.SqlClient.SqlDataReader` mit `CommandBehavior.SequentialAccess`.
- Bei `--output` wird die `MaxRowsBeforeFile`-Schwelle aus F-012 ignoriert (explizit in Datei).

## Akzeptanzkriterien

1. `bp-log-viewer session log 4711 --level error` liefert nur `resulttype=2` (Error).
2. `--text "timeout"` findet alle Zeilen mit `timeout` in `result`.
3. `result` mit 50 KB wird in `result_preview` auf 4000 Zeichen gekürzt; `--full-result` liefert vollständigen String.
4. Bei > `MaxRows` ohne `--output` → die ersten `MaxRows` + Warning + Exit 2 (wie F-002).
5. Tabelle automatisch erkannt: bei 0 Zeilen in `NonUnicode` automatisch Fallback auf `Unicode`.

## Out of Scope

- Live-Tail (laufende Session beobachten) — Vorschlag für F-015+.
- Log-Rotation / Archivierung.
- Parsen der `attributexml` (zu spezifisch, kann opt-in später kommen).

## Abhängigkeiten

- F-001, F-011, F-012, F-013.
- `resulttype`-Codes versionsabhängig — Mapping wird zur Laufzeit aus einer Lookup-View gelesen oder hartkodiert mit Konfigurations-Override (offene Frage).

## Offene Fragen

1. Soll `--level debug` standardmäßig ignoriert werden (BP-Default loggt Debug nicht)? (Vorschlag: nein, Filter gilt für alle, auch wenn Tabelle leer.)
2. Mapping `resulttype → name` als Hartcode oder Lookup-Tabelle? (Vorschlag: Hartcode mit dokumentiertem Default; spätere BP-Versionen können abweichen.)

## Verlauf

| Datum | Status | Änderung |
|:------|:-------|:---------|
| 2026-10-13 | 📋 Entwurf | Initial angelegt |
