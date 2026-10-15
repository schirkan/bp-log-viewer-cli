# BP Log Viewer CLI – vollständige Parameter- und Nutzungsreferenz

Diese Referenz beschreibt den Implementierungsstand im Repository vom 15.10.2026, nicht nur die verkürzte CLI-Hilfe. Beispiele verwenden fiktive Kennungen und Namen. Bei abweichenden Binary-Versionen die Version prüfen und die Implementierung als maßgebliche Quelle heranziehen.

## 1. Voraussetzungen und Aufruf

- Windows x64 und .NET 8 Runtime x64; framework-dependent, single-file, untrimmed. .NET Framework 4.x reicht nicht aus.
- Erreichbare Blue-Prism-7.5.x-Datenbank auf SQL Server oder LocalDB und entsprechend berechtigter Datenbankzugang.
- Vorhandene `bp-log-viewer.exe` und eingerichtete Verbindung. Das CLI steuert keine RPA-Ausführungen.

```powershell
$cli = 'D:\Tools\BPLogViewer\bp-log-viewer.exe'
& $cli version
$exitCode = $LASTEXITCODE
& $cli session list --max-rows 50
$exitCode = $LASTEXITCODE
```

Der Pfad ist ein Beispiel, kein fest vorausgesetzter Installationsort. Verwende einen vorhandenen absoluten Windows-Pfad und `&`; verlasse dich nicht auf die PATH-Suche. Namen mit Leerzeichen in Anführungszeichen setzen.

Allgemeine Syntax: `bp-log-viewer [globale-optionen] <befehl> <unterbefehl> [kennung] [optionen]`.

- Globale Wertoptionen funktionieren vor dem Hauptbefehl oder nach dem Unterbefehl.
- Befehlsspezifische Optionen und Positionsargumente hinter den Unterbefehl setzen.
- Haupt- und Unterbefehl direkt nebeneinander schreiben: `session list --max-rows 50`, nicht `session --max-rows 50 list`.
- Optionen als getrennte Tokens schreiben: `--max-rows 50`, nicht `--max-rows=50`.
- Befehle und Optionsnamen sind case-sensitive. `--level` und `--format` verlangen die dokumentierten Kleinbuchstaben; `--type process|object` akzeptiert auch andere Groß-/Kleinschreibung.
- Unbekannte Optionen werden überwiegend mit `filter_unknown_option` gewarnt und ignoriert, nicht zuverlässig abgewiesen. Befehlsspezifische Optionen können dieselbe Warnung erzeugen, obwohl sie anschließend vom Befehlsparser verarbeitet werden.
- Wiederholte einfache globale Wertoptionen verwenden den letzten Wert; `--status` sammelt mehrere Werte. Befehlsspezifische Mehrfachoptionen stehen unten.
- Keine allgemeine Unterstützung für `--` als Ende der Optionsliste, Paging, `--offset`, `--tail` oder Follow-Modus voraussetzen.

## 2. Hilfe und Version

| Aufruf | Verhalten |
|---|---|
| Ohne Argumente, `help`, `--help`, `-h` | Root-Hilfe als Klartext auf stderr, Exit 0 |
| `session help`, `resource help`, `process help`, `queue help`, `audit help` | Kurze Syntaxhilfe für die Gruppe; auch ohne Unterbefehl oder mit `--help`/`-h` möglich |
| `version`, `--version`, `-v` | Ein JSONL-Objekt mit Versionsinformationen; bevorzugt `version` verwenden |

Die globale Filterprüfung kann auch bei Hilfe-/Versionsflags Warnungen erzeugen. Eine Hilfeoption hinter einem bereits gewählten Unterbefehl, etwa `session log --help`, ist keine vollständige Unterbefehlshilfe und kann eine echte Abfrage bzw. einen Parserfehler auslösen.

Versionsfelder: `cli_version`, `build_commit`, `build_date`, `target_framework`, `runtime`. Bei Single-File-Builds kann `build_date` ohne `BUILD_DATE`-Umgebungsvariable `unknown` sein; `GIT_COMMIT` ist ein Fallback für Commit-Metadaten.

## 3. Globale Optionen

Global akzeptiert bedeutet nicht global wirksam. Die Filtermatrix im nächsten Abschnitt ist verbindlich.

