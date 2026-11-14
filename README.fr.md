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
* **Tout ce que vous avez sous la main** — RAW de nombreux appareils (voir
  « Compatibilité RAW »), JPEG, PNG, TIFF, BMP, GIF, WebP, même mélangés
  dans un seul dossier : le format de chaque fichier est détecté, même quand
  l'extension manque ou trompe.
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

| Vous avez | Vous voulez | Il suffit de |
|---|---|---|
| Un dossier de RAW après un shooting | Des JPEG à partager | le sélectionner, choisir JPEG (le choix par défaut) |
| Des RAW à passer dans un autre logiciel | Une image sans perte | choisir TIFF ou PNG (16 bits), et garder le RAW |
| Un dossier qui mélange CR2, TIFF et PNG | Un seul format partout | tout sélectionner, choisir le format |
| Des BMP ou PNG volumineux | Des fichiers plus compacts, sans perte | choisir WebP sans perte |

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
> **Exécuter quand même**. Pour la même raison, votre navigateur peut
> signaler le téléchargement ; la vérification ci-dessous vous dit si le
> fichier est bien celui qui a été publié.

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

### Ce que fait l'installeur

* Il copie les trois programmes (voir « Comment c'est fait ») dans
  `%LOCALAPPDATA%\Programs\explorer-convert-images`, ajoute au menu Démarrer
  un raccourci **Convertir les images**, et inscrit le programme dans
  **Paramètres > Applications** pour qu'on puisse le désinstaller de là.
* Il ajoute l'entrée de menu au registre, **pour l'utilisateur courant
  seulement** (`HKEY_CURRENT_USER`) : sur les fichiers image, sur les
  dossiers et sur le fond d'un dossier. Les types de fichiers gardent le
  programme auquel ils sont associés : l'entrée ne devient jamais ce que
  fait un double-clic.
* Installer une version plus récente par-dessus une plus ancienne est une
  mise à jour : la version précédente est d'abord supprimée, entrée de menu
  comprise.
* Le désinstalleur supprime tous les fichiers et toutes les entrées de
  registre du programme, et uniquement ceux-là : les entrées d'autres
  programmes ne sont jamais touchées.
* Rien d'autre n'a besoin d'être présent sur votre machine : les
  environnements d'exécution .NET et Windows App SDK sont fournis avec le
  programme, LibRaw est intégrée au moteur, et les bibliothèques d'exécution
  C et C++ sont liées statiquement.

## Utilisation

Clic droit sur une sélection (fichiers et/ou dossiers), sur un dossier, ou
sur le fond d'un dossier ouvert → **Convertir les images…**.

* **Sous Windows 11**, l'entrée se trouve dans le menu classique : clic
  droit, puis **Afficher plus d'options** (ou Maj + clic droit, ou
  Maj+F10). Apparaître dans le menu de premier niveau de Windows 11 demande
  un paquet signé, ce que le projet ne fournit pas encore (voir
  `PROJECT_CONTEXT.fr.md`). Windows 10 n'a que le menu classique : l'entrée
  se trouve donc dans le menu qui s'affiche tout de suite.
* Le raccourci **Convertir les images** du menu Démarrer ouvre la même
  fenêtre sans rien de sélectionné ; elle propose alors de choisir un
  dossier à convertir.

La fenêtre montre ce qui a été trouvé (sous-dossiers compris), permet de
choisir le format de destination, la qualité JPEG, ce qu'il faut faire quand
un fichier existe déjà et le dossier de destination, puis convertit en
journalisant chaque fichier. Par défaut, le dossier de destination est le
dossier le plus profond qui contient toute la sélection. Fermer la fenêtre
pendant une conversion demande confirmation ; les fichiers déjà convertis
restent où ils sont.

La fenêtre peut aussi se lancer à la main, en dehors de l'Explorateur, avec
des fichiers et/ou des dossiers en arguments :

