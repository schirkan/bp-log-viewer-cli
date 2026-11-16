# Feature-Spec — Environment Variables

> Liefert die Environment Variables aus `BPAEnvironmentVar` und die Prozess-Zuordnung
> aus `BPAProcessEnvironmentVarDependency`. Beide Tabellen gehören zu Control Room →
> Environment und beantworten die Frage „welche Variablen existieren und welcher
> Prozess benutzt welche".

## Identifikation

- **ID:** F-015
- **Titel:** Environment Variables
- **Slug:** environment-variables
- **Status:** ✅ Fertig
- **Priorität:** Mittel
- **Zugewiesen:** —
- **Erstellt am:** 2026-11-16
- **Letzte Aktualisierung:** 2026-11-16

## Ziel / Anwendungsfall

Ein Blue-Prism-Prozess referenziert Environment Variables über
`BPAProcessEnvironmentVarDependency`. Bei der Fehleranalyse ist die Frage „welche
Variable fehlt oder hat den falschen Wert?" sonst nur durch Blick in die
Control-Room-Oberfläche beantwortbar. Das CLI liefert beide Seiten direkt aus der
Datenbank: die Variablen selbst und die Zuordnung zu den Prozessen.

**Keine Redaction:** Die Werte in `BPAEnvironmentVar` sind normale
Konfigurationswerte der Prozessumgebung (Pfade, Schalter, Kennungen). Sie werden
unverändert ausgegeben. Secrets gehören nach Blue-Prism-Best practice in
`BPACredentials` und nicht in Environment Variables.

## Funktionale Anforderungen

1. Befehl: `bp-log-viewer environment vars` — listet alle Environment Variables.
2. Befehl: `bp-log-viewer environment dependencies` — listet die Zuordnung
   Variable → Prozess.
3. Tabellen:
   - `BPAEnvironmentVar` — Spalten `name`, `datatype`, `value`, `description`, `Id`
   - `BPAProcessEnvironmentVarDependency` — Spalten `id`, `processID`, `refVariableName`
   - Der Prozessname kommt per `LEFT JOIN BPAProcess` auf `processid`.
4. Filter `environment vars` (alle optional, Muster wie `process list`):
   - `--name <name>` — Präfix-Match (`name%`)
   - `--name-contains <text>` — Contains-Match (`%text%`)
   - Bei Kombination bestimmt die zuletzt angegebene Option Wert und Match-Modus.
5. Filter `environment dependencies` (alle optional):
   - `--process-name <name>` — Präfix-Match auf den Prozessnamen
   - `--variable <name>` — Präfix-Match auf `refVariableName`
6. Sortierung:
   - `environment vars`: `name ASC`
   - `environment dependencies`: Prozessname ASC, dann `refVariableName` ASC
7. Limit per `--max-rows <n>` (Default 1000; Hard-Ceiling wie F-002). Truncation
   meldet `output_truncated` + Exit 2.
8. Ausgabe als jsonl-Objekte (snake_case), ein Objekt je Datensatz:
   - `environment vars`: `id`, `name`, `datatype`, `value`, `description`
   - `environment dependencies`: `id`, `process_id`, `process_name`, `variable_name`
   - `null`-Felder werden weggelassen (Naming-Policy `SnakeCaseLower`).
9. Globale Optionen (`--output`, `--format`, `--max-rows`, `--connection-string`)
   gelten wie in allen anderen Datenbefehlen.
10. Leere Tabelle → leere Ausgabe, Exit 0, keine Warnung.

## Nicht-funktionale Anforderungen

- Parametrisierte Queries (CWE-89-Schutz); `value` ist ein reserviertes Wort und
  wird als `[value]` quotiert.
- `LEFT JOIN` auf `BPAProcess`: eine Dependency ohne passenden Prozess bleibt
  sichtbar (`process_name` wird weggelassen), statt zu verschwinden.
- JSONL auf stdout/stderr ist UTF-8 ohne BOM, auch bei Pipe-Umleitung.
- Antwort < 1 s für 10.000 Zeilen.

## Akzeptanzkriterien

1. `bp-log-viewer environment vars` liefert je Variable ein jsonl-Objekt mit
   `name`, `datatype`, `value` und `description`.
2. `--name X` liefert nur Variablen, deren Name mit `X` beginnt;
   `--name-contains X` liefert alle mit `X` im Namen.
3. `bp-log-viewer environment dependencies` liefert je Abhängigkeit `id`,
   `process_id`, `process_name` und `variable_name`.
4. Der Prozessname wird per Join aufgelöst; ohne Treffer wird `process_name` weggelassen.
5. `--process-name` und `--variable` filtern jeweils auf Präfix.
6. `--output` schreibt dieselben Datensätze in die Datei; `row_count` zählt nur
   Datenzeilen.
7. Fehlender Optionswert → `filter_invalid` im CLI-Dispatcher, `command_invalid`
   beim direkten Command-Aufruf; jeweils Exit 11.
8. Datenbankfehler → `db_error`, Exit 5 (wie `CommandHost.WriteDbError`).

## Out of Scope

- `BPAProcessEnvVar` und `BPAPackageEnvironmentVar` (prozess- und paketbezogene
  Zuordnungen ohne eigene Werte) — bleiben unberührt.
- `BPAEnvironment` / `BPAEnvironmentType` (die Umgebung selbst) — die
  Produktversion wird bereits in `self-test` gelesen.
- Werte aus `BPACredentials` — gehören nicht in Environment Variables.
- Schreibender Zugriff.

## Abhängigkeiten

- F-001, F-011, F-012, F-013.

## Offene Fragen

(keine)

## Verlauf

| Datum | Status | Änderung |
|:------|:-------|:---------|
| 2026-11-16 | Entwurf | Initial angelegt (Martin-Vorgabe: `BPAEnvironmentVar` + `BPAProcessEnvironmentVarDependency` einbinden, keine Redaction) |
| 2026-11-16 | Fertig | Implementiert: `environment vars` + `environment dependencies`, Ausgabe als jsonl-Objekte |
| 2026-11-16 | Validierung | Testvariablen per offizieller BP-CLI `/setev` angelegt; UTF-8-Umleitung, Filter-Reihenfolge und dokumentierte Fehlercodes geprüft. |
