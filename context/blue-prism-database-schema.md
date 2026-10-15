# Blue Prism — Datenbank-Schema (Befund)

**Datenbank:** `BluePrism`
**Instanz:** `(localdb)\BluePrismLocalDB`
**Schema:** `dbo` (alle Objekte)
**Gesamt:** 213 Objekte — 196 Base Tables, 17 Views

Diese Doku fokussiert auf Tabellen, die für Fehleranalyse und Ursachenforschung
relevant sind. Reine Konfig-/Auth-Tabellen werden hier nicht im Detail aufgeführt.

## Tabellen-Kategorien

| Kategorie | Tabellen | Zweck |
|:----------|:---------|:------|
| **Sessions & Logs** | `BPASession`, `BPASessionLog_NonUnicode`, `BPASessionLog_Unicode`, `BPASessionLog_*_pre65` | Was wurde wann von wem ausgeführt + Logzeilen |
| **Prozesse** | `BPAProcess`, `BPAProcessBackup`, `BPAPackage*`, `BPAProcess*Dependency` | Prozess-Definitionen und Beziehungen |
| **Ressourcen** | `BPAResource`, `BPAResourceConfig`, `BPAHybridResource` | Roboter-/Runtime-Instanzen |
| **Work Queues** | `BPAWorkQueue`, `BPAWorkQueueItem`, `BPAWorkQueueItemTag`, `BPAWorkQueueLog`, `BPAWorkQueueItemAggregate`, `BPAWorkQueueFilter` | Queue-Definition, Items, Item-Verlauf |
| **Schedules** | `BPASchedule`, `BPAScheduleList`, `BPAScheduleTrigger`, `BPAScheduleAlert`, `BPAScheduleLog`, `BPAScheduleLogEntry` | Zeit-/Event-gesteuerte Auslöser |
| **Exceptions** | `BPAExceptionType` | Definition registrierter Exception-Typen |
| **Status-Lookups** | `BPAStatus` | Session-Status-Codes (9 Einträge) |
| **Audit** | `BPAAuditEvents` | Wer hat wann was geändert (177 Einträge in dieser Instanz) |
| **Skills / Web** | `BPASkill`, `BPASkillVersion`, `BPAWebApi*`, `BPAWebService*`, `BPAWebSkillVersion` | Skills und Web-Service-Definitionen |
| **Dashboards / MI** | `BPADashboard*`, `BPADashboardTile`, `BPMI*` | Dashboard-Konfiguration und MI-Snapshots |
| **Paketierung / Release** | `BPARelease`, `BPAReleaseEntry`, `BPAPackage*` | Release- und Paket-Verwaltung |
| **Sync** | `BPASync*` | Multi-Environment-Sync |
| **Auth / Benutzer** | `BPAUser`, `BPAUserRole*`, `BPAPerm*`, `BPAGroup*`, `BPACredentials*`, `BPAInternalAuth`, `BPAKeyStore`, `BPASSOGroupRoleMapping` | Benutzer, Rollen, Credentials, SSO |
| **Diagnose / DB-Maintenance** | `BPDiagnostics*`, `BPADBMaintenance*`, `BPADBVersion` | Diagnose- und DB-Wartung |
| **Sonstige** | `BPACache`, `BPAFont`, `BPAEnvironment*`, `BPACalendar`, `BPAPublicHoliday*`, `BPATag`, `BPATask`, `BPADataPipeline*`, `BPADataTracker`, `BPAWebhooks*`, `BPAScreenshot`, `BPAVal*` | Hilfsobjekte |

## Aktueller Befüllungs-Stand (auf diesem System)

| Tabelle | Zeilen | Bemerkung |
|:--------|-------:|:----------|
| `BPASession` | 2 | Trainings-Daten |
| `BPASessionLog_NonUnicode` | 8 | einzige aktive Log-Tabelle |
| `BPASessionLog_Unicode` | 0 | ungenutzt (NonUnicode-DB-Server-Setup) |
| `BPASessionLog_*_pre65` | 0 | leer (kein Pre-6.5-Bestand) |
| `BPAAuditEvents` | 177 | realer Audit-Bestand |
| `BPAResource` | 1 | eine Resource registriert |
| `BPAProcess` | 31 | 31 Prozesse deployed |
| `BPASchedule` | 1 | ein Schedule |
| `BPAScheduleLog` | 0 | keine Triggerläufe |
| `BPAExceptionType` | 16 | 16 Exception-Typen registriert |
| `BPAStatus` | 9 | 9 Status-Codes |
| `BPAWorkQueue` | 0 | keine Queues auf diesem System |
| `BPAWorkQueueItem` | 0 | (folgt aus 0 Queues) |
| `BPAWorkQueueLog` | 0 | (folgt aus 0 Queues) |

