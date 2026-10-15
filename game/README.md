# Projet Unity actif : Quantum 3

**Choix réseau réexaminé le 8 octobre : Quantum 3 est la base du MOBA Genesis.**
Le sample reste jouable pendant son adaptation ; Fusion Server est conservé dans
`game-fusion/` comme repli technique. L'AppID Quantum 3 distinct de l'AppID Fusion est
maintenant renseigné dans `Assets/QuantumUser/Resources/PhotonServerSettings.asset` ; sa
connexion reste à vérifier. [Décision et périmètre](../docs/DECISION-RESEAU-REVUE.md).

Le ZIP fourni par l'utilisateur est extrait ici : Twin Stick Shooter **Quantum 3.0.9**,
build SDK 1932. Version d'origine : **2021.3.45f2** ; premier import effectué sous
**6000.6.4f1 arm64**, avec WebGL. Les corrections de migration compilent avec cet éditeur ;
le redémarrage sans erreur est confirmé par l'utilisateur et le journal montre le mode Default.
L'utilisateur a ensuite essayé le sample local et confirme son fonctionnement. Le build
WebGL local avec bots a réussi sous Unity 6000.6.4f1 ; Chrome affiche une partie et
l'utilisateur rapporte qu'elle tourne sur son Redmi 11C, avec quelques saccades.
Aucun match **en ligne**, temps d'image mesuré ni replay complet n'est validé.
[Relevé](../research/evidence/unity-6000.6-qualification.json).

Les 6 656 fichiers ont été vérifiés à l'extraction, `.meta` et licences compris. Depuis,
les modules Chog et les adaptations Unity ont été ajoutés ; Unity a également migré des
packages et réglages. Le ZIP et ses empreintes restent la référence d'origine :
[import](../research/evidence/quantum-sample-import.json).

Les patches de compatibilité portent sur `EventBetter`, les fichiers SDK fusionnés
`QuantumUnityRuntime`/`QuantumUnityEditor`, `AttackColorSetter` et le pool FX legacy.
Les nouvelles API sont conditionnées à la version Unity ; les IDs 64 bits ne sont pas
tronqués pour retrouver les anciens int. Réexaminer ces petits patches lors d'une mise à
jour Photon. Les packages IAP et Advertisement Legacy ont été retirés après vérification
de leur absence d'utilisation. Aucun parcours $CHOG n'est implémenté par cette suppression.
Le script Editor d'Advertisement Legacy déclenchait l'import de MDR ; si sa fenêtre est
encore ouverte, choisir **Ignore - Do not ask again during this session**. Unity a résolu
le lock sans Ads et recompilé les assemblies corrigées avec succès.

```sh
python3 tools/unity/verify_sample.py
```

À lancer depuis la racine `chogathon`. Le script signale les fichiers d'origine modifiés
ou manquants et liste les ajouts. Après migration, ses écarts sont attendus et doivent être
examinés ; il ne restaure ni n'écrase de fichier.

## Premier lancement

L'import est terminé et Unity a redémarré sans erreur. Pour le premier essai local :

1. Ouvrir **seule** `Assets/QuantumUser/Scenes/Gameplay.unity` par un double-clic dans Project.
   Ne pas garder le menu ouvert comme seconde scène : le runner local se désactive si
   plusieurs scènes sont chargées.
2. Faire **Play**, sélectionner un personnage puis **Start** dans l'onglet Game.
3. Déplacement par les positions WASD (ZQSD sur AZERTY), visée souris, attaque au clic gauche,
   compétence spéciale au clic droit. Maintenir pour viser, relâcher pour lancer ; la jauge
   spéciale doit couvrir le coût de la compétence. La scène active `FillWithBots`.

Ce chemin utilise `QuantumRunnerLocalDebug` en mode `DeterministicGameMode.Local` et
ne demande pas d'AppId. Premier essai fonctionnel confirmé par l'utilisateur.

### Contrôles tactiles et essai navigateur

L'adaptation conserve les joysticks et bindings du sample. `MobileButtonsManager` choisit
leur visibilité selon le périphérique ; `ShowOnEditor` reste l'option d'aperçu dans l'éditeur.
`Chog.Controls` lit les actions, conserve la dernière visée pour le cast au relâchement et
garde un doigt propriétaire par contrôle. Les scripts UI remettent à zéro leurs valeurs
en cas de perte de focus, pause ou désactivation. Les règles de combat Quantum sont inchangées.

Les **neuf tests EditMode ont passé dans Unity 6000.6.4f1** le 8 octobre à 01:31:25 UTC.
Ils utilisent le vrai Input System et son fixture officiel, avec les bindings du fichier
`PlayerControls.inputactions`. Résultat et chemin XML dans
`.artifacts/unity/latest-controls-tests.json`. Pour les relancer, hors Play :
**Chog → Qualification → Run Controls Tests**. Ce résultat ne valide pas le multitouch
d'un téléphone réel ; le premier appareil déclaré pour cet essai est un Redmi 11C.
[Qualification](../research/evidence/web-touch-controls-qualification.json).

**Chog → Qualification → Build Local WebGL (Bots)** construit uniquement Gameplay,
sans AppId ni changement de la liste des scènes réseau. C'est le chemin prévu pour le
premier essai de commandes sur téléphone. Le build doit être servi par HTTP(S), avec
les en-têtes de compression appropriés. Le build local a réussi ;
[preuve et limites](../research/evidence/quantum-webgl-local-2026-10-08.json).
Le build conservé est dans
`.artifacts/unity/20261008T211903735Z-493cd8a128e3468b95bbda4ad3c4ff7b/webgl/`
à la racine du dépôt. Pour relancer son serveur de test sur le Wi-Fi :

```sh
python3 tools/unity/serve_webgl.py --host 0.0.0.0 --port 8081 \
  --directory .artifacts/unity/20261008T211903735Z-493cd8a128e3468b95bbda4ad3c4ff7b/webgl
```

Sur le Redmi connecté au même Wi-Fi, ouvrir l'adresse actuelle du Mac avec le port 8081
dans Chrome ; le 8 octobre elle est `http://192.168.1.85:8081/`. C'est un serveur local
de qualification, pas une publication de la PWA.

Le **build réseau plus récent** est servi séparément au port **8082** :
`http://192.168.1.85:8082/` tant que l'IP et le serveur restent actifs. Son écran d'accueil
**Photon Quantum** montre les boutons **QUICK PLAY** et **PARTY MENU** à gauche. Dans
l'éditeur Unity sur le Mac, ouvrir la scène
`Assets/Photon/QuantumMenu/QuantumSampleMenu.unity`, puis appuyer sur **Play** : la scène
`Assets/QuantumUser/Scenes/Gameplay.unity` démarre directement dans le jeu et n'affiche
pas ce menu. Sur l'accueil Quantum, cliquer **PARTY MENU → CREATE**. Sur l'écran
**Team Preparation**, avant **Start**, ouvrir la roue dentée en haut à droite : le panneau
**Session** affiche le code
du salon et le bouton **Copy**. Sur le Redmi connecté au même Wi-Fi, ouvrir l'URL ci-dessus
dans Chrome, puis **PARTY MENU → JOIN** et saisir ce code. Vérifier que **Connected
Players** affiche **2/6** avant de lancer la partie avec **Start** sur le Mac. Tester ensuite
le déplacement et les tirs sur les deux appareils.
Le port 8081 désigne toujours l'ancien build local avec bots et ne teste pas Photon.

Pour l'installation sur un autre poste et les essais réseau/web :

