# G9 — Concept de réalisation SmartJam

> **Comment utiliser ce document**  
> Décris l'architecture technique du système. Montre comment les composants
> s'enchaînent depuis l'entrée audio jusqu'à l'affichage et la génération.  
> Complète avec les vrais noms de classes quand tu as codé.

---

## Pipeline global

```
┌────────────────────────────────────────────────────────────────┐
│  Source Audio                                                  │
│  ┌─────────────┐   ┌──────────────┐                           │
│  │  SineGen    │   │  WavReader   │  (micro WASAPI à venir)   │
│  │  (offline)  │   │  (offline)   │                           │
│  └──────┬──────┘   └──────┬───────┘                           │
│         └────────┬─────────┘                                  │
│                  │  float[] samples                            │
│                  ▼                                             │
│         ┌─────────────────┐                                    │
│         │ PitchDetector   │ NWaves YIN                        │
│         │ Service         │ → Hz + NoteName                   │
│         └────────┬────────┘                                    │
│                  │                                             │
│                  ▼ (à venir)                                   │
│         ┌─────────────────┐                                    │
│         │ MusicAnalyzer   │ Accumule notes → estime tonalité  │
│         │ Service         │ + accord en cours                 │
│         └────────┬────────┘                                    │
│                  │                                             │
└──────────────────┼─────────────────────────────────────────────┘
                   │
┌──────────────────┼─────────────────────────────────────────────┐
│  Génération                                                    │
│                  ▼                                             │
│         ┌─────────────────────────┐                            │
│         │ AccompanimentGenerator  │ Règles musicales           │
│         │ Service                 │ → List<Chord>              │
│         └──────────┬──────────────┘                            │
│                    │                                           │
│            ┌───────┴────────┐                                  │
│            ▼                ▼                                  │
│    ┌──────────────┐  ┌──────────────┐                         │
│    │ MidiExporter │  │  (Playback)  │  (à venir : MIDI play)  │
│    │              │  │              │                          │
│    └──────────────┘  └──────────────┘                         │
└────────────────────────────────────────────────────────────────┘
                   │
┌──────────────────┼─────────────────────────────────────────────┐
│  UI Avalonia MVVM                                              │
│                  ▼                                             │
│         ┌─────────────────────┐                                │
│         │ MainWindowViewModel │ Expose données via bindings   │
│         │                     │ Reçoit commandes (buttons)    │
│         └──────────┬──────────┘                                │
│                    │  (bindings)                               │
│                    ▼                                           │
│         ┌─────────────────────┐                                │
│         │  MainWindow.axaml   │ Vue XAML/Avalonia              │
│         └─────────────────────┘                                │
└────────────────────────────────────────────────────────────────┘
```

---

## Description des composants

### Services (logique métier)

| Classe | Rôle | Dépendances |
|--------|------|-------------|
| `PitchDetectorService` | YIN pitch detection | NWaves |
| `AccompanimentGeneratorService` | Génère progressions d'accords | Aucune |
| `SineAudioSource` | Génère un sinus test | Aucune |
| `MusicAnalyzerService` *(à faire)* | Estime tonalité + accord | — |
| `AccompanimentPlayerService` *(à faire)* | Play/stop/loop MIDI | DryWetMIDI |

### ViewModel

| Classe | Responsabilité |
|--------|---------------|
| `MainWindowViewModel` | Expose toutes les données (ObservableProperty) et commandes (RelayCommand) pour l'UI |

### Vue (UI)

| Fichier | Responsabilité |
|---------|---------------|
| `MainWindow.axaml` | Fenêtre principale (bindings vers ViewModel) |

---

## Patterns utilisés

- **MVVM** : séparation Vue / ViewModel / Modèle
- **Services injectés** : chaque service est instancié dans le ViewModel (pas de DI complexe pour le TPI)
- **ObservableProperty** (CommunityToolkit.Mvvm) : toute propriété du VM notifie l'UI automatiquement
- **RelayCommand** : chaque bouton est lié à une méthode du VM

---

## Dépendances externes

| Lib | Version | Utilisation |
|-----|---------|-------------|
| Avalonia | 12.0.1 | Framework UI cross-platform |
| CommunityToolkit.Mvvm | 8.4.1 | MVVM helpers (ObservableProperty, RelayCommand) |
| NWaves | 0.9.6 | Algorithme YIN (pitch detection) |
| Melanchall.DryWetMidi | 8.0.3 | Export/lecture MIDI |
