# POC Pitch — SmartJam

Proof of concept pour la **détection de hauteur sonore (pitch)** en mode **offline** (sans micro ni carte son).

## Ce que fait ce POC

- Génère un **signal sinusoïdal** à une fréquence donnée (ex. 440 Hz = La4)
- OU lit un **fichier WAV** mono PCM 16 bits
- Applique l'algorithme **YIN** (via NWaves) sur des fenêtres glissantes
- Affiche dans la console : **temps (s)**, **fréquence (Hz)**, **nom de la note**

### Exemple de sortie

```
=== SmartJam — POC Pitch Detection ===
Source : sine
Génération d'un sinus à 440 Hz pendant 3 secondes...
Signal prêt : 132300 échantillons @ 44100 Hz (3.0s)

Temps (s) | Fréquence (Hz) | Note
----------+----------------+------
     0.00s |          441.0 Hz | A4
     0.05s |          441.0 Hz | A4
     ...
```

## Comment lancer

### Prérequis
- .NET 8.0 SDK (`dotnet --version`)

### Mode sinus (sans aucun fichier)
```bash
dotnet run -- --source sine --freq 440
dotnet run -- --source sine --freq 261.63   # C4 (Do du milieu)
dotnet run -- --source sine --freq 329.63   # E4 (Mi)
dotnet run -- --source sine --freq 523.25   # C5 (Do octave supérieure)
```

### Mode WAV
```bash
dotnet run -- --source wav --path ../../assets/wav/A4_440Hz_3s.wav
```

> Un fichier WAV de test (`A4_440Hz_3s.wav`) est fourni dans `assets/wav/`.  
> Il peut être regénéré avec : `python3 assets/wav/generate_test_wav.py`

## Structure du code

| Fichier | Rôle |
|---------|------|
| `Program.cs` | Point d'entrée — lit les arguments, prépare le signal, appelle l'analyse |
| `SineGenerator.cs` | Génère un tableau `float[]` représentant un sinus |
| `WavReader.cs` | Lit un fichier WAV PCM 16 bits (sans dépendance externe) |
| `PitchAnalyser.cs` | Applique YIN sur le signal + convertit Hz → nom de note |

## Algorithme YIN

YIN (de Cheveigné & Kawahara, 2002) est une méthode de détection de pitch basée sur
la **fonction de différence cumulée normalisée (CMDF)** :

1. Pour chaque fenêtre de signal (4096 samples ≈ 93ms), on calcule la périodicité.
2. On cherche le "lag" minimal qui satisfait le seuil CMDF (0.15 par défaut).
3. Ce lag correspond à la période T de la note → fréquence = sampleRate / T.

**Avantage pour le TPI** : algorithme connu, documenté, pas d'IA, 100% local.

## Bibliothèque utilisée

- **NWaves** v0.9.6 — https://github.com/ar1st0crat/NWaves
  - Classe utilisée : `NWaves.Features.Pitch.FromYin(...)`