> Dieses System ist eine **Entwicklungs-/Trainings-Instanz** mit minimaler Historie.
> Produktivsysteme haben typisch Millionen Zeilen in `BPASessionLog_NonUnicode`.
> Der BP Log Viewer CLI muss auch mit großen Datenmengen umgehen — siehe F-012 (Output-to-File).

## Schlüssel-Tabellen (Spalten)

### `BPASession` — Session-Header (2 Zeilen)

| Spalte | Typ | Nullable | Bemerkung |
|:-------|:----|:--------:|:----------|
| `sessionid` | uniqueidentifier | NO | PK |
| `sessionnumber` | int | NO | fortlaufende Session-Nr. (menschenlesbar) |
| `startdatetime` | datetime | YES | UTC |
| `enddatetime` | datetime | YES | NULL solange laufend |
| `processid` | uniqueidentifier | YES | FK → `BPAProcess.processid` |
| `starterresourceid` | uniqueidentifier | YES | wer hat gestartet (Resource) |
| `starteruserid` | uniqueidentifier | YES | wer hat gestartet (User, bei Schedule/API) |
| `runningresourceid` | uniqueidentifier | YES | auf welcher Resource gelaufen |
| `runningosusername` | nvarchar(50) | YES | OS-User der Resource |
| `statusid` | int | NO | FK → `BPAStatus.statusid` |
| `startparamsxml` | nvarchar(MAX) | YES | Start-Parameter als XML |
| `logginglevelsxml` | nvarchar(MAX) | YES | Logging-Levels zur Laufzeit |
| `sessionstatexml` | nvarchar(MAX) | YES | Data Item-/Stage-Zustand (kann groß sein) |
| `queueid` | int | YES | FK → `BPAWorkQueue.ident` (falls aus WQ gestartet) |
| `stoprequested` | datetime | YES | Stop-Request-Zeit |
| `stoprequestack` | datetime | YES | ACK vom Runtime |
| `lastupdated` | datetime | YES | Heartbeat |
| `laststage` | nvarchar(MAX) | YES | Name der zuletzt betretenen Stage |
| `warningthreshold` | int | YES | Minuten bis Warnung |
| `starttimezoneoffset` | int | YES | TZ-Offset in Sekunden (7200 = +02:00, 3600 = +01:00); zur Lokalisierung des UTC-Datetime-Werts |
| `endtimezoneoffset` | int | YES | |
| `lastupdatedtimezoneoffset` | int | YES | |
| `terminationreason` | tinyint | YES | Code für Abbruchgrund |
| `exceptiontype` | nvarchar(50) | YES | Exception-Typ (siehe `BPAExceptionType`) |
| `exceptionmessage` | nvarchar(MAX) | YES | Exception-Text |
| `sessionsource` | tinyint | NO | 1=Scheduler, 2=Trigger, 3=API, 4=Work Queue, 5=Studio … |

### `BPAStatus` — Status-Lookup (9 Zeilen)

Typische Codes: `1 = Initialised`, `2 = Running`, `3 = Completed`, `4 = Completed with Exceptions`, `5 = Terminated`, `6 = Waiting`, `7 = Paused`, `8 = Pending`, `9 = Stopping`.
Werte bitte zur Laufzeit per `SELECT * FROM BPAStatus` verifizieren (sind versionsabhängig).

### `BPASessionLog_NonUnicode` — Log-Zeilen (8 Zeilen)

| Spalte | Typ | Nullable | Bemerkung |
|:-------|:----|:--------:|:----------|
| `logid` | bigint | NO | PK, monothonisch wachsend |
| `sessionnumber` | int | NO | FK zu `BPASession.sessionnumber` (nicht sessionid!) |
| `stageid` | uniqueidentifier | YES | FK zu `BPATree` (Stage-Definition) |
| `stagename` | varchar(128) | YES | Stage-Name (menschenlesbar) |
| `stagetype` | int | YES | Code für Stage-Typ (Start/End/Action/Decision/…) |
| `processname` | varchar(128) | YES | Prozess-Name (denormalisiert) |
| `pagename` | varchar(128) | YES | Page innerhalb des Prozesses |
| `objectname` | varchar(128) | YES | Business Object (bei Action-Stage) |
| `actionname` | varchar(128) | YES | Action-Name (bei Action-Stage) |
| `result` | varchar(MAX) | YES | Ergebnis-/Fehlertext |
| `resulttype` | int | YES | Code für Ergebnis-Typ (Info/Warn/Error) |
| `startdatetime` | datetime | YES | Stage-Start |
| `enddatetime` | datetime | YES | Stage-Ende |
| `attributexml` | varchar(MAX) | YES | Attribute als XML (kann groß sein) |
| `automateworkingset` | bigint | YES | Speicherverbrauch (RSS) bei Stage-Ende |
| `targetappname` | varchar(32) | YES | angesprochene Anwendung |
| `targetappworkingset` | bigint | YES | Speicherverbrauch der Target-App |
| `starttimezoneoffset` | int | YES | |
| `endtimezoneoffset` | int | YES | |
| `attributesize` | bigint | NO | Größe von `attributexml` in Bytes |