| Option | Werte / Standard | Wirkung und Hinweise |
|---|---|---|
| `--connection-string <cs>` | ADO.NET-Verbindungszeichenfolge | Höchste Override-Priorität für Datenbefehle; bei `self-test` derzeit nicht angewendet. Nur dokumentiert, für Agent-Aufrufe keine Zugangsdaten als Argumente verwenden. |
| `--output <path>` | Kein Standardpfad | JSONL-Datei statt Datensätzen auf stdout; dort erscheint ein Dateimarker. Root-Beschränkung und Überschreiben beachten. |
| `--out-file <path>` | Alias von `--output` | Gleiche Wirkung. |
| `--max-rows <n>` | Ganzzahl 0 bis 100000 | Meist Standard 1000; `audit list` 500. Mit Ausgabeziel und ohne explizites Limit: 100000. 0 ist gültig, liefert bei begrenzten Listen keine Zeilen und ggf. Trunkierungswarnung; bedeutet nicht unbegrenzt. |
| `--since <datetime>` | Befehlsabhängig | Untere Zeitgrenze inklusive. ISO-8601 empfohlen; ohne Offset als UTC interpretiert, Offset in UTC normalisiert. |
| `--until <datetime>` | Befehlsabhängig | Obere Zeitgrenze exklusiv. Wenn beide angegeben: `since <= until`; gleicher Zeitpunkt ist gültig, aber ein leeres Intervall. |
| `--process <name>` | Kein Filter | Exakter Prozessname bei Session-Listen und Session-Fehlern; nicht der Namensfilter von `process list` oder `audit list`. |
| `--resource <name>` | Kein Filter | Exakter Ressourcenname bei Session-Listen und Session-Fehlern; nicht der Namensfilter von `resource list` oder `audit list`. |
| `--status <code-or-name>` | Mehrfach möglich | Session-Status aus `BPAStatus`, Ressourcenstatus oder Queue-Item-Status; Bedeutung hängt vom Befehl ab. |
| `--has-exception` | Aus | Bei `session list`: nur Datensätze mit nicht-null `exceptiontype`. Kein universeller Filter für jeden Abbruch. |
| `--level <level>` | Kein Filter | `info`, `warn`, `error`, `debug`; nur `session log`. |
| `--text <text>` | Kein Filter | SQL-LIKE-Suche in Session-Exceptiontext, Log-Ergebnis oder Queue-Key/Exception-Grund, je nach Befehl. |
| `--format <format>` | Akzeptiert `jsonl`, `table` | Derzeit keine Tabellenformatierung implementiert: Datenbefehle liefern auch bei `table` JSONL. |
| `--verbose` | Aus | Wird geparst, derzeit ohne zusätzliche Ausgabe. |

LIKE-Suchen verwenden SQL-Muster: `%`, `_` und Zeichenklassen können als Wildcards wirken; sie werden nicht als literale Zeichen escaped. Vergleiche und Case-Sensitivity von Datenwerten hängen von der SQL-Server-Collation ab. `--text` kann teuer sein und erzeugt bei entsprechenden Befehlen `text_filter_perf_warning`; andere Filter möglichst zuerst einschränken.

## 4. Filterwirkung und Standards

`--connection-string` und `--output` wirken auf alle folgenden Datenbefehle. `--format` und `--verbose` ändern ihre Ausgabe derzeit nicht.

| Befehl | Wirksame globale Datenfilter | Zeitfeld / Standardzeitraum | Standardlimit / Sortierung |
|---|---|---|---|
| `session list` | since, until, process, resource, status, has-exception | Session-Start; letzte 24h bis jetzt | 1000; Start absteigend |
| `session get` | Keine Listenfilter | Kennung bestimmt Session | 1 Session; verknüpfte Items separat auf 100 begrenzt |
| `session log` | level, text | Keine serverseitige Zeitfilterung | 1000; `logid` aufsteigend, also früheste Einträge zuerst |
| `session errors` | since, until, process, resource, text | Session-Start; letzte 24h bis jetzt | 1000; Start absteigend |
| `session errors --aggregate` | Wie `session errors` | Session-Start; letzte 24h bis jetzt | Kein Zeilenlimit; Anzahl absteigend |
| `resource list` | status | Kein Zeitfilter | 1000; Name aufsteigend |
| `process list` | Keine globalen Datenfilter | Nur spezifisches `--has-errors-since` | 1000; Name aufsteigend |
| `queue list` | Keine globalen Datenfilter | Kein Zeitfilter | 1000; Name aufsteigend |
| `queue items` | since, until, text; status hinter Unterbefehl | `lastupdated`; ohne Zeitangaben alle passenden Items | 1000; Priorität absteigend, dann Update absteigend |
| `audit list` | since, until | Ereigniszeit; since = jetzt minus 7 Tage, until = offen | 500; Ereigniszeit absteigend |

