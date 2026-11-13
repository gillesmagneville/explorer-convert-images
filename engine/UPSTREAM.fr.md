<!-- SPDX-License-Identifier: GPL-3.0-only -->

# D'où vient ce moteur

🇬🇧 [English version](UPSTREAM.md)

`engine/` est une copie du moteur de conversion de
[nautilus-convert-images](https://github.com/gillesmagneville/nautilus-convert-images),
où il est développé.

| | |
|---|---|
| Copié depuis | nautilus-convert-images **1.3.5**, commit `3019d43` (2026-11-03) |
| Fichiers | `src/` de l'amont → `engine/src/`, `vendor/` → `engine/vendor/`, `Cargo.toml` → `engine/Cargo.toml` ; `Cargo.lock` est à la racine de ce dépôt |

## Différences avec l'amont

Uniquement ce que Windows impose. Le comportement sous Linux est inchangé :
les tests de l'amont passent toujours, sans modification.

* **`src/libraw.rs`** — sous Unix, LibRaw ouvre lui-même le fichier à partir
  de son chemin, comme en amont. Ailleurs, le fichier est lu par Rust et
  remis à LibRaw sous forme de tampon mémoire (`libraw_open_buffer`) :
  l'interface par chemin de LibRaw ne comprend que la page de codes
  historique de Windows, si bien qu'un chemin contenant d'autres caractères
  (des accents, selon la langue du système) ou dépassant 260 caractères
  échouerait.
* **`src/cli.rs`, `src/main.rs`** — nouvelle option `--input-list <fichier>`
  de `detect` et `convert` : des chemins supplémentaires, un par ligne, lus
  dans un fichier. Une ligne de commande Windows est limitée à environ
  32 000 caractères. Une liste illisible est signalée par un événement
  `fatal_error` (`could not read the input list: …`). La description
  affichée par `--help` nomme ce projet.
* **`Cargo.toml`** — le paquet tire sa version, son édition et sa licence de
  l'espace de travail de ce dépôt. Les dépendances et leurs versions fixées
  sont celles de l'amont.
* **`vendor/libraw-sys`** — trois modifications, listées dans
  `vendor/README.fr.md` (bibliothèque d'exécution C++ statique avec
  MinGW-w64, largeur de `time_t` sous Windows, un déclencheur de
  reconstruction).

## Reporter une modification de l'amont

1. Comparez l'amont avec cette copie, par exemple :
   `diff -ru ~/Projects/nautilus-convert-images/src ~/Projects/explorer-convert-images/engine/src`
   — en dehors des différences ci-dessus, les deux doivent être identiques.
2. Appliquez la modification de l'amont à `engine/`, en conservant les
   différences ci-dessus.
3. Si un message du moteur ou la liste des extensions a changé, suivez les
   tests qui échouent (`cargo test`, puis les tests C#) : ils nomment ce qui
   doit suivre dans `shell-verb/src/extensions.rs` et dans `Msg.cs`.
4. Mettez à jour le tableau en tête de ce fichier (version et commit), dans
   les deux langues.
