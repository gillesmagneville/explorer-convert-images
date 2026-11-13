<!-- SPDX-License-Identifier: GPL-3.0-only -->

# Architecture d'explorer-convert-images

🇬🇧 [English version](ARCHITECTURE.md)

Ce document est le contrat technique du projet : ce que fait chaque
programme, ce qu'ils se disent, et ce qui doit changer ensemble. La vision
et les principes sont dans `PROJECT_CONTEXT.fr.md`.

## Vue d'ensemble

```
 Explorateur de fichiers              (explorer.exe — rien de nous ne s'y exécute)
      │  clic droit → « Convertir les images… »
      │  COM : lance le gestionnaire, lui donne toute la sélection en un appel
      ▼
 explorer-convert-images-verb.exe     le gestionnaire de menu (Rust)
      │  écrit les chemins sélectionnés dans un fichier texte
      │  lance : ExplorerConvertImages.exe --input-list FICHIER
      ▼
 ExplorerConvertImages.exe            la fenêtre (C#, WinUI 3)
      │  lance, autant de fois que nécessaire :
      │    convert-images-engine.exe detect  --input-list FICHIER …
      │    convert-images-engine.exe convert --input-list FICHIER …
      │  lit un événement JSON par ligne sur sa sortie standard
      ▼
 convert-images-engine.exe            le moteur (Rust) : détecte et convertit
```

Trois programmes séparés, à dessein :

* **Rien ne s'exécute dans l'Explorateur de fichiers.** Le gestionnaire est
  un serveur COM *hors processus* : l'Explorateur dialogue avec un programme
  à nous, il ne charge pas une DLL à nous. Un bug de notre côté ne peut ni
  faire planter ni ralentir `explorer.exe`.
* **La fenêtre ne décode jamais une image.** Tout le travail sur les images
  est fait par le moteur, dans son propre processus : la fenêtre reste
  réactive, un fichier corrompu peut au pire faire signaler une erreur au
  moteur, et annuler revient simplement à arrêter ce processus.
* **Le moteur ne sait rien de l'interface de Windows.** C'est un programme
  en ligne de commande, avec un contrat réduit et stable (ci-dessous), le
  même code que la version Linux.

Tout est installé **par utilisateur** (fichiers sous `%LOCALAPPDATA%`,
registre sous `HKEY_CURRENT_USER`) : aucun droit d'administrateur, ni à
l'installation ni ensuite.

---

# Organisation du projet

```
engine/                       convert-images-engine
  src/main.rs, cli.rs         ligne de commande, événements JSON
  src/formats.rs              détection des formats
  src/convert.rs              conversion
  src/raw.rs, libraw.rs       décodage RAW (rawloader/imagepipe, puis LibRaw)
  vendor/libraw-sys/          LibRaw et ses liaisons
  UPSTREAM.fr.md              d'où vient cette copie, ce qui diffère
shell-verb/                   explorer-convert-images-verb
  src/main.rs                 ligne de commande (-Embedding, --register, --unregister)
  src/plan.rs                 les entrées de registre, sous forme de données
  src/registry.rs             les écrit et les retire         (Windows seulement)
  src/com.rs                  l'objet COM et son serveur      (Windows seulement)
  src/launch.rs               fichier de sélection, lancement de la fenêtre
  src/extensions.rs           types de fichiers qui reçoivent l'entrée de menu
  src/labels.rs               texte de l'entrée de menu, par langue
  examples/selftest.rs        vérification de bout en bout sur un vrai Windows
  examples/simulate_explorer.rs  invoque le gestionnaire sur des chemins donnés
app/
  ConvertImages.Core/         tout de la fenêtre sauf la fenêtre :
    Selection.cs                arguments, sélection, dossier de destination
    Engine/                     lancement du moteur, ses événements, ses messages
    Localization/               textes (Msg.cs) et leur traduction
  ConvertImages.Core.Tests/   tests de ce qui précède ; génère le .pot
  ExplorerConvertImages/      l'application WinUI 3
    App.xaml(.cs)               démarrage, apparence standard des contrôles
    MainWindow.cs               la fenêtre, construite en code
po/                           fr.po, de.po, es.po, explorer-convert-images.pot
installer/installer.nsi       installeur NSIS
```

---

# Le gestionnaire de menu (`explorer-convert-images-verb`)

## Entrées de registre