```powershell
& "$env:LOCALAPPDATA\Programs\explorer-convert-images\ExplorerConvertImages.exe" `
    "$HOME\Pictures\img001.cr2" "$HOME\Pictures\img002.jpg"
```

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
* Le moteur en ligne de commande (`convert-images-engine.exe`, son aide et
  ses messages) est en anglais.

## Formats

| | Formats |
|---|---|
| **Sources** | RAW (extensions reconnues : CR2/CR3, NEF/NRW, ARW/SRF/SR2, DNG, ORF, RAF, RW2, PEF, SRW, X3F, 3FR, RWL, GPR… ; voir « Compatibilité RAW »), JPEG, PNG, TIFF, BMP, GIF, WebP |
| **Destinations** | JPEG (qualité réglable, 100 par défaut), PNG, TIFF (non compressé), BMP, WebP sans perte (VP8L) |

PNG, TIFF, BMP et WebP sont **authentiquement sans perte**, vérifié par
comparaison pixel à pixel lors de la validation du moteur dans
nautilus-convert-images (canal alpha et 16 bits compris). JPEG reste par
nature un format avec perte — limite du format lui-même, pas de cet outil,
et jamais présentée comme autre chose. La détection se fait par extension,
puis par analyse du contenu, avec en dernier recours une tentative de
décodage réel. Les RAW sont décodés à pleine résolution (zone active du
capteur).

Le moteur, le gestionnaire de menu et la fenêtre sont découplés (voir
`ARCHITECTURE.fr.md`) : le moteur (`convert-images-engine.exe`) est un
programme autonome en ligne de commande, utilisable indépendamment de
l'Explorateur ou de la fenêtre (voir « Ligne de commande »).

Le moteur est celui de nautilus-convert-images, adapté pour fonctionner sous
Windows : `engine/UPSTREAM.fr.md` indique quelle version ce dépôt contient et
ce qui a été modifié.

## Compatibilité RAW

Reconnaître un fichier comme RAW (par son extension) ne garantit pas de
pouvoir le décoder : cela dépend de l'appareil.

* **Une conversion, pas un développement** — pour un fichier RAW en
  entrée, le rendu est celui des données brutes du boîtier : balance des
  blancs de l'appareil et conversion en sRGB, sans retouche (pas de
  correction d'objectif — vignetage, distorsion —, pas de netteté ni de
  réduction de bruit, pas de réglage créatif d'exposition ou de contraste).
  Ce n'est donc ni le rendu du JPEG du boîtier (plus contrasté, coins
  corrigés, parfois recadré en 16:9 alors que l'image convertie couvre tout
  le capteur), ni celui d'un logiciel de développement comme darktable ou
  RawTherapee, qui ajoutent leurs propres réglages par défaut. Pour un
  résultat travaillé, convertissez en TIFF ou PNG 16 bits et retouchez dans
  un tel logiciel.
* **Ordre de décodage** — `rawloader` d'abord ; s'il ne connaît pas le
  boîtier, repli automatique sur [LibRaw](https://www.libraw.org/),
  embarquée dans le programme. Si les deux échouent, l'erreur est affichée
  pour ce fichier et le lot continue avec le suivant.
* **Testé en usage réel** — uniquement des fichiers Sony ARW, dont ceux du
  RX100 VI (absent de la base de `rawloader`, donc décodé par le repli),
  vérifiés par l'auteur avec nautilus-convert-images, qui a le même moteur.
  Canon, Nikon, Fujifilm, Olympus, Panasonic, Pentax, Sigma, DNG… sont pris
  en charge d'après ce qu'annoncent `rawloader` et LibRaw, mais n'ont pas
  été vérifiés par l'auteur ; la fidélité des couleurs peut varier d'un
  boîtier à l'autre.
* **Limites du repli LibRaw** — il est compilé sans les bibliothèques
  facultatives : il ne lit ni les DNG compressés avec perte (JPEG) ou en
  virgule flottante compressée (deflate), ni les Sigma Foveon (X3F ;
  `rawloader` en lit certains). Le CR3 de Canon n'existe pas dans
  `rawloader` : il dépend donc entièrement du repli, et n'a pas été testé.
* **Deux décodeurs, deux rendus** — le décodeur principal (`imagepipe`)
  applique une courbe de tons de base légère pour passer du signal linéaire
  du capteur à un aspect naturel ; le repli LibRaw n'en applique pas et ses
  images tendent à paraître plus sombres. Un lot qui mêle des boîtiers
  traités par l'un et par l'autre peut donc présenter des luminosités un
  peu différentes.
* **Chemins accentués ou très longs** — sous Windows, le moteur donne le
  fichier à LibRaw depuis la mémoire : un chemin contenant des accents ou
  de plus de 260 caractères est lu comme n'importe quel autre.
* **Fichier refusé ou aux couleurs douteuses ?** Signalez-le via les
  [issues](https://github.com/gillesmagneville/explorer-convert-images/issues)
  en indiquant le modèle d'appareil.

## Ligne de commande

Le moteur peut aussi s'utiliser seul, depuis une ligne de commande. Il écrit
un objet JSON par ligne sur sa sortie standard (détection, puis progression
de la conversion), ce qui permet à la fenêtre de le suivre :

```powershell
$engine = "$env:LOCALAPPDATA\Programs\explorer-convert-images\convert-images-engine.exe"
& $engine formats
& $engine detect --recursive "$HOME\Pictures"
& $engine convert --recursive "$HOME\Pictures" `
    --output-dir "$HOME\Pictures\converted" --format png
```

