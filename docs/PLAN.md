# Plan de développement — MOBA Genesis

Version de travail actualisée le 8 octobre 2026. **Choix de développement réexaminé :
Unity + Quantum 3, dans `game/`.** [Décision et travail borné](DECISION-RESEAU-REVUE.md).
Le sample avec bots est construit en WebGL (~22,2 Mo), le menu et une partie locale
s'affichent dans Chrome ; l'utilisateur rapporte qu'il fonctionne sur Redmi 11C avec
quelques saccades. Performance mesurée, multijoueur Quantum et résultats validés restent
à faire. Le prototype Fusion Server Archer est conservé comme référence technique.
Les 27 tests catalogue, 14 tests Quantum et neuf tests Unity documentent le travail précédent.
[Décision réseau actuelle](DECISION-RESEAU-REVUE.md) ; [comparaison Fusion antérieure](DECISION-RESEAU.md).
Les choix confirmés par l'utilisateur sont distingués des recommandations de conception.
L'avancement réel est dans [STATUS.md](STATUS.md), les frontières techniques dans
[ARCHITECTURE-QUANTUM.md](ARCHITECTURE-QUANTUM.md). Ce document remplace les anciens dossiers d'idéation.

## 1. Produit et ambition

**Faire de chaque Genesis un héros que son propriétaire reconnaît dans sa façon de combattre,
fait évoluer et retrouve d'une partie à l'autre.** Jeu d'action en équipe, mobile, immédiatement
compréhensible, avec des moments spectaculaires produits par les interactions de combat.

Confirmé : MOBA multijoueur en vue du dessus 3D/isométrique ; préférence PWA ; Unity possible ;
Genesis centraux, traits et rareté fonctionnels ; $CHOG comme monnaie payante ; placeholders
visuels au début ; prototype rapide et possibilité d'enrichir le produit après cette étape.
Le plafond de 100 connexions Photon simultanées est accepté pour le MVP ; l'application
PUN2 chogtanks existante doit rester en service avec son AppId et son forfait distincts.

Recommandation : une première carte à une voie, **3 contre 3**, trois archétypes, attaque de
base et deux compétences. Détruire le noyau adverse, protégé par une tour ; des vagues de
minions permettent d'avancer. Sessions visées de 4 à 6 minutes, durée maximale initiale de
8 minutes. Ces nombres sont des paramètres de départ à mesurer, pas des résultats établis.

Le prototype rapide garde la même boucle : sélectionner son héros, entrer en partie, se battre,
détruire la base, voir le résultat et rejouer. Les premières versions ont moins de variantes,
moins d'effets et une progression limitée. Elles utilisent les fondations du produit final.

Le plafond vient de trois extensions compatibles avec cette base : davantage de signatures
de combat issues des traits, davantage de spécialisations persistantes de chaque Genesis,
et davantage d'interactions entre héros et objectifs. Une nouvelle carte ou un 5v5 demande
en revanche un nouveau travail de level design, d'équilibrage et de performance.

## 2. Ce que la recherche change concrètement

