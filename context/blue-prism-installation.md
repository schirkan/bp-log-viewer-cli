# Blue Prism — Lokale Installation (Befund)

**Erfasst am:** 2026-10-13
**Methode:** Direkter Zugriff auf dem System, auf dem der BP Log Viewer CLI laufen soll.
**Zweck:** Single-Source-of-Truth für die Spezifikation und spätere Entwicklung.

## Identifikation

| Feld | Wert |
|:-----|:-----|
| Produkt | Blue Prism (SS&C) |
| Version | 7.5.1.18099 |
| APIVersion | 7.5.1.18099-abgYW6n6WTCA4HczaNM0ZyqrNjycPHXD9cMQdfddgyMA= |
| ProductCode | d0dfbf8d-637b-4ef6-9cdc-f43cb1e76d86 |
| InstallLocale | (leer) |
| InstallDir | `C:\Program Files\Blue Prism Limited\Blue Prism Automate\` |

## Installations-Struktur

```
C:\Program Files\Blue Prism Limited\
└── Blue Prism Automate\
    ├── Automate.exe            # Haupt-UI (Designer/Runtime)
    ├── AutomateC.exe           # offizielle CLI (→ "sehr langsam" laut Martin)
    ├── AutomateS.exe           # Service-Wrapper
    ├── AutomateConfig.exe      # Konfig-UI
    ├── BPServer.exe            # Application Server (REST API)
    ├── BPServerService.exe     # Service-Host für BPServer
    ├── BPDiagnostics.exe       # Diagnose-Tool
    ├── BluePrismUi.exe         # Browser-Wrapper (Electron)
    ├── BluePrism.MessagingHost.exe
    ├── BluePrism.JavaScriptInvoker.exe
    ├── NLog.config             # NLog-Konfig (NLog 5.0)
    ├── Server.NLog.config      # serverseitiges Logging
    ├── DotNetHost\BluePrism.DotNetGateway.Host.exe
    ├── Installers\SqlLocalDB.msi
    ├── Plugins\…
    └── skills\…                # Standard-BP-Skills
```

## Service

| Name | Anzeigename | Status |
|:-----|:------------|:-------|
| `Blue Prism Server` | Blue Prism Server | gestoppt (nicht zwingend relevant für DB-Zugriff) |

## Registry

Schlüssel: `HKLM\SOFTWARE\Blue Prism Limited\Automate`

| Wert | Inhalt |
|:-----|:-------|
| Version | 7.5.1.18099 |
| ProductCode | d0dfbf8d-637b-4ef6-9cdc-f43cb1e76d86 |
| APIVersion | 7.5.1.18099-abgYW6n6WTCA4HczaNM0ZyqrNjycPHXD9cMQdfddgyMA= |
| LocalDB | Pfad zum `SqlLocalDB.msi` |
| InstallDir | `C:\Program Files\Blue Prism Limited\Blue Prism Automate\` |

> **Kein** DB-Connection-String in der Registry. Die Verbindung wird zur Laufzeit vom BP-Service aufgebaut.

## ProgramData

Pfad: `C:\ProgramData\Blue Prism Limited\Blue Prism\`

| Inhalt | Bemerkung |
|:-------|:----------|
| `Automate.NLog.config` | NLog-Konfig (5.174 Bytes, Stand 26.03.2026) |
| `BluePrism.Ui.Logs.config` | UI-Logging (476 Bytes) |
| `Server.NLog.config` | Server-Logging (5.156 Bytes) |
| `com.blueprism.messaging-manifest.json` | Messaging-Manifest (353 Bytes) |
| `Logs\` | NLog-Ausgabe (inkl. Archiv) |

> **Kein** DB-Connection-String in ProgramData.

## SQL Server / LocalDB

| Eigenschaft | Wert |
|:------------|:-----|
| Instanzname | `BluePrismLocalDB` |
| Version | 17.0.4025.3 (SQL Server 2022) |
| Besitzer | `OPENCLAWPC\Admin` |
| Status nach Start | running |
| Pipe | `np:\\.\pipe\LOCALDB#C5348404\tsql\query` |
| Authentifizierung | Windows Integrated (Trusted_Connection / `Integrated Security=true`) |
| Datenbankname | **`BluePrism`** (nicht `BPAFramework` — bei BP 7.5.x umbenannt) |
| Auto-Create | Nein (manuell gestartet) |

Zusätzlich vorhanden, aber leer: `MSSQLLocalDB` (nur System-DB).

## Frameworks / Bibliotheken (laut config-Dateien)

- **.NET Framework:** v4.8 (von allen .exe angefordert)
- **Microsoft.Data.SqlClient:** 2.0.20168.4 (durch Binding-Redirects)
- **Newtonsoft.Json:** 13.0.0
- **System.Text.Json:** 8.0.0.6
- **NLog:** 5.0.0
- **Autofac:** 8.0.0
- **System.Reactive:** 5.0.0

> Implikation für den BP Log Viewer CLI: gleiche Major-Versionen (Microsoft.Data.SqlClient ≥ 5.x, System.Text.Json ≥ 8.x) nutzen, um Kompatibilität zu beweisen.

## Konsequenzen für die CLI-Architektur

1. **Connection-String kommt nicht aus Config/Registry der BP-Installation** — der BP Log Viewer CLI braucht eine eigene Konfiguration (`appsettings.json`).
2. **Auth ist Windows Integrated** — beim Spawn durch den KI-Agent (MCP-Host, läuft als `OPENCLAWPC\Admin`) funktioniert die Authentifizierung ohne Passwort.
3. **LocalDB muss laufen** — der BP Log Viewer CLI muss die Instanz bei Bedarf starten können, oder im Fehlerfall darauf hinweisen.
4. **Pfad-Anchoring zwingend** — der KI-Agent spawnt die Binary aus einem fremden CWD; relative `--output`-Pfade MÜSSEN gegen `AppContext.BaseDirectory` aufgelöst werden, sonst landet die Auslagerung in einem falschen Verzeichnis (siehe `dotnet-config-path-anchoring`-Skill).
5. **Self-contained single-file trimmed** — `dotnet publish -p:PublishSingleFile=true -p:PublishTrimmed=true` für Distribution; JSON-Pfade brauchen Source-Generator (siehe `dotnet-trimmed-binary-json-fix`-Skill).