Ignorierte Filter sind kein Fehler: Beispielsweise begrenzt `session log 4711 --since ...` das Log nicht zeitlich, `process list --process X` filtert keinen Prozess, und `audit list --resource X` filtert kein Audit-Ziel. Nutze die befehlsspezifischen Optionen.

## 5. Befehlsreferenz

### `self-test`

Prüft Konfiguration, DB-Verbindung, Status-Cache, BP-Version, Kernobjekte, aktive Logtabelle und Schreibzugriff. Keine eigenen Optionen.

```powershell
& $cli self-test
$exitCode = $LASTEXITCODE
```

- Mehrere JSONL-Schrittobjekte auf stdout mit `step`, `status`, `detail` und schrittspezifischen Feldern; abschließend `self_test: ok|degraded` und `duration_ms`.
- Prüfschritte: `config`, `db_connect`, `status_cache`, `bp_version`, `tables`, `active_log_table`, `write_test`.
- Fehlende Konfiguration: Exit 3; fehlgeschlagene Verbindung: Exit 5, jeweils frühzeitiger Abbruch.
- Sonstige degradierte Checks können trotzdem Exit 0 liefern. Schrittstatus und Abschlussobjekt auswerten.
- Schreibt und löscht eine Datei unter `temp` neben der Binary; das Verzeichnis kann bestehen bleiben. Kein DB-Schreibzugriff.
- `--connection-string`, `--output`, `--max-rows`, `--format` und Filter werden hier nicht angewendet. Konfigurations-/ENV-Verbindung verwenden; `BP_LOG_VIEWER_OUTPUT_ROOT` steuert diesen Schreibtest nicht.

### `session list`

Syntax: `session list [globale-optionen]`. Quelle: `BPVSessionInfo`.

```powershell
& $cli session list --process 'Rechnungsverarbeitung' --resource 'BOT-01' --since '2026-10-15T00:00:00Z' --until '2026-10-16T00:00:00Z' --max-rows 50
& $cli session list --has-exception --max-rows 50
```

`--status` ist mehrfach möglich und verknüpft Werte mit ODER. Namen oder IDs müssen in der zur Laufzeit geladenen `BPAStatus`-Tabelle existieren; unbekannte Werte liefern `unknown_status_value`, Exit 11. Keine festen Statuscodes voraussetzen. `--text` und `--level` wirken hier nicht.

Ausgabe: `session_number`, `session_id`, `process`, `process_id`, `resource`, `resource_id`, `status`, `status_name`, `started_at`, `ended_at`, `duration_sec`, `last_stage`, `exception_type`, `exception_message`.

Session-Datetime-Werte werden als UTC interpretiert und zum gespeicherten Offset in **Sekunden** konvertiert (7200 = +02:00). Beispiel: 11:13 UTC → 13:13+02:00, nicht 11:13+02:00. Dies gilt auch für `session get` und einzelne `session errors`. Logs bleiben UTC (`+00:00`); unterschiedliche Offsets sind konsistent, wenn die UTC-Zeitpunkte übereinstimmen. `--since`/`--until` filtern den UTC-Zeitpunkt, nicht die lokalisierte Uhrzeit.

### `session get`

Syntax: `session get <positive-session-number> [--include-state-xml]` oder `session get --session-id <uuid> [--include-state-xml]`.

| Parameter | Wirkung |
|---|---|
| `<session-number>` | Positive Ganzzahl; genau eine Session auswählen. |
| `--session-id <uuid>` | Alternative zur Session-Nummer; beides gleichzeitig ist ungültig. |
| `--include-state-xml` | Startparameter- und Session-State-XML zusätzlich laden; erzeugt `state_xml_included` mit Zeichenlängen. Standardmäßig nicht ausgegeben. |

```powershell
& $cli session get 4711
& $cli session get --session-id '11111111-2222-3333-4444-555555555555'
& $cli session get 4711 --include-state-xml
```

