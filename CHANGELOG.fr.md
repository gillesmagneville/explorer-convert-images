<!-- SPDX-License-Identifier: GPL-3.0-only -->

# Changelog

🇬🇧 [English version](CHANGELOG.md)

Toutes les modifications notables de ce projet sont documentées dans ce fichier.

Le format suit [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/),
et ce projet adhère au [versionnage sémantique](https://semver.org/lang/fr/).

Chaque version a une section dont le titre est sa date et sa version, sous
cette forme exacte : `## 2026-11-20 — v0.1.0`. Les notes de version publiées
sur GitHub en sont tirées (voir « Publier une version » dans
`CONTRIBUTING.fr.md`).

## [Non publié]

### Modifié

- Documentation portée au niveau de celle de nautilus-convert-images
  (anglais et français) : le README contient désormais en entier la section
  « Compatibilité RAW », un tableau d'usages courants, ce que fait
  l'installeur, l'utilisation du moteur en ligne de commande, la mise en
  place et ce que couvrent les tests ; `ARCHITECTURE.fr.md` décrit le moteur
  (détection des formats, décodage RAW, conversion) ; `CONTRIBUTING.fr.md`
  explique comment ajouter un format de destination ; `PROJECT_CONTEXT.fr.md`
  gagne une section « Extensibilité ».

## 2026-11-13 — v1.0.0

Première version du projet : le pendant Windows de nautilus-convert-images,
sous la forme de trois programmes, d'un installeur et d'un workflow de
construction.

### Ajouté

- **Le gestionnaire de menu** (`explorer-convert-images-verb.exe`, Rust) :
  ajoute **Convertir les images…** au menu contextuel de l'Explorateur de
  fichiers — sur les types d'image que connaît le moteur, sur les dossiers
  et sur le fond d'un dossier — et ouvre la fenêtre sur la sélection. C'est
  un gestionnaire COM hors processus : rien n'est chargé dans l'Explorateur,
  toute la sélection arrive en un seul appel quelle que soit sa taille, et
  tout est enregistré pour le seul utilisateur courant.
- **La fenêtre** (`ExplorerConvertImages.exe`, C#, WinUI 3) : ce qui a été
  détecté, format de destination (JPEG par défaut), qualité JPEG, conduite à
  tenir quand un fichier existe, dossier de destination (avec création d'un
  nouveau dossier), progression et journal en direct, confirmation avant de
  quitter pendant une conversion. Lancée sans rien à convertir, elle
  explique comment utiliser l'entrée de menu et propose de choisir un
  dossier.
- **Le moteur** (`convert-images-engine.exe`, Rust) : le moteur de
  nautilus-convert-images 1.3.5, adapté à Windows. Nouvelle option
  `--input-list` de `detect` et `convert`, pour recevoir par un fichier une
  sélection de n'importe quelle taille.
- **Quatre langues** : anglais, français, espagnol et allemand, pour
  l'entrée de menu, la fenêtre et l'installeur. Les traductions de la
  version Linux sont réutilisées.
- **L'installeur** (NSIS) : par utilisateur, sans droits d'administrateur ;
  il retire la version précédente lors d'une mise à jour, et le
  désinstalleur retire tous les fichiers et toutes les entrées de registre
  du programme.
- **Un workflow GitHub Actions** qui teste les trois programmes, les
  construit (les programmes Rust sous Linux, la fenêtre sous Windows), les
  vérifie ensemble sur un vrai Windows, et produit l'installeur avec son
  fichier `.sha256` ; sur une étiquette de version, il prépare une version
  en brouillon.
- **Des tests** : du moteur, du gestionnaire de menu (entrées de registre,
  objet COM), de la logique de la fenêtre avec le vrai moteur, des
  traductions — et des tests qui comparent ce qui est écrit à deux endroits
  (version, noms des programmes, extensions de fichiers, textes).

### Vérifié à ce jour, et non vérifié

- Vérifié sous Linux : les tests Rust et C# ; les deux programmes Rust
  construits pour Windows et testés sous Wine (y compris le moteur sur des
  fichiers aux noms accentués, l'objet COM, ainsi que l'enregistrement et le
  retrait des entrées de menu) ; l'installeur sous Wine (installation, mise
  à jour, refus d'un dossier qui n'est pas le nôtre, désinstallation).
- Vérifié sur un vrai Windows, par le workflow de construction : les tests
  C# avec le moteur Windows ; les tests du gestionnaire de menu propres à
  Windows ; la construction de la fenêtre ; et un contrôle du démarrage — la
  fenêtre s'ouvre et reste ouverte, lancée directement et par le
  gestionnaire de menu (l'objet COM) comme le fait l'Explorateur de
  fichiers.
- Vérifié à la main sous Windows 11 : l'installeur ; l'entrée de menu
  utilisée depuis l'Explorateur de fichiers lui-même (sur une image, sur
  plusieurs, sur un dossier et sur le fond d'un dossier) ; les conversions ;
  la désinstallation.
- **Non vérifié** : Windows 10, et Windows sur ARM (où les programmes
  tournent grâce à l'émulation intégrée à Windows 11). Les traductions
  espagnole et allemande n'ont pas été relues par des locuteurs natifs.

### Limites connues

- Sous Windows 11, l'entrée est dans le menu classique (**Afficher plus
  d'options**, ou Maj + clic droit). Apparaître dans le menu de premier
  niveau demande un paquet signé et doté d'une identité, ce que le projet
  ne fournit pas encore.
- L'installeur n'est pas signé : Windows ou le navigateur peut afficher un
  avertissement d'éditeur inconnu au téléchargement ou au lancement.