> **`resulttype`-Codes** (typische Werte, versionsabhängig verifizieren):
> `0 = Information`, `1 = Warning`, `2 = Error`, `3 = Debug`, `4 = Exception`.

### `BPAAuditEvents` — Audit-Trail (177 Zeilen)

| Spalte | Typ | Nullable | Bemerkung |
|:-------|:----|:--------:|:----------|
| `eventid` | int | NO | PK |
| `eventdatetime` | datetime | NO | UTC |
| `sCode` | nvarchar(10) | YES | Audit-Code (z. B. `RESSTART`, `SESSCOMP`) |
| `sNarrative` | nvarchar(500) | YES | Kurzbeschreibung |
| `gSrcUserID` | uniqueidentifier | NO | wer hat gehandelt |
| `gTgtUserID` | uniqueidentifier | YES | Ziel-User (falls zutreffend) |
| `gTgtProcID` | uniqueidentifier | YES | Ziel-Prozess (falls zutreffend) |
| `gTgtResourceID` | uniqueidentifier | YES | Ziel-Resource (falls zutreffend) |
| `comments` | nvarchar(MAX) | YES | Freitext |
| `EditSummary` | nvarchar(MAX) | YES | Zusammenfassung (bei Edits) |
| `oldXML` | nvarchar(MAX) | YES | vorheriger Stand |
| `newXML` | nvarchar(MAX) | YES | neuer Stand |

### `BPAExceptionType` — Exception-Typen (16 Zeilen)

| Spalte | Typ | Nullable | Bemerkung |
|:-------|:----|:--------:|:----------|
| `id` | uniqueidentifier | NO | PK |
| `type` | nvarchar(30) | YES | Exception-Typ-Name (z. B. `AppModelException`) |

> Auf diesem System nur 2 Zeilen in `BPAExceptionType` befüllt.
> Achtung: Spalten `description` und `detail` existieren NICHT — vorherige Annahme war falsch.

### `BPAProcess` — Prozess-Definition (31 Zeilen)

| Spalte | Typ | Nullable | Bemerkung |
|:-------|:----|:--------:|:----------|
| `processid` | uniqueidentifier | NO | PK |
| `ProcessType` | nvarchar(1) | NO | `P` = Process, `O` = Object |
| `name` | nvarchar(128) | NO | Prozess-Name |
| `version` | nvarchar(20) | YES | Semver-ähnlich |
| `description` | nvarchar(1000) | YES | |
| `processxml` | nvarchar(MAX) | YES | serialisierte Prozess-Definition (kann MBs groß sein) |
| `compressedxml` | image | YES | komprimierte Variante |
| `wspublishname` | varchar(255) | YES | Web-Service-Name (falls veröffentlicht) |
| `runmode` | int | NO | `0 = Foreground`, `1 = Background`, `2 = Session` |
| `sharedObject` | bit | NO | geteilt mit anderen Prozessen |
| `useLegacyNamespace` | bit | NO | VBO-Namespace-Modus |
| `hasStartupParameters` | bit | YES | |
| `createdate`, `createdby`, `lastmodifieddate`, `lastmodifiedby` | … | … | Metadaten |

### `BPAResource` — Roboter/Runtime (1 Zeile)

| Spalte | Typ | Bemerkung |
|:-------|:----|:----------|
| `resourceid` | uniqueidentifier | PK |
| `name` | nvarchar(128) | Anzeigename |
| `statusid` | int | FK → `BPAStatus` |
| `DisplayStatus` | varchar(10) | z. B. `Available`, `Running`, `Disconnected` |
| `processesrunning` | int | aktuell laufende Prozesse |
| `actionsrunning` | int | aktuell laufende Actions |
| `lastupdated` | datetime | Heartbeat |
| `LastTimeLoggedIn` | datetime | |
| `pool`, `controller` | uniqueidentifier | Zugehörigkeit |
| `FQDN` | nvarchar(MAX) | DNS-Name |
| `logtoeventlog`, `logcodestage` | bit | Logging-Flags |
| `ssl`, `diagnostics` | int / bit | |

