**Chogathon : choisir une expérience ambitieuse à partir de faits**

**Première exploration dépassée.** Les recommandations ci-dessous ne sont plus actuelles. Après les corrections sur l'évolution et la précision de la cible, lire [la réflexion du 7 octobre](reflexion.md). Les faits et sources restent conservés pour la traçabilité ; les hypothèses Relay, Heist et Studio ne sont pas validées.

Recherche exploratoire effectuée le 6 octobre 2026. Elle associe sources primaires, littérature classique et récente, examen de produits et lecture de code. Elle ne constitue ni une revue systématique exhaustive ni une étude de demande menée auprès de la communauté.

**Ma recommandation provisoire est de tester Chog Relay : un personnage NFT dont les aventures ont des conséquences dans plusieurs expériences, et dont le détenteur peut confier temporairement le contrôle à d'autres joueurs.** L'ambition porte sur la continuité vécue du personnage. La valeur à démontrer est très concrète : « Ce Chog se souvient de ce que nous avons fait, et cela change ce que nous pouvons faire ensuite. »

Cette recommandation reste réversible. Les faits permettent de définir une bonne expérience candidate ; ils ne permettent pas encore d'affirmer que les détenteurs la veulent. Nous comparerons Relay à un jeu coopératif centré sur les traits et à un studio de personnages communautaires.

**Le brief impose un choix d'expérience.** Le NFT Chog est une condition de qualification. Les pondérations sont nouveauté 30 %, plaisir 20 %, rétention 20 %, clarté 15 % et exécution 15 %. La construction dure quatorze jours, suivis d'une semaine de jugement ; la vidéo est limitée à trois minutes. Le prix global prévu est de 15 000 $, réductible à 10 000 $. Le token $CHOG donne éventuellement un bonus dont le poids n'est pas indiqué. [Brief officiel](https://chogathon.chog.xyz/).

Le code du compte à rebours situe la fin des inscriptions au **8 octobre 2026 à 17 h, heure de Paris**, et la fin de construction au **22 octobre à 17 h**. Cette seconde date est calculée à partir du code, et le calendrier n'a pas été corroboré par une annonce des organisateurs. Le relevé conserve le calcul exact. [Relevé du site](evidence/chogathon-site-facts.json).

J'en déduis qu'un projet techniquement sophistiqué doit rendre sa nouveauté compréhensible et plaisante rapidement. Un protocole invisible peut soutenir l'expérience ; il ne fournit pas à lui seul une raison d'y revenir.