1. Dans Unity Hub, ajouter ce dossier `game`, qui contient `Assets`, `Packages` et
   `ProjectSettings`. Utiliser un éditeur **exécutable** avec son module WebGL.
   Si 2021.3.45f2 est déjà disponible, il permet la comparaison d'origine. La cible de
   migration recommandée est Unity 6 pour le web mobile et Reown ; ne pas installer un
   ancien éditeur uniquement pour ce test. Toute ouverture en version supérieure est une
   migration à relever, pas une compatibilité déjà validée. Le ZIP et les empreintes
   restent la référence récupérable.
2. Attendre la résolution des packages, l'import et la génération Quantum. Si Safe Mode
   affiche les anciennes erreurs après une correction externe, faire **Assets → Refresh**.
   Confirmer une Console sans erreur de compilation et la sortie effective du Safe Mode.
3. `Assets/QuantumUser/Resources/PhotonServerSettings.asset` contient déjà **AppIdQuantum**
   fourni pour ce projet. Le laisser distinct des AppID Fusion et PUN.
4. Ouvrir `Assets/Photon/QuantumMenu/QuantumSampleMenu.unity` puis Play. La seconde scène
   de build est `Assets/QuantumUser/Scenes/Gameplay.unity`.
5. Menu **Chog → Qualification → Build WebGL**. La commande ajoutée utilise les scènes
   activées et les réglages existants. Le dossier `.artifacts/unity/<exécution>/` contiendra
   `webgl/` et `build-report.json`. Le build **local avec bots** a été exécuté ; ce chemin
   réseau avec menu et AppID Quantum a réussi le 9 octobre depuis l'éditeur ouvert ; un
   Chrome isolé a créé un salon et lancé une partie à bots. Deux clients et le téléphone
   restent à tester. [Preuve](../research/evidence/quantum-online-webgl-2026-10-09.json).
   Une tentative antérieure en batch avait été interrompue par le service de licence
   Unity ; son journal est dans `.artifacts/unity/quantum-online-appid-build.log`.

Équivalent batch, depuis `chogathon`, une fois le chemin réel connu :

```sh
mkdir -p .artifacts/unity
"/chemin/reel/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -projectPath "$PWD/game" -buildTarget WebGL \
  -executeMethod Chog.Editor.QualificationBuild.WebGL \
  -logFile "$PWD/.artifacts/unity/editor.log"
```

Un échec avant l'exécution de la méthode (import, compilation, licence) n'aura pas de
`build-report.json` ; consulter le log. Un build réussi ne valide ni le multijoueur, ni
les contrôles mobiles, ni les performances. Servir le build avec les en-têtes correspondant
à sa compression avant les essais navigateur ; ouvrir directement `index.html` en `file://`
n'est pas le test web prévu.

### Performance overlay (9 October)

`Assets/Chog/Diagnostics/` adds an on-device overlay, off by default. In a WebGL build, append
`?perf=1` to the page URL; in Play Mode, tick **Chog > Diagnostics > Performance HUD In Play
Mode**. During a Quantum session it shows frame time p50/p95/p99, frames above 33 ms and 50 ms,
Quantum simulation time, predicted frames, ping, input offset and the GPU string, and writes one
`CHOG_PERF` JSON line per minute to the console (visible through `chrome://inspect` for a phone).
The first three seconds of a session are skipped. It also shows main-thread busy time, the
running session's rate, rollback window and checksum interval, the target frame rate and the
quality level. With `?perf=1`, these URL parameters override test settings without a rebuild:
`hz`, `rollback`, `checksum` (session config; effective only when this client creates the room,
since the server keeps the first config it receives), `fps` (`Application.targetFrameRate`,
`-1` lets the browser pace frames) and `quality` (Unity quality index, 0 is the cheapest).
Example: `http://192.168.1.85:8082/?perf=1&checksum=0&hz=30`. Serve the newest qualification
build with:

```sh
python3 tools/unity/serve_webgl.py --latest --host 0.0.0.0 --port 8082
```

The sampling helpers are covered by EditMode tests in `Assets/Chog/Diagnostics/Tests/`.

With `?perf=1` the game also posts that minute report to `/perf-report` on the server it was
loaded from. `tools/unity/serve_webgl.py` keeps each one, with the client address, as a line of
`.artifacts/perf-reports.jsonl`: a run on a phone can be read from the Mac, no screenshot
needed. The report carries the page URL and the status lines of the overlay (portraits,
effects). Two more page options help to split a cost: `fx=0` turns off everything a Genesis
signature draws, attack and signs (`Assets/Chog/Match/GenesisEffects.cs`; its cap on live
particle effects now only matters for an attack that names an effect prefab, and none does),
and `quality=0` forces the cheapest quality level. When comparing
runs, serve one fixed build (`--directory <build>/webgl`) and not `--latest`, or a new build
replaces the one under test. The automated browser is software-rendered: it shows CPU,
simulation and memory, never what an effect costs a phone's GPU.

### Entering a match as a Genesis (9 October)

A match whose config names a `GenesisRoster` asset casts real Genesis: a player can enter as
one, and every bot is drawn from the roster, never twice in the same match. A Genesis plays the
class its Body trait gives it (brute is the Knight, ranger the Archer, controller the
Spellcaster), shows its own name above its character, and its player keeps only that class on
the selection screen. Each such character carries a `GenesisHero` component (token id, metadata
id, role, shape, effect, expression, tier) for the systems that will read the kit.

The Genesis comes from the player's wallet (next section). Two **test entries** remain for
work without a wallet, with the **canonical token id** and not the number in the name:

- WebGL build: append `?token=1795` to the page URL (CHOG #1287 is token 1795).
- Play Mode: copy the token id, then **Chog > Genesis > Play As Token Id From Clipboard**;
  **Play Without A Genesis** clears it.

**A test entry is a declaration, not a proof:** anyone can type any id. Since 9 October it
only works where the game is developed: in the Editor, and on a page served from this computer
or the local network (`localhost`, a `.local` name, a private IPv4 address:
`LocalGenesis.IsPrivatePage`). **On a public address `?token=`, `?charged=` and `?bots=` are
ignored:** there, a Genesis is played by its holder or by a guest the holder lent it to (below).
The match itself still trusts what each player's game declares. A Genesis chosen in the wallet
wins over a lent one, and a lent one over a test entry. A token that is not in the roster, or
whose role has no character, enters as a recruit.

### Wallet, the player's Genesis, the recruit and the career (9 October)

**On the main menu** a panel (`Assets/Chog/Screens/WalletPanel.cs`) takes the player from a
wallet to a hero:

| The browser has | The panel shows | The player enters as |
| --- | --- | --- |
| no wallet | how to open the page in a wallet's browser | a recruit |
| a wallet, not connected | CONNECT WALLET | a recruit until connected |
| a wallet that holds no Genesis | "This wallet holds no Chog Genesis" | a recruit |
| a wallet that holds Genesis | their portraits, six a page; tap one to play it | the chosen Genesis |
| a link with a pass (`?pass=`), wallet or not | "YOU ARE A GUEST", the lent Genesis, who lent it and for how long | the lent Genesis |

The wallet is the one the browser provides (`window.ethereum`): the extension on a computer,
the wallet's own browser on a phone. `Assets/Chog/Match/Wallet/ChogWallet.jslib` is the whole
bridge; no wallet library is in the build. `PlayerWallet` keeps the state, asks the game
services which Genesis the address holds on Monad (`GameServices`, `/api/` next to the page;
see [`server/README.md`](../server/README.md)), and remembers the last choice.

On a phone, a normal browser has no wallet. When the page is served over HTTPS, the panel then
offers OPEN IN METAMASK: it leaves for `https://link.metamask.io/dapp/<address of the game>`,
the link MetaMask documents for opening an address in the browser of its app
(`PlayerWallet.MetaMaskLink`). No such link was found documented for Rabby or Backpack. On the
local test server, which is plain HTTP, the button does not show.

**A wallet app connected from the normal browser (12 October, written, never run with a real
wallet).** The browser inside MetaMask or Rabby is locked upright, so the path above shows
the game standing. The second way keeps the player in the phone's own browser: the panel
shows CONNECT WALLET, a window over the game lists wallet apps, the player approves in the
app and comes back. It is offered only when the game services give a Reown project id
(`GET /api/wallet`, see [`server/README.md`](../server/README.md)); without one the page
behaves as before.

| Piece | Where | What it does |
| --- | --- | --- |
| The page's wallet | `Assets/WebGLTemplates/Chog/TemplateData/wallet-connector.js` (`window.chogWallet`) | gives the game one wallet object: the browser's own, else the connected app. Loads Reown AppKit from jsDelivr at a fixed version (`@reown/appkit-cdn@1.8.24`, which knows Monad mainnet), only when a player asks to connect or comes back to such a connection |
| The bridge | `ChogWallet.jslib` | asks the page for the wallet object; `ChogWalletWatchConnector` tells the game that connecting is possible and when the app's account changes |
| The state | `PlayerWallet.ThroughApp` | true on a page without its own wallet that can connect an app; the panel words the wait accordingly, and the game stops taking every key while the window is open (it has a search field) |

Everything after the connection is unchanged: signing in, a purchase, a rental and a stake
speak to the wallet object through the same four calls. Sign-in by e-mail or a social account
is switched off in that window: a Genesis lives in a wallet the player already has. Not known
until a phone tries it: whether the wallet app brings the player back to the browser by
itself, and whether the game page survives the trip.

In the first three cases the panel also shows SEE EVERY HERO, which leaves the game for the
hero page next to it (`hero/`, see [`web/README.md`](../web/README.md)): the place where a
visitor without a Genesis can look at any of the 1,969 heroes.

**A recruit** is a player without a Genesis in a Genesis match: the plain character of the
class they pick, with that class's special, and 85% of its health (`Recruit.cs`; a first
number). The preparation screen says so above the class buttons (`RecruitNote.cs`).

**Signing in and the career.** With a Genesis chosen, SIGN IN TO RECORD ITS CAREER asks the
wallet to sign a message (no transaction, no cost). From then on, when an online match ends,
`CareerReporter` sends its result to the services for that Genesis, a band on screen shows the
career once it is recorded, and the menu panel and the hero card show it: matches, wins and
badges (First match, First win, Three in a row, Veteran). The career is kept per token, so it
follows the Genesis when it is sold. Without a sign-in the player still plays their Genesis and
nothing is recorded.

**Lending a Genesis (9 October).** A signed-in holder taps LEND IT beside the career: the panel
shows a link (`GenesisLoan`), SHARE THE LINK hands it to the share sheet of the phone or copies
it, STOP LENDING kills every link handed out for that Genesis so far. Whoever opens the link
plays that Genesis **without a wallet** (`GuestPass`): the menu says "YOU ARE A GUEST", who
lent the Genesis and for how many more days, and the matches go to the career of the Genesis.
A link lasts thirty days at most and dies earlier when the holder stops lending or when the
Genesis leaves the holder's wallet. The browser of the guest keeps the pass, so the plain
address of the game still plays the lent Genesis at the next visit. This is how a judge or a
friend without a Genesis plays one: the holder sends them a link. Nothing is signed and no
transaction is sent for a loan; the rules the services apply are in
[`server/README.md`](../server/README.md).

**Mastery and the second form (10 October).** A Genesis earns mastery by playing: one point a
match, one more for a win; level 2 at 3 points, then 8, 15, 25 and 40 (the rule is in
`server/src/career.js`, the game only shows it). The menu, the hero card and the band at the
end of a match say "Mastery 2  5/8". At **mastery 2** the Genesis may play the **second form**
of its attack, another way to throw it, never plainly better:

| Eyes give | First form | Second form |
| --- | --- | --- |
| beam | a laser, 2,200, reach 12 | broad beam: more than twice as wide, 1,700, reach 9.6 |
| wave | a wide stare, 1,800, reach 9.3 | long stare: a third narrower, 1,500, reach 12.5 |
| flash | a fan in front, 2,600, reach 3.7 | wide flash: reach 5.1, 2,000 |
| nova | a burst around the hero, radius 3, 2,000 | tight nova: radius 1.9, 2,800 |
| puddle | a zone of radius 3, 900 a tick | wide puddle: radius 4, 650 a tick |

The numbers are first numbers (`SecondSweeps` in `GenesisSignatureImporter.cs`, 180 more
generated signatures). On the menu a button beside the Genesis says which form it enters with
and switches it (`GenesisForms`); before mastery 2 the button says what to reach. Mastery
belongs to the Genesis like its career: a new holder, or a guest it is lent to, finds the second
form unlocked. The match trusts the form each game declares, as it does for the Genesis itself.
On a private address `?form=1` plays the second form without mastery, for tests.

**Buying the second form with $CHOG (10 October, not live).** When the game services are given
a deployed shop contract, the same button reads "MASTERY 2, OR NOW FOR 500 $CHOG" for a holder
whose wallet is connected: a tap asks the wallet to be on Monad, to let the shop take the
price, then to buy for that Genesis (`GenesisShop`, two confirmations in the wallet, no
signature of another kind). The purchase is recorded on the chain for the token id, so it
follows the Genesis; the price is read from the contract. A wallet that does not hold enough
$CHOG is told so before it is asked anything. **No shop is deployed: today the button never
shows a price.** The whole path was run on a local copy of Monad mainnet with a stand-in
wallet ([screens](../research/evidence/genesis-chog-purchase-on-fork-headless-2026-10-10.jpg),
steps in [`contracts/README.md`](../contracts/README.md)); it was never run with a real wallet
or on the real chain.

**Renting a Genesis for $CHOG (10 October, not live).** When the game services are given a
deployed rental contract, two things appear on the menu (`GenesisRental`, screens in
`WalletPanel.Rental.cs`):

| Who | Button | What happens |
| --- | --- | --- |
| A holder, for the chosen Genesis | RENT IT OUT, beside LEND IT | a screen to pick a price a day (steps from 1 to 5,000 $CHOG) and offer it: one confirmation in the wallet. Offered, the button reads "RENTED OUT AT 10 A DAY" and the screen lets the holder change the price or take the offer back |
| Any connected wallet | RENT A HERO, beside the title, or large when the wallet has no Genesis to play | the Genesis that are offered, each with its price; the player picks one and 1, 3 or 7 days and pays: up to two confirmations (letting the rental take the cost, then renting). The rented Genesis is then chosen, with "RENTED 2 d 23 h" under its image |

The payment goes from the renter to the holder, less the share the contract burns; the Genesis
never leaves the holder's wallet, who keeps playing it. A rented Genesis is played like one's
own: after signing in, its matches count for its career (`rentedMatches` says how many), and
its second form follows the same rule. The free link loan is unchanged and stays beside it.
**No rental contract is deployed: today neither button shows.** Rehearsed on a local copy of
Monad mainnet with a stand-in wallet
([screens](../research/evidence/genesis-rental-on-fork-headless-2026-10-10.jpg), steps in
[`contracts/README.md`](../contracts/README.md)); never run with a real wallet or on the real
chain.