Zusätzlich zu Session-Feldern: `is_running`, `queue_id`, optional `start_params_xml`, `session_state_xml`, `truncated`, `work_queue_items`. Die eingebetteten Queue-Items enthalten `key_value`, `status`, `attempt`, `exception_reason`, `exception_reason_tag`, `loaded_at`, `finished_at`.

`work_queue_items` ist fest auf 100 begrenzt. `truncated: true` und eine Warnung zeigen Kürzung an, aber der Exit-Code bleibt bei erfolgreicher Ausgabe 0. `--max-rows` und `--output` heben diese interne Grenze nicht auf. Fehlende Session: Warnung `session_not_found`, Exit 10.

### `session log`

Syntax: `session log <positive-session-number> [--stage <name>] [--full-result] [--level <level>] [--text <text>] [--max-rows <n>]`.

| Parameter | Wirkung |
|---|---|
| `<session-number>` | Positive Ganzzahl; keine direkte UUID-Auswahl. UUID zuerst mit `session get --session-id` auflösen. |
| `--stage <name>` | Exakter Stage-Name. |
| `--full-result` | Vollständigen `result`-Text statt Vorschau bis 4000 Zeichen laden; Ausgabefeld bleibt `result_preview`. |
| `--level info` | Resulttyp 0. |
| `--level warn` | Resulttyp 1. |
| `--level error` | Resulttypen 2 und 64. Typ 4 (`Exception`) ist nicht enthalten. |
| `--level debug` | Resulttyp 3. |
| `--text <text>` | LIKE-Suche im vollständigen `result`, nicht nur der ausgegebenen Vorschau. |

```powershell
& $cli session log 4711 --level error --max-rows 200
& $cli session log 4711 --stage 'Anwendung öffnen' --full-result --max-rows 200
& $cli session log 4711 --text 'Timeout' --max-rows 200
```

Ausgabe: `log_id`, `source_table`, `stage_name`, `stage_type`, `result_type`, `result_type_name`, `started_at`, `ended_at`, `duration_ms`, `result_preview`, `attribute_size_bytes`. Resulttypnamen werden optional aus `BPALogResultType` ergänzt; bei Problemen Warnung `result_type_table_unavailable` und Fallback-Namen.

**Wichtige Grenzen:**

- Keine Wirkung von `--since`, `--until`, `--process`, `--resource`, `--status` oder `--has-exception`.
- Aufsteigende `logid`-Sortierung: Kürzung schneidet spätere Einträge ab, nicht frühere. Es gibt keinen Startoffset, keinen Zeitfilter, keinen Tail-Modus. Für vollständigen Kontext Limit erhöhen oder Stage/Text gezielt filtern; oberhalb 100000 bleibt eine Lücke.
- Die aktive Tabelle wird datenbankweit gewählt: Nicht-leere `BPASessionLog_NonUnicode` hat Vorrang, sonst `BPASessionLog_Unicode`. Es werden nicht beide Tabellen für die ausgewählte Session zusammengeführt.
- Wenn beide Tabellen leer sind: `no_log_entries`, Exit 0. Eine leere Ausgabe für eine bestimmte Session beweist weder deren Existenz noch Fehlerfreiheit; gegebenenfalls `session get` und `source_table`/Tabellenwahl prüfen.

### `session errors`

Syntax: `session errors [--aggregate] [--exception-type <type>] [globale-optionen]`.

| Parameter | Wirkung |
|---|---|
| `--aggregate` | SQL-Aggregation nach Exception-Typ statt einzelner Sessions. |
| `--exception-type <type>` | Mehrfach möglich; ODER-Auswahl der Exception-Typen. |
| `--text <text>` | LIKE-Suche im Session-Exceptiontext. |

```powershell
& $cli session errors --since '2026-10-15T00:00:00Z' --until '2026-10-16T00:00:00Z' --process 'Rechnungsverarbeitung' --max-rows 50
& $cli session errors --aggregate --since '2026-10-01T00:00:00Z' --until '2026-10-16T00:00:00Z'
& $cli session errors --exception-type 'System Exception' --text 'Timeout' --max-rows 50
```

Wählt Sessions mit nicht-null `exceptiontype`, nicht sämtliche Arten eines Prozessabbruchs. `--status`, `--level` und `--has-exception` verändern die Auswahl nicht. Ohne Aggregation dieselben Felder wie `session list`.

