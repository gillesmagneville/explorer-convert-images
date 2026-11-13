<!-- SPDX-License-Identifier: GPL-3.0-only -->

# explorer-convert-images

🇬🇧 [English version](README.md)

**Convertissez vos photos d'un clic droit, directement depuis l'Explorateur
de fichiers de Windows — RAW compris, en pleine résolution, sans recadrage
ni redimensionnement.**

[![Dernière version](https://img.shields.io/github/v/release/gillesmagneville/explorer-convert-images)](https://github.com/gillesmagneville/explorer-convert-images/releases/latest)
[![Licence GPL-3.0](https://img.shields.io/badge/license-GPL--3.0-blue)](LICENSE)

Sélectionnez des fichiers ou des dossiers entiers dans l'Explorateur de
fichiers, clic droit → **Convertir les images…**, choisissez le format et
le dossier de destination : c'est tout.

explorer-convert-images est le pendant Windows de
[nautilus-convert-images](https://github.com/gillesmagneville/nautilus-convert-images)
(le même outil pour GNOME Fichiers, sous Linux) : même moteur de
conversion, mêmes textes, derrière une fenêtre Windows native.

> **État : version 1.0.0.** Essayé à la main sous Windows 11 —
> installation, entrée de menu (sur des fichiers, des dossiers et le fond
> d'un dossier), conversions, désinstallation — et vérifié sur un vrai
> Windows par le workflow GitHub Actions de ce dépôt, qui construit les
> trois programmes et l'installeur à chaque modification. **Pas encore
> essayé** : Windows 10 et Windows sur ARM. Voir
> [CHANGELOG.fr.md](CHANGELOG.fr.md).

> **Note :** l'interface utilisateur est disponible en **anglais, français,
> espagnol et allemand** (voir [Langues](#langues)). Les autres documents
> (`PROJECT_CONTEXT.md` pour la vision et les principes, `ARCHITECTURE.md`
> pour le contrat technique, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`,
> `SECURITY.md`, `CHANGELOG.md`) existent en **anglais et en français** :
> chacun a en tête un lien pour changer de langue, et les fichiers français
> se terminent par `.fr.md`.

## Pourquoi l'utiliser ?

* **Sans détour** — pas d'application à lancer, pas de ligne de commande,
  pas d'import : la conversion est dans le menu contextuel de vos fichiers.
* **Tout ce que vous avez sous la main** — RAW de nombreux appareils, JPEG,
  PNG, TIFF, BMP, GIF, WebP, même mélangés dans un seul dossier : le format
  de chaque fichier est détecté, même quand l'extension manque ou trompe.
* **Votre qualité, intacte** — pleine résolution, jamais de recadrage ni de
  redimensionnement ; PNG, TIFF, BMP et WebP sont réellement sans perte
  (transparence et profondeur 16 bits conservées). Pour un RAW, cela
  s'applique à l'image développée, pas au fichier d'origine : conservez-le.
* **Rapide** — moteur en Rust, tous les cœurs du processeur travaillent en
  parallèle sur les lots.
* **N'importe quel nombre de fichiers** — toute la sélection arrive d'un
  coup, dans une seule fenêtre, quelle que soit sa taille.
* **Vous gardez la main** — vous voyez ce qui a été détecté avant de
  lancer, choisissez le dossier de destination (on peut en créer un) et ce
  qui se passe si un nom existe déjà (ignorer, renommer, remplacer), puis
  suivez la progression en direct.
* **Sans danger pour l'Explorateur** — rien n'est chargé dans
  `explorer.exe` : l'entrée de menu lance des programmes séparés.
* **Léger pour votre système, et libre** — installé pour votre seul compte
  utilisateur, sans droits d'administrateur ; rien d'autre à installer ;
  licence GPL-3.0.

## Installation

Nécessite Windows 10 (version 1809 ou ultérieure) ou Windows 11, en
64 bits.

Téléchargez `explorer-convert-images-X.Y.Z-setup.exe` (`X.Y.Z` = numéro de
version) depuis la page des
[versions](https://github.com/gillesmagneville/explorer-convert-images/releases/latest)
et lancez-le. Il installe le programme pour votre compte utilisateur, dans
`%LOCALAPPDATA%\Programs\explorer-convert-images`, et ne demande aucun droit
d'administrateur.

> **Note :** l'installeur n'étant pas signé numériquement, Windows affichera
> très probablement au premier lancement un avertissement bleu
> **« Windows a protégé votre ordinateur »** (SmartScreen). C'est normal
> pour un petit projet libre non signé — ce n'est pas le signe d'un logiciel
> malveillant. Cliquez sur **Informations complémentaires**, puis sur
> **Exécuter quand même**.

Pour vérifier votre téléchargement, chaque installeur est publié avec son
propre fichier `.sha256`. Dans PowerShell, dans le dossier du
téléchargement :

```powershell
(Get-FileHash .\explorer-convert-images-X.Y.Z-setup.exe -Algorithm SHA256).Hash
Get-Content .\explorer-convert-images-X.Y.Z-setup.exe.sha256
```

Les deux empreintes doivent être identiques (à la casse près).

Pour désinstaller : **Paramètres > Applications > Applications
installées**, puis `explorer-convert-images` > **Désinstaller**. L'entrée de
menu et tous les fichiers du programme sont supprimés.

## Utilisation

Clic droit sur une sélection (fichiers et/ou dossiers), sur un dossier, ou
sur le fond d'un dossier ouvert → **Convertir les images…**.

* **Sous Windows 11**, l'entrée se trouve dans le menu classique : clic
  droit, puis **Afficher plus d'options** (ou Maj + clic droit, ou
  Maj+F10).
* Le raccourci **Convertir les images** du menu Démarrer ouvre la même
  fenêtre sans rien de sélectionné ; elle propose alors de choisir un
  dossier à convertir.

La fenêtre montre ce qui a été trouvé (sous-dossiers compris), permet de
choisir le format de destination, la qualité JPEG, ce qu'il faut faire quand
un fichier existe déjà et le dossier de destination, puis convertit en
journalisant chaque fichier.

## Langues

L'interface — l'entrée du menu contextuel, la fenêtre, les libellés et les
messages d'erreur, l'installeur — est disponible en **anglais, français,
espagnol et allemand**. La langue est celle de Windows (votre langue
d'affichage) ; pour toute autre langue, l'interface s'affiche en anglais.

* Le texte de l'**entrée de menu** est écrit à l'installation du programme,
  dans la langue qu'a Windows à ce moment-là. Après avoir changé la langue
  de Windows, relancez l'installeur pour que l'entrée suive.
* Pour essayer la fenêtre dans une autre langue sans changer Windows,
  définissez la variable d'environnement `CONVERT_IMAGES_LANGUAGE` (`fr`,
  `de`, `es`, `en`) avant de lancer `ExplorerConvertImages.exe`.
* Les traductions espagnole et allemande n'ont pas encore été relues par
  des locuteurs natifs : les corrections sont bienvenues (voir
  `CONTRIBUTING.md`, qui explique aussi comment ajouter une langue).
* Les messages d'erreur produits par le moteur lui-même sont traduits ; le
  texte renvoyé par les bibliothèques tierces (par exemple le détail d'une
  erreur de `rawloader` ou de LibRaw) reste en anglais.

## Formats

| | Formats |
|---|---|
| **Sources** | RAW (extensions reconnues : CR2/CR3, NEF/NRW, ARW/SRF/SR2, DNG, ORF, RAF, RW2, PEF, SRW, X3F, 3FR, RWL, GPR…), JPEG, PNG, TIFF, BMP, GIF, WebP |
| **Destinations** | JPEG (qualité réglable, 100 par défaut), PNG, TIFF (non compressé), BMP, WebP sans perte (VP8L) |

PNG, TIFF, BMP et WebP sont **réellement sans perte** (canal alpha et
16 bits compris). Le JPEG reste un format avec perte par nature — une limite
du format lui-même, pas de cet outil. La détection se fait par l'extension,
puis par l'analyse du contenu, avec en dernier recours une vraie tentative
de décodage. Les fichiers RAW sont décodés en pleine résolution (zone active
du capteur).

Le moteur est celui de nautilus-convert-images : ce qu'il lit, et avec quel
résultat, est décrit dans la section
[« Compatibilité RAW »](https://github.com/gillesmagneville/nautilus-convert-images/blob/main/README.fr.md)
de ce projet. `engine/UPSTREAM.fr.md` indique quelle version ce dépôt
contient et ce qui a été modifié pour Windows.

## Comment c'est fait

Trois programmes, installés dans le même dossier (voir
`ARCHITECTURE.fr.md`) :

| Programme | Rôle | Langage |
|---|---|---|
| `explorer-convert-images-verb.exe` | Reçoit la sélection depuis le menu de l'Explorateur et ouvre la fenêtre dessus | Rust |
| `ExplorerConvertImages.exe` | La fenêtre (WinUI 3, l'apparence native de Windows 11) | C# |
| `convert-images-engine.exe` | Détecte les formats et convertit ; utilisable seul en ligne de commande | Rust |

## Construction

**Le plus simple est de laisser GitHub tout construire.** Chaque push lance
le workflow `.github/workflows/build.yml`, qui teste les trois programmes,
les construit et produit l'installeur. Téléchargez-le depuis l'onglet
**Actions** du dépôt : ouvrez l'exécution, puis **Artifacts > installer**.

Pour construire à la main, deux machines interviennent, parce que la
fenêtre ne se compile que sous Windows alors que le reste se construit sous
Linux :

```bash
# Sous Linux (Ubuntu/Debian) : les deux programmes Rust et l'installeur
sudo apt-get install nsis gcc-mingw-w64-x86-64-posix g++-mingw-w64-x86-64-posix
rustup target add x86_64-pc-windows-gnu
./build-installer.sh --app DOSSIER_DE_LA_FENETRE_PUBLIEE
```

```powershell
# Sous Windows (SDK .NET 10) : la fenêtre, sous forme de dossier
dotnet publish app/ExplorerConvertImages -c Release -r win-x64 -p:Platform=x64 -o DOSSIER
```

Chaque exécution du workflow conserve aussi, dans l'artefact
`explorer-convert-images-folder`, le dossier complet qu'installe
l'installeur (la fenêtre et les deux programmes Rust) ;
`./build-installer.sh --dist DOSSIER` transforme un tel dossier en
installeur. Détails et variantes : `CONTRIBUTING.fr.md`.

### Publier une version

Fixez la version (`tools/set-version.sh X.Y.Z`), donnez à la version sa
section dans `CHANGELOG.md` et `CHANGELOG.fr.md`, validez, puis poussez une
étiquette `vX.Y.Z`. Le workflow construit l'installeur et prépare une
version en **brouillon** qui le contient avec son fichier `.sha256`, les
notes étant tirées des deux journaux (anglais d'abord, français ensuite).
Rien n'est public tant que vous ne publiez pas le brouillon. Pas à pas :
`CONTRIBUTING.fr.md`.

## Tests

```bash
# Rust : le moteur et la partie du gestionnaire de menu indépendante du système
cargo test

# C# : la logique de la fenêtre, avec le vrai moteur quand il est construit
cargo build --release -p convert-images-engine
dotnet run -c Release --project app/ConvertImages.Core.Tests
```

Les deux s'exécutent sous Linux comme sous Windows. Le workflow exécute en
plus, sur un vrai Windows : les tests du gestionnaire de menu propres à
Windows (son objet COM), les tests C# avec le moteur Windows, et `selftest`,
qui lance la fenêtre construite puis parcourt toute la chaîne —
gestionnaire de menu, puis fenêtre — comme le fait l'Explorateur de
fichiers. `selftest` est un outil de développement (voir
`CONTRIBUTING.fr.md`) : lancé sans argument sur un Windows où le programme
est installé, il vérifie de la même façon la chaîne installée.

## Note sur la chaîne de compilation

* **Rust** — `rust-toolchain.toml` fixe la version utilisée par ceux qui
  ont `rustup` (c'est le cas du workflow). Le moteur garde, inchangées, les
  versions de dépendances fixées par nautilus-convert-images (voir la
  « Note sur la chaîne de compilation » de son README), pour que les deux
  copies du moteur restent faciles à comparer. Le gestionnaire de menu
  utilise le crate `windows`, qui demande Rust 1.82 ou plus récent.
* **Cible Windows** — les programmes Rust sont construits pour
  `x86_64-pc-windows-gnu` (MinGW-w64), depuis Linux : le script de
  construction du LibRaw embarqué ne prend pas en charge le compilateur de
  Microsoft. Les bibliothèques d'exécution C et C++ sont liées
  statiquement : les programmes n'ont besoin d'aucune DLL supplémentaire.
* **.NET** — SDK .NET 10 (`global.json`) ; la version du Windows App SDK est
  fixée dans `app/ExplorerConvertImages/ExplorerConvertImages.csproj`.

## Organisation

```
explorer-convert-images/
├── engine/                      convert-images-engine (Rust) : copie du moteur
│   │                            de nautilus-convert-images, voir UPSTREAM.fr.md
│   ├── src/
│   └── vendor/libraw-sys/       LibRaw, compilé dans le moteur
├── shell-verb/                  explorer-convert-images-verb (Rust)
│   ├── src/                     entrées de registre, objet COM, lancement de la fenêtre
│   └── examples/                selftest et simulate_explorer (outils de test)
├── app/
│   ├── ConvertImages.Core/      ce que fait la fenêtre, sans la fenêtre (C#)
│   ├── ConvertImages.Core.Tests/ ses tests, et le générateur du .pot
│   └── ExplorerConvertImages/   la fenêtre WinUI 3 (C#)
├── po/                          traductions (fr, de, es) et leur modèle
├── installer/installer.nsi      l'installeur NSIS
├── tools/                       set-version.sh, release-notes.sh, make_icon.py
├── build-installer.sh           construit l'installeur, sous Linux
├── .github/workflows/build.yml  teste, construit et empaquette le tout
├── VERSION                      la version du projet
├── Cargo.toml, Cargo.lock       espace de travail Rust (engine + shell-verb)
└── README, ARCHITECTURE, PROJECT_CONTEXT, CONTRIBUTING, CHANGELOG,
    CODE_OF_CONDUCT, SECURITY    (chacun en anglais et en français)
```

## Sécurité

Pour signaler une vulnérabilité, merci de ne pas ouvrir de ticket public : suivez la [politique de sécurité](SECURITY.fr.md).

## Licence

GPL-3.0-only. Voir [LICENSE](LICENSE).

LibRaw, intégré au moteur, garde ses propres licences : voir `LICENSE.LGPL`
et `LICENSE.CDDL` dans `engine/vendor/libraw-sys/LibRaw/`.

Le code de conduite ([CODE_OF_CONDUCT.fr.md](CODE_OF_CONDUCT.fr.md)) est le
Contributor Covenant 3.0, adapté à ce projet ; il est distribué sous sa
propre licence, [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/deed.fr).

explorer-convert-images est développé par Gilles MAGNEVILLE. Gilles n'est
pas développeur professionnel — ce projet a été réalisé avec l'aide de
Claude.ai.