**What this does and does not prove.** The address is what the wallet declares; the list of
Genesis is read on the chain by the services; the signature proves the player controls the
address, and the services check on the chain that this address holds the token before every
credit. The match itself still accepts any declared token, and a result is whatever the
player's page reports: see the limits in `server/README.md`.

**How it was checked, and what was not.** In headless Chrome through the LAN address, with a
stand-in for the browser wallet (`tools/unity/qualify_webgl.mjs`, `CHOG_BROWSER_WALLET`): no
wallet, a refusal, an address without Genesis, the real address that holds token 1795 (its one
Genesis appears), a real address that holds sixteen (three pages, the third Genesis tapped is
the one that enters the match). The signed path ran with a public test key that really signs
(`CHOG_BROWSER_WALLET_KEY`) against services started with `--pretend-holder`, since no key of
a real holder is available here: sign-in, a whole match, "credited: 1 match, 1 win". **Not
done: a real wallet.** MetaMask, Rabby or Backpack on a phone or as an extension has not been
tried; that is for a holder to do.

These runs are scripted, to be made again before a build goes to the phone:

```sh
sh tools/unity/checks/browser_check.sh wallet http://<lan address>:8084 one 0x80fb079237af2a634ba9b95263ba0bd53d20cd64
sh tools/unity/checks/browser_check.sh signed http://<lan address>:8085 signed     # services with --pretend-holder
sh tools/unity/checks/browser_check.sh match  http://<lan address>:8084 rares 181 40 70,539,111
sh tools/unity/checks/browser_check.sh rentout http://<lan address>:8085 offer      # a local copy of the chain, see contracts/README.md
sh tools/unity/checks/browser_check.sh rent    http://<lan address>:8085 rented 1 2 420
```

The third one enters as a token with its special charged and names the bots (`?bots=`), then
prints which signatures fired, at which simulation tick, and the damage they dealt. The last
two send real transactions, to a local copy of the chain only: a holder offers the chosen
Genesis, and a wallet rents the first offer for three days, signs in and plays a match with it.

The roster is generated, then imported by the Editor before each requested build (or with
**Chog > Genesis > Import Roster**):

```sh
python3 tools/hero-catalog/run.py roster           # writes content/genesis-roster.json
python3 tools/hero-catalog/run.py roster --check   # fails if that file is stale
```

The import writes `Assets/QuantumUser/Resources/GenesisRoster.asset` and sets
`RuntimeConfig.GenesisRoster` in `TSS-QuantumMenuSceneInfo.asset`, the config of matches
started from the menu. Starting the Gameplay scene directly uses its own local config, which
has no roster, so that path keeps the sample cast.

### Genesis signatures (9 October)

The special attack of a Genesis is decided by its Eyes (the shape) and its Aura (the effect),
whatever class its Body gives: five shapes by ten effects. Its rarity (Base and Tier) decides
how that attack is delivered:

| Rarity | Delivery | In a match |
| --- | --- | --- |
| Common (1,279 Genesis) | direct | one hit at full strength |
| Uncommon (590) | echo | fires twice, half a second apart, each at 60% of the damage; the second one leaves from where the hero then stands and faces |
| Rare Skull (60) | return | goes out at 60%, then a second strike comes back at 80%: an attack that travels returns from where it ended towards where the hero then stands, over walls; one that does not travel strikes again 0.8 s later where the hero then stands |
| Epic Radioactive (30) | persistent-zone | hits at 50%, then leaves a zone where it ended for 3 s (500 per tick before the Aura's share, radius at least 2.2), green when the Aura has no colour |
| Legendary 1:1 (10) | inversion | **not built**, plays like a direct hit |

180 signatures are authored: the fifty pairs of shape and effect direct and as an echo, and
the forty pairs without the puddle as a return and as a persistent zone (no Genesis has the
puddle with those two rarities). Asset names end with the delivery. A delivery without assets
of its own falls back to the direct signature (`GenesisRoster.TryGetSignature`), and the hero
card says so. The shares are first numbers taken from the direction written in
`docs/PLAN.md` (4.3): a rarer delivery spends the same strength another way; none is balanced.

**What the first numbers add up to.** Damage on one target that takes every hit, before the
Aura's share:

| Delivery | beam | wave | flash | nova | puddle |
| --- | --- | --- | --- | --- | --- |
| direct | 2200 | 1800 | 2600 | 2000 | 900 a tick, 3 or 4 ticks in 3 s |
| echo (2 x 60%) | 2640 | 2160 | 3120 | 2400 | no Genesis |
| return (60% + 80%) | 3080 | 2520 | 3640 | 2800 | no Genesis |
| persistent zone (50% + 500 a tick, 3 or 4 ticks) | no Genesis | 900 + 1500 to 2000 | no Genesis | 1000 + 1500 to 2000 | no Genesis |

So today a rarer Genesis deals more when everything lands: 120% for an echo, 140% for a
return, up to about 160% for a zone on a target that stays in it. That contradicts the rule
that rarity changes the gesture and not the strength, unless the harder second strike is judged
worth that much. It is the first thing to settle when the numbers are tuned.

| Shape (Eyes) | What it does | First-pass numbers |
| --- | --- | --- |
| beam | a laser from the eyes that crosses every enemy on its line and stops at a wall | 2200 damage, reach about 12 |
| wave | a wide front that travels forward over walls and hits each enemy once | 1800 damage, reach about 9.2 |
| flash | a burst just in front of the hero | 2600 damage, reach about 3.7 |
| nova | a burst around the hero | 2000 damage, radius 3 |
| puddle | a lobbed shot that leaves a damaging zone for 3 s | 900 damage per second, radius 3, lobbed up to 8 |

| Effect (Aura) | Colour | Share of the damage | What it adds |
| --- | --- | --- | --- |
| raw (no Aura) | none | 100% | nothing |
| burn | orange | 70% | 300 per second for 3 s |
| chill | light blue | 85% | the target is slowed for 2.5 s |
| hex | purple | 75% | the target is rooted for 1.2 s |
| mend | pink | 80% | the caster heals 700 per enemy touched |
| purify | white | 90% | the target loses 2 of its attack energy |
| rally | royal blue | 85% | the caster deals 35% more for 4 s |
| haste | yellow | 90% | the caster regains 2 attack energy |
| veil | grey | 90% | the target loses half of its special charge |
| toxin | green | 50% | 220 per second for 5 s |

Each effect has a sign, drawn in the colour of its Aura with a dark outline (`GenesisIcons`):
a burst for a plain hit, a flame for burn, a snowflake for chill, a star for hex, a plus for
mend, a ring for purify, an arrow for rally, a bolt for haste, a cloud for veil, bubbles for
toxin. It pops on each target the attack touches, and a smaller one stays beside the head of
the target while the effect lasts (`GenesisCombatFeedback` listens to the
`GenesisSignatureHit` event).

**In the match.** Under the special stick of the touch controls, a label names the special of
the local Genesis: its shape, and its effect in the colour of the Aura ("WAVE" over "CHILL";
`Assets/Chog/Screens/SpecialButtonLabel.cs`). At the end, the screen says "Victory!" or
"Defeat" for the player's own team, where the sample said which side won.

**Lookbook.** `?lookbook=1` in the page URL covers the menu with one screen of every drawing:
the ten signs, then the pair of eyes of each Eyes value and of each 1:1 Genesis
(`GenesisLookbook`, 57 drawings today). It is the place to compare them with the art.

**Drawn from the art, not from an effect pack.** The aim is that each attack shows the trait it
comes from. All five shapes are drawn that way, for fifty-eight Eyes values
(`GenesisSignatureImporter.EyeLooks`). Nothing a Genesis signature shows comes from the
bundled Cartoon FX pack any more, neither the attack nor its marks on the targets; the pack is
only still used by the sample's own basic attacks:

- **Lasers (beam).** `GenesisBeam` draws the laser as the collection draws it on laser-eyed
  Genesis, two beams, one per eye, each a dark outline, a flat colour and a lighter core, in the
  colours of the caster's Eyes value (green, purple, cyan, and red, orange and yellow for the
  fire laser).