Aggregatfelder: `exception_type`, `count`, `first_seen`, `last_seen`, `processes`, `resources`, `sample_session_numbers` (maximal 5), `sample_message_preview` (maximal 200 Zeichen). Gruppierung nur nach Typ, nicht nach identischer Meldung oder Ursache. `first_seen`/`last_seen` beziehen sich auf Session-Starts. `--max-rows` wird im Aggregatmodus nicht zur Begrenzung benutzt; der Parser validiert den Wert dennoch. `--output` kann die Aggregate in eine Datei schreiben.

### `resource list`

Syntax: `resource list [--name <prefix> | --name-contains <text>] [--status <value>] [--max-rows <n>]`.

| Parameter | Wirkung |
|---|---|
| `--name <prefix>` | Namenspräfix, nicht exakter Vergleich. |
| `--name-contains <text>` | Name enthält das SQL-LIKE-Muster. |
| `--status <value>` | Mehrfach möglich. Auflösbare `BPAStatus`-Werte werden als IDs verwendet; andere Werte als `DisplayStatus`-Namen verglichen. ODER-Verknüpfung. |

```powershell
& $cli resource list --name 'BOT-' --max-rows 50
& $cli resource list --name-contains 'Finance' --max-rows 50
```

Ausgabe: `resource_id`, `name`, `display_status`, `status_id`, `processes_running`, `actions_running`, `last_updated`, `last_logged_in`, `current_session_number`, `current_session_started_at`, `fqdn`. Die aktuelle Session wird als jüngste Session ohne Endzeit ermittelt; dies allein beweist keine aktive oder gesunde Ausführung.

### `process list`

Syntax: `process list [--name <prefix> | --name-contains <text>] [--type process|object] [--has-errors-since <datetime>] [--max-rows <n>]`.

| Parameter | Wirkung |
|---|---|
| `--name <prefix>` | Namenspräfix. |
| `--name-contains <text>` | Enthaltenes Namensmuster. Nicht zusammen mit `--name` verwenden. |
| `--type process|object` | Prozess oder Business Object; DB-Codes P/O werden umgesetzt. |
| `--has-errors-since <datetime>` | Nur Prozesse/Objekte mit Sessions mit nicht-null Exception-Typ seit dem angegebenen Session-Startzeitpunkt. Ohne Offset UTC. |

```powershell
& $cli process list --type process --name-contains 'Rechnung' --max-rows 50
& $cli process list --has-errors-since '2026-10-01T00:00:00Z' --max-rows 50
```

Ausgabe: `process_id`, `name`, `type`, `version`, `last_modified`, `last_session_number`, `last_session_started_at`, `session_count_total`, `session_count_failed_since`, `avg_duration_sec`.

Ohne `--has-errors-since` zählt `session_count_failed_since` alle vorhandenen Sessions mit Exception-Typ. Durchschnittsdauer ist standardmäßig enthalten, basiert auf Sessions mit Endzeit und kann ohne solche Sessions fehlen. Es gibt keinen implementierten `--avg-duration`-Schalter und keine Ausgabe von Prozess-XML.

### `queue list`

Syntax: `queue list [--name <prefix> | --name-contains <text>] [--only-exceptions] [--only-running] [--max-rows <n>]`.

| Parameter | Wirkung |
|---|---|
| `--name <prefix>` | Namenspräfix. |
| `--name-contains <text>` | Enthaltenes Namensmuster. |
| `--only-exceptions` | Nur Queues mit mindestens einem als `Exception` gezählten Item. |
| `--only-running` | Nur Queues mit `running = true`. Zusammen mit `--only-exceptions`: UND. |

```powershell
& $cli queue list --only-exceptions --max-rows 50
& $cli queue list --name-contains 'Rechnung' --only-running --max-rows 50
```

Ausgabe: `queue_id`, `name`, `key_field`, `running`, `max_attempts`, `process_id`, `item_count_total`, `item_count_pending`, `item_count_locked`, `item_count_completed`, `item_count_exception`, `item_count_deferred`, `oldest_pending_loaded_at`, `newest_pending_loaded_at`, `stale`.

