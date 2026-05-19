# SmartJam

SmartJam est une application de **détection de note en temps réel** réalisée dans le cadre du TPI.  
Elle permet d’analyser un signal audio, d’estimer la fréquence jouée, puis d’afficher la note correspondante dans une interface graphique.

L’application peut fonctionner de deux façons :
- avec un **oscillateur de test interne**, pour vérifier le fonctionnement sans matériel externe ;
- avec une **entrée audio réelle**, via une interface audio ou un microphone compatible.

---

## Objectif du projet

Le but de SmartJam est de proposer un outil capable de :
- capter un signal audio ;
- détecter la hauteur dominante ;
- afficher la fréquence et la note associée ;
- fournir quelques informations musicales complémentaires, comme un historique des notes détectées.

---

## Prérequis

Pour utiliser SmartJam, il faut :

- **Windows 10 ou 11**
- **.NET 10 SDK**
- éventuellement une **interface audio** si l’on souhaite tester le mode entrée réelle
- de préférence un pilote audio correct si un périphérique externe est utilisé

Pour vérifier que .NET est installé :

```bash
dotnet --version
```

---

## Lancer l’application

Depuis la racine du projet :

```bash
cd src/SmartJam
dotnet run
```

L’application démarre ensuite dans sa fenêtre principale.

---

## Utilisation rapide

### 1. Mode test avec oscillateur interne
Ce mode permet de tester l’application sans brancher d’instrument.

- lancer l’application ;
- vérifier que le mode **TestOscillator** est sélectionné ;
- régler la fréquence souhaitée, par exemple **440 Hz** ;
- cliquer sur **Monitoring ON** ;
- observer la fréquence détectée et la note affichée.

Exemple :
- **440 Hz** doit correspondre à **A4**
- **110 Hz** doit correspondre à **A2**

Ce mode est utile pour vérifier rapidement que la détection fonctionne correctement.

---

### 2. Mode entrée audio réelle
Ce mode permet d’utiliser un signal venant d’un périphérique audio.

- sélectionner le mode **Live** ;
- vérifier que l’entrée audio est correctement configurée ;
- activer **Monitoring ON** si nécessaire ;
- jouer une note propre et stable ;
- observer la note détectée dans l’interface.

Ce mode dépend du matériel utilisé, du pilote audio disponible et de la qualité du signal entrant.

---

## Réglages principaux

L’interface permet notamment de :

- choisir le mode de fonctionnement ;
- activer ou désactiver le monitoring ;
- visualiser le niveau du signal ;
- afficher la fréquence détectée ;
- afficher la note correspondante ;
- consulter un historique récent des notes jouées.

Selon la version utilisée, certains réglages audio plus avancés peuvent également être disponibles.

---

## Conseils d’utilisation

Pour obtenir de meilleurs résultats :

- jouer une note isolée et stable ;
- éviter les bruits parasites ;
- utiliser un niveau d’entrée suffisant, sans saturation ;
- tester d’abord avec l’oscillateur interne avant de passer à une entrée réelle.

---

## Tests simples à effectuer

Quelques essais rapides permettent de vérifier le bon fonctionnement de l’application :

- **440 Hz** avec l’oscillateur interne → note attendue : **A4**
- **110 Hz** avec l’oscillateur interne → note attendue : **A2**
- jeu d’une note simple sur une basse ou un autre instrument monophonique → détection visuellement cohérente

---

## Structure générale du projet

Le projet contient :

- l’application principale **SmartJam**
- des dossiers de test et de preuve de concept
- des ressources de test audio
- une documentation liée au TPI

---

## Bibliothèques utilisées

SmartJam s’appuie notamment sur les outils suivants :

- **NAudio** pour la gestion audio ;
- **NWaves** pour l’analyse du signal et la détection de pitch ;
- **Avalonia** pour l’interface graphique.

---

## Remarque

SmartJam est un prototype fonctionnel centré sur l’analyse de note en temps réel.  
Certaines idées envisagées au départ, comme une génération d’accompagnement plus poussée, n’ont pas été entièrement intégrées dans cette version.
