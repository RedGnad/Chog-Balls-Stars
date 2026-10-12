# Archive de décision réseau — Fusion Server, 8 octobre 2026

**Décision remplacée le même jour par [le réexamen Quantum 3](DECISION-RESEAU-REVUE.md).**
Le raisonnement et les tarifs ci-dessous documentent le choix Fusion précédent ; ils ne
décrivent plus l'architecture active du MOBA.

**Décision technique : Unity + Photon Fusion 2, topologie serveur dédié dès le MVP.**
L'utilisateur demande de trancher maintenant, avec priorité au mobile, aux Genesis et à
$CHOG, en évitant une migration après le développement du jeu. Cette décision remplace
D06 Quantum candidat. `game-fusion/` contient désormais le SDK et une arène Server/Client ;
six contrôles réseau passent via Photon dans un même processus Editor sans rendu.
Le sample Quantum reste conservé dans `game/`. [Preuve réseau](../research/evidence/fusion-network-qualification.json).
L'utilisateur a ensuite validé explicitement ce choix et accepté 100 connexions Photon
simultanées comme plafond initial du MVP.

## Raisonnement et alternatives

| Solution | Fait documenté | Conséquence pour ce MOBA | Décision |
| --- | --- | --- | --- |
| Quantum 3 | Chaque client exécute la simulation déterministe. Le culling réduit la prédiction distante, pas l'exécution des frames vérifiées. | Bonne cohérence, flux réseau d'inputs et replays utiles ; IA, navigation et combat vérifiés restent des calculs sur chaque mobile. | Écarté pour cette architecture. Le sample acquis reste une référence. |
| Fusion Shared | Pas de resimulation réseau classique ; Photon le recommande pour mobile/WebGL. Les clients ont autorité sur leurs objets. | Avantage structurel de CPU et simplicité d'hébergement ; résultats et dégâts déclarés par des clients ne deviennent pas fiables grâce à un backend de paiement. | Écarté pour la carrière persistante prévue. |
| Fusion Host | Un joueur est l'autorité de la partie. | Charge, confiance et disponibilité dépendent de son appareil et de sa connexion. | Écarté pour le mobile public. |
| Fusion Server | Le serveur simule le monde ; le client peut prédire seulement son avatar et interpoler les autres objets. | IA des bots/minions et décisions de combat sur notre serveur ; profil Genesis et résultat contrôlés à la même autorité. | Retenu ; hébergement supplémentaire accepté comme contrepartie du choix. |

