# Réexamen du moteur réseau — 8 octobre 2026

**Décision de développement : Unity + Photon Quantum 3 pour le MOBA Genesis.**
Le projet `game/` redevient la base du jeu ; `game-fusion/` est conservé comme preuve
technique et solution de repli, sans développement parallèle de contenu. Cette décision
remplace le choix Fusion Server du début de journée. Elle n'est pas motivée par la seule
quantité de code déjà écrite : elle compare le produit voulu, les coûts certains et les
risques que nous pouvons mesurer avant d'étendre le jeu.

## Pourquoi ce choix

| Critère | Quantum 3 | Fusion 2 Server | Conclusion pour le périmètre actuel |
| --- | --- | --- | --- |
| Expérience de combat | Sample complet déjà essayé par l'utilisateur : trois héros, six capacités, bots/stratégie d'équipe et mode de match. Build WebGL local lancé dans Chrome. | Une tranche Archer, un bot d'entraînement et des contrôles réseau Editor ; expérience du sample et match MOBA à reconstruire. | Quantum fournit une référence jouable immédiatement, ce qui laisse plus de cycles aux Genesis, à la boucle MOBA et aux essais. |
| Mobile/PWA | Photon documente l'export web et Android du sample. Chaque client simule les frames vérifiées ; le build local tourne sur Redmi 11C selon l'utilisateur, avec quelques saccades. Aucun temps d'image n'est mesuré. | Le serveur peut calculer bots, minions et combat ; le client prédit son héros. WebGL en client/serveur exige une option explicite ; Photon recommande Shared pour mobile/WebGL et lui attribue une meilleure latence que Host/Server. Le gain sur Redmi n'est pas mesuré. | L'offload Fusion est un bénéfice architectural réel, mais pas une performance démontrée pour notre petite arène 3v3. La topologie WebGL ajoute une contrainte certaine. |
| Genesis, achat $CHOG, carrière | Backend indépendant du moteur pour propriété, profils et achats. Photon Quantum propose des webhooks pour valider `RuntimePlayer`/config, un GameResult webhook et un runner de replay pour contrôler les résultats avant crédit définitif. Ces parcours restent à implémenter et tester. | Profil et résultat peuvent venir directement d'un serveur de partie que nous opérons. Admission, achats, idempotence et backend restent à construire. | Les achats n'imposent pas Fusion ; ils doivent être confirmés hors tick sous les deux SDK. Sans gain monétaire de match ou information secrète forte au MVP, la vérification Quantum est acceptable comme architecture cible, sous réserve de la livrer. |
| Exploitation et coût | Photon Cloud et notre backend ; pas de parc de serveurs de partie pour le MVP. | Photon Cloud **plus** builds Linux, hébergement, orchestration, santé et dimensionnement des parties. | Le surcoût Fusion est certain, sa capacité pour 100 CCU ne l'est pas encore. |
| Évolutivité | Simulation déterministe, bots et capacités pilotées par données ; contenu MOBA à ajouter. Tous les clients connaissent l'état nécessaire à la simulation, ce qui limite un vrai brouillard de guerre secret. | Autorité serveur et état caché plus naturels ; contenu MOBA à ajouter. | Quantum garde un plafond suffisant pour le produit confirmé. Un futur besoin de secrets de jeu ou de récompenses monétaires serait un nouveau critère. |

Sources primaires : [sample Twin Stick Shooter](https://doc.photonengine.com/quantum/v3/game-samples/twin-stick-shooter),
[topologies Fusion/WebGL](https://doc.photonengine.com/fusion/v2/fusion-choose),
[prédiction sélective Fusion](https://doc.photonengine.com/fusion/v2/manual/advanced/time-synchronization),
[protection, webhooks et résultats Quantum](https://doc.photonengine.com/quantum/v3/manual/cheat-protection),
[runner de replay Quantum](https://doc.photonengine.com/quantum/v3/manual/quantum-project),
[serveur dédié Fusion](https://doc.photonengine.com/fusion/v2/concepts-and-patterns/dedicated-server-overview).
Preuves locales : [build et lancement Quantum dans Chrome](../research/evidence/quantum-webgl-local-2026-10-08.json),
[tranche Fusion et limites](../research/evidence/fusion-archer-slice.json).

Ce tableau distingue les capacités documentées des mesures. Il ne prétend ni que Quantum
est plus rapide sur mobile ni que son webhook rend un résultat client automatiquement
incontestable. La protection de la progression suppose admission vérifiée, résultats
comparés et rejeu côté backend des cas litigieux ; une simple déclaration du navigateur
ne crédite pas la carrière.

## Périmètre borné du travail

1. **Verrou mobile/web :** premier essai Redmi 11C positif, avec un peu de lag rapporté.
   Mesurer maintenant chargement, stabilité, deux doigts, temps d'image et mémoire. Refaire avec une charge
   représentative du futur match ; la comparaison directe avec l'Archer Fusion actuel
   serait trompeuse. Si le coût vient surtout du rendu/asset, optimiser ce coût avant de
   changer de moteur. Si la simulation Quantum empêche un match stable sur l'appareil,
   réexaminer Fusion avant d'ajouter le contenu MOBA.
2. **Premier match en ligne :** AppId Quantum 3 distinct, deux navigateurs/appareils,
   reconnexion, bots qui remplacent les places libres ; ne pas compter l'essai local avec
   bots comme preuve réseau.
3. **Partie MOBA minimale :** une voie, 3v3, vagues, tour, noyau, mort/respawn, victoire,
   résultat et revanche. Réutiliser les capacités et bots utiles du sample, remplacer
   Coin Grab ; ne pas porter un second mode pour « compléter le sample ».
4. **Genesis et économie :** admission liée au tokenId canonique et à la propriété ; deux
   héros de test aux traits visibles, puis couverture du catalogue réel ; achat $CHOG
   confirmé avant le match ; profil figé pendant la partie ; résultat attribué une fois
   après contrôle. Les interfaces de catalogue/services restent indépendantes de Photon.
5. **Finition et preuve :** mobile, lisibilité, son, tutoriel court, test détenteurs,
   stabilité et déploiement. Les assets du sample sont des références internes ; vérifier
   leur licence individuelle avant une distribution publique.

Ce périmètre exclut du premier match : second mode, classement à enjeux monétaires,
brouillard de guerre secret, éditeur général de compétences et catalogue commercial large.
Les extensions peuvent être ajoutées après une partie complète testée. Les dates du plan
précédent étaient des hypothèses ; les critères de passage et mesures décident du jalon.
