# POC Accompagnement — SmartJam

Proof of concept pour la **génération algorithmique d'une progression d'accords**
et son **export au format MIDI** (.mid), sans carte son ni installation de logiciel de musique.

## Ce que fait ce POC

- Prend en entrée : tonalité, style musical, tempo, nombre de mesures
- Génère une **progression d'accords** (algorithme basé sur la gamme majeure)
- Exporte vers un fichier **MIDI standard** (.mid) que tu peux ouvrir dans VLC, GarageBand, REAPER, etc.

### Exemple de sortie

```
=== SmartJam — POC Accompagnement ===
Tonalité : C  |  Style : pop  |  Tempo : 120 BPM  |  Mesures : 8

Progression générée (8 accords) :
  I — C maj [60, 64, 67]
  V — G maj [67, 71, 74]
  VI — A min [69, 72, 76]
  IV — F maj [65, 69, 72]
  ...

Fichier MIDI exporté : output/progression.mid
```

## Comment lancer

### Prérequis
- .NET 8.0 SDK (`dotnet --version`)

### Lancer le POC
```bash
dotnet run -- --tempo 120 --key C --style pop --bars 8
dotnet run -- --tempo 100 --key G --style jazz --bars 8
dotnet run -- --tempo 80  --key A --style blues --bars 12
```

### Arguments disponibles

| Argument | Description | Valeur par défaut |
|----------|-------------|-------------------|
| `--tempo` | BPM (60–240) | 120 |
| `--key` | Tonalité (C, D, E, F, G, A, B) | C |
| `--style` | Style (pop, jazz, blues) | pop |
| `--bars` | Nombre de mesures | 8 |
| `--output` | Chemin du fichier .mid | `output/progression.mid` |

### Écouter le fichier MIDI exporté

- **Windows** : double-clic sur le fichier `.mid` → Windows Media Player
- **VLC** : `vlc output/progression.mid`
- **DAW** : importer dans REAPER, GarageBand, Ableton Live, etc.

## Structure du code

| Fichier | Rôle |
|---------|------|
| `Program.cs` | Point d'entrée — lit les arguments, orchestre génération + export |
| `AccompanimentGenerator.cs` | Génère la progression (règles musicales, pas d'IA) |
| `MidiExporter.cs` | Exporte vers .mid via DryWetMIDI |

## Algorithme de génération

### Principe (règles musicales)

1. **Gamme majeure** : intervalles `[0, 2, 4, 5, 7, 9, 11]` depuis la tonique
2. **Triade sur chaque degré** :
   - Majeur = root + 4 demi-tons + 7 demi-tons
   - Mineur = root + 3 demi-tons + 7 demi-tons
3. **Pattern de progression** selon le style :
   - `pop`   → I – V – vi – IV (ex. Do–Sol–La min–Fa)
   - `jazz`  → ii – V – I – I (ex. Ré min–Sol–Do–Do)
   - `blues` → I – IV – I – V (ex. Do–Fa–Do–Sol)

### Format MIDI

- MIDI Type 0 (une seule piste)
- PPQN = 480 ticks par noire (standard)
- Mesure 4/4 = 1920 ticks
- SetTempo + TimeSignature en entête
- NoteOn / NoteOff pour chaque note de chaque accord

## Bibliothèque utilisée

- **Melanchall.DryWetMidi** v8.0.3 — https://github.com/melanchall/drywetmidi
  - Classes utilisées : `MidiFile`, `TrackChunk`, `NoteOnEvent`, `NoteOffEvent`, `SetTempoEvent`