### `BPAWorkQueue` / `BPAWorkQueueItem` (0 Zeilen)

Schlüssel-Spalten `BPAWorkQueue`:

| Spalte | Typ | Bemerkung |
|:-------|:----|:----------|
| `id` | uniqueidentifier | PK (neuere BP-Version) |
| `ident` | int | zusätzliche Auto-ID, wird von `BPASession.queueid` referenziert |
| `name` | nvarchar(255) | |
| `keyfield` | nvarchar(255) | Name des Schlüsselfelds |
| `running` | bit | |
| `maxattempts` | int | Wiederholungs-Limit |
| `processid` | uniqueidentifier | Default-Prozess |
| `resourcegroupid` | uniqueidentifier | |
| `encryptid` | int | Verschlüsselungs-Modus |
| `sessionexceptionretry` | bit | |

Schlüssel-Spalten `BPAWorkQueueItem`:

| Spalte | Typ | Bemerkung |
|:-------|:----|:----------|
| `id` | uniqueidentifier | PK |
| `ident` | bigint | Auto-ID |
| `queueid` | uniqueidentifier | FK → `BPAWorkQueue.id` |
| `keyvalue` | nvarchar(255) | Schlüsselwert |
| `status` | nvarchar(255) | `Pending` / `Locked` / `Completed` / `Exception` / `Deferred` |
| `attempt` | int | Versuchszähler |
| `loaded` / `completed` / `exception` / `deferred` / `finished` | datetime | Lebenszyklus-Zeitstempel |
| `exceptionreason` | nvarchar(MAX) | Fehlertext |
| `exceptionreasonvarchar` | nvarchar(400) | Kurzform (manche Stellen referenzieren das) |
| `exceptionreasontag` | nvarchar(415) | Exception-Tag/-Typ |
| `data` | nvarchar(MAX) | Item-Daten (typisch JSON) |
| `priority` | int | |
| `worktime` / `prevworktime` / `attemptworktime` | int | Bearbeitungsdauer (Sekunden) |
| `sessionid` | uniqueidentifier | welche Session hat das Item verarbeitet |
| `sla` / `sladatetime` | bigint / datetime | SLA |

## Views (für die KI-Konsumierung relevant)

| View | Zweck |
|:-----|:------|
| `BPVSession` | Join-View über `BPASession` (keine Namens-Auflösung) |
| `BPVSessionInfo` | `BPASession` + aufgelöste Prozess-/Resource-/User-Namen — **primäre Quelle für Anzeige** |
| `BPVWorkQueueItem` | `BPAWorkQueueItem` + aufgelöste Daten — **primäre Quelle für Item-Anzeige** |
| `BPVGroupedActiveObjects` / `BPVGroupedActiveProcesses` | aggregierte Listen |
| `BPVAnnotatedScheduleLog` | Schedule-Log mit Annotationen |
| `vw_Audit` / `vw_Audit_improved` | Audit-Events aufbereitet |

> Für fast alle Lese-Queries sind die `BPV*`-Views die bevorzugte Quelle — sie
> liefern direkt aufgelöste Namen und sparen LEFT-JOINs im Code.

## Konsequenzen für die CLI-Architektur

1. **Views nutzen, wo möglich** — `BPVSessionInfo` für Session-Listen/Details, `BPVWorkQueueItem` für Item-Listen. Schneller und konsistenter mit dem offiziellen Tool.
2. **`sessionid` vs. `sessionnumber`** — intern ist `sessionnumber` (int) der Join-Schlüssel zu `BPASessionLog_*`. Für die KI ist `sessionnumber` der menschenlesbare Identifier.
3. **Unicode vs. NonUnicode** — bei BP 7.5.x ist die NonUnicode-Variante auf der genutzten Collation aktiv. CLI muss beide unterstützen und beim Start prüfen, welche befüllt ist.
4. **Zeitstempel in UTC + TZ-Offset** — pro Zeile eigener Offset in Sekunden (`starttimezoneoffset`, `endtimezoneoffset`). CLI konvertiert UTC zum gespeicherten Offset (z. B. 11:13 UTC mit 7200 → 13:13+02:00), ohne den Zeitpunkt zu verändern. Session-Logs bleiben UTC; Session-Filter vergleichen UTC-Werte.
5. **Result-Typen und Status-Codes** — nicht hartcodieren, sondern zur Laufzeit aus `BPAStatus` und `BPAExceptionType` ableiten und in der Antwort mitliefern.