| Observation ou référence | Conséquence pour notre projet | Limite |
| --- | --- | --- |
| Le barème Chogathon donne 30 % à l'interaction nouvelle avec le NFT, 20 % au fun et 20 % à la rétention ; l'intégration NFT est requise. | Démontrer tôt deux Genesis qui se jouent différemment, puis une histoire persistante attachée au héros. | Aucun score de jury futur n'est prédit. |
| Genesis : 1 969 NFT ; metadata gelées après révélation dans le contrat examiné. | Conserver les traits d'origine et stocker l'évolution du jeu sous l'identité canonique du NFT. | L'image et les traits du contrat Genesis ne deviennent pas modifiables. |
| 302 JSON Genesis récupérés, couvrant les cinq tiers ; le Legendary 1:1 de ce corpus ne comporte que Base et Tier. | Prévoir des règles explicites pour l'absence d'un trait et des fiches individuelles pour les 1:1. | Corpus partiel, non aléatoire ; aucune fréquence extrapolée aux 1 969 NFT. |
| Le sample Quantum acquis fournit combat top-down, capacités, équipes et bots ; il se lance en WebGL sur Mac et, selon l'utilisateur, fonctionne sur Redmi 11C avec quelques saccades. | Étendre ce jeu vers un match MOBA et les profils Genesis ; garder simulation et contrôles indépendants des services. | Aucun FPS, mémoire ou multijoueur téléphone mesurés ; Coin Grab n'est pas encore le MOBA. |
| Fusion Server peut limiter la prédiction au héros local et produire un résultat immédiatement côté serveur. | Conserver le prototype comme repli documenté, sans porter davantage de contenu avant une raison mesurée. | Aucun gain de FPS sur Redmi établi ; serveur de partie et orchestration ajoutent un coût certain. |
| MDA relie règles, comportements émergents et expérience ressentie. | Tester la lisibilité, les sensations et les situations produites par une mécanique avant d'en multiplier les variantes. | Cadre conceptuel de 2004, pas méta-analyse ni preuve de succès commercial. |
| Supercell a réduit les bonus de Hypercharge en octobre 2025 en invoquant le contre-jeu. | Un pouvoir spectaculaire doit annoncer son danger et laisser une réponse à l'adversaire. | Une décision d'un studio, pas une règle universelle démontrée. |
| Dota 2 a supprimé les Facets dans le patch 7.41 après leur introduction en 7.36. | Ne pas présenter l'empilement de variantes comme une recette de succès ; borner les combinaisons et mesurer leur utilité. | Le patch établit la suppression, pas sa cause. |