Sources : [topologies Fusion](https://doc.photonengine.com/fusion/v2/fusion-choose),
[prédiction sélective Fusion](https://doc.photonengine.com/fusion/v2/manual/advanced/time-synchronization),
[culling Quantum](https://doc.photonengine.com/quantum/v3/video-tutorials/prediction-culling),
[résultats et limites Quantum](https://doc.photonengine.com/quantum/v3/manual/cheat-protection).

Il s'agit d'une décision d'architecture fondée sur la répartition documentée du travail et
de l'autorité. **Aucun benchmark identique Fusion/Quantum sur notre Redmi n'a été identifié
ni effectué. Aucun gain de FPS chiffré n'est établi.** Le rendu, le téléchargement, la mémoire,
les snapshots et la latence restent des coûts à maîtriser ; déléguer l'IA ne les élimine pas.
Quantum possède un moteur optimisé pour la resimulation ; sa lenteur générale n'est pas déduite.

Photon recommande Shared pour WebGL et déconseille Host/Server par défaut. En Server,
le téléphone sera **client**, l'instance de partie sera un build Linux sans rendu. Le chemin
WebGL client/serveur exige l'option `Enable ClientServer Modes in WebGL builds` ; les limites
de transport navigateur et le relais doivent être vérifiés. Le surcoût possible de latence
par rapport à Shared est une contrepartie retenue, pas un détail masqué.
[Restriction WebGL officielle](https://doc.photonengine.com/fusion/v2/fusion-choose#webgl_builds).

Le critère qui départage Shared et Server est la carrière Genesis persistante déjà prévue,
avec des spécialisations achetées. Le paiement pourrait être sûr dans les deux ; cela ne
prouve pas les résultats du combat. Le serveur produit ceux-ci sans devoir reconstruire un
moteur de replay déterministe. Pas de nouvelle règle de mises ou de gains monétaires.

## Coûts vérifiés et capacité

Tarifs publics en USD consultés le 8 octobre 2026, hors taxes, trafic supplémentaire et
services de jeu. Ils sont identiques pour Quantum et Fusion.

| Connexions Photon | Prix publié | Trafic inclus |
| --- | --- | --- |
| 20 développement | 0 $, développement/non commercial | 60 Go/mois |
| 100 lancement | 0 $, une application Fusion **ou** Quantum par studio/client Photon | 0,3 To/mois |
| 500 Public Cloud | 125 $/mois | 1,5 To/mois |
| 1 000 Public Cloud | 250 $/mois | 3 To/mois |
| 2 000 Public Cloud | 500 $/mois | 6 To/mois |
| 3 000 Premium, exemple de pic facturé | 1 500 $/mois calculés à 0,50 $/CCU ; minimum Premium 1 000 $ | 3 Go par CCU de pic/mois |

[Tarifs Fusion](https://www.photonengine.com/fusion/pricing),
[tarifs Quantum](https://www.photonengine.com/quantum/pricing).

« Client » désigne ici le studio titulaire du compte Photon. La FAQ limite cette offre
à une application Fusion ou Quantum par studio/compte ; PUN possède ses propres forfaits.
Nous en déduisons que l'application PUN2 chogtanks n'utilise pas ce droit gratuit Fusion/Quantum.
Créer une application Fusion 2 distincte sur le compte existant, sous réserve que le droit
100 CCU n'ait pas déjà été activé sur une autre application Fusion/Quantum. Ne pas modifier
chogtanks, qui doit rester en service plusieurs mois. Le compte n'a pas été inspecté.
[Règle officielle](https://doc.photonengine.com/photon/current/pricing),
[forfaits PUN](https://www.photonengine.com/pun/pricing).

Création : [dashboard](https://dashboard.photonengine.com/), `YOUR > APPS > Development`,
`CREATE A NEW APP`, Photon SDK **Fusion**, SDK Version **Fusion 2**. Vérifier ensuite le
forfait de cette nouvelle application et activer **Free 100 CCU** pour le lancement.
Un affichage initial de 20 CCU correspond au développement ; il ne prouve pas l'activation
du forfait 100. L'AppId fourni a été appliqué dans le nouveau projet et sa connexion cloud
fonctionne ; cela ne vérifie pas l'activation du forfait 100.
[Procédure AppId](https://doc.photonengine.com/fusion/v2/getting-started/appid-instructions).

La version stable publiée du SDK Unity est **2.1.3 (build 2390, 17 septembre 2026)** ; elle
relève du choix « Fusion 2 » dans le dashboard. La page SDK liste Unity 2021.3.45,
2022.3.45, 6.0.x et 6.3.x, pas notre 6000.6.4f1. Notre compilation C# Editor et nos six contrôles
réseau réussissent avec Mono.Cecil 1.10.2 et un correctif ciblé d'initialisation du gestionnaire
de scènes ; cela qualifie ce scénario précis. Le build WebGL a réussi et le parcours
entraînement/tactile a été essayé dans Chrome avec émulation. Téléphone, multijoueur
WebGL et Linux non testés. [Relevé local](../research/evidence/fusion-sdk-qualification.json).
[Versions officielles](https://doc.photonengine.com/fusion/v2/getting-started/sdk-download).

Les CCU sont des connexions simultanées, pas des NFT, des comptes ou des utilisateurs
mensuels. La facturation utilise la somme des pics mensuels par région. Le gratuit est
plafonné : les connexions supplémentaires sont refusées. Les plans dès 500 ont du burst,
avec conditions et facturation ; le burst n'est pas une capacité gratuite illimitée.
La FAQ consultée permet l'add-on 100 CCU pour 12 mois sur le gratuit ; le prix publié de
95 $ donnerait 200 CCU pour cette période. Vérifier le cumul proposé au dashboard avant achat.
[Règles de facturation](https://doc.photonengine.com/photon/current/pricing).

La lecture Genesis conservée du 7 octobre indique **1 969 NFT**, pas 1 969 détenteurs uniques
ou joueurs actifs. À titre de scénarios, 3 000 comptes peuvent produire 100, 500 ou 3 000
connexions ; aucune probabilité n'est attribuée. Ne pas déduire une pointe de fréquentation
de la collection ou de la règle marketing CCU/MAU de Photon.
[Lecture conservée](../research/evidence/genesis-mutability-facts.json).

Fusion Server exige aussi notre hébergement de parties. Photon Public Cloud suffit ;
**Enterprise n'est pas requis pour héberger notre build de jeu**.
[Serveurs dédiés Fusion](https://doc.photonengine.com/fusion/v2/concepts-and-patterns/dedicated-server-overview).
Pendant le développement, une machine disponible peut exécuter le serveur. Cela n'établit
pas sa capacité ni la disponibilité d'un lancement public entièrement gratuit.

Repères matériels vérifiés chez Hetzner en Europe : CX33 à 8,49 €/mois et CCX13 à
42,99 €/mois, hors IPv4 et taxes. Les CX partagent leur CPU ; les CCX ont des ressources
CPU dédiées. Ce sont des **prix de machines, pas des devis pour 100 ou 500 joueurs** ; le
stock disponible et la capacité de notre build ne sont pas établis.
[Tarifs juin 2026](https://docs.hetzner.com/general/infrastructure-and-availability/price-adjustment/),
[ressources partagées/dédiées](https://docs.hetzner.com/cloud/servers/faq/).

Budget total : Photon + machines de parties + API/base + CDN + RPC + stockage et trafic.
Le nombre de parties 3v3 par machine sera mesuré à p95 du tick serveur, CPU/RAM et latence
constants. Le comptage Photon effectif des peers serveur doit aussi être relevé avant de
convertir un forfait CCU en nombre exact de parties. Un `PlayerCount` n'est pas un relevé de facturation.
Le gratuit convient aux essais ; **500 CCU est le premier palier publié avec marge de burst
pour une ouverture confortable**, sans présumer que 500 est notre fréquentation future.

## Contrat d'architecture retenu

- Le serveur reçoit une admission liée au wallet authentifié, NFT canonique, match et slot.
  Il vérifie l'admission et utilise le profil de traits/rareté résolu par notre backend.
- Le mobile prédit déplacement et feedback immédiat de son héros. Les décisions d'IA,
  touches, dégâts, cooldowns, objectifs et victoire sont validés par le serveur.
- Les achats $CHOG sont confirmés avant l'équipement au lobby. Aucune transaction blockchain
  ni attente de wallet dans le tick de combat. Le profil du match reste figé.
- Le résultat provient du serveur authentifié, est lié à son instance et au manifeste, puis
  crédité une seule fois. Les replays deviennent une extension de diagnostic/spectateur,
  pas une condition d'autorité de chaque résultat.
- Une même topologie en MVP et en production. Host peut servir à une vérification locale
  du code client/serveur ; Shared ne sert pas de première architecture à migrer plus tard.

## Travail conservé et remplacement explicite

`shared/Chog.Heroes` n'a aucune référence Unity/Photon : données, résolution et 27 tests
restent utilisables. Le nouveau projet le référence par package local. `PointerCapture`
a été transféré depuis `Chog.Controls` ; les contrôles de l'arène utilisent Input System.
Les neuf tests précédents ne prouvent pas la livraison UI/tactile du nouveau projet.
`Chog.Genesis`, le binder, les assets et systèmes Quantum ainsi que les 14 tests SDK
nécessitent un remplacement Fusion. Ils ne sont pas présentés comme une intégration Fusion.

La transition utilise le projet Fusion identifiable `game-fusion/`, avec le sample Quantum
et ses preuves conservés. La première arène à placeholders est intégrée : autorité, mouvement,
projectile/dégâts, départ et nouvelle connexion qualifiés. Genesis et le résultat de match
authentifié restent à brancher ; le test accepte des clients de développement.
Prochaine action : essayer navigateur et Redmi face au serveur, puis
livrer P1 avec deux profils Genesis et leurs capacités. Les mesures mobiles règlent
l'architecture retenue ; elles ne reportent pas le choix du SDK.