Écrites par `explorer-convert-images-verb.exe --register` (lancé par
l'installeur), retirées par `--unregister` (lancé par le désinstalleur).
Elles sont décrites une seule fois, sous forme de données, dans `plan.rs` ;
toutes sont sous `HKEY_CURRENT_USER\Software\Classes` :

```
CLSID\{3062A5F8-D1CE-49DC-8ADE-8B1BC53CF1BA}
    (par défaut)                 = explorer-convert-images context-menu handler
    LocalServer32\(par défaut)   = "<dossier d'installation>\explorer-convert-images-verb.exe"

<parent>\shell\ExplorerConvertImages
    (par défaut)                 = Convertir les images…     (dans la langue de Windows)
    Icon                         = "<dossier d'installation>\ExplorerConvertImages.exe",0
    MultiSelectModel             = Player
    NeverDefault                 = (vide)
    command\DelegateExecute      = {3062A5F8-D1CE-49DC-8ADE-8B1BC53CF1BA}
```

`<parent>` vaut tour à tour :

* `SystemFileAssociations\.<ext>` pour chaque extension d'image connue du
  moteur (`extensions.rs`) — l'entrée apparaît alors quel que soit le
  programme auquel le type de fichier est associé, sans toucher à cette
  association ;
* `Directory` — clic droit sur un dossier ;
* `Directory\Background` — clic droit sur le fond d'un dossier ouvert.

À quoi sert chaque valeur :

* **`DelegateExecute`** fait de l'entrée un verbe *COM* : l'Explorateur crée
  l'objet de cette classe et lui remet la sélection, au lieu de lancer un
  programme par fichier sélectionné comme le ferait une simple ligne de
  commande.
* **`LocalServer32`** fait vivre cet objet dans notre propre programme,
  lancé à la demande, plutôt que dans une DLL chargée par l'Explorateur.
* **`MultiSelectModel = Player`** garde l'entrée disponible quel que soit le
  nombre d'éléments sélectionnés. Une simple entrée en ligne de commande
  disparaît au-delà de 15 éléments sélectionnés (100 au mieux) ; un verbe
  COM dans ce mode n'a pas de limite.
* **`NeverDefault`** garantit que l'entrée ne devient jamais l'action du
  double-clic, même pour un type de fichier auquel aucun programme n'est
  associé.

La désinstallation supprime exactement les clés `ExplorerConvertImages` et
la clé de la classe, puis retire les clés parentes qu'elle a pu créer
**uniquement si elles sont restées complètement vides** : les entrées des
autres programmes ne sont jamais touchées.

## L'objet COM et son cycle de vie

`com.rs` suit le schéma de l'exemple « ExecuteCommandVerb » de Microsoft :

1. L'Explorateur demande la classe à COM ; COM lance
   `explorer-convert-images-verb.exe -Embedding`.
2. `serve()` enregistre la fabrique de classe (`CoRegisterClassObject`,
   serveur local, usage unique) et traite les messages dans un appartement
   à un seul thread.
3. L'Explorateur crée l'objet et appelle
   `IObjectWithSelection::SetSelection` (toute la sélection, sous forme
   d'`IShellItemArray`), puis `IExecuteCommand::Execute`. Pour le fond d'un
   dossier il n'y a pas de sélection : le dossier arrive par
   `IExecuteCommand::SetDirectory`.
4. `Execute` se contente de mettre la commande en attente et **rend la main
   aussitôt** — l'Explorateur n'attend jamais. La boucle de messages lit
   ensuite les chemins de la sélection, écrit le fichier de sélection et
   lance la fenêtre.
5. L'Explorateur libère l'objet ; le programme se termine.

Des délais garantissent que le programme ne traîne jamais : 30 secondes
s'il est lancé mais jamais appelé, 10 secondes après la commande si l'objet
n'est jamais libéré.

Les échecs (la fenêtre ne peut pas être lancée, par exemple) sont ajoutés à
`explorer-convert-images-verb.log` dans le dossier temporaire de
l'utilisateur : le programme n'a ni fenêtre ni console.

## Transmission de la sélection

Une ligne de commande Windows est limitée à environ 32 000 caractères —
quelques centaines de chemins. La sélection voyage donc toujours dans un
**fichier** : un chemin par ligne, en UTF-8, dans le dossier temporaire de
l'utilisateur. Le gestionnaire l'écrit et passe `--input-list FICHIER` à la
fenêtre ; la fenêtre écrit le sien (complet, dans un format connu) pour le
moteur, et supprime les deux à sa fermeture.

## Ce que change Windows 11

L'entrée est un verbe de menu contextuel classique : sous Windows 11 elle
apparaît dans **Afficher plus d'options**. Apparaître dans le menu de
premier niveau de Windows 11 demande un autre mécanisme (un gestionnaire
`IExplorerCommand` déclaré par un paquet doté d'une identité, et signé) ; la
conception actuelle laisse la porte ouverte — le gestionnaire resterait le
même programme — mais ne le fait pas.

---

# La fenêtre

`ExplorerConvertImages` est une application WinUI 3 (Windows App SDK),
**non empaquetée** et **autonome** (*self-contained*) : sa construction
produit un simple dossier contenant le programme, l'environnement
d'exécution .NET et celui du Windows App SDK. Rien n'a besoin d'être présent
au préalable sur la machine de l'utilisateur.

Elle est répartie en deux projets :

* **`ConvertImages.Core`** — du .NET ordinaire, sans dépendance propre à
  Windows : lecture des arguments et de la sélection, choix du dossier de
  destination par défaut, lancement du moteur et lecture de ses événements,
  traduction. Il se construit et se teste sous Linux comme sous Windows.
* **`ExplorerConvertImages`** — la fenêtre proprement dite (`MainWindow.cs`,
  construite en code) et la classe de l'application. `App.xaml` ne fait
  qu'apporter l'apparence standard des contrôles. Ce projet ne se construit
  que sous Windows.

Règle : **tout ce qui peut se décider sans fenêtre va dans `Core`**, pour
être testé partout ; `MainWindow.cs` se contente d'afficher et de demander.

Comportements à connaître :

* Lancée sans rien à convertir (le raccourci du menu Démarrer), la fenêtre
  explique comment utiliser l'entrée de menu et propose de choisir un
  dossier.
* Le dossier de destination par défaut est le dossier le plus profond qui
  contient toute la sélection (`Selection.CommonParentDirectory`).
* Le moteur est cherché à côté de l'application
  (`convert-images-engine.exe`) ; la variable d'environnement
  `CONVERT_IMAGES_ENGINE` donne un autre chemin (développement et tests).
* Fermer la fenêtre pendant une conversion demande confirmation ; confirmer
  arrête le moteur. Les fichiers déjà convertis restent sur le disque.
* Une erreur que personne n'a traitée est ajoutée à
  `explorer-convert-images-app.log` dans le dossier temporaire de
  l'utilisateur avant l'arrêt de l'application.

---

# Le moteur (`convert-images-engine`)

Le moteur est une copie de celui de nautilus-convert-images, adaptée pour
se construire et fonctionner sous Windows (`engine/UPSTREAM.fr.md`). La
détection des formats, le décodage RAW et la conversion sont ceux décrits
dans l'`ARCHITECTURE.fr.md` de ce projet ; ce qui suit est la partie dont
dépend la fenêtre.

## Interface en ligne de commande (`cli.rs` / `main.rs`)

```
convert-images-engine formats
convert-images-engine detect   [--recursive] [--input-list <fichier>] [<chemins...>]
convert-images-engine convert  [--recursive] [--input-list <fichier>]
                                 --output-dir <dossier> --format <jpeg|png|tiff|bmp|webp>
                                 [--quality <1-100>] [--on-collision <skip|rename|overwrite>]
                                 [--threads <n>] [<chemins...>]
```

`--input-list <fichier>` ajoute les chemins listés dans un fichier (un par
ligne, en UTF-8, avec ou sans marque d'ordre d'octets, fins de ligne Unix ou
Windows) à ceux donnés en arguments.

Chaque ligne écrite sur la sortie standard est un objet JSON complet (une
ligne = un événement), en UTF-8, pour que la fenêtre puisse lire au fur et à
mesure sans attendre la fin du processus.

### Événements de `detect`

```json
{"event": "file", "path": "...", "format": "RAW Canon (CR2)", "is_raw": true}
{"event": "summary", "total": 42, "counts": {"RAW Canon (CR2)": 30, "JPEG": 12}}
```

### Événements de `convert`

```json
{"event": "start", "total": 42}
{"event": "file_done", "index": 1, "total": 42, "input": "...", "output": "...", "status": "ok"}
{"event": "file_done", "index": 2, "total": 42, "input": "...", "status": "error", "message": "..."}
{"event": "summary", "total": 42, "ok": 40, "errors": 2, "output_dir": "..."}
```

### Erreurs fatales (les deux commandes)

```json
{"event": "fatal_error", "message": "..."}
```

suivie du code de sortie 1 : le dossier de destination n'a pas pu être créé,
ou le fichier `--input-list` n'a pas pu être lu.

Les champs `message` sont des **messages techniques en anglais**, non
localisés : la fenêtre les traduit (voir « Internationalisation »). Les
libellés de format de l'événement `file` (`"Unknown"`, `"Generic RAW"`…)
sont eux aussi traduits côté fenêtre.

Ce schéma est le contrat entre le moteur et la fenêtre (`EngineEvent.cs` le
lit) : sa forme ne doit pas changer sans mettre à jour ce document **et** le
code qui le consomme. Un événement ou un champ que la fenêtre ne connaît pas
est ignoré.

### Politique de collision (`--on-collision`)

* `skip` — ignorer le fichier si la destination existe déjà.
* `rename` (par défaut) — ajouter ` (1)`, ` (2)`, ... jusqu'à trouver un nom
  libre.
* `overwrite` — remplacer le fichier existant.

---

# Internationalisation

Les textes source sont en anglais ; les traductions sont des catalogues
gettext, `po/fr.po`, `po/de.po`, `po/es.po`.

* **`Msg.cs`** (`ConvertImages.Core/Localization`) déclare tous les textes
  de la fenêtre, sous forme de constantes. La plupart sont, mot pour mot,
  ceux de la version Linux, pour que ses traductions soient réutilisées.
* Les catalogues sont **embarqués sous forme de texte** dans
  `ConvertImages.Core` et lus au démarrage (`PoCatalog.cs`) : pas d'étape de
  compilation, rien à installer. La langue est la langue d'affichage de
  Windows (`CultureInfo.CurrentUICulture`) ; une variante régionale utilise
  sa langue de base ; tout le reste donne l'anglais.
* **`po/explorer-convert-images.pot`** est généré à partir de `Msg.cs` par
  le programme de tests (`--write-pot`). Les tests échouent s'il n'est pas à
  jour, si un texte de `Msg.cs` manque ou n'est pas traduit dans un
  catalogue, si un catalogue contient un texte que `Msg.cs` n'a plus, ou si
  une traduction ne conserve pas les `{paramètres}` de sa source.

## Messages du moteur

Le moteur émet des messages d'erreur techniques **en anglais**. La fenêtre
les rend dans la langue de l'utilisateur grâce à
`Msg.EngineMessageTemplates` : chaque modèle est un message anglais avec des
champs `{nom}` (par exemple `"cannot open RAW file: {detail}"`), transformé
en expression régulière et traduit comme n'importe quel autre texte
(`EngineMessageTranslator.cs`).

* Les parties variables sont conservées telles quelles et traduites
  récursivement si elles correspondent à leur tour à un modèle — c'est ainsi
  que l'erreur combinée « `cannot open RAW file: …` ; `LibRaw fallback: …` »
  est rendue en entier.
* Un message **inconnu** est affiché tel quel, en anglais : changer un
  message du moteur ne casse jamais la fenêtre, il cesse simplement d'être
  traduit.
* **Quand vous changez un message dans `engine/src/*.rs`**, mettez à jour le
  modèle correspondant dans `Msg.cs`, régénérez le `.pot` et traduisez la
  nouvelle entrée. Les tests vérifient que chaque modèle existe encore dans
  le code Rust.

## Textes hors de la fenêtre

* **L'entrée de menu** est une valeur de registre lue par l'Explorateur
  lui-même. Elle est écrite par `--register` dans la langue qu'a Windows à
  ce moment-là (`labels.rs`), d'où le conseil de relancer l'installeur après
  un changement de langue de Windows.
* **L'installeur** a ses propres textes (`LangString` dans
  `installer.nsi`), dans les quatre mêmes langues.

---

# L'installeur

`installer/installer.nsi` (NSIS), construit sous Linux par
`build-installer.sh`.

* Par utilisateur : `RequestExecutionLevel user`, fichiers dans
  `%LOCALAPPDATA%\Programs\explorer-convert-images`, informations de
  désinstallation sous `HKEY_CURRENT_USER`.
* Le dossier d'installation n'est pas demandé. Comme ce dossier est supprimé
  en entier par une mise à jour et par le désinstalleur, l'installeur refuse
  un dossier (donné avec `/D=`) qui contient autre chose qu'une précédente
  installation à nous.
* Une mise à jour retire d'abord les entrées de menu avec le gestionnaire de
  la version *précédente* (la liste des types de fichiers peut différer),
  puis tous ses fichiers.
* L'enregistrement des entrées de menu est fait par le gestionnaire
  lui-même (`--register`), pas par le script de l'installeur : les entrées
  de registre n'ont qu'une seule description, `plan.rs`.

---

# Ce qui doit concorder

Plusieurs choses sont écrites à plus d'un endroit parce qu'elles
appartiennent à des outils différents. Chaque paire est gardée par un test,
de sorte qu'un changement à un endroit échoue bruyamment tant que l'autre
n'a pas suivi.

| Quoi | Où | Gardé par |
|---|---|---|
| Version du projet | `VERSION`, `Cargo.toml`, `Cargo.lock` (les trois se fixent avec `tools/set-version.sh`) | tests de `plan.rs` |
| Noms de fichier des trois programmes | `plan.rs`, `installer.nsi`, le `.csproj` de l'application, `EngineRunner.cs`, `engine/Cargo.toml` | tests de `plan.rs` et d'`EngineTests.cs` |
| Extensions d'image | `engine/src/formats.rs`, `shell-verb/src/extensions.rs` | tests d'`extensions.rs` |
| Texte de l'entrée de menu | `po/*.po`, `labels.rs`, `installer.nsi`, `Msg.MenuLabel` | tests de `labels.rs` et de `LocalizationTests.cs` |
| Identifiants de format et de collision | `engine/src/convert.rs`, `cli.rs`, `ConversionOptions.cs` | tests d'`EngineTests.cs` |
| Messages du moteur | `engine/src/*.rs`, `Msg.cs` | tests d'`EngineTests.cs` |
| Textes et traductions | `Msg.cs`, `po/*.po`, le `.pot` | tests de `LocalizationTests.cs` |

---

# Construction et intégration continue

`.github/workflows/build.yml` enchaîne trois tâches :

1. **`rust`** (Linux) — `cargo test`, puis compilation croisée du moteur et
   du gestionnaire pour `x86_64-pc-windows-gnu`, et vérification qu'aucun
   des deux n'a besoin d'une DLL de MinGW.
2. **`app`** (Windows) — les tests C# avec le moteur Windows ; les tests du
   gestionnaire propres à Windows (`cargo test`, avec la chaîne de
   compilation de Microsoft) ; `dotnet publish` de la fenêtre ; puis
   `selftest --dist`, qui vérifie le contenu du dossier publié, lance la
   fenêtre construite et passe par le gestionnaire comme l'Explorateur. À
   chaque fois, une fenêtre de l'application doit être à l'écran et y être
   encore vingt secondes plus tard. Si une étape échoue, les erreurs
   enregistrées par Windows sont affichées.
3. **`installer`** (Linux) — NSIS, le fichier `.sha256` et, sur une
   étiquette `vX.Y.Z`, une version en brouillon.

Le dossier publié a besoin d'un fichier que `dotnet publish` n'y met pas de
lui-même : `ExplorerConvertImages.pri`, l'index de ressources par lequel la
fenêtre trouve l'apparence déclarée dans `App.xaml`. Le Windows App SDK ne
le publie que pour les paquets MSIX
([ticket 6720](https://github.com/microsoft/WindowsAppSDK/issues/6720)) ;
sans lui, rien n'est signalé à la construction, et la fenêtre s'arrête
toute seule une dizaine de secondes après son lancement.
`PublishWinUIResources.targets`, importé par le projet de la fenêtre, ajoute
le fichier au dossier publié et fait échouer la publication s'il manque ;
`selftest` et `build-installer.sh` le vérifient à nouveau.

Pourquoi les programmes Rust sont construits sous Linux pour Windows : le
script de construction du LibRaw embarqué ne prend pas en charge le
compilateur de Microsoft, et la chaîne GNU produit les mêmes programmes sur
la machine Linux d'un développeur et dans l'intégration continue. Ils sont
liés statiquement (`.cargo/config.toml`, `vendor/libraw-sys/build.rs`) et
n'ont besoin d'aucune DLL en dehors de celles de Windows.

---

# Philosophie des dépendances

* **Les dépendances du moteur sont celles de nautilus-convert-images**, aux
  mêmes versions fixées : les deux copies doivent rester faciles à comparer.
* **Le gestionnaire** ne dépend que du crate `windows` (les liaisons Rust de
  Microsoft), et seulement dans une construction pour Windows.
* **`ConvertImages.Core`** n'a aucune dépendance en dehors de .NET lui-même ;
  ses tests n'utilisent aucun cadre de test. Rien n'est téléchargé pour le
  construire ou le tester.
* **La fenêtre** ne dépend que du Windows App SDK, à une version fixée.
* Toute nouvelle dépendance doit se justifier par un bénéfice net évident.

# Note sur la chaîne de compilation

Voir la section « Note sur la chaîne de compilation » de `README.fr.md`.