- **Visors, headsets and glasses (flash).** A flat fan of light opening from the hero, in the
  colours of that visor or pair of glasses; the two sides of the 3D glasses are blue and red.
- **Crying eyes (puddle).** A pale blue tear is lobbed and leaves a puddle of tears.
- **Hard looks (wave).** The stare itself: the hero's own pair of eyes, large, on a strip in
  the hero's colour, rolls forward above the area it hits. The eyes are drawn as the art draws
  them: an X for Dead, a lidded ball for Side Eye, a slanted lid for Angry, pupils apart for
  Retard, clown make-up, the ski goggles of MOG and Chog Viper as one band of their colours,
  the pixel sunglasses of Thug.
- **Soft looks (nova).** Eight discs in the hero's colour, each with one of its eyes, open in
  a ring around it: hearts for Heart, arcs for Happy, rings for Round, dashes for Flat,
  chevrons for XD.

These are a few triangles and one unlit material, no particles (`GenesisShape` and
`GenesisEyes`, chosen by `AttackData.VisualKind`). An attack of the other team keeps its
colours and gets a red outline. **Approximations:** the eye drawings were made from one or two
images per Eyes value (eleven rare colour variants were not looked at and follow their family); the strip uses
the colour of the Skin trait, which in the art is the colour of the hair and spikes, not of
the cream face (when an eye has nearly the colour of that skin, a red heart on red, the skin
under it is drawn lighter); an Eyes value that is not in the table draws plain dark eyes.

**The ten 1:1 Genesis** carry no Eyes and no Skin value, so the look of each is authored for
its token (`GenesisSignatureImporter.Uniques`, read by `GenesisRoster.LookOf`): Blaze bursts in
glowing yellow eyes on orange, Burning Skully stares with black sockets on bone, Midas opens a
gold fan, Redd fires a red laser, Grim a grey one, and so on; their heroes take that colour.
Three of the ten were captured (Blaze, Midas, Burning Skully). Their rarity's delivery,
`inversion`, is still not built.

**The hero is its Genesis.** `GenesisAppearance` puts the real image of the Genesis on the
head of its character (a round image that always faces the camera, in place of the model's
head and helmet) and tints the body with the colour of its Skin trait. The 14 skin colours are
in `content/genesis-skin-colors.json`, derived from the portraits by
`tools/genesis-art/derive_trait_colors.py` (the yellow was chosen by eye).

**Hero card.** For a player who entered as a Genesis, the preparation screen replaces the class
buttons by a card (`Assets/Chog/Screens/GenesisHeroCard.cs`): the portrait, the name, then for
the outfit, the eyes, the aura and the rarity, the value of the trait and what it gives. The values come
from `content/genesis-roster.json`; the sentences are written in the two Editor importers, next
to the numbers they describe, so text and behaviour change together. Under the name of every
other Genesis the team slot prints "class / shape / effect".

How it is built: `GenesisSweepAttackData` is one area that affects each target it touches once;
beam, wave, flash and nova are four settings of it. The puddle reuses the sample's lobbed
projectile and ground zone. A skill names the data its attacks use
(`SkillData.AttackDataOverride`), so the four area shapes share one attack prefab,
`Resources/Genesis/GenesisAttack.prefab`. Its view, `GenesisAttackView`, draws what the attack
data names (`AttackData.VisualKind`): a beam, a fan, a drop, a disc on the ground, a stare, a
burst of eyes. It can still show an effect prefab instead (`AttackData.Visual`), which no
signature uses today.
`Assets/Chog/Editor/GenesisSignatureImporter.cs` holds the table of numbers and drawings and
writes the prefab and the data assets to `Assets/QuantumUser/Resources/Genesis/` at each roster
import; change a number or an effect there, not in the assets. `SkillData.Reach` tells bots how
close a target must be for the special, and sets the length of its aim preview.

The special is still charged the way the sample does it, by landing basic attacks. To look at
signatures without charging them, add `charged=1` to the page URL: every character then enters
the match with its special ready (it applies to the match of the player who creates the room).
With `?perf=1` the console receives one `CHOG_COMBAT skill <asset>` line per skill use and one
`CHOG_COMBAT damage <amount>` line per damage event, which is how the signatures were checked.
**Placeholders:** the numbers are not balanced against a kit budget, and the cone preview of
the flash is longer than its area. An enemy's attack is told from an ally's by its red outline
only; the signs on targets are not marked by team.

### Genesis portraits (9 October)

Each Genesis on the preparation screen shows its real image beside its team slot and on the
hero card (`Assets/Chog/Match/GenesisPortrait.cs`), and in the match the same image is the head
of the hero (see above). One loader serves both (`GenesisArt`). The game loads
`genesis-art/256/{metadata id}.jpg` from the folder of the game page; a missing image leaves
a grey disc and nothing else changes. In Play Mode the Editor reads them from the local
server at `http://127.0.0.1:8083/`.

The originals are 1000 px WebP files on IPFS, which Unity cannot decode, so small JPEG copies
are made once:

```sh
python3 tools/genesis-art/fetch_portraits.py        # every Genesis whose metadata is recorded
python3 tools/unity/serve_webgl.py --latest --host 0.0.0.0 --port 8083   # also serves /genesis-art/
```

The copies go to `.artifacts/genesis-art/` (about 20 KB each), which is not under version
control. The user states that the project holds every right on the collection's art (decision
D19 in `docs/STATUS.md`); nothing public was found that states it, so that is their word, not a
verified licence. A public deployment needs its own copy of the folder.

The game page is served over plain HTTP on the local network, and Unity refuses plain-HTTP
downloads by default except from the machine itself: the portraits then load on `127.0.0.1` and
never on a phone. The qualification build therefore sets
`PlayerSettings.insecureHttpOption` to `AlwaysAllowed`; **check a build through the LAN
address**, not `127.0.0.1`. With `?perf=1` the overlay prints how many portraits were requested,
loaded and failed, and the last error. A portrait that is not loaded is a grey disc.

**Cut out of its background (10 October).** For 1,277 of the 1,969 Genesis (those that look
right, of the usual kind) the game shows the Chog without its background, in the match and on
the menu: the cap, the spikes and the face keep their own outline inside the round frame. The
cuts are made from the finished images, since the collection's layer files are not available
here:

```sh
.artifacts/tools-venv/bin/python tools/genesis-art/cut_out.py learn    # rebuild the backgrounds from the collection
.artifacts/tools-venv/bin/python tools/genesis-art/cut_out.py sheet out.png 1287 172 383   # look at some cuts
.artifacts/tools-venv/bin/python tools/genesis-art/cut_out.py all      # write the trusted cuts and their list
```

