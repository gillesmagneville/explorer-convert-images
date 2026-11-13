<!-- SPDX-License-Identifier: GPL-3.0-only -->

# PROJECT_CONTEXT.md

🇬🇧 [English version](PROJECT_CONTEXT.md)

# explorer-convert-images

**explorer-convert-images** est un module libre pour l'Explorateur de
fichiers de Windows qui convertit des images — y compris les fichiers RAW
des appareils photo — entre les formats courants, directement depuis le menu
contextuel.

C'est le pendant Windows de
[nautilus-convert-images](https://github.com/gillesmagneville/nautilus-convert-images),
qui fait la même chose dans GNOME Fichiers sous Linux. Les deux projets
partagent leur but, leurs principes, leur moteur de conversion et leur
vocabulaire ; chacun a l'intégration et la fenêtre propres à son système.

Son but est de rendre la conversion d'images aussi simple qu'un clic droit,
sans jamais sacrifier la qualité de l'image ni obliger l'utilisateur à
ouvrir un terminal ou un logiciel de retouche.

explorer-convert-images s'adresse :

* aux photographes amateurs et semi-professionnels
* aux utilisateurs de Windows en général
* aux personnes qui reçoivent des fichiers RAW ou TIFF et ont juste besoin
  d'un JPEG (ou l'inverse) sans rien connaître à la photographie
* à celles et ceux qui préparent des postes de travail pour d'autres

Il est conçu pour fonctionner **entièrement en local** : pas de compte, pas
de service en ligne, pas de télémétrie.

> **Note sur ce document :** PROJECT_CONTEXT.md définit *pourquoi* le projet
> existe et *ce à quoi il tient*. Il ne redéfinit pas les contrats exacts
> (le schéma JSON du moteur, les entrées de registre, ce qui doit changer
> ensemble) — ceux-ci vivent à un seul endroit, de façon canonique, dans
> `ARCHITECTURE.fr.md`, pour que les deux documents ne puissent pas diverger
> en silence.

---

# Vision

Convertir un lot de photos sous Windows suppose le plus souvent d'installer
et d'apprendre une application complète, ou de confier ses fichiers à un
service en ligne, pour une tâche qui devrait être triviale.

explorer-convert-images veut répondre à une question simple :

> **« J'ai sélectionné des photos dans mon gestionnaire de fichiers, je les
> veux dans un autre format, sans rien perdre — comment ? »**

Plutôt que de remplacer les outils avancés de traitement RAW, il ne
s'occupe que de la conversion de format, sans retouche, exactement comme le
ferait l'appareil lui-même en exportant un JPEG depuis son écran.

---

# Philosophie du projet

Les cinq premiers principes sont ceux de nautilus-convert-images ; les deux
derniers sont propres à la version Windows.

## 1. Ne jamais perdre plus que ce que le format impose

PNG, TIFF, BMP et WebP (mode sans perte) sont réencodés sans aucune perte
d'information. Le JPEG reste par nature un format avec perte : on ne peut
pas le contourner, et l'outil ne prétend jamais le contraire. La qualité par
défaut (100) réduit cette perte autant que le format le permet.

## 2. Ne jamais recadrer, ne jamais redimensionner

Aucune étape du traitement ne recadre ni ne réduit la résolution d'une
image. Une image RAW est décodée à la résolution exacte de la zone active du
capteur.

## 3. Lecture seule sur les fichiers source

explorer-convert-images ne modifie ni ne supprime jamais le fichier
d'origine. Il lit, convertit, et écrit un nouveau fichier ailleurs (ou sous
un nouveau nom).

## 4. S'insérer dans les habitudes existantes, ne pas en créer de nouvelles

Pas de nouvelle application à apprendre : le point d'entrée est le menu
contextuel de l'Explorateur de fichiers, que l'utilisateur connaît déjà. La
fenêtre ne s'ouvre qu'à la demande, pour cette seule tâche, et ressemble au
reste de Windows.

## 5. Rapide

Le décodage et l'encodage utilisent du code natif (Rust, avec LibRaw en C++
en secours pour les RAW) et tirent parti de tous les cœurs disponibles pour
les conversions par lots.

## 6. Un invité dans l'Explorateur, jamais un risque pour lui

Rien du projet n'est chargé dans l'Explorateur de fichiers. L'entrée de menu
lance des programmes séparés : quoi qu'il arrive de notre côté, le
gestionnaire de fichiers de l'utilisateur continue de fonctionner.

## 7. Laisser le système comme on l'a trouvé

Installé pour le seul utilisateur courant, sans droits d'administrateur. Le
désinstalleur retire tout ce que l'installeur a écrit — fichiers et
registre — et rien de ce qui appartient à un autre programme.

---

# Périmètre du projet

explorer-convert-images se concentre sur :

* la détection du format des images (standard et RAW)
* la conversion de format, sans retouche
* l'intégration au menu contextuel de l'Explorateur de fichiers
* la gestion du dossier de destination (y compris la création d'un nouveau
  dossier) et des collisions de noms de fichiers

explorer-convert-images n'est **pas** :

* un éditeur RAW (pas de réglage d'exposition, de balance des blancs
  manuelle, de courbes ni de recadrage)