Sources : [règles Chogathon](https://chogathon.chog.xyz/),
[contrat et lectures](../research/evidence/genesis-mutability-facts.json),
[recensement partiel des traits](../research/evidence/genesis-trait-census.json),
[sample Photon](https://doc.photonengine.com/quantum/v3/game-samples/twin-stick-shooter),
[résultats et webhooks Quantum](https://doc.photonengine.com/quantum/v3/manual/cheat-protection),
[prédiction Fusion](https://doc.photonengine.com/fusion/v2/manual/advanced/time-synchronization),
[MDA](https://www.cs.northwestern.edu/~hunicke/pubs/MDA.pdf),
[Supercell](https://supercell.com/en/games/brawlstars/blog/game-updates/release-notes-october-2025/),
[patch officiel Valve 7.41](https://www.dota2.com/datafeed/patchnotes?version=7.41&language=english).

Ces références donnent des contraintes et des précédents. Les règles originales ci-dessous
sont des propositions de design ; leur intérêt sera établi par le prototype et les essais.
Les sources complémentaires, limites de collecte et observations sont consignées dans
[le relevé de cette étape](../research/evidence/moba-development-plan-facts.json).

Dans les 302 entrées récupérées, **291 ont Base = Origin** : utiliser seulement Base pour les
classes concentrerait presque tout ce corpus sur un seul rôle. Les sept Rare observés sont
Skull et les trois Epic sont Radioactive ; lignée et tier peuvent donc décrire une même
particularité. Le mapping proposé exploite Body/Head pour varier les rôles et traite les
couples Base/Tier ensemble, au lieu de cumuler deux bonus supposés indépendants. Cela reste
à vérifier sur le répertoire complet.

## 3. Une minute de jeu, précisément

1. Le joueur ouvre le lien. Une scène d'entraînement légère permet de toucher les commandes.
   Pour jouer son Genesis, il connecte son wallet au lobby ; ses NFT apparaissent avec leur
   rôle et une démonstration courte de leur compétence signature. Le chargement de l'arène
   peut avancer pendant la sélection.
2. Il appuie sur Jouer, seul ou dans un salon à code. Le serveur vérifie son droit de jouer
   le NFT et prépare son profil. Les places restantes peuvent être occupées par des bots
   identifiés comme tels. La composition n'impose pas d'attendre un rôle précis.
3. En paysage : joystick gauche pour se déplacer ; bouton d'attaque à droite, visée assistée
   au toucher et manuelle en glissant ; deux boutons de compétence avec portée affichée.
   Sur ordinateur : déplacements clavier, souris, deux touches. Aucune rotation manuelle
   de caméra n'est nécessaire.
4. Le joueur escorte une vague, esquive un tir, utilise sa compétence de classe pour entrer
   ou sortir du combat, puis déclenche sa **signature Genesis**. Les adversaires voient
   l'annonce au sol, la direction et l'effet ; les règles de ciblage sont lisibles.
5. La tour choisit d'abord les minions à portée ; frapper un héros protégé par la tour donne
   l'aggro à l'attaquant. La mort déclenche une réapparition courte. La destruction de la
   tour ouvre le noyau ; détruire le noyau termine la partie sur tous les clients.
6. Le résultat s'affiche immédiatement ; la progression persistante apparaît confirmée
   après vérification. Le bouton Rejouer est direct. Un achat ou un changement de
   spécialisation a lieu au lobby.

Réglages de départ : première vague après 5 s puis toutes les 20 s ; trois minions par vague ;
réapparition 6 s ; protection au spawn annulée par une action offensive. À 6 minutes, la
pression de siège augmente de manière annoncée et symétrique. À 8 minutes, départage par
noyau restant puis tour restante ; égalité exacte = match nul. Le périmètre ne nécessite pas
encore de boutique d'objets pendant la partie, de last-hit obligatoire, de jungle complète
ou de brouillard de guerre. Ces absences sont des choix de première version, pas des limites
des structures de données.

Règles initiales de combat à rendre explicites dans les premières capacités : pas de dégâts
alliés ; obstacles bloquant les projectiles ordinaires ; les ralentissements ne s'additionnent
pas, le plus fort s'applique ; un même impact ne touche une cible qu'une fois. Les effets
aller-retour ont deux phases identifiées, les effets périodiques une cadence définie. La
visée assistée privilégie un héros ennemi visible à portée, puis la cible valide la plus
proche ; la visée manuelle permet de tirer dans une direction libre. Les contrôles de foule,
leurs durées et les exceptions de ciblage appartiennent au catalogue de capacités.

La signature utilise d'abord un cooldown simple. Une jauge d'éveil alimentée par les combats
et objectifs est une amélioration candidate après les premières parties. Le hasard n'est pas
ajouté uniquement pour rappeler le casino : on teste d'abord si l'anticipation des pouvoirs,
les prises de risque et les renversements de situation suffisent à produire l'expérience voulue.

## 4. Du vrai NFT à un héros jouable

### 4.1 Trois couches distinctes

**Origine** : contrat, tokenId canonique, metadataId, traits bruts et image source.
**Profil de combat** : classe, attaque, compétence de classe, signature et variante active,
résolus à partir d'un catalogue de règles versionné.
**Carrière** : maîtrise, spécialisations acquises, historique et apparence du héros dans le jeu.

L'origine reste stable. Les règles peuvent être équilibrées entre deux versions. La carrière
suivra le NFT lors d'un transfert selon la politique proposée ; le classement personnel et les
préférences de contrôle suivent le joueur. Cette distinction doit être visible au lobby.

### 4.2 Rôle des traits

La table suivante fixe les responsabilités proposées. **L'affectation de chaque valeur réelle
sera une table de contenu relue, pas une règle implicite fondée sur le numéro du NFT.**

| Donnée | Effet prévu | Première livraison |
| --- | --- | --- |
| Body + Head | Déterminent un archétype et sa compétence de classe par table explicite. Body donne le rôle principal ; des exceptions Head nommées sont possibles. | Trois rôles ; priorité et exceptions documentées ; aucune obligation d'avoir les deux traits. |
| Eyes + Aura | Déterminent la famille de signature : trajectoire, zone ou effet de contrôle. La règle explique quel trait a été déterminant. | Trois familles de signature au maximum ; une absence d'aura donne une variante neutre intentionnelle. |
| Base + Form | Déterminent la lignée et les exceptions structurelles, notamment Skull, Radioactive et 1:1. | Résolution explicite des lignées et des fiches 1:1 ; pas de pénalité pour un trait manquant. |
| Tier | Modifie le fonctionnement de la signature selon une expression de rareté. | Une expression par tier ; même nombre de boutons et de slots. |
| Autres traits | Identité visuelle, portrait et éléments de présentation ; certains pourront acquérir un rôle dans une version ultérieure. | Les effets de combat déjà actifs sont affichés clairement ; les autres traits ne promettent pas un bonus caché. |
| Maîtrise + achat $CHOG | Ouvrent une spécialisation compatible avec les gènes du héros. | Une alternative jouable à prévisualiser ; une seule variante équipée par slot. |

Trois archétypes proposés : **Brute** (proximité, charge et occupation de l'espace), **Tireur**
(projectiles, portée et repositionnement), **Perturbateur** (portée intermédiaire et contrôle
d'une zone). Ces noms, les capacités et leurs valeurs sont de la conception à tester. Ils ne
sont pas présentés comme les personnages déjà livrés avec le sample Photon.

Exemple réel de données : le token canonique `973` pointe sur `CHOG #465`, qui possède notamment
`Aura: Fire` et `Body: Black Trackset`. Une règle de design peut lui attribuer une signature
de feu. Le nom « Fire » est observé ; son effet de combat est notre création.

### 4.3 Une rareté qui change le geste

Proposition à tester : **la rareté transforme la manière de délivrer le pouvoir**. Le commun
garde une signature complète, immédiate et fiable. Les autres expressions échangent une
part de cette simplicité contre d'autres possibilités.

| Tier observé | Expression candidate de la signature | Contrepartie à régler |
| --- | --- | --- |
| Common | Une frappe directe, un impact principal. | Peu de contrôle prolongé ; meilleure immédiateté. |
| Uncommon | Un écho retardé du même effet. | Puissance répartie entre deux impacts ; second impact anticipable. |
| Rare | Un effet aller-retour, avec retour lié à la position du lanceur. | Le rendement dépend du placement ; première frappe moins forte. |
| Epic | Une zone persistante issue de l'effet. | Dégâts répartis dans le temps ; l'adversaire peut sortir de la zone. |
| Legendary / 1:1 | Une signature attribuée individuellement à partir de primitives existantes ; exemple candidat : inversion poussée/attraction. | Budget de portée, dégâts, durée et cooldown propre ; fiche obligatoire pour chaque 1:1. |

Le tableau définit des directions mécaniques, pas une garantie d'équilibre. Une variante
avec le même total de dégâts peut être beaucoup plus forte par son contrôle ou sa facilité
d'utilisation. Chaque variante a donc des cibles, une annonce, une portée et des contres
décrits ; on mesure ses usages et ses interactions. La valeur marchande et le rang de rareté
d'une marketplace ne déterminent pas les statistiques.

**Prototype** : deux Genesis réels, deux comportements visibles, une seule famille de signature.
**Version complète initiale** : toute la collection peut être résolue ; trois archétypes et
quelques familles suffisent pour que des NFT différents partagent certaines briques. On
n'annonce pas 1 969 gameplay uniques. Les cinq tiers ont un comportement défini ; la variante
de rareté remplace la forme de la signature, elle ne rajoute pas une cascade de bonus.

La bibliothèque est volontairement bornée : trois archétypes × trois familles × cinq expressions
donnent **45 cellules de compatibilité potentielles** à examiner, avant les exceptions 1:1.
Toutes n'ont pas à être utilisées ; les interactions entre personnages demandent également
des essais. Ce calcul mesure une taille de catalogue, pas une preuve de maîtrise de l'équilibrage.

### 4.4 Résolution sans surprise

- Entrée : NFT canonique + metadata de référence + version de règles + spécialisation possédée.
- Sortie : `HeroDefinition` avec identifiants de capacités et explication lisible de leur origine.
- Chaque champ absent a une règle nommée ; chaque 1:1 a une fiche. Un trait inconnu lors d'une
  mise à jour signale un catalogue à corriger ; il ne devient pas secrètement un héros aléatoire.
- Une validation parcourt tous les NFT récupérés, vérifie les slots, les références, les
  compatibilités et les exceptions ; l'ouverture à tous les détenteurs exige une couverture
  complète du répertoire et le contrôle des identifiants on-chain.
- Le profil d'une partie est figé au départ. Un patch, un achat ou un changement de wallet
  ne modifie pas les capacités pendant cette partie.

## 5. Évolution et place de $CHOG

La carrière est attachée au Genesis. La maîtrise gagne des paliers après des matchs validés ;
elle peut ouvrir des spécialisations et retracer les exploits du héros. Les conditions de
progression viennent d'événements de simulation, pas d'un compteur envoyé librement par le client.
Les premières valeurs de palier se règlent pendant les essais ; aucune cadence de retour
quotidien n'est prétendue validée.

**Premier usage payant proposé : acheter une spécialisation permanente compatible avec son
Genesis, puis l'équiper au lobby.** Exemple : pour un héros à signature de feu, transformer
un impact concentré en balayage plus large et moins intense. La spécialisation conserve
l'identité des traits et l'expression du tier ; l'achat élargit la façon de jouer le héros.
Une version de base complète est disponible sans achat et les alternatives se testent dans
l'entraînement. Une voie d'obtention par maîtrise est recommandée pour la version publique.

| Question | Réponse prévue |
| --- | --- |
| Qui paie, pour quoi ? | Le propriétaire paie un prix connu en $CHOG pour une variante nommée et montrée avant l'achat. |
| Où va le token ? | Directement du wallet à la trésorerie de jeu configurée dans le contrat de boutique. |
| À quoi est attaché l'achat ? | Au triplet chaîne / collection / tokenId ; il accompagne le héros si le NFT est transféré. |
| Quand l'effet est-il accordé ? | Après succès de la transaction et reconnaissance de l'événement d'achat confirmé par le backend. |
| Que se passe-t-il en cas de refus ou d'échec ? | Aucun déblocage ; aucun débit si la transaction a revert. Refuser une signature laisse le joueur au lobby. |
| Si le paiement réussit mais que l'API tombe ? | L'indexeur reprend au dernier bloc traité et matérialise l'achat une seule fois ; statut « confirmation en cours » consultable. |
| Si le NFT a changé de propriétaire ? | Le contrat contrôle `ownerOf` au moment de l'achat. L'accès à la prochaine partie est contrôlé de nouveau. |

Le prix, la trésorerie et le catalogue commercial sont à fixer avant déploiement. Le paiement
emploie le token $CHOG vérifié sur Monad mainnet ; les premiers tests utilisent un token de
test ou un fork local. Les fonctionnalités de burn, permit, gasless ou taxe du token ne sont
pas présumées. La boucle ne requiert aucune transaction pendant le combat.

## 6. Ordre de construction et critères de passage

Les jours ci-dessous sont une hypothèse de répartition sur 14 jours, à réviser avec les
mesures du téléphone et le premier match en ligne. Un jalon dépend de son parcours vérifié,
pas de sa date. La [décision réseau active](DECISION-RESEAU-REVUE.md) borne la réutilisation
du sample et les raisons qui justifieraient un réexamen du moteur.

| ID / cible indicative | Livraison jouable | Critère de passage |
| --- | --- | --- |
| P0 — début | Base Quantum web/mobile et réseau. | Build local chargé dans Chrome et sur Redmi ; temps d'image et mémoire relevés ; AppId Quantum 3, deux appareils en ligne, déconnexion/reprise essayées ; couverture metadata tracée. |
| P1 — premier combat Genesis | Deux Genesis de test aux traits lisibles. | Deux clients voient les mêmes capacités, dégâts, mort/respawn ; attaque + compétence de classe + signature ; faux profil refusé ; aucun wallet dans le tick. |
| P2 — partie MOBA entière | Une voie, 3v3, bots, minions, tour et noyau. | Victoire, résultat et revanche sur les deux équipes ; deux matchs successifs sans manipulation de scène ; bots poursuivent l'objectif. |
| P3 — identité et carrière | Wallet, propriété et profil admis ; progression persistante. | Ownership et tokenId canonique vérifiés ; RuntimePlayer/config contrôlés ; résultat comparé et rejoué si litigieux ; crédit une seule fois et reprise après reconnexion. |
| P4 — $CHOG | Une spécialisation achetable et utilisable. | Transaction et événement confirmés ; refus, doublon et panne/reprise testés ; aucun avantage appliqué avant confirmation ; couverture des tiers et cas 1:1 vérifiée. |
| P5 — expérience | Contrôles mobile, chargement, tutoriel court, sons, lobby et revanche. | Test détenteurs, défauts prioritaires corrigés, charge 3v3 mesurée sur appareil cible, stabilité de session. |
| P6 — soumission | Démo accessible et explication concise. | Vidéo de trois minutes au plus, licences et réutilisation déclarées, installation/reprise vérifiées, budget et exploitation documentés. |

**État P0 : partiel.** Le build local Quantum avec bots a réussi sous Unity 6000.6.4f1
et le match démarre dans Chrome. L'utilisateur indique qu'il tourne sur Redmi 11C, assez
fluide avec quelques saccades ; aucun FPS ou coût de simulation n'a été mesuré. L'AppID
Quantum 3 est configuré et l'utilisateur rapporte que la création de salon fonctionne dans
l'éditeur ; une session en ligne à deux appareils reste à vérifier. Les anciens
contrôles Fusion Server prouvent cette autre tranche seulement et ne valident pas P0 Quantum.
[Preuves et prochaine action](STATUS.md).

**Séquence immédiate :** construire le WebGL réseau et faire rejoindre deux clients,
dont le Redmi ; instrumenter ensuite la scène Quantum en ligne. Reprendre le combat et les bots du sample,
remplacer Coin Grab par les objectifs MOBA, puis brancher deux profils Genesis résolus
par le catalogue. Ne pas développer en parallèle un roster Fusion. Une baisse due aux
meshes, textures ou effets se traite côté présentation ; une saturation de simulation
mesurée à charge représentative déclenche le réexamen réseau avant P2.

Le chemin critique est : web/mobile → réseau à deux clients → combat Genesis → partie
entière → admission et résultats vérifiés → achat → stabilisation. Les règles du
catalogue, les fiches de traits et les tests de propriété/commerce peuvent avancer
indépendamment. Réserver la fin de période à la livraison et à la soumission.

### Si un jalon échoue

- P0 : relever frame time, mémoire et temps de simulation sur Redmi. Corriger le rendu
  si c'est le goulot ; ne comparer Fusion qu'à charge représentative si Quantum lui-même
  est le goulot. Un blocage d'AppId ou de WebGL en ligne se diagnostique séparément.
- P1/P2 : régler d'abord visée, déplacement, lisibilité et objectif de victoire.
  Garder une seule voie et deux Genesis de preuve avant d'élargir le catalogue jouable.
- P3 : une victoire déclarée uniquement par un client ne crédite pas la carrière.
  Résoudre webhook/résultats/replay avant d'ouvrir la progression persistante.
- P4 : limiter la boutique à une variante utile, mais terminer achat, confirmation
  et récupération après panne de bout en bout.
- P5 : réduire coût graphique ou simulation selon les mesures. Un passage au natif ou
  un changement de SDK serait une décision explicite fondée sur la cause observée.

## 7. Extensions si le développement avance vite

| Priorité | Amélioration | Fondation réutilisée | Preuve attendue avant intégration |
| --- | --- | --- | --- |
| E1 | Meilleures sensations : visée, télégraphes, impacts, caméra, son, retours tactiles disponibles. | Vues séparées de la simulation. | Les joueurs comprennent mieux les touches et choisissent de rejouer. |
| E2 | Signatures supplémentaires pour des traits reconnaissables et 1:1 plus individualisés. | Catalogue + primitives de capacités. | Nouveau comportement utile, lisible et couvert par les cas de compatibilité. |
| E3 | Éveil temporaire du héros, alimenté par actions et objectifs. | État de match et transformation d'une capacité. | Moment spectaculaire compréhensible, sans avantage irréversible de l'équipe déjà devant. |
| E4 | Arbre de maîtrise court, seconde spécialisation et historique d'exploits du Genesis. | Carrière par NFT et résultats validés. | Le joueur sait ce qu'il veut essayer au match suivant. |
| E5 | Un objectif neutre qui force un déplacement et un conflit de décision avec la voie. | IA, ciblage, événements de match et carte. | Davantage de situations intéressantes ; durée et lisibilité toujours acceptables. |
| E6 | Prêt de héros à un ami, avec autorisation explicite du propriétaire. | Droits de jeu séparés de la possession. | Révocation, durée, récompenses et sessions concurrentes définies et testées. |
| E7 | Seconde carte, mode supplémentaire, spectateur/replay visible. | Carte et règles versionnées ; capture de replay. | N'augmente pas le taux d'échec des parcours existants ni les files d'attente. |

Prendre l'amélioration qui répond au défaut observé le plus coûteux pour l'expérience. Ne pas
consommer une avance simplement en augmentant le nombre de systèmes. Les animations et modèles
3D peuvent remplacer les placeholders progressivement dès que les volumes de collision et la
lisibilité sont stables.

## 8. Vérifier le gameplay et l'expérience

Première séance visée : 6 à 10 personnes, incluant des détenteurs Chog et des personnes qui
n'ont pas entendu l'explication du développeur. Accès à des détenteurs déclaré par l'utilisateur ;
nombre de testeurs et appareils non confirmés. Les bots sont annoncés et les sessions avec bots
sont analysées séparément. Ce petit groupe donne des observations qualitatives ; il ne prouve
ni une rétention à l'échelle ni un équilibre de taux de victoire.

| Question testée | Mesure ou observation | Cible de départ |
| --- | --- | --- |
| Comprend-on le but ? | Décrire comment gagner après une minute, sans aide orale. | Au moins 4 des 5 premiers nouveaux testeurs. |
| Les commandes répondent-elles ? | Actions ratées à cause de l'interface, double-touch, visée, esquive ; chronologie input → feedback local. | Corriger chaque défaut empêchant un geste de combat fiable. |
| Le NFT compte-t-il ? | Faire jouer deux héros et demander la différence perçue, sans montrer la fiche auparavant. | Différence concrète citée par 4 des 5 premiers testeurs. |
| Y a-t-il un moment à raconter ? | Observer une action combinée, un sauvetage ou un renversement ; demander ce qui s'est passé. | Une situation identifiée et comprise, à améliorer par itération. |
| A-t-on envie de rejouer ? | Proposer de terminer la séance ; noter les replays choisis librement. | Signal exploratoire, aucun seuil de rétention prétendument scientifique. |
| Le jeu tient-il sur mobile ? | 20 min de session sur Safari iOS et Chrome Android, temps d'image, mémoire, chauffe, changements d'app. | Plancher visé 30 FPS ; p95 du temps d'image ≤ 33,3 ms sur les appareils retenus. |

Mesurer séparément temps de téléchargement à froid, compilation/démarrage, retour à chaud,
attente de salon et durée de match. Budget initial proposé : payload de première partie
≤ 25 Mo compressés, démarrage à froid ≤ 20 s sur un réseau de référence à 20 Mb/s, retour
à chaud ≤ 5 s. Ce sont des cibles à qualifier au P0 ; les appareils et le réseau exacts
doivent accompagner tout résultat. Une moyenne de FPS seule ne démontre pas la fluidité.

Tests réseau : plusieurs vrais clients, Wi-Fi puis 4G, latence artificielle ajoutée documentée,
passage en arrière-plan, perte réseau, reprise et fermeture d'un client. Pour la PWA, tester
aussi le changement vers le wallet puis le retour à l'écran de jeu, avec et sans installation.

## 9. Ce qui doit rester explicite pendant le développement

- Chaque tâche renvoie à un jalon et à un critère observable de cette page.
- Une décision de comportement ou de responsabilité met à jour le plan ou l'architecture.
  Les valeurs de gameplay vivront ensuite dans le catalogue ; ne pas dupliquer leurs chiffres
  dans plusieurs documents.
- `STATUS.md` contient la prochaine tâche, les décisions ouvertes et les preuves des tâches
  terminées. Un build absent, une lecture de documentation ou un mock ne valent pas un test réussi.
- Un écart temporaire indique sa conséquence et la condition pour le supprimer. Aucun mock
  de propriété, de paiement ou de résultat ne reste activable dans la version publique.
- Les améliorations choisies après essai remplacent les anciennes décisions dans le document
  actif ; la raison du changement reste dans le journal de décisions.

**Prochaine action de développement** : l'AppID Quantum 3 est renseigné ; lancer le menu
réseau et jouer depuis deux clients en ligne, puis mesurer le build sur Redmi avec une
charge représentative. Brancher ensuite
deux profils Genesis, attaque + A + signature et mort/respawn synchronisés. Le projet
[Quantum est ouvrable](../game/README.md) et sa partie locale WebGL est prouvée ;
l'AppId Fusion existant n'est pas réutilisable pour Quantum. Les
[motifs du choix actuel](DECISION-RESEAU-REVUE.md) et les limites sont explicites.