`stale` bedeutet: ältestes Pending-Item vor mehr als 24 Stunden geladen. Kein direkter Beleg für einen Prozessfehler. Zähler sind keine zeitlich gefilterte Auswertung; globale Zeit-/Statusfilter wirken hier nicht.

### `queue items`

Syntax: `queue items <queue-name> [optionen]` oder `queue items --queue-id <uuid> [optionen]`.

| Parameter | Wirkung |
|---|---|
| `<queue-name>` | Exakter Queue-Name; alternativ UUID, nicht beides. |
| `--queue-id <uuid>` | Direkte Queue-UUID; nicht mit Positionsname kombinieren. |
| `--status <status>` | Mehrfach hinter dem Unterbefehl; ODER-Auswahl. Typische Werte: `Pending`, `Locked`, `Completed`, `Exception`, `Deferred`; keine Enum-Validierung, tatsächliche DB-Werte beachten. |
| `--include-tags` | Tags zusätzlich laden; ohne Option fehlt `tags` in JSON. |
| `--full-data` | Vollständige Item-Daten statt maximal 1000 Zeichen; Feld bleibt `data_preview`. |
| `--since`, `--until` | Grenzen für `lastupdated`, nicht `loaded`, `completed` oder Exception-Zeit. |
| `--text <text>` | LIKE-Suche in Key-Wert oder Exception-Grund, nicht in `data_preview`. |

```powershell
& $cli queue items 'Rechnungen' --status 'Exception' --since '2026-10-15T00:00:00Z' --until '2026-10-16T00:00:00Z' --include-tags --max-rows 100
& $cli queue items --queue-id 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee' --status 'Pending' --status 'Locked' --max-rows 50
& $cli queue items 'Rechnungen' --text 'Timeout' --full-data --max-rows 20
```

`--status` hier stets hinter `queue items` setzen: der Befehlsparser verwendet nur dort stehende Statuswerte, nicht die zuvor global gesammelten Werte. Bei Namensauflösung ohne Treffer: `queue_not_found`, Exit 10. Eine vorhandene UUID ohne Items oder eine nicht existierende UUID kann eine leere Liste mit Exit 0 ergeben.

Ausgabe: `item_id`, `ident`, `key_value`, `status`, `attempt`, `loaded_at`, `locked_at`, `completed_at`, `exception_at`, `exception_reason`, `exception_tag`, `priority`, `work_time_sec`, `session_id`, `last_updated`, `data_preview`, optional `tags`.

Mit `session_id` lässt sich über `session get --session-id <uuid>` die zugehörige Session untersuchen. Tag-Laden benutzt einen SQL-Parameter je Item: `--include-tags` konservativ begrenzen (beispielsweise 100 oder 1000). Große Resultsets können die SQL-Server-Grenze von 2100 Parametern überschreiten, auch wenn `--max-rows` noch innerhalb des CLI-Limits liegt.

### `audit list`

Syntax: `audit list [--code <code>] [--user <prefix>] [--target-process <prefix>] [--target-resource <prefix>] [--include-xml] [--full-comments] [globale-optionen]`.

| Parameter | Wirkung |
|---|---|
| `--code <code>` | Mehrfach möglich; exakte Codes, ODER-Auswahl. Beispielsweise `RESSTART`, wenn im Zielsystem vorhanden. |
| `--user <prefix>` | Präfix des Quellbenutzers (`src_user`), nicht Zielbenutzer. |
| `--target-process <prefix>` | Präfix des Zielprozesses. |
| `--target-resource <prefix>` | Präfix der Zielressource. |
| `--include-xml` | Alte und neue XML-Inhalte laden; standardmäßig weggelassen. |
| `--full-comments` | Vollständige Kommentare statt maximal 500 Zeichen; Feld bleibt `comments_preview`. |

```powershell
& $cli audit list --code 'RESSTART' --target-resource 'BOT-' --since '2026-10-15T00:00:00Z' --until '2026-10-16T00:00:00Z' --max-rows 100
& $cli audit list --target-process 'Rechnung' --full-comments --max-rows 20
```

Ausgabe: `event_id`, `event_at`, `code`, `narrative`, `src_user`, `tgt_user`, `tgt_process`, `tgt_resource`, `comments_preview`, optional `old_xml`, `new_xml`. Kein `audit summary`-Befehl implementiert.

## 6. JSONL, Streams und Export

