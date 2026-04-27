# B5 — Processus métiers SmartJam

> **Comment utiliser ce document**  
> Décris chaque processus "comme il devrait se passer" : entrée → traitement → sortie.  
> Inclure les cas d'erreur (ce qui se passe si ça plante).  
> Remplis la colonne "Implémenté" au fur et à mesure que tu codes.

---

## Liste des processus

| # | Nom du processus | Déclencheur | Résultat attendu | Cas d'erreur | Implémenté |
|---|-----------------|-------------|-----------------|--------------|------------|
| 1 | Sélection source audio | Utilisateur choisit "Sine" ou "WAV" | Source active, paramètres visibles | Source invalide → message d'erreur | ☑ Squelette |
| 2 | Détection de pitch (offline) | Clic "Analyser" | Hz + nom de note affiché | Signal vide, bruit → "—" affiché | ☑ Squelette |
| 3 | Détection de pitch (temps réel) | Buffer audio reçu (micro) | Hz + note mis à jour en continu | Périphérique non dispo → erreur | ☐ À faire |
| 4 | Estimation accord/tonalité | N notes accumulées (~2–5s) | Tonalité probable + accord en cours | Pas assez de notes → "inconnu" | ☐ À faire |
| 5 | Génération progression d'accords | Clic "Générer" | Liste d'accords affichée | Style/tonalité invalide → défaut pop/C | ☑ Squelette |
| 6 | Export MIDI | Après génération | Fichier .mid dans `output/` | Écriture impossible → message d'erreur | ☑ POC |
| 7 | Lecture accompagnement (play) | Clic "Play" | Son joué en boucle | Périphérique MIDI non dispo → erreur | ☐ À faire |
| 8 | Arrêt accompagnement (stop) | Clic "Stop" | Son s'arrête | — | ☐ À faire |
| 9 | Changement de tempo/style | Modification des paramètres | Régénération automatique ou sur demande | — | ☑ Squelette |

---

## Détail des processus principaux

### Processus 2 — Détection de pitch (offline)

**Acteurs** : Utilisateur  
**Déclencheur** : Clic sur "Analyser le pitch"  
**Précondition** : Source audio sélectionnée (Sine ou WAV)

**Flux principal** :
1. L'utilisateur sélectionne "Sine" et entre une fréquence (ex. 440 Hz)
2. Le système génère un signal sinusoïdal de 1 seconde
3. `PitchDetectorService.DetectPitch()` est appelé → YIN analyse le signal
4. La fréquence (Hz) et la note (ex. "A4") sont affichées

**Flux alternatif (source vide)** :
- Si le signal est vide → afficher "—" et message "Signal vide"

**Flux alternatif (pas de pitch)** :
- Si YIN ne trouve pas de pitch → afficher "— Hz / —" et message "Aucun pitch détecté"

---

### Processus 5 — Génération progression

**Déclencheur** : Clic "Générer la progression"  
**Entrée** : Tonalité + Style + Tempo + Mesures

**Flux principal** :
1. `AccompanimentGeneratorService.Generate()` calcule les accords
2. La progression est affichée (ex. "I — C maj, V — G maj, ...")
3. Option export : `MidiExporter.Export()` → fichier .mid

---

## Notes de rédaction TPI

Pour B5, pense à inclure :
- Le **déclencheur** (bouton, événement, temporisation)
- Le **résultat observable** (ce que voit l'utilisateur)
- Le **destinataire** (utilisateur, autre processus)
- Les **cas d'erreur** (périphérique manquant, données invalides)
- La **fréquence** (temps réel, sur demande, périodique)
