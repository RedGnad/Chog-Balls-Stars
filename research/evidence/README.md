**Méthode et preuves conservées**

**Mise à jour du 7 octobre :** les documents actifs de développement sont dans
[docs/PLAN.md](../../docs/PLAN.md) et [docs/ARCHITECTURE.md](../../docs/ARCHITECTURE.md).
Les 302 metadata Genesis enregistrées changent le constat antérieur « aucun JSON récupéré ».
Le relevé reste partiel : 1 667 fichiers sont encore absents, aucune fréquence globale n'est
déduite de ce corpus non aléatoire. Les valeurs brutes sont conservées et leur SHA256 est
vérifié par le script d'analyse ; la validité cryptographique du DAG IPFS n'a pas été testée.

Reproduire les décomptes, sans réseau, depuis la racine du projet :

```sh
python3 research/scripts/analyze_genesis_metadata.py --input research/evidence/genesis-metadata.jsonl --output research/evidence/genesis-trait-census.json
```

La collecte peut être reprise par `research/scripts/fetch_genesis_metadata.py` avec le même
chemin `--output`. Elle saute les entrées déjà enregistrées, utilise désormais un rythme
plus bas et s'arrête au premier HTTP 429. Après une reprise, régénérer le recensement et
actualiser les décomptes dans le plan et l'état courant avant de les citer.

Dates de référence utilisateur : 6 et 7 octobre 2026, Europe/Paris. Les recherches et relevés utilisent uniquement des informations publiques. Les opérations blockchain sont des lectures `eth_chainId`, `eth_blockNumber` et `eth_call` ; aucune transaction n'a été diffusée. Un essai `web3_sha3` a renvoyé « Method not found » ; les sélecteurs du second relevé ont été calculés localement par Keccak-256, vérifié sur le hash vide et `ownerOf(uint256)`.