- Ein JSON-Objekt pro Zeile, keine äußere Array-Klammer; JSONL und NDJSON bezeichnen dasselbe Zeilenformat.
- Datensätze auf stdout, Warnungen und Fehler als JSONL auf stderr. Hilfe ist Klartext auf stderr. Streams nicht mit `2>&1` vermischen, wenn Daten automatisch geparst werden sollen.
- Feldnamen in `snake_case`; null-Felder werden normalerweise weggelassen. Fehlendes Feld und leerer Text sind nicht dasselbe.
- Unicode-Zeichen bleiben lesbar, JSON-Steuerzeichen escaped.
- Fehlertyp: `{"code":"filter_invalid","message":"...","exit_code":11}`.
- Warnungstyp: `{"warning":"output_truncated","detail":"..."}`.

Beispiel zur zeilenweisen Auswertung ohne Ergebnisdatei; stderr bleibt separat sichtbar:

```powershell
$lines = & $cli session list --max-rows 50
$exitCode = $LASTEXITCODE
$rows = @($lines | ForEach-Object { $_ | ConvertFrom-Json })
$rows | Select-Object session_number, process, status_name, exception_message
```

Exit-Code vor anderen nativen Aufrufen erfassen. Bei nicht-null Exit trotzdem prüfen, ob Teilresultate vorliegen. Für automatisierte Erfassung von stderr/stdout getrennte Prozessstreams verwenden; Umleitungsdateien nur nach Freigabe erzeugen.

### Datei-Ausgabe

Nur auf ausdrücklichen Exportwunsch:

```powershell
$env:BP_LOG_VIEWER_OUTPUT_ROOT = 'D:\BPLogExports'
& $cli session log 4711 --max-rows 50000 --output 'session-4711-neu.jsonl'
$exitCode = $LASTEXITCODE
```

- Ohne ENV-Override ist der erlaubte Root das Binary-Verzeichnis, nicht das aktuelle Arbeitsverzeichnis. Relative Pfade werden darunter aufgelöst.
- Auch absolute Pfade müssen innerhalb des Roots liegen. Root-Verlassen durch `..`, Unix-Pfade auf Windows und Verzeichnisse als Ziel werden abgewiesen. Für einen anderen Exportordner den Root nur bewusst und auf Wunsch festlegen.
- Parent-Verzeichnisse werden bei Bedarf angelegt. Schreiben erfolgt über `<ziel>.tmp`, danach Ersetzen der Zieldatei: bestehende Dateien können überschrieben werden. Verwende einen unbenutzten Dateinamen.
- Statt Datensätzen erscheint auf stdout `{"output_written":"<absoluter Pfad>","row_count":123,"bytes":4567}`.
- `bytes` ist eine Schätzung der UTF-8-Nutzlast inklusive angenommener Ein-Zeichen-Newlines; bei Windows-CRLF nicht identisch mit der tatsächlichen Dateigröße.
- Ohne explizites `--max-rows` aktiviert ein Ausgabeziel bei begrenzten Listen 100000. Größere Trefferzahlen werden weiterhin abgeschnitten und führen zu Exit 2; eine Datei garantiert keine Vollständigkeit.
- Bei Schreibfehlern kann eine `.tmp`-Datei zurückbleiben. Nicht als fertiges Ergebnis verwenden.

## 7. Exit-Codes und Warnungen

| Code | Bedeutung / Behandlung |
|---|---|
| 0 | Erfolgreicher Aufruf oder Hilfe; trotzdem Warnungen, `self_test: degraded` und interne Session-Item-Kürzung prüfen. Leere Liste ist möglich. |
| 1 | Nicht anderweitig behandelter Fehler (`unhandled_exception`). |
| 2 | Listen-/Logresultat am Zeilenlimit gekürzt; Teilresultate vorhanden. |
| 3 | Fehlende/ungültige Verbindungskonfiguration in den entsprechenden Konfigurationschecks. |
| 5 | DB-Verbindung, Abfrage oder erforderliches Status-Laden fehlgeschlagen. |
| 6 | Ergebnisdatei nicht schreibbar oder Pfad unzulässig. |
| 8 | `--max-rows` überschreitet 100000. |
| 10 | Session per Kennung nicht gefunden oder Queue-Name nicht auflösbar. |
| 11 | Unbekannter Befehl, ungültige Filter, Kennung oder spezifische Parameter. |

