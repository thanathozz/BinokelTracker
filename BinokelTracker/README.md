# Binokel Tracker

Schwäbische Binokel-Kartenspiel Tracker App — gebaut mit **.NET MAUI Blazor Hybrid** für Windows, iOS, Android und macOS.

---

## Schnellstart (Windows + Rider)

### 1. Voraussetzungen installieren

- **[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)** herunterladen und installieren
- Nach Installation Terminal öffnen und MAUI-Workload installieren:
  ```powershell
  dotnet workload install maui
  ```
- **JetBrains Rider** (2024.1 oder neuer)

### 2. Überprüfen
```powershell
dotnet --version            # → 8.0.x
dotnet workload list        # → maui muss aufgelistet sein
```

### 3. Projekt öffnen & starten
1. ZIP entpacken
2. Rider öffnen → `File` → `Open` → `BinokelTracker.sln` wählen
3. Rider lädt NuGet-Pakete automatisch (dauert beim ersten Mal ~1 Min)
4. Oben in der **Run-Konfiguration** sicherstellen:
   - **Target Framework**: `net8.0-windows10.0.19041.0`
   - **Run Configuration**: `Windows Machine`
5. **▶ Run** drücken (oder `Shift+F10`)
6. Die App öffnet sich als Windows-Fenster!

### Falls Rider das Framework nicht anzeigt
- `File` → `Settings` → `Build, Execution, Deployment` → `Toolset` → sicherstellen dass das .NET 8 SDK erkannt wird
- Im Terminal: `dotnet restore` im Projektordner ausführen
- Rider neustarten

---

## iOS-Signierung & Veröffentlichung

### Für Entwicklung (auf eigenem Gerät testen)

1. **Apple Developer Account** (kostenlos reicht für eigenes Gerät):
   - https://developer.apple.com registrieren
   - In Xcode: `Xcode` → `Settings` → `Accounts` → Apple ID hinzufügen

2. **Signing in der .csproj konfigurieren** — füge in `BinokelTracker.csproj` innerhalb der `<PropertyGroup>` hinzu:
   ```xml
   <CodesignKey>Apple Development</CodesignKey>
   <CodesignProvision>Automatic</CodesignProvision>
   ```

3. Alternativ in Rider: `Run` → `Edit Configurations` → iOS Signing einstellen

### Für App Store Veröffentlichung

1. **Apple Developer Program** beitreten (99 €/Jahr):
   - https://developer.apple.com/programs/

2. **App ID erstellen** im Apple Developer Portal:
   - Bundle ID: `com.binokel.tracker` (oder eigene)

3. **Provisioning Profile** erstellen:
   - Typ: „App Store"
   - App ID zuweisen
   - Zertifikat: „Apple Distribution"

4. **In .csproj für Release konfigurieren**:
   ```xml
   <PropertyGroup Condition="'$(Configuration)' == 'Release'">
     <CodesignKey>Apple Distribution: Dein Name (TEAMID)</CodesignKey>
     <CodesignProvision>Dein Provisioning Profil Name</CodesignProvision>
     <ArchiveOnBuild>true</ArchiveOnBuild>
     <RuntimeIdentifier>ios-arm64</RuntimeIdentifier>
   </PropertyGroup>
   ```

5. **Archive erstellen**:
   ```bash
   dotnet publish -f net8.0-ios -c Release
   ```

6. **Hochladen**:
   - Die `.ipa`-Datei über **Transporter** (Mac App Store) oder Xcode Organizer an App Store Connect hochladen
   - In [App Store Connect](https://appstoreconnect.apple.com) die App-Metadaten pflegen und zur Prüfung einreichen

---

## Projektstruktur

```
BinokelTracker/
├── BinokelTracker.sln          # Solution (in Rider öffnen)
├── BinokelTracker.csproj       # Projekt-Definition
├── MauiProgram.cs              # App-Startup & DI
├── App.xaml(.cs)               # MAUI Application
├── MainPage.xaml(.cs)          # Host-Page mit BlazorWebView
├── _Imports.razor              # Globale Razor-Imports
├── GlobalUsings.cs             # C# Global Usings
│
├── Models/
│   └── GameModels.cs           # Game, Round, RuleSet, RulePresets
│
├── Services/
│   └── GameStorageService.cs   # Persistenz via LocalStorage
│
├── Pages/                      # Blazor-Komponenten (UI)
│   ├── Main.razor              # Root-Komponente, Navigation
│   ├── GameList.razor          # Spielübersicht
│   ├── NewGameForm.razor       # Neues Spiel + Regelauswahl
│   ├── AddRoundForm.razor      # Runde erfassen
│   └── GameDetail.razor        # Spieldetails & Punktestand
│
├── wwwroot/
│   ├── index.html              # Blazor Host-HTML
│   └── css/app.css             # Komplettes Styling
│
├── Resources/
│   ├── Styles/
│   │   ├── Colors.xaml
│   │   └── Styles.xaml
│   ├── Images/                 # App-Icons hier ablegen
│   ├── Fonts/
│   └── Splash/
│
├── Platforms/
│   ├── iOS/
│   │   ├── Info.plist          # iOS-Konfiguration
│   │   ├── AppDelegate.cs
│   │   └── Program.cs
│   ├── Android/
│   │   ├── AndroidManifest.xml
│   │   ├── MainActivity.cs
│   │   └── MainApplication.cs
│   └── MacCatalyst/
│       ├── Info.plist
│       ├── AppDelegate.cs
│       └── Program.cs
│
└── Properties/
    └── launchSettings.json
```

---

## Regel-Presets

| Preset | Spieler | Teams | Ziel | 2× Minus | Durch | Bettel |
|--------|---------|-------|------|----------|-------|--------|
| Schwäbisch Klassisch | 3 | Nein | 1000 | Nein | Nein | Nein |
| Schwäbisch Scharf | 3 | Nein | 1000 | Ja | Ja | Nein |
| Vierer-Kreuz | 4 | Ja | 1500 | Nein | Nein | Nein |
| Turnier | 3 | Nein | 1500 | Ja | Ja | Ja |
| Benutzerdefiniert | 3/4 | Frei | Frei | Frei | Frei | Frei |

---

## App-Icon hinzufügen

Lege dein App-Icon als `appicon.svg` (oder `appicon.png`, 1024×1024) in `Resources/Images/` ab. MAUI generiert automatisch alle benötigten Größen.

Füge in der `.csproj` hinzu (falls nicht vorhanden):
```xml
<ItemGroup>
  <MauiIcon Include="Resources\Images\appicon.svg" />
  <MauiSplashScreen Include="Resources\Splash\splash.svg" Color="#191613" />
</ItemGroup>
```

---

## Troubleshooting

| Problem | Lösung |
|---------|--------|
| `workload maui not found` | `dotnet workload install maui` |
| iOS-Build schlägt fehl | Xcode aktualisieren, `sudo xcode-select -r` |
| Rider erkennt MAUI nicht | Rider 2024.1+ verwenden, `dotnet restore` ausführen |
| Signing-Fehler | Apple Developer Account in Xcode einrichten |
| Simulator startet nicht | In Xcode unter Window → Devices einen Simulator anlegen |
# BinokelTracker