* un outil pour renommer ou organiser une photothèque
* un service de conversion en ligne ni un programme d'arrière-plan
* un fournisseur de vignettes ou d'aperçus pour l'Explorateur de fichiers

---

# Plateformes visées

Windows 10 (version 1809 ou ultérieure) et Windows 11, en 64 bits (x64).

Windows sur ARM n'est pas une cible à part entière : les programmes x64
devraient y fonctionner grâce à l'émulation intégrée à Windows 11, mais cela
n'a pas été vérifié.

---

# Expérience utilisateur

L'utilisation d'explorer-convert-images doit rester simple :

1. Sélectionner des fichiers et/ou des dossiers dans l'Explorateur de
   fichiers.
2. Clic droit → **Convertir les images…** (« Convert images… » en anglais,
   « Convertir imágenes… » en espagnol, « Bilder konvertieren … » en
   allemand : la langue suit celle de Windows). Sous Windows 11 : dans
   **Afficher plus d'options**.
3. Choisir le format de destination et le dossier de sortie.
4. Convertir.

Aucune étape de configuration préalable, aucun compte, aucune connexion
réseau.

---

# Formats

**Formats source détectés** : JPEG, PNG, TIFF, BMP, GIF, WebP, et un large
éventail de formats RAW d'appareils photo (Canon, Nikon, Sony, Adobe DNG,
Olympus, Fujifilm, Panasonic, Pentax, Samsung, Sigma, Hasselblad, Leica,
GoPro...). Les formats de destination et leurs garanties (sans perte ou non)
sont ceux du moteur, définis dans l'`ARCHITECTURE.fr.md` de
nautilus-convert-images.

---

# Fiabilité et transparence

explorer-convert-images ne prétend jamais qu'une conversion est sans perte
quand elle ne l'est pas (le cas du JPEG). Quand une limite existe, elle est
documentée et affichée, pas cachée.

Il en va de même pour le projet lui-même : ce qui a été vérifié et ce qui ne
l'a pas été est écrit (voir la note d'état du README et le CHANGELOG), et ce
qui peut être contrôlé par un test l'est par un test plutôt que par une
promesse.

---

# Un moteur, deux projets

Le moteur de conversion est développé dans nautilus-convert-images ; ce
dépôt en contient une copie, adaptée pour se construire et fonctionner sous
Windows. Le choix d'une copie plutôt que d'une dépendance partagée garde
chaque projet complet et indépendant, au prix du report manuel des
correctifs du moteur. Pour que ce prix reste faible :

* la copie reste aussi proche que possible de sa source, les différences
  étant limitées à ce que Windows impose et listées dans
  `engine/UPSTREAM.fr.md` ;
* le comportement du moteur n'est pas modifié ici : un correctif ou une
  fonctionnalité a d'abord sa place en amont.

---

# Objectifs de performance

* Détection : quasi instantanée, même sur plusieurs milliers de fichiers
* Conversion : parallélisée sur tous les cœurs disponibles
* Une sélection de n'importe quelle taille, transmise d'un seul coup
* Aucun programme résident : le gestionnaire de menu s'exécute l'instant
  d'un clic, la fenêtre et le moteur le temps d'une conversion
* Aucune télémétrie, aucune dépendance à un service en ligne

---

# Principes de code

* Rust stable uniquement — pas de fonctionnalités nightly
* Le Rust `unsafe` est cantonné aux appels à LibRaw (moteur) et à Windows
  (gestionnaire de menu), chacun avec sa justification
* La fenêtre contient aussi peu de logique que possible : ce qui peut se
  décider sans fenêtre vit dans une bibliothèque testée sur tous les
  systèmes
* Une dépendance n'est ajoutée que si elle apporte un bénéfice net évident —
  voir « Philosophie des dépendances » dans `ARCHITECTURE.fr.md`
* Ce qui est écrit à deux endroits est gardé par un test — voir « Ce qui
  doit concorder » dans `ARCHITECTURE.fr.md`

---

# Vision future

De futures versions pourraient comporter :

* un installeur signé numériquement, qui supprimerait l'avertissement
  SmartScreen
* une entrée dans le menu contextuel de premier niveau de Windows 11
* ce que le moteur gagnera en amont (nouveaux formats, options)
* des langues d'interface supplémentaires

Rien de tout cela n'est nécessaire pour que l'outil soit utile.

---

# Ce que le projet ne sera pas

explorer-convert-images ne cherchera jamais à devenir :

* un éditeur RAW complet
* un gestionnaire de photothèque
* un outil généraliste de traitement par lots (redimensionnement,
  filigrane, filtres...)

Il doit rester un outil de conversion de format ciblé et prévisible.

---

# Valeurs du projet

explorer-convert-images tient à :

* la fidélité au fichier source
* la clarté (dire quand un format est limité par nature, plutôt que le
  cacher)
* la rapidité
* la facilité d'intégration (un menu contextuel, pas une nouvelle
  application à apprendre)
* le respect du système de l'utilisateur
* la transparence sur le fonctionnement de l'outil et sur ses limites

Par-dessus tout :

**Une conversion ne doit jamais surprendre l'utilisateur — ni par une perte
de qualité non annoncée, ni par un recadrage, ni par une résolution
réduite.**
