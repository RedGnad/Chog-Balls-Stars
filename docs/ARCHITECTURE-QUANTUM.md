# Architecture active — MOBA Genesis avec Quantum 3

Décision du 8 octobre 2026 : [réexamen réseau](DECISION-RESEAU-REVUE.md). Ce document
décrit **la cible**, pas des services déjà livrés. L'état prouvé est dans [STATUS](STATUS.md).
Le sample local dans `game/` est la base de combat ; `game-fusion/` est conservé pour
comparaison, sans partager ses composants réseau avec le jeu Quantum.

## Frontières

| Couche | Responsabilité | Vérité acceptée |
| --- | --- | --- |
| `HeroCatalog` / `HeroResolver` partagés | Convertir contrat, tokenId canonique, traits présents/absents, tier et règles versionnées en profil de héros. | Metadata de référence et règles versionnées ; jamais le numéro CHOG affiché comme tokenId. |
| Simulation Quantum | Inputs, capacités, PV, bots, minions, tours, noyaux, mort/respawn et victoire. | Profil de match admis ; état déterministe et inputs confirmés. |
| Présentation Unity | Modèles, caméra, visée, HUD, son, télégraphes et contrôles tactiles. | Frames/événements Quantum ; ni animation ni interface ne décident des dégâts. |
| Services de jeu | Wallet, propriété Genesis, droit d'équiper, carrière et version de catalogue. | Preuve de signature, lecture de propriété et base du service. |
| Commerce | Achat $CHOG et spécialisation déverrouillée. | Événement de transaction confirmé et indexé ; aucun hash fourni par le client seul. |
| Validation des résultats | Admission, comparaison des résultats, rejeu en cas de divergence, crédit idempotent. | Webhook/réplay vérifiés ; jamais une victoire déclarée par une seule page. |

La simulation déterministe tourne sur les clients Quantum. Photon transporte/confirme
les inputs ; le backend authentifie le joueur et contrôle `RuntimePlayer` ainsi que les
configurations de session au moyen de webhooks. Le profil d'un match est figé avant le
premier tick. La sélection montre pourquoi les traits et le tier donnent une classe,
une signature et une variante ; les règles de gameplay restent dans [PLAN](PLAN.md).

Le résultat visible peut être affiché immédiatement, mais la carrière n'est créditée
qu'après vérification. Le premier niveau compare les résultats transmis par les clients
au GameResult webhook ; si le résultat est absent, contradictoire ou litigieux, le service
doit rejouer l'input stream avec le runner Quantum hors Unity avant de le confirmer.
Ce système est **à développer et tester** ; Quantum Public Cloud ne valide pas à lui seul
la logique du résultat. La politique de rétention des replays, les délais et les cas de
déconnexion doivent être définis avant la progression publique.

L'achat $CHOG est une opération de lobby : contrat, indexeur et backend confirment
l'acquisition ; la simulation reçoit seulement l'identifiant de variante autorisée.
Aucune requête RPC blockchain n'entre dans le tick. Un transfert de NFT modifie le
droit d'entrer dans un match suivant, pas un match en cours. La carrière est indexée par
chaîne, collection et tokenId canonique, indépendamment du propriétaire courant.

## Exécution et tests

Le client Unity WebGL est servi par HTTP(S) avec les en-têtes gzip corrects. Une enveloppe
PWA apporte installation et reprise d'interface, sans promettre qu'un match continuera
en arrière-plan sur mobile. La cible est une vue haute 3D/isométrique en paysage et deux
contacts simultanés. Le sample contient déjà des contrôles tactiles adaptés, mais leur
qualité sur Redmi 11C reste à mesurer.

Premier chemin critique : match local sur Redmi → match Quantum en ligne entre deux
appareils → 3v3 avec bots, minions, tour et noyau → profils Genesis admis → résultat
vérifié → achat $CHOG → essais détenteurs. Les invariants de propriété, achat et crédit
idempotent ont leurs tests indépendants ; un build qui charge ne valide pas ces parcours.

Le choix moteur est réexaminé uniquement sur un défaut mesuré qui change sa justification :
simulation Quantum trop coûteuse sur l'appareil cible à charge représentative après
optimisation raisonnable, ou besoin nouveau d'état secret/autorité de combat immédiate.
Une baisse causée par le rendu ou les assets se corrige dans la présentation ; elle ne
prouve pas que Fusion serait plus rapide. [Plan de mesure](DECISION-RESEAU-REVUE.md#périmètre-borné-du-travail).

Sources techniques : [simulation et frames Quantum](https://doc.photonengine.com/quantum/v3/manual/frames),
[webhooks et résultats](https://doc.photonengine.com/quantum/v3/manual/cheat-protection),
[runner hors Unity](https://doc.photonengine.com/quantum/v3/manual/quantum-project),
[profilage Quantum](https://doc.photonengine.com/quantum/v3/manual/profiling).