Wichtige Warnungen: `output_truncated`, `filter_unknown_option`, `text_filter_perf_warning`, `session_not_found`, `queue_not_found`, `state_xml_included`, `no_log_entries`, `result_type_table_unavailable`, `bp_version_db_lookup_failed`.

Bei `filter_unknown_option` Schreibweise und tatsächliche Filterwirkung prüfen. Für korrekt geschriebene befehlsspezifische Flags kann die globale Parserwarnung erwartbar sein. Manche Befehlsparser ignorieren fehlende Werte spezifischer Optionen statt sie sicher abzulehnen; stets vollständige dokumentierte Syntax verwenden und Ergebnisse plausibilisieren.

## 8. Konfiguration und Zeitangaben

Die versionierte [Konfigurationsvorlage](../../../../appsettings.template.json) enthält die Root-Section `BpLogViewer`. Lokale Dateien mit echten Verbindungsdaten nicht anzeigen oder in Versionsverwaltung aufnehmen.

Konfigurationssuche: explizites Basisverzeichnis (interne Tests), Binary-Verzeichnis, aktuelles Arbeitsverzeichnis, dessen Parent; die erste vorhandene Datei `appsettings.json` wird verwendet.

Verbindungspriorität, höchste zuerst:

1. `--connection-string` bei Datenbefehlen, nicht bei `self-test`.
2. `BP_DB_CONNECTION`.
3. ENV-Binding mit Prefix `BP_`, beispielsweise `BP_BpLogViewer__Database__ConnectionString`.
4. `BpLogViewer.Database.ConnectionString` aus der Konfigurationsdatei.

ADO.NET akzeptiert LocalDB/Remote SQL Server und die konfigurierte Authentifizierung; kein Passwort oder reales Beispiel in die Skill-Dateien übernehmen. Die eingerichtete Verbindung nutzen, nicht ungefragt ändern.

| Konfiguration / ENV | Tatsächliche Wirkung |
|---|---|
| `Database.ConnectionString` | Datenbankziel und Authentifizierung. |
| `Output.DefaultFormat` | Wird gebunden; aktuelle Datenbefehle schreiben dennoch stets JSONL. |
| `Output.MaxRowsBeforeFile` | Wird gebunden, steuert derzeit keine automatische Auslagerung. |
| `Output.MaxRowsAbsoluteCeiling` | Wird gebunden, aber das tatsächliche CLI-Limit ist die feste Filterkonstante 100000. |
| `Timezone.DisplayTimezone` | Vorlage: `Europe/Berlin`; aktuelle Datenausgabe wird nicht automatisch in diese Zone konvertiert. |
| `BP_LOG_VIEWER_OUTPUT_ROOT` | Pfadanker und erlaubter Root für `--output`/`--out-file`. |

Für Filterzeitpunkte ISO-8601 mit `Z` oder explizitem Offset bevorzugen. Der Parser normalisiert Eingaben auf UTC; ohne Offset nimmt er UTC an. Session-Ausgaben verwenden die gespeicherten Minuten-Offsets; andere DateTime-Spalten werden überwiegend als UTC interpretiert. Bei Datenbanken mit lokalen Serverzeiten kann dadurch eine Zeitverschiebung entstehen. Bei der Analyse tatsächliche Offsets beachten, keine automatische Berlin-Konvertierung behaupten.

## 9. Quellen und Pflege

Bei CLI-Änderungen diese Referenz zusammen mit den relevanten Optionen aktualisieren:

- [Dispatcher und Hilfe](../../../../src/BpLogViewer.Cli/Program.cs)
- [Globaler Filterparser](../../../../src/BpLogViewer.Cli/Filters/FilterParser.cs) und [Limitkonstanten](../../../../src/BpLogViewer.Cli/Filters/Filters.cs)
- [Befehle](../../../../src/BpLogViewer.Cli/Commands) und [SQL-/Reader-Implementierung](../../../../src/BpLogViewer.Cli/Data)
- [Ausgabemodelle](../../../../src/BpLogViewer.Cli/Models) und [JSONL-Ausgabe](../../../../src/BpLogViewer.Cli/Output)
- [Konfigurationssuche](../../../../src/BpLogViewer.Cli/Configuration/ConfigurationLoader.cs) und [Pfadprüfung](../../../../src/BpLogViewer.Cli/Configuration/PathResolver.cs)