They go to `.artifacts/genesis-art/cut/` (5 MB): one small picture per Genesis that says what
to keep of its portrait, and `index.json`, the list the game reads once. A Genesis that is not
in the list keeps its whole round picture. How the method works, what it gets wrong (the
Genesis that look left, the Skulls, the Radioactive, the 1:1) and how it was checked are at the
top of the script and in `docs/STATUS.md` (10 October, 06:40 and 07:00). Look at cuts on a
sheet drawn at random, never on a light ground: a pale face cut away is invisible on it.

### The body of a hero, and where the Chog body goes (10 October)

**Bodies are moved by code, not by animation clips.** `Assets/Chog/Match/ChogBodyAnimator.cs`
moves a body made of rigid parts from what its hero does: it bobs and swings its arms as it
runs, throws both arms forward when it attacks, raises them when its team wins and folds when
it loses. The three class models of the sample are built that way (a `Body` with a head and two
arms under it), so they are moved by it too, and the sample's eleven animation clips and its
animation controller are no longer played. They are still in the project and in the build,
because the character prefabs still point at them: removing that link is an Editor change, not
done.

**Since 11 October a Chog body is in place** (`Assets/Chog/Resources/ChogBody.fbx`, built by
`tools/blender/make_chog_body.py`): every Genesis hero wears it. It has a belly and spikes
on its back. On 12 October a second one was tried in the game for an hour (variant B of
`tools/blender/make_chog_body_variants.py`: a slim body, pink mittens and feet, a dark
outline, no spikes, since the picture of the Genesis shows them) and taken out again: the
user found it worse, because taken alone the model had no head, no spikes and no face, so
nothing of a Chog was left in 3D. The first body is back; a whole 3D Chog, head included,
is being drawn (`tools/blender/README.md`). What stayed from that try: the materials of a
body are told apart by name when the hero is coloured (`ChogBody.PartColour`): `ChogGlove`
is pink, `ChogOutline` nearly black, everything else takes the Skin. The body follows the
contract below; a better model replaces it by taking its place.

**To hand over the Chog body**, export one model made of four parts and save it, as a prefab
or a model file, under any `Resources` folder with the name `ChogBody` (for instance
`Assets/Chog/Resources/ChogBody.prefab`). From then on every Genesis hero wears it instead of
the model of its class (`ChogBody.Wear`), with its real image as the head and its Skin colour
on the parts. What the model must respect:

| Point | What is expected |
| --- | --- |
| Parts | four objects named `Body`, `Head`, `armLeft` and `armRight` (letter case does not matter; a longer name that ends the same way is fine for the head and the arms). No skeleton |
| Arrangement | the head and the arms under the body, or beside it: the game puts them under it |
| Orientation | looking along +Z, +Y up, standing on the ground with its feet at the origin |
| Origins | the origin of each arm at its shoulder, the arm hanging down: the arm turns about that point. The origin of the head at the neck |
| Size | about 1.2 units tall, the height of the class models, before the 0.85 scale the game applies; in a match every body is then drawn 1.55 times larger |
| Material | one material; its main colour is replaced by the Skin colour of each Genesis |

`?bodies=1` opens a page that shows every body standing, running, attacking, winning and
losing: the three class models, a stand-in of four plain parts, and the Chog body once it is
there. On a private address `?body=standin` puts that stand-in in the place of the Chog body
in a match, to try the slot before the model exists.

