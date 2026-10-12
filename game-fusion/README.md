# Chog Arena — projet Unity Fusion

**Prototype technique conservé.** La [décision de développement du 8 octobre](../docs/DECISION-RESEAU-REVUE.md)
retient Quantum 3 dans `game/` pour le MOBA Genesis. Cette arène Fusion permet de revoir
la tranche Archer et les essais réseau ; elle n'est plus le projet de contenu actif.

Ouvrir **ce dossier `game-fusion/`** dans Unity Hub, avec l'éditeur installé
**6000.6.4f1 Apple Silicon**. L'AppID Fusion est déjà configuré.

## Premier essai

Dans la fenêtre **Project** de Unity, ouvrir `Assets/Chog/Scenes/Boot.unity`, puis **Play**.
Choisir **Entraînement hors ligne**. Déplacement : WASD/flèches ou joystick gauche ;
viser puis **relâcher** le clic gauche ou le joystick droit pour une volée de sept tirs.
Le clic droit ou le bouton tactile **ZONE** lance l'attaque spéciale après la seconde de
verrouillage de la volée. Le bot attend d'être touché puis laisse cinq secondes avant de
riposter. La caméra, le modèle/les animations KayKit, les sons, les jauges et l'aperçu de
visée constituent la première reprise du sample Quantum.

Il s'agit encore d'une arène d'entraînement, pas d'un match MOBA complet. Le mode en ligne
utilise **Server/Client** ; Genesis, classes, minions, victoire, carrière et $CHOG restent
à intégrer. Les héros actuels sont des démonstrations. [Portage vérifié et limites](../research/evidence/fusion-archer-slice.json).

## Test réseau reproductible

Le menu **Chog → Qualification → Tester serveur et deux clients Photon** lance un scénario
automatique puis quitte le mode Play. Il crée une vraie session Photon privée, un serveur
et deux clients indépendants, dans le même processus Unity avec physiques séparées.

Le scénario vérifie les héros répliqués, l'autorité du serveur et des inputs, le
déplacement, les dégâts vus des deux côtés, la volée exacte de sept tirs, la zone spéciale,
le départ d'un client et l'arrivée d'un autre. **Les huit contrôles ont passé.** La nouvelle
connexion ne constitue pas encore une reprise authentifiée du héros/NFT initial.
[Résultat, limites et empreintes des sources](../research/evidence/fusion-archer-slice.json).

En ligne de commande, sans `-quit` car le scénario quitte Unity à sa fin :

```sh
/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -projectPath /Users/red.g/CascadeProjects/chogathon/game-fusion \
  -executeMethod Chog.Editor.FusionNetworkQualification.Run \
  -logFile /Users/red.g/CascadeProjects/chogathon/.artifacts/fusion/network-qualification.log
```

## Préparer l'essai navigateur et téléphone