`--format` vaut `jpeg`, `png`, `tiff`, `bmp` ou `webp` ; `--quality` (1–100)
s'applique au JPEG ; `--on-collision` vaut `skip`, `rename` (par défaut) ou
`overwrite` ; `--threads` limite le nombre de cœurs utilisés ; `--input-list
FICHIER` ajoute les chemins listés dans un fichier, un par ligne. `&
$engine --help` montre tout. La forme exacte des événements est décrite dans
`ARCHITECTURE.fr.md`.

## Comment c'est fait

Trois programmes, installés dans le même dossier (voir
`ARCHITECTURE.fr.md`) :

| Programme | Rôle | Langage |
|---|---|---|
| `explorer-convert-images-verb.exe` | Reçoit la sélection depuis le menu de l'Explorateur et ouvre la fenêtre dessus | Rust |
| `ExplorerConvertImages.exe` | La fenêtre (WinUI 3, l'apparence native de Windows 11) | C# |
| `convert-images-engine.exe` | Détecte les formats et convertit ; utilisable seul en ligne de commande | Rust |

L'entrée de menu est un verbe COM servi par le gestionnaire de menu, un
programme séparé lancé à la demande : l'Explorateur ne fait que lui confier
la sélection (toute la sélection, en un seul appel, quelle que soit sa
taille, au moyen d'un fichier) et n'est jamais mis en attente. Si quelque
chose se passe mal, les programmes écrivent ce qui est arrivé dans
`explorer-convert-images-verb.log` et `explorer-convert-images-app.log`,
dans le dossier temporaire de votre utilisateur (tapez `%TEMP%` dans la
barre d'adresse de l'Explorateur) : joignez-les à votre signalement de
problème.

## Mise en place

Pour récupérer les sources :

```bash
git clone https://github.com/gillesmagneville/explorer-convert-images.git
cd explorer-convert-images
```

Ce dont vous avez besoin dépend de ce que vous voulez faire (détails, avec
les commandes : `CONTRIBUTING.fr.md`) :

* **Tests du moteur et du gestionnaire de menu** — sous Linux : Rust (avec
  `rustup` : `rust-toolchain.toml` choisit la version) et un compilateur
  C/C++, nécessaire pour construire la LibRaw intégrée au moteur. La
  première construction est plus longue (quelques minutes) ; les suivantes
  sont incrémentales.
* **Tests de la logique de la fenêtre** — le SDK .NET 10, sous Linux comme
  sous Windows.
* **La fenêtre elle-même** — Windows et le SDK .NET 10.
* **Les tests du gestionnaire de menu propres à Windows, et `selftest`** —
  Windows et Rust.
* **L'installeur** — Linux, avec `nsis` et les compilateurs MinGW-w64.

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

`build-installer.sh` construit les deux programmes Rust, les rassemble avec
la fenêtre dans `dist/explorer-convert-images/`, puis construit l'installeur
avec NSIS ; le résultat est `dist/explorer-convert-images-X.Y.Z-setup.exe` et
son fichier `.sha256`. `--no-rust-build` réutilise les programmes Rust déjà
construits. Chaque exécution du workflow conserve aussi, dans l'artefact
`explorer-convert-images-folder`, le dossier complet qu'installe
l'installeur (la fenêtre et les deux programmes Rust) ;
`./build-installer.sh --dist DOSSIER` transforme un tel dossier en
installeur, ce que fait le workflow. Le script vérifie que tout ce dont
l'installeur a besoin est présent, et s'arrête avec un message sinon.
Détails et variantes : `CONTRIBUTING.fr.md`.

### Publier une version

Fixez la version (`tools/set-version.sh X.Y.Z`), donnez à la version sa
section dans `CHANGELOG.md` et `CHANGELOG.fr.md`, validez, puis poussez une
étiquette `vX.Y.Z`. Le workflow construit l'installeur et prépare une
version en **brouillon** qui le contient avec son fichier `.sha256`, les
notes étant tirées des deux journaux (anglais d'abord, français ensuite). Il
refuse de le faire si l'étiquette et le fichier `VERSION` ne concordent pas,
ou s'il manque la section dans un journal. Rien n'est public tant que vous
ne publiez pas le brouillon. Pas à pas : `CONTRIBUTING.fr.md`.

## Tests

Le moteur est celui de nautilus-convert-images, avec ses tests unitaires
inchangés. Il y a été validé par comparaison pixel à pixel
(PNG/TIFF/BMP/WebP identiques bit à bit à la source, y compris avec canal
alpha et profondeur 16 bits ; JPEG avec un écart minime et attendu à
qualité 100). Le repli LibRaw est couvert par des tests unitaires sur un DNG
synthétique.

```bash
# Rust : le moteur et la partie du gestionnaire de menu indépendante du système
cargo test

# C# : la logique de la fenêtre, avec le vrai moteur quand il est construit
cargo build --release -p convert-images-engine
dotnet run -c Release --project app/ConvertImages.Core.Tests
```

Les deux s'exécutent sous Linux comme sous Windows. Les tests vérifient
aussi ce qui est écrit à deux endroits (version, noms des programmes,
extensions de fichier de l'entrée de menu, textes de la fenêtre et du
moteur) et les traductions : catalogues complets, champs de substitution
identiques au texte source. Les tests qui ont besoin d'un outil absent ou
d'un moteur construit sont ignorés, pas en échec.

Le workflow exécute en plus, sur un vrai Windows : les tests du
gestionnaire de menu propres à Windows (son objet COM), les tests C# avec le
moteur Windows, et `selftest`, qui lance la fenêtre construite puis
parcourt toute la chaîne — gestionnaire de menu, puis fenêtre — comme le
fait l'Explorateur de fichiers. `selftest` est un outil de développement
(voir `CONTRIBUTING.fr.md`) : lancé sans argument sur un Windows où le
programme est installé, il vérifie de la même façon la chaîne installée. Les
programmes Windows sont aussi essayés sous Wine, sur Linux
(`CONTRIBUTING.fr.md`).

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
