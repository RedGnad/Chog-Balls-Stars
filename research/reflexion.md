**Réflexion révisée — 7 octobre 2026**

**Propositions écartées après discussion.** Centipede et TV n'ont pas convaincu sur la force de leur mécanique ni sur la justification de leur architecture. La recommandation ci-dessous est caduque. Le [README](../README.md) donne l'état actuel et les sources de méthode consultées dans Master.

Je recommande **CHOG CENTIPEDE**, un jeu où des Genesis sont les segments d'un corps collectif grotesque. Chaque détenteur contrôle son segment ; les accidents affectent les autres et produisent des mutations persistantes. La seconde proposition, **CHOG TV**, fait du Genesis un protagoniste de spectacle interactif plutôt qu'un personnage directement piloté.

Cette recommandation remplace Relay, Heist et Studio. La cible précisée veut rire, éventuellement être choquée, et ne vient ni créer ni résoudre des problèmes. L'ambition doit se trouver dans ce que le système produit à partir d'actions simples. Aucun succès, retour utilisateur ou degré de nouveauté mondiale n'est démontré. Cette recherche est une synthèse ciblée, comprenant plusieurs méta-analyses publiées, pas une revue systématique exhaustive.

**Les faits qui changent le choix**

Le brief pondère nouveauté à 30 %, plaisir à 20 %, rétention à 20 %, clarté et exécution à 15 % chacune. Genesis doit être essentiel à l'expérience. Une intégration utile de $CHOG peut apporter un bonus dont le poids n'est pas annoncé. Quatorze jours de construction et trois minutes de vidéo favorisent une nouveauté visible rapidement, soutenue par un parcours jouable. [Brief](https://chogathon.chog.xyz/), [relevé](evidence/chogathon-site-facts.json).

La collection compte 1 969 tokens dans les lectures publiques. Un nombre de tokens ou d'adresses propriétaires ne mesure pas les joueurs actifs. Le compteur « 488 APPS » et cinq fiches préchargées dans le site ne constituent pas un recensement vérifié des concurrents. Affirmer que notre idée occupe un espace vide serait inventer une donnée.

Le code vérifié « Exact Match » et l'ABI de Genesis ont été récupérés. `revealed()` retourne `true`, `baseURI()` retourne un CID IPFS et `setBaseURI()` échoue si la collection est révélée. L'ABI ne comporte pas de méthode d'évolution, de niveau ou d'XP. Genesis ne propose donc pas nativement notre évolution. [Contrat](https://monadscan.com/address/0xc96d31f8626c6d03fae5dcd3d61e3fb9f4a73763#code), [code conservé](evidence/genesis-ChogMint.sol), [lectures et limites](evidence/genesis-mutability-facts.json).

Nous pouvons associer un état de jeu à `(chainId, collection, tokenId)` : forme, propriétés physiques, accidents et permissions. Le détenteur courant contrôle ce corps ; un transfert de Genesis transmet son contrôle et son histoire selon nos règles. Il s'agit d'une **évolution du personnage associé au NFT**, sans changement de l'image Genesis d'origine sur les marketplaces. Les numéros affichés CHOG diffèrent des token IDs canoniques : l'indexation utilise ces derniers.

Ton CHOGTANKS contient déjà un mécanisme d'évolution que tu identifies comme ton innovation peu exploitée. Sa présence ne suffit pas à l'écarter. La question est ce qu'une autre expérience peut en tirer : une transformation visible et fonctionnelle peut offrir davantage qu'une augmentation numérique. Le GitHub inspire cette hypothèse ; il ne prouve ni adoption ni rétention du mécanisme. [Lecture corrigée](github.md).

**La chaîne de raisonnement**

| Observation | Déduction de conception | Limite |
| --- | --- | --- |
| La cible veut rire avec peu d'effort. | Donner une intention immédiate et une conséquence visible : pousser, s'accrocher, déclencher une catastrophe. | Une interface simple ne garantit pas le rire. |
| L'humour peut utiliser surprise, tension et supériorité. | Montrer une tentative lisible, une rupture inattendue et une récupération rapide. | Des explosions aléatoires ne garantissent pas l'humour. |
| Des corps liés portent l'action d'un joueur sur les autres. | Le mauvais geste de l'ami devient collectif et attribuable. | Cette dépendance peut aussi frustrer. |
| Genesis est possédé et transférable, avec un état d'origine gelé. | Attacher au token un corps distinct et une histoire mécanique transmissible. | Le bénéfice sur un simple compte doit être compris et montré. |
| Ton évolution est peu explorée. | Évoluer à partir des accidents et conserver leurs effets entre parties. | La progression ne suffit pas à produire une expérience plaisante. |
| $CHOG apporte un bonus potentiel. | Donner aux dépenses un effet collectif visible dans le spectacle. | Le bonus ne compense pas une mauvaise expérience. |

**Les appuis et leurs limites**

Saucier et Walter, publication en ligne en 2022 dans un volume daté 2021, synthétisent **80 manipulations expérimentales d'humour**. Surprise, relâchement de tension et supériorité sont associés à un humour plus fortement perçu. Les modalités et instruments de mesure comptent aussi. Cela concerne des messages en recherche sur la persuasion, pas les degens ni la rétention d'un jeu. Le résumé éditeur a été consulté ; le texte intégral n'était pas accessible. [Notice primaire](https://www.tandfonline.com/doi/abs/10.1080/23808985.2022.2033634?scroll=top&tab=permissions).

McGraw et Warren proposent et testent la violation bénigne : une transgression devient humoristique lorsqu'elle est aussi perçue comme acceptable ou sans gravité. Cela inspire une monstruosité cartoon, une humiliation de personnage et une récupération immédiate. C'est un appui pour concevoir le ton, pas une recette universelle de ce qui choquera agréablement notre communauté. [Laboratoire des auteurs](https://leeds-faculty.colorado.edu/mcgrawp/benign_violation_theory.html).

Sio, Kotovsky et Cagan, 2015, synthétisent **43 études, 1 229 participants** sur les exemples en design. Les exemples réduisent la variété des pistes tout en pouvant améliorer nouveauté et qualité. La conclusion utile est d'approfondir un mécanisme plutôt que de collectionner des catégories usuelles. Résumé et extraits éditeur consultés, pas le texte intégral. [Article](https://www.sciencedirect.com/science/article/abs/pii/S0142694X15000290).

Doshi et Hauser, 2024, constatent qu'une aide générative peut améliorer les récits individuels tout en rapprochant les productions entre auteurs. Une extension publiée en 2026 par Wan et Kalman indique que des entrées plus diverses peuvent préserver la diversité. Ces expériences portent sur des histoires, pas des hackathons. Elles rendent une convergence de brainstorming plausible, sans expliquer avec certitude la ressemblance entre deux conversations. [Doshi et Hauser](https://arxiv.org/abs/2312.00506), [Wan et Kalman](https://arxiv.org/abs/2504.13868).

Les méta-analyses sur récompenses et motivation commentées dans [la bibliographie](bibliographie.md) invitent à distinguer une raison de gagner des tokens d'une raison de rire et revenir. Elles ne prouvent pas que $CHOG aurait un effet négatif. Son effet doit être testé séparément.

**Heave Ho** utilise des chaînes de joueurs ; **Chained Together** lie les déplacements ; **Gang Beasts** exploite corps mous et environnements dangereux ; **Marbles on Stream** permet de rejoindre avec une commande puis de regarder. Leurs pages attestent les fonctionnalités. Les avis Steam sont des signaux d'accueil, pas une causalité ni une validation du public Chog. Ces références étayent certaines briques et montrent aussi que le genre est occupé. [Heave Ho](https://store.steampowered.com/app/905340/Heave_Ho/), [Chained Together](https://store.steampowered.com/app/2567870/Chained_Together/), [Gang Beasts](https://gangbeasts.game/), [Marbles](https://www.pixelbypixelstudios.live/getting-started).

**Proposition 1 : CHOG CENTIPEDE**

« Tes potes et toi êtes les morceaux d'une créature dégueulasse. Leurs conneries changent ton corps. » Le traitement est grotesque et cartoon. Le nom est un nom de travail à éprouver auprès de la cible.

Première minute proposée :

1. Quatre Genesis reconnaissables arrivent à l'écran et sont assemblés automatiquement en un corps souple.
2. Un objectif évident apparaît : traverser la pièce et entrer dans une capsule avant la fermeture. On déplace son segment et on s'accroche ; aucun éditeur ou arbre de compétences.
3. Un joueur tire dans le mauvais sens, entraîne le groupe sous une presse et fait écraser son segment. Les autres peuvent le traîner ou le récupérer.
4. Le segment devient plat. Il glisse sous un obstacle, mais un ventilateur le transforme en voile et emporte ses amis.
5. Au tour suivant, cette propriété est encore là. Une courte séquence montre l'accident qui l'a créée et qui l'a provoqué.

L'évolution est **latérale et causale** : écrasé → plat et sensible au vent ; électrocuté → aimant qui attire passerelles et enclumes ; gonflé → flotte mais rebondit trop. Le joueur n'optimise pas une fiche. Il ressent une transformation avec bénéfice et défaut. Limiter les mutations actives et proposer un retour gratuit au corps standard évite de transformer une blague en corvée. L'histoire peut rester lorsque la forme est restaurée.

Chaque segment appartient au détenteur courant d'un token précis, conserve ses mutations entre sessions et transmet ce corps si le token change de main. Les portraits et traits soutiennent sa reconnaissance. Retirer cet ancrage fait perdre les corps possédés et transmissibles et change l'expérience sociale. Un contrôle de portefeuille à l'entrée ne suffirait pas à défendre l'essentialité : cette continuité doit être démontrée.

$CHOG donne au public un pouvoir de spectacle : financer à plusieurs un ventilateur géant, une pluie d'enclumes ou une presse. Chaque contribution ajoute une charge affichée ; au seuil, l'événement rejoint une file courte et s'exécute dans une fenêtre visible. La dépense finance une intervention précise dont l'effet et la provenance sont observables. Les interventions sont plafonnées par manche. Coût, délai de confirmation et interface doivent être éprouvés avant de promettre l'instantanéité. Les premières parties et actions des joueurs restent accessibles sans dépense de $CHOG.

La nouveauté candidate est la combinaison **corps collectif + conséquences persistantes par NFT + public qui provoque ces conséquences**. Aucune de ces briques n'est annoncée comme entièrement nouvelle. Notre question concurrentielle est de savoir si la combinaison produit des scènes distinctives, pas si nous pouvons revendiquer un espace vide.

Risque central : perdre le contrôle peut être énervant plutôt que drôle. Il faut collisions lisibles, récupérations rapides et un geste personnel perceptible. Un échec à ce test invalide le concept même avec un excellent protocole.

La démonstration ambitieuse mais bornée est une salle, deux à quatre joueurs, trois mutations fonctionnelles et une intervention du public. L'extension plausible est quatre à huit segments, combinaisons de mutations, plusieurs catastrophes et séquences partageables. Les 1 969 tokens ne promettent pas 1 969 joueurs simulés ensemble. Une simulation physique autoritaire peut être séparée de la vérification des droits et de la persistance. Le principal coût est le ressenti, la stabilité et la synchronisation.

**Proposition 2 : CHOG TV**

« Ton Genesis est la star d'une émission catastrophique que les autres peuvent saboter. » Le détenteur inscrit volontairement son personnage. Le public regarde et intervient depuis une page simple, sans constituer une équipe ni produire du contenu.

Un Chog tente automatiquement de livrer un gâteau. Un spectateur déclenche une peau de banane ; un autre finance un canon qui sauve momentanément le gâteau mais encastre le Chog dans le décor. Le personnage se relève aplati et cette forme perturbe l'épreuve suivante. Le spectacle possède un objectif compréhensible, une montée en tension, une catastrophe et une reprise. Les routines peuvent être contrôlées ; un LLM n'est pas nécessaire pour déterminer les résultats physiques.

L'ambition est un metteur en scène automatique qui choisit objectifs, accessoires et cadrages, laisse la physique et le public produire les accidents, puis conserve les transformations. $CHOG finance des interventions déterminées et visibles. Un clip et une nouvelle forme donnent une raison candidate de revenir au propriétaire. La première version peut être à la demande : la disponibilité permanente n'est pas nécessaire à la démonstration.

Cette piste convient mieux au plaisir de regarder sans effort et fonctionne sans plusieurs propriétaires connectés ensemble. Son risque : aimer un premier gag puis se lasser, ou se sentir spectateur de son propre personnage. L'intervention doit paraître personnelle et l'évolution doit renouveler le spectacle, pas ajouter une fiche d'état à une animation interchangeable.

**Choix et preuve décisive**

Je choisis Centipede : l'action d'un ami, l'accident et la mutation sont immédiatement incarnés, et le joueur agit directement. TV devient préférable si les essais révèlent surtout une envie de regarder et provoquer ponctuellement, ou une difficulté réelle à former une petite session.

Le test décisif est une salle de 60 à 90 secondes, puis une seconde partie avec la mutation conservée, comparée à une remise à zéro. Observer rire, frustration et envie spontanée de recommencer. Tester ensuite l'effet de l'intervention du public et la friction d'une dépense. [Protocole révisé](validation.md).

Face à un concurrent proche, les preuves défendables sont une transformation qui change la partie suivante, un enchaînement comique lisible, une continuité de propriété vérifiée et des retours spontanés. Le nombre de fonctions et une affirmation de nouveauté invérifiable ne remplacent pas ces preuves.
