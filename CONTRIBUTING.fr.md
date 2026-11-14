<!-- SPDX-License-Identifier: GPL-3.0-only -->

# Contribuer à explorer-convert-images

🇬🇧 [English version](CONTRIBUTING.md)

Merci de l'intérêt que vous portez à ce projet ! Ce document explique
comment signaler un problème, proposer une amélioration ou contribuer du
code.

En participant à ce projet, vous acceptez d'en respecter le
[code de conduite](CODE_OF_CONDUCT.fr.md).

Pour signaler un **problème de sécurité**, merci de ne pas ouvrir de ticket
public : suivez la [politique de sécurité](SECURITY.fr.md).

## Signaler un bug

Ouvrez un ticket avec, autant que possible :

- la version du programme (**Paramètres > Applications > Applications
  installées**) et votre version de Windows (`winver`) ;
- ce que vous avez fait, ce que vous attendiez, ce qui s'est passé ;
- le texte du journal affiché dans la fenêtre, si le problème est une
  conversion ;
- le contenu de ces deux fichiers de votre dossier temporaire (tapez
  `%TEMP%` dans la barre d'adresse de l'Explorateur de fichiers), s'ils
  existent : `explorer-convert-images-verb.log` (l'entrée de menu) et
  `explorer-convert-images-app.log` (la fenêtre) ;
- si le problème vient d'un fichier particulier, ce fichier — seulement si
  vous avez le droit de le partager.

Un fichier RAW que le moteur ne sait pas lire se signale plutôt à
[nautilus-convert-images](https://github.com/gillesmagneville/nautilus-convert-images/issues),
où le moteur est développé.

## Proposer une amélioration

Ouvrez un ticket qui décrit le besoin plutôt que la solution. Lisez d'abord
`PROJECT_CONTEXT.fr.md` : le projet reste volontairement un convertisseur de
format ciblé.

## Contribuer du code

Le projet comporte trois programmes dans deux langages (voir
`ARCHITECTURE.fr.md`) :

| Partie | Où | Se construit sous |
|---|---|---|
| Moteur et gestionnaire de menu (Rust) | `engine/`, `shell-verb/` | Linux, pour Linux (tests) et pour Windows |
| Logique de la fenêtre (C#) | `app/ConvertImages.Core/` | Linux et Windows |
| La fenêtre elle-même (C#, WinUI 3) | `app/ExplorerConvertImages/` | Windows seulement |

Vous n'avez pas besoin d'une machine Windows pour travailler sur
l'essentiel : le workflow GitHub Actions construit et teste tout, fenêtre
comprise, à chaque push et à chaque demande de fusion.

### Mise en place sous Linux (Ubuntu/Debian)

```bash
git clone git@github.com:gillesmagneville/explorer-convert-images.git ~/Projects/explorer-convert-images
cd ~/Projects/explorer-convert-images

# Tests Rust, et tests C# avec le vrai moteur
sudo apt-get install build-essential dotnet-sdk-10.0
cargo test
cargo build --release -p convert-images-engine
dotnet run -c Release --project app/ConvertImages.Core.Tests
```

Pour construire aussi les programmes Windows et l'installeur :

```bash
sudo apt-get install nsis gcc-mingw-w64-x86-64-posix g++-mingw-w64-x86-64-posix
rustup target add x86_64-pc-windows-gnu
cargo build --release --target x86_64-pc-windows-gnu
```

La cible Windows demande un Rust installé avec `rustup` : celui empaqueté
par les distributions ne la fournit pas. `rust-toolchain.toml` choisit alors
tout seul la version de Rust.

### Mise en place sous Windows

Installez le [SDK .NET 10](https://dotnet.microsoft.com/download). Puis,
dans PowerShell, à la racine du dépôt :

```powershell
dotnet run -c Release --project app/ConvertImages.Core.Tests
dotnet publish app/ExplorerConvertImages -c Release -r win-x64 -p:Platform=x64 -o dist\explorer-convert-images
```

Il manque au dossier publié les deux programmes Rust. Prenez-les dans
l'artefact `rust-windows` d'une exécution du workflow (ou construisez-les
sous Linux) et copiez-les dedans ;
`dist\explorer-convert-images\ExplorerConvertImages.exe` peut alors être
lancé, avec des fichiers ou des dossiers en arguments. Pour avoir l'entrée
de menu sans installer, lancez `explorer-convert-images-verb.exe --register`
depuis ce dossier (`--unregister` la retire).

### Construire l'installeur

Sous Linux, à partir d'un dossier contenant la fenêtre publiée :

```bash
./build-installer.sh --app DOSSIER
```

ou à partir d'un dossier complet (l'artefact
`explorer-convert-images-folder` d'une exécution du workflow) :

```bash
./build-installer.sh --dist DOSSIER
```

L'installeur et son fichier `.sha256` sont écrits dans `dist/`.

### Vérifier toute la chaîne sous Windows

`selftest` fait ce que fait l'Explorateur de fichiers sur **Convertir les
images…** et vérifie qu'une fenêtre de l'application arrive à l'écran,
qu'elle y est encore vingt secondes plus tard et que l'application n'a
journalisé aucune erreur. Construisez-le avec Rust sous Windows :

```powershell
cargo build -p explorer-convert-images-verb --example selftest
target\debug\examples\selftest.exe
```

Sans argument, il utilise le programme installé. `simulate_explorer.exe
CHEMIN...` (construit de la même façon) invoque le gestionnaire de menu
installé sur les chemins que vous donnez, ce qui est pratique pour essayer
une grande sélection.

### Essayer les programmes Windows sous Linux, avec Wine

Les tests Windows des deux programmes Rust s'exécutent sous Wine :

```bash
sudo apt-get install wine
CARGO_TARGET_X86_64_PC_WINDOWS_GNU_RUNNER=wine cargo test --target x86_64-pc-windows-gnu
```

L'installeur fonctionne lui aussi sous Wine (c'est un programme 32 bits :
il lui faut le paquet `wine32`). Wine a deux limites à connaître : il ne
laisse pas passer d'un programme à l'autre les appels COM de l'Explorateur
pour les deux interfaces qu'utilise le gestionnaire, et il ne voit pas les
classes enregistrées pour le seul utilisateur courant. La chaîne complète —
menu, gestionnaire, fenêtre — ne peut donc être exercée que sur un vrai
Windows ; la fenêtre elle-même ne fonctionne pas sous Wine.

## Ajouter un nouveau format de destination

C'est le type de contribution le plus courant. Le contrat exact (le tableau
sans perte/profondeur/alpha, les points du code à modifier) est défini dans
`ARCHITECTURE.fr.md`, section « Conversion » (« Point d'extension — ajouter
un nouveau format de destination »). Le moteur est développé dans
nautilus-convert-images (voir « Modifier le moteur » plus bas) : le format y
est ajouté d'abord, puis reporté dans `engine/`. En bref :

1. `engine/src/convert.rs` — nouvelle variante de `TargetFormat` + cas
   d'encodage.
2. `engine/src/main.rs` — entrée dans la sortie de la sous-commande
   `formats`.
3. `app/ConvertImages.Core/Engine/ConversionOptions.cs` — entrée dans
   `Choices.Formats`, avec son texte dans `Localization/Msg.cs` et ses
   traductions (voir « Traduire l'interface ») ; le test qui liste les
   formats, dans `app/ConvertImages.Core.Tests/EngineTests.cs`, suit.
4. `ARCHITECTURE.fr.md` — mettre à jour le tableau des formats.

### Règles pour tout nouveau format

- Ne jamais annoncer un format « sans perte » s'il ne l'est pas vraiment —
  voir « Fiabilité et transparence » dans `PROJECT_CONTEXT.fr.md`. Dans le
  doute, vérifier par une comparaison pixel à pixel (voir la section
  « Tests » de `README.fr.md` pour la méthode employée jusqu'ici).
- Aucun recadrage ni redimensionnement ajouté, même en option, sans en
  discuter d'abord dans une issue — c'est un principe fondateur du projet
  (`PROJECT_CONTEXT.fr.md`, principe n°2).

## Traduire l'interface

Les textes sont dans `app/ConvertImages.Core/Localization/Msg.cs` (anglais)
et leurs traductions dans `po/*.po` (catalogues gettext, modifiables avec un
éditeur de texte ou Poedit).

**Pour corriger une traduction**, modifiez le fichier `.po`, puis lancez les
tests C# : ils vérifient que chaque texte est traduit et conserve ses
`{paramètres}`.

**Pour ajouter ou modifier un texte**, modifiez `Msg.cs`, puis régénérez le
modèle et mettez à jour chaque catalogue :

```bash
dotnet run -c Release --project app/ConvertImages.Core.Tests -- --write-pot
for po in po/*.po; do msgmerge --update --no-fuzzy-matching "$po" po/explorer-convert-images.pot; done
# traduire les nouvelles entrées dans po/*.po, puis :
dotnet run -c Release --project app/ConvertImages.Core.Tests
```

(`msgmerge` vient avec le paquet `gettext` ; on peut aussi recopier les
entrées à la main depuis le `.pot`.)

**Pour ajouter une langue** (l'italien, par exemple) :

1. Copiez `po/explorer-convert-images.pot` vers `po/it.po` ; complétez son
   en-tête (`Language: it`, `Plural-Forms`) et traduisez.
2. Ajoutez `"it"` à `Translator.TranslatedLanguages` (`Translator.cs`).
3. Ajoutez le texte de l'entrée de menu à `LABELS` dans
   `shell-verb/src/labels.rs`, avec l'identifiant Windows de la langue.
4. Dans `installer/installer.nsi` : ajoutez la ligne `MUI_LANGUAGE`, une
   ligne à chaque `LangString` et à `VersionKeys`, et le nom du raccourci du
   menu Démarrer à `DeleteShortcuts`.
5. Lancez `cargo test` et les tests C# : ils vous disent ce qui manque.

Les catalogues connaissent deux règles de pluriel : pluriel à partir de 2
(français) ou pluriel pour tout sauf 1 (anglais, allemand, espagnol,
italien…). Une langue qui suit une autre règle demande d'étendre
`PoCatalog.PluralIndex`.

## Modifier le moteur

Le moteur est développé dans nautilus-convert-images ; `engine/` en est une
copie (`engine/UPSTREAM.fr.md`). Un correctif ou une nouvelle fonctionnalité
du moteur a d'abord sa place là-bas, puis est reporté ici. Ici, ne modifiez
que ce que Windows impose, et notez-le dans `engine/UPSTREAM.md` et
`engine/UPSTREAM.fr.md`.

Si un message du moteur change, mettez à jour son modèle dans `Msg.cs`
(voir « Messages du moteur » dans `ARCHITECTURE.fr.md`) ; si une extension
d'image est ajoutée, ajoutez-la à `shell-verb/src/extensions.rs`. Les tests
échouent tant que les deux côtés ne concordent pas.

## Mettre à jour le Windows App SDK ou .NET

La version du Windows App SDK est la `PackageReference` de
`app/ExplorerConvertImages/ExplorerConvertImages.csproj` ; celle du SDK .NET
est dans `global.json` et dans le workflow. Changez une seule chose à la
fois, dans une demande de fusion à part : le workflow construit la fenêtre
et la lance (`selftest`), et c'est ce qui dit si la nouvelle version
fonctionne.

Après une mise à jour du Windows App SDK, regardez si
`app/ExplorerConvertImages/PublishWinUIResources.targets` est encore
nécessaire : il compense un fichier que `dotnet publish` oublie, et son
en-tête dit comment le savoir. Une construction qui n'affiche plus de ligne
« added to the published files » n'en a plus besoin.

## Publier une version

1. Fixez la version partout où elle est écrite :
   `tools/set-version.sh X.Y.Z`
2. Dans `CHANGELOG.md` et `CHANGELOG.fr.md`, transformez la section
   `[Unreleased]` (`[Non publié]`) en section de la version, avec un titre
   de cette forme exacte — `## 2026-11-20 — vX.Y.Z` — et ouvrez au-dessus
   une nouvelle section `[Unreleased]` vide.
3. Vérifiez les notes qui seront publiées :
   `tools/release-notes.sh X.Y.Z`
4. Validez, puis étiquetez et poussez :
   `git tag vX.Y.Z && git push origin main vX.Y.Z`
5. Attendez le workflow. Sur une étiquette, il crée une version en
   **brouillon** avec l'installeur, son fichier `.sha256` et les notes
   (anglais d'abord, français ensuite). Relisez le brouillon sur la page des
   versions, essayez l'installeur sur une machine Windows, puis publiez.

Le workflow refuse de créer la version si l'étiquette et le fichier
`VERSION` ne concordent pas, ou si la section manque dans l'un des deux
journaux.

## Style de code

- `cargo fmt` avant d'ouvrir une demande de fusion.
- Rust stable uniquement, pas de fonctionnalités nightly.
- La fenêtre contient aussi peu de logique que possible : ce qui peut se
  décider sans fenêtre va dans `ConvertImages.Core`, avec un test.
- Rien n'est jamais chargé dans l'Explorateur de fichiers : le gestionnaire
  de menu reste un serveur COM hors processus.
- Ce qui doit être écrit à deux endroits reçoit un test qui les compare
  (voir « Ce qui doit concorder » dans `ARCHITECTURE.fr.md`).
- Toute nouvelle dépendance doit être justifiée (voir « Philosophie des
  dépendances » dans `ARCHITECTURE.fr.md`).

## Documentation

Si votre modification change un comportement observable, mettez à jour
`CHANGELOG.md` et `CHANGELOG.fr.md` dans la même demande de fusion, sous
`[Unreleased]` (`[Non publié]` dans le fichier français), ainsi que
`ARCHITECTURE.md` / `ARCHITECTURE.fr.md` si le contrat technique change.

Chaque document existe en deux langues : l'anglais (`README.md`,
`PROJECT_CONTEXT.md`, `ARCHITECTURE.md`, `CONTRIBUTING.md`,
`CODE_OF_CONDUCT.md`, `SECURITY.md`, `CHANGELOG.md`, affiché par défaut sur
GitHub) et le français (les mêmes noms avec `.fr.md`). Si vous en modifiez
un, modifiez l'autre. Si vous n'écrivez que dans une langue, dites-le dans
la demande de fusion et le mainteneur traduira.

## Questions

Pour toute question qui n'entre pas dans les cas ci-dessus, ouvrez
simplement un ticket.

## Licence

En contribuant, vous acceptez que vos contributions soient publiées sous la
même licence que le projet (GPL-3.0-only). Le code de conduite est la seule
exception : c'est le Contributor Covenant 3.0, distribué sous CC BY-SA 4.0.
