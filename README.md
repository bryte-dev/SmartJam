# SmartJam 🎸

Application de **détection de pitch en temps réel** développée dans le cadre du TPI.  
Elle capture l'audio (interface ASIO / WASAPI) ou génère un sinus interne, détecte la note jouée et affiche les résultats dans une interface Avalonia.

---

## Structure du projet

```
SmartJam/
├── SmartJam.slnx                    # Solution .NET (ouvrir dans VS / Rider)
│
├── poc/
│   ├── PocPitch/                    # POC offline : détection YIN sur sinus ou WAV
│   └── PocAccompaniment/            # POC : progression d'accords + export MIDI
│
├── src/
│   └── SmartJam/                    # Application principale Avalonia MVVM
│       ├── Audio/
│       │   ├── AudioEngine.cs       # Moteur audio (WASAPI / ASIO) — adapté d'AudioBlocks
│       │   └── SineWaveProvider.cs  # Oscillateur sinus (ISampleProvider NAudio)
│       ├── Services/
│       │   ├── PitchDetectorService.cs         # Détection YIN (NWaves)
│       │   ├── AccompanimentGeneratorService.cs # Génération d'accords (placeholder)
│       │   └── SineAudioSource.cs              # Source offline (offline uniquement)
│       ├── ViewModels/
│       │   └── MainWindowViewModel.cs  # ViewModel principal (MVVM)
│       └── Views/
│           └── MainWindow.axaml        # Interface Avalonia
│
├── assets/
│   └── wav/
│       ├── A4_440Hz_3s.wav          # Fichier WAV de test (440 Hz, 3 s)
│       └── generate_test_wav.py     # Script pour regénérer le WAV
│
└── docs/
    └── tpi/
        ├── B5_processus_metier.md
        ├── G9_concept_realisation.md
        └── G12_tests.md
```

---

## Prérequis

- [.NET 10 SDK](https://dotnet.microsoft.com/download) — vérifier : `dotnet --version`
- Windows 10 / 11 (NAudio WASAPI / ASIO est Windows uniquement)
- Optionnel : Visual Studio 2022 / JetBrains Rider / VS Code + extension C#
- Pour le mode Live : interface audio (ex. Arturia MiniFuse 2) avec driver ASIO installé

---

## Build

```bash
# Build de toute la solution
dotnet build SmartJam.slnx

# Build uniquement l'application principale
dotnet build src/SmartJam/SmartJam.csproj
```

---

## Lancer l'application principale

```bash
cd src/SmartJam
dotnet run
```

L'application démarre avec le mode **TestOscillator** (pas besoin de matériel) :

1. Le mode **TestOscillator** est sélectionné par défaut.
2. Régler la fréquence (ex. `440 Hz` → note A4) et l'amplitude.
3. Cliquer sur **Monitoring ON** → le sinus joue et le pitch est détecté en temps réel.
4. Passer en mode **Live** + **Monitoring ON** pour utiliser une vraie interface audio.
5. **Settings** (placeholder) → permettra de choisir le driver ASIO et les périphériques.

---

## Lancer les POC (tests offline, sans matériel)

### POC Pitch

```bash
cd poc/PocPitch

# Sinus à 440 Hz (devrait détecter A4)
dotnet run -- --source sine --freq 440

# Depuis un fichier WAV
dotnet run -- --source wav --path ../../assets/wav/A4_440Hz_3s.wav
```

### POC Accompagnement

```bash
cd poc/PocAccompaniment

# Pop en Do, 8 mesures, 120 BPM
dotnet run -- --key C --style pop --bars 8 --tempo 120
```

---

## Architecture audio

```
Mode Live (WASAPI / ASIO)
  WasapiCapture / AsioOut (input)
      │ float[] mono
      ├─→ BufferedWaveProvider → WasapiOut (monitoring — l'utilisateur entend ce qu'il joue)
      ├─→ UpdateMeters() → RMS / Peak → UI
      └─→ OnAudioFrame(samples, frames, sampleRate)
              └─→ PitchDetectorService (YIN) → Hz + Note → UI

Mode TestOscillator (WASAPI)
  SineWaveProvider (sinus interne)
      │ float[] mono
      ├─→ WasapiOut (l'utilisateur entend le sinus)
      ├─→ UpdateMeters() → RMS / Peak → UI
      └─→ OnAudioFrame(samples, frames, sampleRate)
              └─→ PitchDetectorService (YIN) → Hz + Note → UI
```

---

## Bibliothèques utilisées

| Lib | Usage | Lien |
|-----|-------|------|
| **NAudio** v2.2.1 | Moteur audio (WASAPI, ASIO, BufferedWaveProvider) | https://github.com/naudio/NAudio |
| **NWaves** v0.9.6 | Algorithme YIN (pitch detection) | https://github.com/ar1st0crat/NWaves |
| **Avalonia** v12.0.1 | Interface graphique (Windows) | https://github.com/AvaloniaUI/Avalonia |
| **CommunityToolkit.Mvvm** v8.4.1 | MVVM (ObservableProperty, RelayCommand) | https://github.com/CommunityToolkit/dotnet |
| **Melanchall.DryWetMidi** v8.0.3 | Export MIDI (POC accompagnement) | https://github.com/melanchall/drywetmidi |

---

## Checklist de complétion

- [x] Structure repo : `poc/PocPitch`, `poc/PocAccompaniment`, `src/SmartJam`, `assets/`, `docs/`
- [x] Solution racine `SmartJam.slnx`
- [x] POC Pitch : sinus + WAV + NWaves YIN + logs Hz + note
- [x] POC Accompagnement : progression d'accords + export MIDI + README
- [x] **AudioEngine** : WASAPI / ASIO, monitoring, RMS/Peak, `OnAudioFrame` (adapté d'AudioBlocks)
- [x] **SineWaveProvider** : oscillateur sinus (mode TestOscillator)
- [x] **UI Avalonia** : Mode dropdown, OscParams, RMS bar, Monitoring ON/OFF, Settings, Freq+Note, Key/Chord, Historique, Log
- [x] **ViewModel** : binding AudioEngine, pitch en temps réel, historique notes, log
- [x] README racine : structure, build, run, architecture audio
- [ ] Service MusicAnalyzerService (estimation tonalité depuis notes accumulées)
- [ ] Modale Settings (sélection driver ASIO / WASAPI + périphériques)

