# G12 — Cas de test SmartJam

> **Comment utiliser ce document**  
> Pour chaque fonctionnalité, définis un cas de test : entrée, résultat attendu, résultat obtenu.  
> Remplis la colonne "Résultat" après chaque test.

---

## Cas de test — POC Pitch

| # | Description | Commande | Résultat attendu | Résultat obtenu | ✓/✗ |
|---|-------------|---------|-----------------|-----------------|-----|
| T1 | Sinus 440 Hz → A4 | `dotnet run -- --source sine --freq 440` | "440 Hz → A4" | 441 Hz → A4 | ✓ |
| T2 | Sinus 261.63 Hz → C4 | `dotnet run -- --source sine --freq 261.63` | "261.63 Hz → C4" | 262 Hz → C4 | ✓ |
| T3 | Sinus 329.63 Hz → E4 | `dotnet run -- --source sine --freq 329.63` | "~330 Hz → E4" | À tester | ☐ |
| T4 | Sinus 523.25 Hz → C5 | `dotnet run -- --source sine --freq 523.25` | "~523 Hz → C5" | À tester | ☐ |
| T5 | WAV test A4 | `dotnet run -- --source wav --path ../../assets/wav/A4_440Hz_3s.wav` | "440 Hz → A4" | 441 Hz → A4 | ✓ |
| T6 | Fréquence hors range (50 Hz) | `dotnet run -- --source sine --freq 50` | "—" (ignorée) | À tester | ☐ |
| T7 | Fréquence hors range (2000 Hz) | `dotnet run -- --source sine --freq 2000` | "—" (ignorée) | À tester | ☐ |

---

## Cas de test — POC Accompagnement

| # | Description | Commande | Résultat attendu | Résultat obtenu | ✓/✗ |
|---|-------------|---------|-----------------|-----------------|-----|
| T8 | Pop en Do 8 mesures | `dotnet run -- --key C --style pop --bars 8` | C-G-Am-F × 2 | C maj, G maj, A min, F maj × 2 | ✓ |
| T9 | Jazz en Sol | `dotnet run -- --key G --style jazz --bars 4` | Am-D-G-G | À tester | ☐ |
| T10 | Blues en La 12 mesures | `dotnet run -- --key A --style blues --bars 12` | A-D-A-E × 3 | À tester | ☐ |
| T11 | Tempo 200 BPM | `dotnet run -- --tempo 200 --key C --style pop` | .mid avec SetTempo correct | À vérifier dans DAW | ☐ |
| T12 | Fichier MIDI créé | Après T8 | `output/progression.mid` existe | ✓ Fichier créé | ✓ |
| T13 | Tonalité inconnue | `dotnet run -- --key X` | Utilise C par défaut + message | À tester | ☐ |
| T14 | Style inconnu | `dotnet run -- --style rock` | Utilise pop par défaut + message | À tester | ☐ |

---

## Cas de test — Application Avalonia

| # | Description | Action | Résultat attendu | ✓/✗ |
|---|-------------|--------|-----------------|-----|
| T15 | Démarrage | `dotnet run` | Fenêtre s'ouvre, interface visible | À tester |
| T16 | Analyse sinus 440 Hz | Source=Sine, Freq=440, clic Analyser | "441.0 Hz", "A4" affiché | À tester |
| T17 | Analyse sinus C4 | Source=Sine, Freq=261.63, clic Analyser | "~262 Hz", "C4" affiché | À tester |
| T18 | Génération pop en C | Key=C, Style=pop, clic Générer | Progression affichée | À tester |
| T19 | Changement de tonalité | Changer Key=G, recliquer Générer | Nouvelle progression en Sol | À tester |

---

## Comment exécuter les tests

```bash
# Tests POC Pitch (depuis poc/PocPitch/)
dotnet run -- --source sine --freq 440
dotnet run -- --source sine --freq 261.63
dotnet run -- --source wav --path ../../assets/wav/A4_440Hz_3s.wav

# Tests POC Accompagnement (depuis poc/PocAccompaniment/)
dotnet run -- --key C --style pop --bars 8
dotnet run -- --key G --style jazz --bars 4
dotnet run -- --key A --style blues --bars 12

# Lancer l'app Avalonia (depuis src/SmartJam/)
dotnet run
```

---

## Notes de rédaction TPI

Pour G12, pense à inclure :
- Les **données d'entrée** exactes
- Le **résultat attendu** précis (valeur, message, fichier)
- Le **résultat obtenu** (capturer la sortie console ou screenshot UI)
- L'état : ✓ (passé), ✗ (échoué), ☐ (pas encore testé)