Le menu **Chog → Qualification → Build WebGL** construit `Builds/WebGL/`.
**Build Archer réussi sous Unity 6000.6.4f1 : environ 12,9 Mo.**
Le build utilise gzip sans fallback de décompression : le serveur HTTP doit fournir
les bons types et `Content-Encoding`. Les quatre fichiers loader/data/framework/wasm
ont répondu correctement sur le serveur local, dont `application/wasm` et `gzip`.
Gzip permet l'essai HTTP sur le Wi-Fi ; Brotli exigerait HTTPS sur le téléphone.
[Documentation Unity](https://docs.unity.com/en-us/engine/6000.5/manual/platform-specific/webgl/building-distribution/deploying).
Depuis la racine du dépôt :

```sh
python3 tools/unity/serve_webgl.py
```

Ouvrir `http://127.0.0.1:8080` dans le navigateur. Pour un téléphone sur le même Wi-Fi :
relancer le script avec `--host 0.0.0.0`, puis ouvrir l'adresse IP locale du Mac, port 8080.
Pour cette arène, tenir le téléphone en paysage. L'entraînement hors ligne affiche aussi
les deux joysticks ; il permet de vérifier les gestes avant le premier essai réseau.
Le serveur de test local n'est pas une livraison HTTPS/PWA installable.

Pour tester le multijoueur manuellement, lancer **Démarrer serveur (développement)** dans
le mode Play de Unity ; ouvrir le build navigateur et choisir **Rejoindre la partie** avec
le même code de salon. Région de qualification : `eu`, version `chog-p0-fusion-1`.
Les deux joysticks permettent le déplacement à gauche, la visée et la volée au relâchement
à droite ; le bouton ZONE lance le spécial. L'entraînement Archer a été essayé dans
**Chrome 155 headless avec SwiftShader** : clavier/souris, puis deux contacts tactiles
émulés simultanés. Les captures montrent les compteurs de volée et de zone et la zone dans
la direction visée. [Résultat et captures](../research/evidence/fusion-archer-slice.json).
Le multijoueur navigateur, le téléphone réel et son ergonomie restent à qualifier ; ce test
ne mesure pas les FPS du Redmi. Le tactile apparaît aussi sur notre Chrome de bureau,
où Input System expose un Touchscreen ; le choix automatique de présentation sera affiné.

Pour reproduire le test de chargement et enregistrer les captures, avec Node 22+ et le
serveur HTTP lancé, depuis la racine du dépôt :

```sh
CHOG_BROWSER_TRAINING=1 node tools/unity/qualify_webgl.mjs
CHOG_BROWSER_TRAINING=1 CHOG_BROWSER_TOUCH=1 node tools/unity/qualify_webgl.mjs
```

Le script utilise un profil Chrome temporaire indépendant puis le supprime. Il contrôle
le chargement et enregistre les commandes/captures ; à chaque nouvelle version, inspecter
les images et les journaux runtime avant de conclure sur le gameplay.

## Origine et correctif

Le portage reprend uniquement des meshes et animations KayKit et trois sons du paquet
`100-CC0-SFX`, avec les métadonnées Unity d'origine ; il n'importe pas les composants
Quantum. La licence du sample Quantum réserve ses assets à l'usage interne sans licence
individuelle. Les pages du créateur indiquent **CC0** pour
[KayKit Dungeon](https://kaylousberg.itch.io/kaykit-dungeon) et
[KayKit Character Animations](https://kaylousberg.itch.io/kaykit-animations) ; le paquet
audio inclut sa [licence locale](../game/Assets/TwinStickShooter/Third%20Party%20Assets/Audio/100-CC0-SFX/Lisence.txt).
Les cinq fichiers `.anim` et le contrôleur Animator copiés du sample doivent encore être
remplacés par des exports KayKit directs ou obtenir une licence explicite avant distribution.

SDK fourni : `Photon-Fusion-2.1.3-Stable-2390.unitypackage`, **2.1.3 build 2390** d'après
`Assets/Photon/Fusion/build_info.txt`. Le `package.json` du fournisseur indique encore
1.1.0 ; il ne sert pas à identifier la version effective du SDK. Les GUID et métadonnées
ont été extraits avec les assets ; [empreintes d'origine](../research/evidence/fusion-sdk-baseline-sha256.json).
Mono.Cecil **1.10.2**, demandé par le helper d'installation Photon, est déclaré explicitement.
Le catalogue C# est référencé en package local ; `PointerCapture` est repris des contrôles
précédents. Les contrôles de cette arène utilisent Input System ; aucun système Quantum.

Un garde-fou initialise le registre du gestionnaire de scènes si le callback Unity
`SubsystemRegistration` ne l'a pas fait. Ce cas a été observé dans l'éditeur de qualification,
après le démarrage réel du mode Play. Le correctif réutilise `ClearStatics()` du SDK et
conserve les fins de ligne d'origine. [Patch à revoir lors d'une mise à jour Photon](../tools/unity/patches/fusion-2.1.3-scene-initialization.patch).
Les autres changements de fichiers fournisseur sont des métadonnées d'import d'images.

Le module Linux Dedicated Server n'est pas installé : le test utilise le mode serveur
dans Unity sans rendu. Il ne démontre pas encore un build Linux déployé, une capacité de
100 CCU, une performance sur téléphone ou un achat. Le projet Quantum reste dans `game/`.
