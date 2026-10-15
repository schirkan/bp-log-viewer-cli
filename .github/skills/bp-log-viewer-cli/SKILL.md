---
name: bp-log-viewer-cli
description: "Verwende diesen Skill für BP Log Viewer CLI: Blue-Prism-Sessions, Logs und Exceptions analysieren, abgebrochene Prozesse untersuchen, Ressourcen, Work Queues und Audit-Ereignisse abfragen. Enthält die vollständige Parameterreferenz, PowerShell-Beispiele, Filterwirkung, JSONL-Ausgabe, Konfiguration und Exit-Codes."
argument-hint: "Gewünschte Abfrage oder Fehleranalyse, möglichst mit Session-Nummer, Prozess, Ressource und Zeitraum."
---

# BP Log Viewer CLI

## Zweck

Das CLI liest Daten einer Blue-Prism-7.5.x-Datenbank (SQL Server oder LocalDB) und liefert strukturierte JSONL-Ergebnisse. Blue Prism ist ein RPA-Tool zur Automatisierung von Geschäftsprozessen; eine Session ist eine einzelne Prozessausführung. Dieses CLI analysiert bestehende Daten, startet oder stoppt aber keine Blue-Prism-Prozesse.

## Referenz

Lies vor der Auswahl von Parametern die [vollständige CLI-Referenz](./references/cli-reference.md). Sie dokumentiert alle implementierten Befehle und Optionen, ihre tatsächliche Wirkung und bekannte Einschränkungen. Nicht jede global akzeptierte Option wirkt auf jeden Befehl.

## Vorgehen

1. Kläre Ziel, Kennung und Zeitraum. Frage nach, wenn der betroffene Prozess oder die Session nicht eindeutig ist.
2. Ermittle den absoluten Pfad zu einer vorhandenen `bp-log-viewer.exe`. Verwende in PowerShell den Aufrufoperator `&`.
3. Prüfe bei Bedarf `version`; verwende `self-test` nur zur Diagnose von Konfiguration oder Verbindung. Beachte: `self-test` schreibt und löscht eine temporäre Testdatei und kann ein `temp`-Verzeichnis hinterlassen.
4. Wähle anhand der Referenz den engsten geeigneten Befehl. Verwende explizite Zeitangaben mit `Z` oder Offset und zunächst ein kleines `--max-rows`.
5. Prüfe stdout, stderr und den unmittelbar nach dem Aufruf erfassten Exit-Code. Parse JSONL zeilenweise, nicht als JSON-Array. Warnungen und leere Ausgabe sind keine Belege für Fehlerfreiheit.
6. Verfeinere Filter oder erhöhe das Limit, wenn entscheidende Daten fehlen. Bei Log-Trunkierung fehlen die späteren Einträge, möglicherweise gerade der Abbruch. Es gibt keine serverseitige Log-Paginierung oder Zeitfilterung.

## Fehleranalyse eines abgebrochenen Prozesses

Die Beispiele verwenden illustrative Namen, Session-Nummern und Zeitangaben. Ersetze sie durch Werte aus der Anfrage bzw. aus den Ergebnissen.

```powershell
$cli = 'D:\Tools\BPLogViewer\bp-log-viewer.exe'
& $cli session list --process 'Rechnungsverarbeitung' --since '2026-10-15T00:00:00Z' --until '2026-10-16T00:00:00Z' --max-rows 50
$exitCode = $LASTEXITCODE
```

- Suche nicht ausschließlich mit `--has-exception`: Ein manuell gestoppter oder anderweitig abgebrochener Lauf muss keine Session-Exception haben.
- Übernimm `session_number` aus dem Ergebnis; Statusnamen werden aus der Datenbank geladen, nicht als feste Codes angenommen.

```powershell
& $cli session get 4711
$exitCode = $LASTEXITCODE
& $cli session log 4711 --level error --max-rows 200
$exitCode = $LASTEXITCODE
& $cli session log 4711 --max-rows 2000
$exitCode = $LASTEXITCODE
```

- Vergleiche Session-Exception, `last_stage` und Log-Einträge. Die Fehlerfilterung umfasst Resulttypen 2 und 64, aber nicht Typ 4 (`Exception`); lies deshalb auch ungefilterte Logs.
- Rekonstruiere die ursprüngliche Exception und mögliche Folgefehler. Ein Fehler im Log allein beweist nicht die Ursache der Terminierung.
- Verwende `--stage '<Stage-Name>' --full-result` gezielt, wenn Vorschauen nicht ausreichen. Ohne Ergebnisdatei sind maximal 100000 Zeilen möglich; auch eine Datei hebt diese Obergrenze nicht auf.
- Korrelierte Queue-Items und Audit-Ereignisse können Hinweise auf Datenfehler oder Bedienhandlungen liefern. Korrelation ist kein Kausalitätsbeweis.
- Für wiederkehrende Fehler: `session errors --aggregate` mit explizitem Zeitraum; danach einzelne `sample_session_numbers` untersuchen. Die Aggregation wird nicht durch `--max-rows` begrenzt.