**The head and the body share the screen (10 October, after the user's phone test).** The
camera looks down at 73 degrees, and until that day the image of the Genesis, 1.7 units wide
on the head, covered the whole body: on the phone the heroes read as floating pictures, "the
heroes are invisible". Now the image is 1.2 units wide and raised 0.55 up the screen, the body
is drawn 1.55 times larger, and the name and the bars move up by the same amount so they stay
above the image (`GenesisLook` in `GenesisAppearance.cs`, `CharacterUIFollow`). The values were
picked from headless captures of six layouts
([before and after](../research/evidence/genesis-body-under-head-headless-2026-10-10.png)); on
a private address `?look=1.2,0.55,1.55` (image size, lift, body scale) tries others without a
new build. **Not seen on a phone yet.** The body is still the class model of the sample in one
flat colour, the Skin colour of the Genesis.

**Limits.** Seen in the viewer in the headless browser
([capture](../research/evidence/chog-body-viewer-headless-2026-10-10.png)) and in headless
matches. A body put in place of the class model does not turn half transparent in a bush for
its own team, as the class models do. The weapons of the class models go with them. A body
drawn larger than its collision circle overlaps a wall slightly when it stands against one.

### A tower and a core for each team (12 October)

Each team defends two structures on the sample's map, built when the match begins
(`Assets/QuantumUser/Simulation/DSL/Structure.qtn`, `System/Structures.cs`,
`System/StructureSystem.cs`):

| Structure | Where | Health | What it does |
| --- | --- | --- | --- |
| Core | in front of the team's spawn points, (0, -12.5) for team 0 and (0, 12.5) for team 1 | 12,000 | when it is destroyed the match ends at once and the other team wins, whatever the coins |
| Tower | 4 units further out, (0.5, -8.5) and (-0.5, 8.5) | 8,000 | every second it takes 500 health from the nearest living enemy hero within 5 units |

A hero has about 6,000 health. The health of the structures was 24,000 and 12,000 at first
and was lowered on 12 October after watching bots play (below); it is still to be tuned by
people playing. The spots were measured on the map's colliders, which turn onto themselves
around the centre (`tools/quantum/draw_map.py` draws the map, `free_ground.py` says how much
room a spot has). The coins still decide a match in which no core falls.

How it fits the sample without touching its assets:

- A structure has `Health`, `Attributes` (health only) and `TeamInfo`, on the layer of the
  heroes, so every attack hurts it as it hurts a hero; it has no `Character`, so nothing made
  for heroes counts it (bots, respawn, team health). An effect it has no attribute for (a
  stun, a slow) does nothing to it (`EffectsHelper.OnApply`).
- `HealthSystem` leaves it to its own end (`Structures.Destroyed`): it stays where it was,
  dead, its collider off, and never comes back.
- Heroes still walk through each other, and are stopped by a standing structure
  (`KCCSettings`: the movement now also looks at the heroes' layer and keeps only structures).
- The match is ended by the machine that already runs it (the Bot SDK state machine "Coin
  Grab Manager", printed by `tools/quantum/list_hfsm.py`): it ends a match when the clock it
  reads is under zero, and `GetTimer` reads it so once a core fell (`Structures.MatchClock`).
  The winner it announces is then the other team (`TriggerQuantumEvent`).
- The judge follows: the replay program reports `coreLostBy`, and with it an even score is
  no draw (`server/src/judge.js`). It also counts the hits of each tower (`towerShots`).
- A bot keeps out of the reach of the other team's towers: inside that reach, and 0.75
  outside it, a push straight away from the tower is added to the direction it wants
  (`Structures.KeepAway`, called at the end of `AISteering.GetDesiredDirection`). Without it
  the bots walked under the towers: in the first match recorded with towers one tower hit 31
  times, the other 12, and the match ended 17 to 0.
- **A bot defends and pushes** (`Structures.BotObjective`, read by the bot's eyes in
  `SensorEyes` and before every other tactic in `SensorTactics`). It defends: a structure that
  is hit calls its team for 4 seconds (`Structure.AlarmUntil`, set by `Structures.Hurt` on the
  damage signal), and the bots of that team go for the living enemy hero nearest to it
  within 10 units. It pushes: the other team's tower while it stands, then its core, when its
  team has more heroes alive than the other, or when a player of its team already stands
  within 5 units of that structure (the bots follow a player in; a bot there draws nobody).
  A bot with an enemy hero within 6 units fights that hero and does not push, and a bot low
  on health does neither. Nothing was added to the bots' state machine: the state that fights
  a hero does the rest with a structure as its target, and a bot that attacks a structure is
  let under the towers of that team.

**What the player sees** (`Assets/Chog/Match/StructureViews.cs`): the tower is a totem with
the head of a Chog, and what it shoots with is the laser of its eyes; the core is a heart
inside a crown of Chog spikes. Both are flat pictures facing the camera, like the heads of the
heroes, on a base in the colour of their side (blue for the player's team, red for the other)
under a bar of their health; a destroyed one stays as a grey ruin. Two lines near the top of
the screen: a large one for a few seconds when a structure falls ("ENEMY TOWER DESTROYED",
"YOUR CORE FELL"), and a smaller red one that pulses for as long as a structure of the
player's own team is being hit ("YOUR TOWER IS UNDER ATTACK"), which reads the same alarm
that calls the bots.

**Watching the bots play** (`python3 tools/quantum/watch_bots.py <replay.json> [seeds]
[--all-bots]`): the judge replays one record with other seeds, and can hand the recorded
player's hero to a bot, so each seed is another match of bots, played in under a second. It
prints who won and how, the hits of each tower, the health each structure had left and what
fell when. What it showed on 12 October, on matches of about two minutes:

| Structures | Who plays | Towers | Cores |
| --- | --- | --- | --- |
| 12,000 and 24,000, bots that defend and push | six bots | one fell in 5 matches of 10 | none fell, the weakest had 19,050 left |
| 8,000 and 12,000 (kept) | six bots | one fell in 8 matches of 10 | none fell, the weakest had 6,404 left |
| 8,000 and 12,000 | two bots and an idle player against three bots | the idle player's fell in 6 of 6, after 24 to 63 seconds | the idle player's fell in 2 of 6 |

So between even teams of bots the coins still end the match, with a tower down on the way,
and a team that plays a hero short can lose its core in a minute or two. Whether a core should
fall more often between even teams is a choice of design, not made yet.

**Checked:** six tests of the rules on a real Quantum frame (29 in all), a replay in which a
core is made to fall at a given tick (`Chog.Quantum.Verifier ... --test-core-falls 1@1500`:
the match ends that second for team 0, and for team 1 when the other core falls), the
structures seen standing in a headless match
([capture](../research/evidence/structures-and-body-b-close-up-headless-2026-10-12.png)),
towers shooting in whole matches played there (hits of exactly 500 in the damage log), and
the bots of the other team attacking the tower of an idle player's team in a headless match
([capture](../research/evidence/bots-push-a-tower-headless-2026-10-12.png); that tower hit
them 20 times and the core behind it lost 875 health). **Seen on 15 October:** a whole match
played in the headless browser (`browser_check.sh respawn http://127.0.0.1:8084 ... 150 60`,
build `20261012T041344613Z`, through Photon's cloud, the player as a visitor who walks into
the middle and then stands still) ended by a fallen core: "YOUR TOWER IS UNDER ATTACK", then
"YOUR TOWER FELL" (picture 25), "YOUR CORE IS UNDER ATTACK", then "YOUR CORE FELL" and
"Defeat" (picture 39) ([sheet of the 60
pictures](../research/evidence/match-ended-by-core-headless-sheet-2026-10-15.png),
[tower](../research/evidence/your-tower-fell-headless-2026-10-15.png),
[core](../research/evidence/your-core-fell-defeat-headless-2026-10-15.png)). The judge, on the
record Photon sent, agrees: team 0's tower fell at tick 2,599 and its core at 3,644, the match
ended at 3,645, team 1 won 9 to 6, a verdict that is not void. **Not seen:** the bots coming
back to defend (the rule is tested, nobody watched it happen); the ruin of a structure at full
size on a phone. **Not done:** a bot slides along a structure that is in its way; no sound.

### The page the game is served in (10 October)

The build no longer uses the engine's page. Ours is `Assets/WebGLTemplates/Chog/` (the build
script selects it): `index.html`, `manifest.webmanifest` and three icons made by
`python3 tools/web/make_page_icons.py` from one Genesis portrait (a working icon: CHOG #1287,
the user's own).

| What | How it behaves |
| --- | --- |
| Whole screen | on a phone, the first touch asks the browser for the whole screen, then asks to keep it on its side. Where the browser refuses (an iPhone, or a browser inside another app), nothing is asked again |
| Held upright | a notice asks to turn the phone, with CONTINUE UPRIGHT for a browser that cannot turn (the ones inside some wallet apps are locked upright) |
| Loading | the game's title and a bar ([picture](../research/evidence/game-page-loading-headless-2026-10-10.png)). On an iPhone it says how to get the whole screen: Share, then Add to Home Screen |
| A file that does not arrive | the engine's loader only reports it and stops. The page opens itself again, twice, then shows what went wrong and a TRY AGAIN button |
| Installed | the manifest makes the page installable (whole screen, on its side). No offline copy is kept, so a new build is never hidden by an old one |
| Title and icon | "Chog Genesis" in the tab and on the home screen |

Seen in the headless browser only: a match through the LAN address with the page, the notice
when held upright ([picture](../research/evidence/game-page-upright-headless-2026-10-10.png)),
and a load where the first request for `webgl.framework.js.gz` got no answer (the user's
phone showed that exact error on 10 October): the page opened itself again and the match
started. `python3 tools/unity/serve_webgl.py --drop-first framework.js` makes that failure.
**On the user's phone (Redmi, Chrome 126, 10 October 04:30):** the whole screen is used; the
game's picture went from 1508 x 560 to 1650 x 720 at the same pace, and the user found it
"much better". **Not seen:** what an iPhone shows, the notice when the phone is held upright,
and a load that fails on a real phone. There is no install button in the game: installing
goes through the browser's own menu.

### Building without the menus (9 October)

`Assets/Chog/Editor/BuildRequestWatcher.cs` lets tooling request a WebGL build from the open
Editor. From the repository root:

```sh
date -u > .artifacts/unity/webgl-build-request      # ask for a build
printf refresh > .artifacts/unity/webgl-build-request  # compile and import only, no build
cat .artifacts/unity/webgl-build-status.json        # refreshing, importing, building, then a final state
```

The Editor refreshes assets first, so changed scripts are compiled, then imports the Genesis
roster, and it waits while Play Mode is active. If nothing happens, bring the Editor to the front once: a freshly added script is
only picked up when the Editor regains focus. A server started with `--latest` then serves the
new build without a restart.

### A match played again outside the game, and who a player is for Photon (11 October)

- **The record of a match.** On a private address, `?record=1` makes the page keep the
  commands of its match (`Assets/Chog/Screens/MatchRecorder.cs`) and, when the match ends,
  send them to the test server as the engine's replay file (`.artifacts/replays/`, named
  after the winner and the last tick the game announced). A test tool: on a public address
  it does nothing, and the record that counts there is the one Photon's server sends to the
  game services.
- **The simulation's assets of a build.** Every build writes `quantum-db.json` beside its
  `webgl/` folder (`QualificationBuild.ExportSimulationAssets`). The engine's own export is
  not used: it throws on this Unity version.
- **Replaying.** `python3 tools/quantum/headless.py verify <record> <build
  folder>/quantum-db.json <report.json> --dotnet <dotnet>` plays the record with the real
  simulation outside Unity and writes who won (`tools/quantum/Verifier.cs`). The record and
  the assets must come from the same build. `browser_check.sh duel-host` and `duel-guest`
  play a match of two pages in opposite teams to make such a record.
- **Who a player is for Photon.** `Assets/Chog/Screens/PhotonIdentity.cs` asks the services
  (`api/photon`) whether Photon's cloud asks them who connects. When it does, the game
  connects with the wallet's session, or a guest's pass, or nothing. When it does not, which
  is the case everywhere today, the game connects as before. Never run against Photon's
  authentication: the dashboard is not set up (`docs/DEPLOY.md`).

### Staking $CHOG on a match (11 October)

Shown only where the game services stake on matches (`GET api/wager` answers `enabled`); as
no wager contract is deployed, that is nowhere but on the test bench today.

- **Before the match** (`Assets/Chog/Screens/WagerPanel.cs`, on the Team Preparation screen):
  a signed-in player picks an amount (1 to 1,000 $CHOG) or sees the one the match is already
  played for, and taps STAKE. `GenesisWager.Stake` asks the referee for a ticket, then the
  wallet: be on Monad, allow the stake, stake. The panel then says how many staked and what a
  winner gains. The team staked on is the team of the player's character at that moment.
- **At START** (`CharacterSelectionUI.OnStartClicked`): when someone staked, the stakes are
  locked first (`GenesisWager.WhenLocked`); the match starts when the services say they are.
  While a ticket handed out can still be used the panel counts down and asks again.
- **After the match** (`Assets/Chog/Screens/WagerResult.cs`, a strip at the top of the menu):
  the match is judged once its room closes, so the answer comes on the menu. The stake is
  remembered by the browser until it is paid or given back. The strip says who won and
  offers CLAIM (or GET IT BACK): the wallet brings the referee's decision to the contract,
  which pays every winner or gives every stake back, whoever brings it.
- **Trying it without Photon**: `sh tools/contracts/wager_bench.sh <build folder>` (a local
  copy of Monad, the wager, services with the judge), `serve_webgl.py --port 8085 --services
  http://127.0.0.1:8091`, `node server/rehearsal/photon_standin.mjs enter|finish` for
  Photon's part, and `browser_check.sh stake`, `stake-host`, `stake-guest`, `staked`.
- **Limits.** Seen in the headless browser with test wallets on a local copy of the chain
  only: never with a real wallet, never on a phone, never with Photon's own calls. A player
  who changes team after staking gets the match called off (stakes come back). Only the
  player who starts the match locks the stakes: if that player is not signed in, the match
  is played unlocked and the stakes come back.

## Ce que l'inspection change dans notre développement

| Point | Code réellement présent | Travail à effectuer après le premier build |
| --- | --- | --- |
| Mouvement et vue | Simulation `Transform2D`, KCC, vues 3D, `LocalPlayerCameraFollow`. | Garder la simulation plane, régler caméra et carte de voie. |
| Trois héros | Prototypes Knight, Archer, Spellcaster dans `QuantumUser/Resources`. | Créer nos prototypes à placeholders ; les rôles Genesis ne sont pas automatiquement ceux du sample. |
| Création du personnage | `RuntimePlayer.PlayerAvatar` → `PlayerJoiningSystem` → `CharacterSelectionSystem`. | Faire sélectionner un prototype et un loadout résolus depuis le Genesis ; figer le profil à l'admission. |
| Capacités | `CharacterAttacks` référence `BasicSkillData` et `SpecialSkillData`. `SkillData` crée les attaques et gère intervalle/coût. | Ajouter le troisième slot, son input, HUD, bot, cooldown et DSL généré. Attaque + deux compétences n'existe pas encore. |
| Traits / tiers | Package `Chog.Heroes`, `GenesisLoadoutBinder`, asset `GenesisCombatLoadout`, champs RuntimePlayer et sélection dans le Frame. 14 tests SDK passent avec des assets de test. | Créer les vrais assets de signature, appeler le binder depuis le lobby et vérifier le combat. Les contrôles de cohérence ne prouvent pas la propriété du NFT. |
| Mobile web | UI choisie au runtime ; lecteur `Chog.Controls`, visée conservée au relâchement, capture du doigt et remise à zéro. Neuf tests Unity passés. | Vérifier le navigateur/téléphone et le déplacement + visée simultanés ; retour du wallet à intégrer ensuite. |
| Équipes / bots | `MAX_TEAM_SIZE = 3`, remplissage de six slots dans `AISetupHelper`, remplacement dans l'input/IA. | Tester limites d'équipe, reconnexion et slots ; remplacer les objectifs Coin Grab par escorte/défense. |
| Commandes de développement | `CommandsSystem` accepte sélection de prototype, téléportation et spawn. | Isoler les commandes de debug avant PvP public ; un profil approuvé doit être la seule source des héros jouables. |
| Victoire | HFSM Coin Grab et `MatchSystem`. | Construire vagues, ciblage de tour, protection du noyau et fin de partie MOBA. |
| Replay | Export fourni dans l'éditeur ; modèle runner .NET 8 inclus. | Enregistrer une vraie session, exporter DB/LUT, générer et compiler la simulation puis comparer les checksums. |

Les fichiers `SystemSetup.cs` et `SystemSetup.User.cs` sont quasiment vides. Le sample
configure ses systèmes dans **`QuantumUser/Resources/TSS-SystemsConfig.asset`** ; partir de
cet asset pour les extensions, sans ajouter par erreur des systèmes en double.

Pour le premier replay, activer l'enregistrement des inputs et checksums dans la connexion
Quantum, puis utiliser pendant la session **Tools → Quantum → Export → Replay (Include
Asset DB)**. Le menu **Tools → Quantum → Export → Dotnet Quantum.Simulation - Generate
Project** est présent. Le modèle fourni cible `net8.0` pour le runner et `netstandard2.1`
pour la simulation. Le SDK .NET 8.0.425 est disponible dans `.artifacts/dotnet-sdk` et
`python3 tools/quantum/headless.py test` compile la simulation et exécute 41 tests avec le
SDK officiel. Aucun replay complet n'a encore été enregistré/rejoué. Un replay de client sert au diagnostic P0 ;
la validation de carrière P3 utilisera le flux approuvé décrit dans l'architecture.

Les détails du produit restent dans [PLAN](../docs/PLAN.md) et
[ARCHITECTURE](../docs/ARCHITECTURE-QUANTUM.md). Cette page décrit le projet Unity et
les points de reprise du sample.

## Assets et versions

Le sample d'origine utilise le rendu intégré avec Post Processing 3.4.0 et Input System 1.7.0.
Unity 6.6 a résolu Post Processing 3.5.4 et Input System 1.20.0 dans le manifest courant ; ne
pas mélanger une migration URP avec la qualification réseau. Gamma est déjà configuré.
[Unity 6 documente le web mobile](https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-browsercompatibility.html) ;
[Reown exige Unity ≥ 2022.3 et Gamma](https://docs.reown.com/appkit/unity/core/installation).
Les versions finales seront figées après compilation et essai sur téléphone.

[License.txt](License.txt) autorise l'utilisation du sample pour le développement interne
et distingue les assets artistiques, qui demandent leurs licences propres pour la diffusion.
Les conserver pour la comparaison locale ; remplacer les visuels par nos placeholders
avant la démo publique, ou documenter individuellement leurs droits. Aucun asset n'a été publié.