| Fichier | Contenu |
| --- | --- |
| [genesis-card-and-laser-headless-2026-10-09.json](genesis-card-and-laser-headless-2026-10-09.json) | The hero card (portrait, then outfit, eyes and aura with what each gives) and the beam redrawn as the laser of the caster's eyes after looking at six laser-eyed Genesis. Card and first frame of the fire laser seen in headless Chrome; the full beam and the three other colours were not captured. Two screenshots. |
| [genesis-effects-perf-phone-2026-10-09.json](genesis-effects-perf-phone-2026-10-09.json) | The user's phone after the effects were lightened: 31 and 32 fps over two minutes with 44 effects shown, the level of the morning's baseline, read from the reports the game posts to the local server. The lagging build itself was never measured, and which change fixed it is not known. |
| [genesis-aura-effects-headless-2026-10-09.json](genesis-aura-effects-headless-2026-10-09.json) | The Aura changes what the signature does: fifty signatures authored; in one headless online match the raw, burn and rally variants fired with the authored numbers (burn ticks and the rally bonus visible in the damage log), no error. Seven effects and all on-target visuals were not observed. |
| [genesis-phone-fix-and-visuals-2026-10-09.json](genesis-phone-fix-and-visuals-2026-10-09.json) | Why the portraits never showed on the user's phone (Unity refuses plain-HTTP downloads except to loopback, so every check on 127.0.0.1 passed), the fix and its check through the LAN address; and the first distinct visuals of the signatures (nova and flash seen, beam and wave not captured). Four screenshots. The phone itself was not re-tested by the time of this record. |
| [genesis-signatures-headless-2026-10-09.json](genesis-signatures-headless-2026-10-09.json) | The special of a Genesis follows its Eyes: in two headless online matches the five signature skills fired (bots) and each authored damage value was dealt at least once, with no class special of the sample and no error. Counts of skill uses and damage events from the `CHOG_COMBAT` console lines. Numbers and visuals are placeholders; no human fired one. |
| [genesis-portrait-headless-2026-10-09.json](genesis-portrait-headless-2026-10-09.json) | The real image of a Genesis as a round portrait above its character and beside its team slot, for the player (CHOG #1287) and a bot (CHOG #200). Headless Chrome, two screenshots, image source and mirror recorded. The image copies are local test files; redistribution rights are not established. |
| [genesis-owners-multicall-2026-10-09.json](genesis-owners-multicall-2026-10-09.json) | Owner of every Genesis read in one Multicall3 `eth_call` (0.68 s): 1,969 tokens, 1,011 owners at block 111821264. Written by `research/scripts/scan_genesis_owners_multicall.py`. A read at one block, not a proof of who controls a wallet. |
| [genesis-entry-headless-2026-10-09.json](genesis-entry-headless-2026-10-09.json) | A player enters an online match as a Genesis declared with `?token=1795`: selection screen locked to the Knight and labelled "CHOG #1287 - Knight", character labelled "CHOG #1287", one bot labelled "CHOG #313". Headless Chrome, one client, two screenshots. Records the first build's silent fallback (roster missing from the Quantum asset database) and its fix. Ownership is not checked. |
| [quantum-tuning-runs-phone-2026-10-09.json](quantum-tuning-runs-phone-2026-10-09.json) | Phone A/B on one build: 30 Hz with checksums off reads about 32 fps, median frame 30 to 31 ms, Quantum update 8 ms, against 26 fps, 37 ms and 15 ms untuned; non-simulation work 17 to 18 ms in both. Also records that the WebGL build starts at quality 'Fastest' and that frames were already browser-paced. Three screenshots and two headless checks. |
| [quantum-baseline-repeat-phone-2026-10-09.json](quantum-baseline-repeat-phone-2026-10-09.json) | Five solo online runs on the phone that all used the untuned 60 Hz configuration because no new build had been made: repeatability baseline (median frame 36 to 41 ms, Quantum update 14 to 16 ms) and proof that the URL overrides were not applied. |
| [quantum-online-perf-phone-2026-10-09.json](quantum-online-perf-phone-2026-10-09.json) | First measured online run on the user's phone (Mali-G52 MC2): about 28 fps, median frame 33 ms, p95 57 ms, Quantum update median 12.8 ms, at six heroes and 60 Hz, untuned build. Three overlay screenshots with hashes; unattributed points listed. |
| [quantum-online-two-devices-2026-10-09.json](quantum-online-two-devices-2026-10-09.json) | First online Quantum match between the Mac Editor and the phone (party code, 2 humans, 4 bots), fluid per the user, unmeasured; Quick Play pairing failure analysed from the menu code, cause not attributed. |
| [chogathon-site-facts-2026-10-09.json](chogathon-site-facts-2026-10-09.json) | Site re-read on 9 October: sign-up extended to 72 h, derived end 23 October 17:00 Paris, deliverables (shared repository, pre-existing work) and judging table quoted, page hash. |
| [fusion-archer-slice.json](fusion-archer-slice.json) | Première tranche Archer portée dans Fusion : règles, présentation, bot, huit contrôles Photon, build WebGL et parcours Chrome bureau/tactile émulé ; captures, empreintes et limites. |
| [quantum-to-fusion-reuse-inspection.json](quantum-to-fusion-reuse-inspection.json) | Inspection préalable des six compétences, caméra et dépendances des trois personnages ; empreintes conservées. Graphe Unity avec composants Quantum et références non résolues, pas un paquet importable. Ce relevé statique précède le portage Archer. [Comparaison des samples](../../docs/ARCHITECTURE.md#31-reprendre-lexpérience-du-twin-stick-shooter). |
| [fusion-sdk-qualification.json](fusion-sdk-qualification.json) | Archive fournie par l'utilisateur, SDK 2.1.3 build 2390 importé, AppID configuré et compilation C# Editor réussie. Un patch source ciblé et 20 métadonnées d'images modifiées ; Build WebGL réussi ; limites dans le relevé web. |
| [fusion-sdk-baseline-sha256.json](fusion-sdk-baseline-sha256.json) | Empreinte des 542 fichiers SDK extraits avec leurs GUID ; référence avant import Unity et correctif. |
| [fusion-webgl-qualification.json](fusion-webgl-qualification.json) | Build WebGL/IL2CPP réussi, 12,7 Mo, MIME/gzip vérifiés ; menu, entraînement et deux joysticks essayés dans Chrome avec émulation tactile. Captures et empreintes ; aucun FPS téléphone ou multijoueur navigateur validé. |
| [fusion-network-qualification.json](fusion-network-qualification.json) | Six contrôles passent via le cloud Photon : serveur et deux clients dans un même processus Unity, physiques séparées ; autorité, mouvement, dégâts, départ et nouvel arrivant. Pas de reprise authentifiée ou d'essai téléphone. |
| [fusion-acquisition-2026-10-08.json](fusion-acquisition-2026-10-08.json) | Premier refus de téléchargement web conservé ; suivi documentant l'archive locale fournie ensuite, son import et la connexion cloud réussie. |
| [network-engine-decision-2026-10-08.json](network-engine-decision-2026-10-08.json) | Décision Fusion 2 Server : sources primaires, répartition du calcul, autorité, tarifs identiques Quantum/Fusion et coûts serveur séparés ; aucune performance ou capacité non mesurée revendiquée. [Décision active](../../docs/DECISION-RESEAU.md). |
| [unity-6000.6-qualification.json](unity-6000.6-qualification.json) | Éditeur 6000.6.4f1 arm64 et WebGL présents, corrections compilées, redémarrage sans erreur et sample local fonctionnel confirmés par l'utilisateur. Build et réseau non validés. |
| [web-touch-controls-qualification.json](web-touch-controls-qualification.json) | Adaptation des contrôles tactiles, visée au relâchement, capture des doigts et remise à zéro ; quatre assemblages compilés avec Unity réel. Neuf tests exécutés et passés dans Unity. Aucun essai téléphone. |
| [quantum-headless-qualification.json](quantum-headless-qualification.json) | SDK .NET local, 27 tests catalogue et 14 tests d'intégration avec Quantum 3.0.9.1932, dont un vrai Frame ; aucune session de gameplay, propriété authentifiée ou replay complet validé. |
| [quantum-sample-import.json](quantum-sample-import.json) | ZIP fourni ensuite par l'utilisateur : provenance locale, version réelle 2021.3.45f2, CRC et hashes vérifiés, 6 656 fichiers extraits. |
| [quantum-sample-baseline-sha256.json](quantum-sample-baseline-sha256.json) | Empreinte de chaque fichier d'origine, permettant d'examiner les modifications/migrations ultérieures. |
| [quantum-sample-inspection.json](quantum-sample-inspection.json) | Code réellement examiné, versions, contrôles mobiles WebGL manquants, deux slots, points d'intégration Genesis et prérequis encore absents. Aucun jeu exécuté. |
| [p0-qualification.json](p0-qualification.json) | Premier diagnostic avant réception du ZIP : bibliothèque C# exécutée, 27 tests passés et empreintes des sources. La disponibilité du sample est actualisée dans les relevés ci-dessus. |
| [prototype-genesis-bindings.json](prototype-genesis-bindings.json) | Trois lectures tokenURI au même bloc Monad pour les identifiants canoniques des fixtures 512, 561 et 973. |
| [prototype-resolved-heroes.json](prototype-resolved-heroes.json) | Sortie réelle du module C# pour les trois fixtures : profils, versions, hashes et explications des règles. |
| [genesis-trait-census-first240.json](genesis-trait-census-first240.json) | Photographie historique des 240 premières lignes du corpus ; son SHA256 concerne ce préfixe, conservé dans le fichier append-only. |
| [moba-development-plan-facts.json](moba-development-plan-facts.json) | Recherche de préparation du plan : faits, limites de collecte, références de conception et responsabilités réseau ; recommandations séparées des validations. |
| [genesis-metadata.jsonl](genesis-metadata.jsonl) | 302 réponses JSON brutes de metadata Genesis, avec URI, date et SHA256 ; identifiants de metadata, pas tokenId canoniques. |
| [genesis-trait-census.json](genesis-trait-census.json) | Recensement partiel reproductible des 302 entrées sur 1 969 attendues, absences de traits, tiers, exemples et identifiants manquants. Aucune extrapolation. |
| [genesis-metadata-gateway-probe.json](genesis-metadata-gateway-probe.json) | Premier essai réussi de lecture JSON via Pinata et échecs d'autres passerelles ; comprend les traits de CHOG #465 associé au token 973 par le relevé RPC antérieur. |
| [chogathon-site-facts.json](chogathon-site-facts.json) | Données factuelles du site, empreinte SHA-256 de la page consultée, calcul du calendrier et distinction entre affichage préchargé et concurrence vérifiée. |
| [chog-contract-requests.json](chog-contract-requests.json) | Requêtes de lecture du nom, de l'offre, des interfaces et d'une URI, accompagnées des lectures réseau et bloc. |
| [chog-contract-responses.json](chog-contract-responses.json) | Réponses JSON-RPC brutes à ce premier lot. Les appels utilisent `latest` et ne sont pas garantis atomiquement au même bloc. |
| [chog-token-uri-requests.json](chog-token-uri-requests.json) | Cinq requêtes `tokenURI` fixées au bloc public 111 148 251 (`0x69ffcdb`). |
| [chog-token-uri-responses.json](chog-token-uri-responses.json) | Réponses brutes pour les identifiants 1, 512, 973, 1053 et 1848. |
| [chog-contract-facts.json](chog-contract-facts.json) | Décodage des résultats et limites de leur portée. |
| [redgnad-public-repositories.json](redgnad-public-repositories.json) | Inventaire de 36 dépôts publics obtenu par l'API GitHub, avec branche et date de mise à jour. |
| [redgnad-code-review-manifest.json](redgnad-code-review-manifest.json) | Cinq fichiers examinés, commits, liens permanents et empreintes du contenu lu. |
| [genesis-ChogMint.sol](genesis-ChogMint.sol) | Source `src/ChogMint.sol` extraite de la page publique Monadscan, affichée vérifiée « Exact Match ». |
| [genesis-abi.json](genesis-abi.json) | ABI publique extraite de la même page. |
| [genesis-reveal-requests.json](genesis-reveal-requests.json) | Requêtes de bloc, `revealed()`, `baseURI()` et `totalSupply()` ; lectures `latest`, non atomiques. |
| [genesis-reveal-responses.json](genesis-reveal-responses.json) | Réponses brutes au second relevé. |
| [genesis-mutability-facts.json](genesis-mutability-facts.json) | Résultats décodés, empreintes du code et de l'ABI, constats sur l'évolution et limites. |
| [production-reference-facts.json](production-reference-facts.json) | Références primaires de production et de mécanismes ; distinction entre prototype, produit commercial et résultat utilisateur. |
| [mobile-game-design-facts.json](mobile-game-design-facts.json) | Marché mobile 2025/H1 2026, fiches éditeur, incertitude en game design, PWA/Telegram ; mesures distinctes, accès effectif et limites. |
| [moba-feasibility-and-reuse.json](moba-feasibility-and-reuse.json) | Format MOBA 3D confirmé, copie locale de chogtanks-dev et empreintes, templates Photon, Reown/WebGL et limites de la lecture statique. |
| [chog-erc20-requests.json](chog-erc20-requests.json), [réponses](chog-erc20-responses.json), [constats](chog-erc20-facts.json) | Lectures publiques du token $CHOG, adresse officielle, proxy minimal et limites de l'inspection ; précédents Yuga pour les paiements en APE et la validation des scores. |

Le second relevé confirme `revealed = true` et une `baseURI` IPFS. Dans le code vérifié, `setBaseURI` retourne une erreur `MetadataFrozen` après révélation ; aucune fonction d'évolution, d'XP ou de niveau n'est exposée. La collection d'origine n'est donc pas évolutive au sens du mécanisme proposé. Un état de personnage conservé dans un contrat ou service distinct peut cependant évoluer sans modifier les métadonnées Genesis. Cet examen ne constitue pas un audit de sécurité indépendant.

La réponse `true` pour ERC-721 et `false` pour ERC-4907 reflète le comportement déclaré de `supportsInterface`. Elle ne constitue pas un audit du contrat ou une preuve d'absence de toute autre forme de délégation.

Les URI observées pointent vers un CID IPFS commun et des numéros de fichier différents des token IDs. Deux passerelles ont renvoyé une notice de fonctionnement par service worker ; une troisième a expiré. Aucun contenu JSON IPFS de la collection n'a été récupéré. Les traits mentionnés dans le dossier sont lus sur OpenSea, pas extrapolés de ces URI. Aucune distribution complète des traits, concentration de propriété ou activité des détenteurs n'a été mesurée.

Les pages du hackathon et de Chog Builders ont été lues directement par HTTPS, après échec du lecteur web. La page du hackathon contient aussi des interfaces d'administration et des configurations client ; seules les informations publiques utiles au cadrage sont conservées dans la synthèse. Aucun accès administratif ou appel à une base de données applicative n'a été effectué.

La lecture du GitHub distingue documentation et code. L'arborescence récursive de CHOGTANKS est tronquée par l'API ; le fichier choisi a toutefois été récupéré directement à son commit. Aucun résultat de test, déploiement, volume ou usage annoncé par ces dépôts n'a été vérifié pendant cette recherche.

La littérature a été sélectionnée pour ses mécanismes, ses méthodes et ses limites, avec des résultats qui nuancent les hypothèses favorables. Les accès effectifs sont précisés dans la bibliographie. Les dates d'indexation des résultats web ne sont pas des dates de publication. Les pages commerciales attestent des fonctionnalités décrites, pas leur succès.

Pour reproduire les lectures fixées au bloc, envoyer le fichier `chog-token-uri-requests.json` au RPC indiqué dans `chog-contract-facts.json`, avec le type de contenu `application/json`. Certains points RPC peuvent ne plus servir l'état historique de ce bloc ; un fournisseur archive peut alors être nécessaire. Aucun identifiant privé ou accès portefeuille n'est requis.
