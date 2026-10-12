# MOBA Genesis, Chogathon

**Un MOBA multijoueur en vue du dessus 3D/isométrique, de préférence en PWA mobile.
Les Genesis deviennent des héros dont les traits et la rareté modifient le combat ;
leur carrière évolue dans le jeu et $CHOG permet d'acquérir des spécialisations.**

L'utilisateur demande un prototype fonctionnel rapidement, des placeholders visuels au
départ et une architecture permettant ensuite d'améliorer le gameplay et l'expérience.

**Décision réexaminée le 8 octobre : Unity + Photon Quantum 3 dans `game/`.** Le sample
Twin Stick Shooter donne déjà le combat, trois héros et des bots. Son build local WebGL
(~22,2 Mo) démarre dans Chrome ; l'utilisateur indique qu'il fonctionne sur Redmi 11C,
avec quelques saccades. La performance mesurée, le match Quantum en ligne, Genesis et
$CHOG restent à livrer. [Motifs et périmètre borné](docs/DECISION-RESEAU-REVUE.md) ;
[preuve du build](research/evidence/quantum-webgl-local-2026-10-08.json).
Le [prototype Fusion Server](game-fusion/README.md) reste conservé comme référence
technique : arène Archer et huit contrôles réseau Editor, sans match MOBA complet.
Le format 3v3 à une voie et les règles de signature restent des recommandations.

| Document actif | Usage |
| --- | --- |
| [Plan de développement](docs/PLAN.md) | Expérience, rôle des traits/raretés, $CHOG, jalons et critères de validation, extensions si l'on avance vite. |
| [Architecture](docs/ARCHITECTURE-QUANTUM.md) | Simulation, catalogue, identité, persistance, résultats, boutique et PWA. |
| [État courant](docs/STATUS.md) | Prochaine tâche, décisions, ce qui a vraiment été vérifié et ce qui reste ouvert. |
| [Décision réseau](docs/DECISION-RESEAU-REVUE.md) | Comparaison Quantum/Fusion, choix Quantum, limites et conditions de réexamen. |
| [Photon architecture audit](docs/PHOTON-ARCHITECTURE-AUDIT.md) | Independent audit of 2026-10-09: Quantum 3 kept behind a measured mobile gate, evidence matrix with primary sources, remaining work in blocks, flip conditions. |
| [Game notes](game/README.md) | What is built in the Unity project: Genesis entry, signatures drawn from the art, wallet, recruit, career, page options, how to build and check without the menus. |
| [Game services](server/README.md) | Ownership read on Monad, sign-in with a wallet signature, career per Genesis; what is checked and what is not. |
| [Hero page](web/README.md) | The light page where a holder looks up a Genesis as its hero card. |
| [$CHOG contracts](contracts/README.md) | The rental of a Genesis (a holder offers it at a price a day, a player pays, a share is burned) and the purchase of a Genesis's second form: both tested, both rehearsed from the game on a local copy of Monad, **neither deployed**; how to rehearse and how to deploy. |
| [Mobile presentation, 10 October](research/reports/mobile-presentation-2026-10-10.md) | How the game should be opened on a phone: tab, installed web app, store wrapper or native; how a wallet connects there; what shipped games do; a recommendation in testable blocks and what would change it. |
| [Developer tools](tools/dev/README.md) | Working on the game while the Editor stays open: compile and run the view tests outside Unity, ask the Editor for a build, snapshot the sources. |
| [Field census, 9 October](research/reports/chogathon-field-census-2026-10-09.md) | Who else is building: how the hackathon site works once sign-up is closed, eight public repositories read in their code, what it means for us. A sample, to run again. |
| [Putting the game online](docs/DEPLOY.md) | The prepared path (one Node process, `render.yaml`), what was tried locally, what the user has to decide. `sh tools/release/check_all.sh` runs every check that needs no Editor. |

Le [module de résolution des héros](shared/Chog.Heroes/README.md) transforme trois Genesis
réels en profils versionnés ; **27 tests catalogue et 14 tests d'intégration Quantum passent sous .NET 8**. Le
[catalogue de prototype](content/README.md) contient les attributions explicites de traits.
Le binder applique des références de capacités dans un vrai Frame de test ; le lobby et
les assets de signatures restent à brancher. [Résultats et limites](research/evidence/quantum-headless-qualification.json).

```sh
python3 tools/hero-catalog/run.py test
python3 tools/hero-catalog/run.py inspect
python3 tools/quantum/headless.py test
```

Le [projet Unity actif est dans `game/`](game/README.md) : Quantum 3.0.9, archive d'origine Unity
2021.3.45f2, fichiers vérifiés à l'extraction. L'utilisateur a ouvert le projet sous
**6000.6.4f1 Apple Silicon**, avec WebGL. Les corrections de compatibilité compilent ;
[relevé de migration](research/evidence/unity-6000.6-qualification.json).

**Pour essayer :** ouvrir `game/` dans Unity **6000.6.4f1 Apple Silicon**, puis
`Assets/QuantumUser/Scenes/Gameplay.unity`, **Play → choisir un héros → Start**.
La scène locale remplit avec des bots et n'a pas besoin d'AppId.
[Procédure Unity et WebGL](game/README.md). Le build local est aussi servi sur le Wi-Fi
du Mac à `http://192.168.1.85:8081/` tant que cette adresse et le serveur restent actifs.

**Status 9 October:** a first online Quantum match between the Mac Editor and the phone
(party code, 2 humans, 4 bots) was fluid and playable per the user, not yet measured.
Measured on the phone (Helio G85 class): 30 Hz simulation gives about 32 fps against 26 at
60 Hz; the project now runs at 30 Hz with checksums off.
**Next step:** build the MOBA rules and two Genesis heroes, measuring each new system on the
phone with `?perf=1`. Headless checks: `python3 tools/quantum/headless.py test --dotnet
/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet`.
Serve the newest build with `python3 tools/unity/serve_webgl.py --latest --host 0.0.0.0 --port 8082`.
Derived hackathon end: 23 October 17:00 Paris. [Criteria and limits](docs/STATUS.md).

Les metadata de **302 Genesis sur 1 969** ont été récupérées et analysées : cinq tiers
représentés, plusieurs champs facultatifs, au moins un Legendary 1:1 avec seulement deux
traits. [Recensement partiel](research/evidence/genesis-trait-census.json).
Les fréquences de cet ensemble ne sont pas extrapolées à toute la collection.

Les [preuves et sources](research/evidence/README.md) documentent les observations et leurs
limites. La [lecture de CHOGTANKS](research/evidence/moba-feasibility-and-reuse.json) identifie
ce qui peut être réutilisé. Les [anciennes orientations](research/historique-orientations.md)
et les autres dossiers de recherche sont historiques ; ils ne définissent plus le produit.

Pour reprendre le travail : lire l'état courant puis le plan ; conserver une preuve des
tests, mettre à jour les décisions qui changent et noter la prochaine action. Les instructions
de continuité sont dans [AGENTS.md](AGENTS.md).