**Le point de départ matériel est une petite collection, dotée de traits variés.** Le contrat public `0xc96d31f8626c6d03fae5dcd3d61e3fb9f4a73763`, sur Monad, retourne le nom Chog Genesis et une offre totale de **1 969 NFTs**. Les appels et réponses sont conservés. [Relevé du contrat](evidence/chog-contract-facts.json), [explorateur](https://monadscan.com/address/0xc96d31f8626c6d03fae5dcd3d61e3fb9f4a73763).

La page OpenSea consultée indique **1 019 adresses détentrices**, et expose treize catégories de traits, dont Head, Eyes, Body et Aura. Ces chiffres proviennent d'une page indexée ; les adresses ne représentent pas nécessairement autant de personnes ou de joueurs actifs. [Collection](https://opensea.io/collection/chog-genesis). Un exemple public expose notamment Wizard Hat, White tuxedo, Dead eyes et Fire aura. Ces attributs attestent une matière visuelle et combinatoire ; leur traduction en rôles serait notre décision de design. [Exemple de NFT](https://opensea.io/item/monad/0xc96d31f8626c6d03fae5dcd3d61e3fb9f4a73763/846).

Le site builders revendique plus de 40 000 membres Discord et 80 000 abonnés X, et propose tests communautaires et visibilité. Ce sont des déclarations de l'organisateur, sans mesure indépendante d'activité ou garantie de distribution pour notre projet. [Chog Builders](https://build.chog.xyz/).

Les cinq fiches préchargées et le compteur « 488 APPS » présents dans le code du hackathon ne constituent pas un recensement vérifié des concurrents. Aucune probabilité de gagner ni estimation de marché n'est déduite de ces éléments.

**Conséquence de conception : faire fonctionner le produit avec peu de personnes présentes.** Une aventure jouable immédiatement, avec coopération différée et invitations, me paraît plus robuste qu'un produit dépendant de nombreuses connexions simultanées. C'est une inférence à tester. Un mode multijoueur en temps réel peut ensuite enrichir un noyau qui fonctionne déjà.

Tu confirmes par ailleurs un accès à un NFT ou à des détenteurs pour les essais. Le nombre de participants disponibles et leur engagement restent à établir. Tes compétences ne servent pas de filtre : mobile, navigateur, 3D et multijoueur restent possibles selon les besoins de l'expérience.

**Ce que la littérature permet de retenir.** MDA propose de relier les règles aux comportements qu'elles produisent et aux émotions recherchées. Pour chaque piste, nous devons pouvoir nommer un moment de plaisir, puis la règle qui le rend possible. Il s'agit d'un cadre de conception, pas d'une preuve de succès. [Hunicke, LeBlanc et Zubek, 2004](https://www.cs.northwestern.edu/~hunicke/MDA.pdf).

Quatre études classiques associent autonomie, compétence et relations sociales à l'appréciation du jeu ; elles donnent des raisons d'examiner choix significatifs, maîtrise et coopération. Elles ne démontrent pas la rétention d'une application NFT. [Ryan, Rigby et Przybylski, 2006](https://selfdeterminationtheory.org/SDT/documents/2006_RyanRigbyPrzybylski_MandE.pdf).

Les récompenses demandent une analyse nuancée. Une méta-analyse de 128 études trouve des effets négatifs de certaines récompenses attendues sur la motivation intrinsèque ; une autre, portant sur 183 études et 212 468 participants, montre que motivation et incitations peuvent conjointement prédire la performance. Leurs tâches et mesures ne sont pas celles de Chogathon. Notre hypothèse de départ sera une expérience appréciable sans rémunération, avec incitations étudiées séparément. [Deci et al., 1999](https://pubmed.ncbi.nlm.nih.gov/10589297/), [Cerasoli et al., 2014](https://selfdeterminationtheory.org/wp-content/uploads/2017/06/2014_Cerasoli_Intrinsic.pdf).

Les agents génératifs de Park et collègues illustrent mémoire, réflexion et coordination dans un environnement de vingt-cinq agents. Leur évaluation porte notamment sur la crédibilité du comportement, pas sur une rétention commerciale. [Park et al., 2023](https://arxiv.org/abs/2304.03442).

Deux résultats récents empêchent de traiter l'IA comme une garantie de plaisir. Une expérience de 2026, avec 72 participants, rapporte des bénéfices lorsque les objets générés et dialogues sont intégrés aux règles d'un prototype ; elle porte sur des sessions courtes. Un mémoire de master de la même année, avec 102 participants, ne trouve pas de différence significative de plaisir entre dialogue génératif et dialogue scripté, malgré des différences d'immersion. [Yin et al., 2026, résumé et extraits éditeur](https://www.sciencedirect.com/science/article/abs/pii/S1875952126001163), [Jahn, 2026](https://essay.utwente.nl/essays/109393).

Enfin, une analyse de 259 articles critique les usages superficiels de la théorie de l'autodétermination dans la recherche sur les jeux. Ajouter un choix, une jauge et un classement ne valide donc pas mécaniquement autonomie, compétence et rétention. Nous observerons les comportements et les raisons du retour. [Tyack et Mekler, 2024](https://arxiv.org/abs/2405.12639).

**Les précédents imposent de préciser la différence.**

| Précédent vérifié | Ce qui existe déjà | Implication pour notre proposition |
| --- | --- | --- |
| [CryptoKitties](https://guide.cryptokitties.co/guide/cat-features/genes) | Traits héréditaires et reproduction | Faire des traits une mécanique n'est pas nouveau en soi. |
| [Aavegotchi](https://docs.aavegotchi.com/own/game-items/aavegotchis) | Personnalité, progression, attachement mesuré dans le jeu et prêt | Un compagnon NFT avec XP ou prêt serait une variante d'une catégorie établie. |
| [Forgotten Runes](https://opensea.io/blog/articles/in-conversation-with-magic-machine-the-team-behind-forgotten-runes) | Personnages détenus et histoires communautaires rattachées aux tokens | Une biographie persistante seule offre une différenciation faible. |
| [Colony, documentation actuelle](https://parallel.life/colony/game-guide/overview) | Colonist conversationnel, décisions et création d'équipement | Un personnage IA dans un jeu existe déjà ; le whitepaper initial n'est pas la preuve du produit actuel. |
| [Nifty Island](https://docs.niftyisland.com/creator/agents/tutorials/game-integration/) | Agents intégrés aux îles, avec choix d'apparence | Poser un agent dans un monde 3D ne suffit pas à établir la nouveauté. |
| [Ton CHOGTANKS](https://github.com/RedGnad/chogtanks-dev) | Code de vérification et évolution d'un NFT de jeu | Une nouvelle jauge d'évolution prolongerait ton propre précédent. |

Nous n'avons pas testé ces produits ni vérifié leurs métriques de rétention. Cette comparaison documente des fonctions annoncées ou des sources accessibles, et ne constitue pas une analyse exhaustive de la concurrence.

**Piste A — Chog Relay : une aventure continue, portée par le personnage.**

La promesse : « Ton Chog vit des aventures avec différentes personnes ; ce qu'il a vécu change sa prochaine aventure. » Le détenteur conserve le NFT et peut autoriser un autre joueur à jouer un épisode. Les conséquences suivent l'identité du token.

Exemple proposé : Alice réalise un petit casse 3D avec son Chog et choisit d'aider un garde plutôt que de voler son badge. Cette décision crée un contact. Alice confie ensuite une mission à Bob, qui ouvre une expérience mobile : le contact reconnaît ce même Chog et propose une voie d'entrée différente. Bob décide de protéger un complice. Alice retrouve ensuite cette relation, ses conséquences et le nom des contributeurs dans le personnage.

Le moment fort est la reconnaissance d'une action antérieure dans un autre contexte, suivie d'une conséquence jouable. La mémoire ne doit pas seulement produire une ligne de dialogue : elle modifie une possibilité, une relation ou une stratégie. Une règle claire décide de l'effet. L'IA peut interpréter un ton ou raconter le résultat, sous contrainte des événements validés.

Le NFT fournit l'identité déjà détenue, le contrôle transférable et la continuité entre joueurs. Le changement de détenteur doit déplacer les pouvoirs d'administration, conserver les événements du personnage et invalider les permissions devenues obsolètes. Un changement de joueur via délégation doit également conserver cette continuité. Remplacer le NFT par un compte personnel supprimerait cette relation de contrôle et de transmission indépendante des comptes du jeu. Ce bénéfice doit être visible dans la démonstration.

La nouveauté candidate est **une mémoire qui produit des conséquences jouables entre expériences et entre contributeurs**. Les éléments constitutifs ont des précédents ; nous ne revendiquons pas une invention mondiale. Pour convaincre, le second épisode doit dépendre d'une conséquence du premier que l'utilisateur comprend.

La raison de revenir serait découvrir et choisir ce qui arrive au personnage après une contribution. La délégation pourrait relier détenteurs et invités. Les inconnues décisives sont leur volonté de déléguer, leur intérêt pour une histoire partagée et la compréhension des conséquences. Un avatar auquel on s'attache ne se délègue pas forcément volontiers.

La démonstration cible deux épisodes courts, deux joueurs et une seule conséquence persistante réellement appliquée. Deux clients lisent le même état public ; un simple changement de page sans conséquence serait insuffisant. Une version 3D peut porter l'action, une version mobile le suivi et les décisions différées. Une ville autonome ou un catalogue de jeux seraient des extensions ultérieures.

**Piste B — Chog Heist : des traits qui rendent la coopération intéressante.**

La promesse : « Assemble les bons Chogs pour réussir un casse que cette équipe seule peut résoudre de cette manière. » Les attributs de la collection deviennent des capacités complémentaires et des informations différentes. Un accessoire ouvre une interaction, une aura révèle un indice, une tenue crée une occasion de diversion. Ces correspondances sont des choix proposés, pas des fonctions actuelles des NFTs.

L'expérience peut commencer par un épisode solo, puis accepter la contribution différée d'un deuxième personnage. Les participants découvrent une solution en combinant ce que chacun voit. Le moment fort est une collaboration intelligible : « Ton Chog m'a permis cette action, et ma décision a changé ta suite. »

Le token rattache rôle, progression et permissions au personnage détenu. Les invités jouent un rôle autorisé par un détenteur. Le test d'essentialité reste exigeant : si les rôles peuvent être remplacés par une sélection libre de classes sans perte compréhensible, le NFT est encore un habillage. Nous devons faire jouer les personnages existants, leur histoire et leur disponibilité autorisée.

La piste offre potentiellement un plaisir d'usage plus immédiat et plus facile à observer que Relay. Son risque principal est la dépendance aux autres : l'absence d'un trait ou d'un coéquipier ne doit pas immobiliser une équipe. Plusieurs solutions doivent exister. La rareté ne doit pas devenir une supériorité automatique ; les traits fréquents doivent avoir des contributions utiles.

Le premier test est une mission avec deux rôles et deux solutions. Une distribution complète des traits devra précéder tout équilibrage de collection. Nous disposons d'exemples et de catégories, pas encore d'un recensement des 1 969 métadonnées.

**Piste C — Chog Studio : des personnages que la communauté met en scène.**

La promesse : « Fais jouer les vrais Chogs de la communauté dans une courte scène que leurs détenteurs peuvent approuver et prolonger. » Un auteur propose une scène ou un petit comic interactif. Les détenteurs y autorisent leurs personnages et choisissent une réaction. Le résultat publie les contributions et les liens avec les épisodes précédents.

Le moment fort est se reconnaître dans une création drôle ou inattendue, puis voir les autres personnages y répondre. Des formats récurrents pourraient créer une habitude : débat absurde, tribunal, duel d'improvisation, mini-série. Leur intérêt devra être comparé à une création libre utilisant de simples avatars.

Le NFT sert à authentifier qui contrôle la participation du personnage et la continuité de ses contributions. Il ne prouve pas, à lui seul, une licence commerciale sur l'image. Les droits accordés par Chog devront être précisés avant toute promesse de licences, royalties ou merchandising. Pour un essai, le système peut rester centré sur l'accord de participation et l'attribution.

Cette piste pourrait convenir à la culture de memes annoncée par Chog, mais cette adéquation culturelle n'est pas une preuve de demande. Le principal risque est produire des contenus que les détenteurs approuvent sans que le public ait envie de les regarder ou de revenir. Nous testerons séparément plaisir de création et intérêt des spectateurs.

La première démonstration est une scène courte avec trois personnages, un choix approuvé par leurs contrôleurs et une suite qui utilise ce choix. Nous privilégions un format écrit ou illustré rapidement modifiable ; la vidéo et l'animation avancée se justifieront si elles améliorent les essais.

**Comparaison : les inconnues comptent davantage qu'une note inventée.**

| Dimension | Relay | Heist | Studio |
| --- | --- | --- | --- |
| Valeur immédiate proposée | Une action antérieure change le présent | Une combinaison de personnages rend une solution possible | Des personnages familiers créent une scène partageable |
| Place du NFT | Continuité, contrôle et contribution transmissibles | Distribution et histoire des rôles autorisés | Accord de participation et attribution |
| Hypothèse de retour | Suivre et influencer les conséquences | Rejouer avec une équipe ou une solution différente | Créer ou suivre des épisodes récurrents |
| Difficulté centrale | Rendre la continuité intéressante et lisible | Équilibrer les rôles et gérer les absents | Faire exister une audience récurrente |
| Expérience à tester d'abord | Deux épisodes causalement liés | Une mission à informations complémentaires | Une scène suivie d'un second épisode |

Je testerais **Relay et Heist en premier**, avec deux expériences d'une durée comparable. Relay examine un usage plus spécifique du caractère transmissible du token ; Heist vérifie plus directement le plaisir de coopération. Studio reste une alternative si les entretiens révèlent une activité créative forte et une faible envie de déléguer ou de jouer.

**L'architecture doit servir ce qui est ressenti.** Une proposition minimale pour Relay est un registre d'événements et d'autorisations rattaché à `(chainId, collection, tokenId)`. Le NFT Genesis ne serait pas reminté et ses métadonnées ne seraient pas supposées modifiables. Le jeu conserverait son état dans une couche distincte qui vérifie le détenteur actuel.

Le contrat Genesis retourne `false` pour l'interface ERC-4907, standard de droit d'usage temporaire. Nous ne pouvons donc pas supposer qu'il accepte directement `setUser`. Un registre externe ou une autorisation signée contrôlée par le détenteur peut exprimer les permissions du jeu. [Lecture effective](evidence/chog-contract-facts.json), [standard ERC-4907](https://eips.ethereum.org/EIPS/eip-4907).

ERC-6551 fournit un modèle de compte associé à un NFT existant. C'est une option si le personnage doit réellement posséder des objets, pas un passage obligé pour tenir un journal et contrôler des permissions. Son existence ne prouve ni son déploiement utilisable sur Monad ni la compatibilité de notre montage ; ces points seraient vérifiés pendant l'implémentation. [Proposition ERC-6551, statut Review lors de la consultation](https://eips.ethereum.org/EIPS/eip-6551).

Un hash d'événement prouve l'intégrité d'un contenu retrouvé, pas la vérité d'un résultat de jeu. Les règles et l'autorité qui acceptent les événements devront être explicites. Une mémoire en langage naturel ne doit pas réécrire l'état canonique. Le stockage des récits et leur récupération doivent également fonctionner : un lien IPFS seul n'est pas une garantie d'accès.

Autre détail vérifié : le token blockchain **973** pointe vers le fichier **465.json**, et le token 512 vers **4.json**. Le jeu doit employer les identifiants canoniques, puis résoudre le nom affiché. Ces cinq URI sont relevées au même bloc ; leur contenu IPFS n'a pas pu être récupéré pendant cette recherche. [Échantillon](evidence/chog-contract-facts.json).

**L'ambition se justifie par une preuve d'expérience.** Nous pouvons mobiliser la 3D, le multijoueur et l'IA si chacun améliore une interaction observée. Les ressources se concentreraient d'abord sur la reconnaissance du personnage, une action gratifiante, une conséquence claire et un retour volontaire. Un produit de grande ampleur pourra ensuite développer les épisodes, les clients et les collaborations.

Le plan de validation détaille les entretiens et les essais avant cette décision. Les premières conditions d'abandon sont : le NFT reste décoratif, la conséquence n'est pas comprise, les détenteurs refusent le partage de contrôle, ou le retour repose essentiellement sur une récompense promise. Les nombres proposés sont des seuils de décision pour notre pilote, pas des benchmarks prouvés. [Plan de validation](validation.md).

Pour la vidéo, une séquence de moins de trois minutes devrait montrer le premier choix, la contribution d'un invité, puis sa conséquence dans le second client. L'inscription peut utiliser le pitch provisoire : **“Chog Relay turns an existing Chog NFT into a shared adventure character whose experiences unlock consequences across games and players.”** La justification proposée est : **“The existing NFT determines who controls the character. Authorized contributors act on the same persistent identity, and the character's history follows the token instead of a player's account.”** Ces formulations devront être adaptées au résultat des essais et aux fonctions réellement livrées.
