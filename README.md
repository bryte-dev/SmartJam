# SmartJam 🎸

Application de **détection de pitch** et **génération d'accompagnement musical**, développée dans le cadre du TPI.

> **Mode offline** : tous les POC fonctionnent **sans micro, sans carte son, sans installation musicale**.  
> On utilise soit un **sinus généré** soit un **fichier WAV** pour tester.

---

## Structure du projet

```
SmartJam/
├── SmartJam.slnx                  # Solution .NET (ouvrir dans VS/Rider)
│
├── poc/
│   ├── PocPitch/                  # POC : détection de pitch (NWaves YIN)
│   │   ├── PitchAnalyser.cs       # Algorithme YIN + conversion Hz→note
│   │   ├── SineGenerator.cs       # Génère un sinus de test
│   │   ├── WavReader.cs           # Lit un fichier WAV PCM 16 bits
│   │   └── README.md              # Guide du POC
│   │
│   └── PocAccompaniment/          # POC : progression d'accords + export MIDI
│       ├── AccompanimentGenerator.cs  # Génère des accords (règles musicales)
│       ├── MidiExporter.cs        # Exporte vers .mid (DryWetMIDI)
│       └── README.md              # Guide du POC
│
├── src/
│   └── SmartJam/                  # Application principale Avalonia MVVM
│       ├── Services/
│       │   ├── PitchDetectorService.cs      # Service pitch (YIN)
│       │   ├── AccompanimentGeneratorService.cs  # Service génération
│       │   └── SineAudioSource.cs          # Source audio offline
│       ├── ViewModels/
│       │   └── MainWindowViewModel.cs      # ViewModel principal (MVVM)
│       └── Views/
│           └── MainWindow.axaml            # Interface Avalonia
│
├── assets/
│   └── wav/
│       ├── A4_440Hz_3s.wav         # Fichier WAV de test (La4, 440 Hz, 3s)
│       └── generate_test_wav.py    # Script pour regénérer le WAV
│
└── docs/
    └── tpi/
        ├── B5_processus_metier.md  # Template processus métiers (B5)
        ├── G9_concept_realisation.md  # Architecture technique (G9)
        └── G12_tests.md            # Cas de test (G12)
```

---

## Prérequis

- [.NET 8.0+ SDK](https://dotnet.microsoft.com/download) — vérifier : `dotnet --version`
- Optionnel : Visual Studio 2022 / JetBrains Rider / VS Code + C# extension

---

## Build

```bash
# Build de toute la solution
dotnet build SmartJam.slnx

# Build d'un seul projet
dotnet build poc/PocPitch/PocPitch.csproj
dotnet build poc/PocAccompaniment/PocAccompaniment.csproj
dotnet build src/SmartJam/SmartJam.csproj
```

---

## Lancer les POC (tests offline, sans matériel)

### POC Pitch — Détection de hauteur sonore

```bash
cd poc/PocPitch

# Test 1 : sinus à 440 Hz (devrait détecter "A4")
dotnet run -- --source sine --freq 440

# Test 2 : Do du milieu (C4, 261.63 Hz)
dotnet run -- --source sine --freq 261.63

# Test 3 : depuis le fichier WAV de test fourni
dotnet run -- --source wav --path ../../assets/wav/A4_440Hz_3s.wav
```

Sortie attendue :
```
Temps (s) | Fréquence (Hz) | Note
----------+----------------+------
     0.00s |          441.0 Hz | A4
     0.05s |          441.0 Hz | A4
     ...
```

### POC Accompagnement — Génération + export MIDI

```bash
cd poc/PocAccompaniment

# Pop en Do, 8 mesures, 120 BPM
dotnet run -- --key C --style pop --bars 8 --tempo 120

# Jazz en Sol, 8 mesures
dotnet run -- --key G --style jazz --bars 8 --tempo 100

# Blues en La, 12 mesures
dotnet run -- --key A --style blues --bars 12 --tempo 80
```

Le fichier `.mid` est créé dans `poc/PocAccompaniment/output/progression.mid`.  
Pour l'écouter : **VLC**, **Windows Media Player**, ou n'importe quel DAW.

---

## Lancer l'application principale (Avalonia)

```bash
cd src/SmartJam
dotnet run
```

L'application affiche :
- **Détection de pitch** : choisir source (Sine/WAV), entrer une fréquence, cliquer "Analyser"
- **Génération d'accompagnement** : choisir tonalité/style/tempo, cliquer "Générer"

> ⚠️ Sur Linux/macOS, l'interface Avalonia peut nécessiter un environnement graphique (X11 ou Wayland).

---

## Comment documenter (TPI)

Les templates sont dans `docs/tpi/` :

| Fichier | Document TPI | À remplir |
|---------|-------------|-----------|
| `B5_processus_metier.md` | B5 — Processus métiers | Ajouter déclencheurs, résultats, cas d'erreur |
| `G9_concept_realisation.md` | G9 — Concept de réalisation | Compléter le schéma + vrais noms de classes |
| `G12_tests.md` | G12 — Cas de test | Remplir les résultats obtenus |

### Générer le WAV de test

Si le fichier `assets/wav/A4_440Hz_3s.wav` est manquant :
```bash
python3 assets/wav/generate_test_wav.py
```

---

## Bibliothèques utilisées

| Lib | Usage | Lien |
|-----|-------|------|
| **NWaves** v0.9.6 | Algorithme YIN (pitch detection) | https://github.com/ar1st0crat/NWaves |
| **Melanchall.DryWetMidi** v8.0.3 | Génération + export MIDI | https://github.com/melanchall/drywetmidi |
| **Avalonia** v12.0.1 | Interface graphique cross-platform | https://github.com/AvaloniaUI/Avalonia |
| **CommunityToolkit.Mvvm** v8.4.1 | MVVM (ObservableProperty, RelayCommand) | https://github.com/CommunityToolkit/dotnet |

---

## Checklist de complétion

- [x] Structure repo : `poc/PocPitch`, `poc/PocAccompaniment`, `src/SmartJam`, `assets/`, `docs/`
- [x] Solution racine `SmartJam.slnx`
- [x] POC Pitch : sinus + WAV + NWaves YIN + logs Hz + note
- [x] POC Accompagnement : progression d'accords + export MIDI + README
- [x] App Avalonia MVVM : squelette avec services + UI
- [x] README racine : build / run / test offline + comment documenter
- [ ] Service MusicAnalyzerService (estimation tonalité depuis notes accumulées)
- [ ] Service AccompanimentPlayerService (playback MIDI en temps réel)
- [ ] Intégration micro/WASAPI (lecture en temps réel)

