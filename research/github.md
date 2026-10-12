**Ce que ton GitHub apporte à la recherche**

Tu demandes explicitement que les compétences ne limitent pas les idées. Cet examen cherche donc des situations, des règles et des architectures qui peuvent inspirer une expérience nouvelle. Il ne cherche pas à déduire le projet à choisir de la technologie la plus présente dans tes dépôts.

L'API publique retourne **36 dépôts** lors de la consultation du 6 octobre 2026. La page de profil indexée en affiche 34 ; le relevé API est conservé. Douze README ont été demandés, onze étaient disponibles. Cinq fichiers de code ont été lus à des commits précis. Aucun projet n'a été construit, lancé ou audité ; aucune affirmation d'usage réel ou de tests réussis n'est déduite de sa documentation. [Inventaire](evidence/redgnad-public-repositories.json), [manifest des fichiers](evidence/redgnad-code-review-manifest.json).

| Projet | Observation documentée | Inspiration possible | Limite de l'observation |
| --- | --- | --- | --- |
| [Rob-a-Base](https://github.com/RedGnad/Rob-a-Base) | Le fichier `theft.ts` contient état du vol, vérifications de portée, alertes et événements de transport du butin. | Une action sociale produit une conséquence visible et une situation racontable : bon point de départ pour réfléchir au plaisir de coopération ou de rivalité. | L'intérêt de ces interactions auprès des joueurs n'a pas été mesuré ici. |
| [CHOGTANKS](https://github.com/RedGnad/chogtanks-dev) | `ChogTanksNFTManager.cs` contient tokenId, niveau, autorisation d'évolution et sélecteurs d'appels NFT. Le README décrit une progression portée par un NFT propre au jeu. | Tu identifies ce mécanisme comme ton innovation peu exploitée. C'est un point de départ à approfondir : transformer le corps et ses interactions pourrait produire une expérience nouvelle. La présence de précédents ne suffit pas à l'écarter. | Les capacités annoncées et le fonctionnement actuel n'ont pas été testés. Le contrat du jeu est distinct de Genesis. |
| [Viky-index](https://github.com/RedGnad/Viky-index) | `EventHandlers.ts` construit des identifiants d'événements et des agrégats par jour, condition et contrat. | Un journal de personnage pourrait conserver des conséquences factuelles et reconstruire une chronologie compréhensible. | Un indexeur financier ne prouve pas qu'une chronologie suscite un attachement ou un retour. |
| [Axion](https://github.com/RedGnad/Axion) | `scorecard.ts` définit des faits de résultat et leur signature EIP-712, avec un historique accumulé. | Distinguer la narration d'un événement de sa trace vérifiable ; laisser d'autres expériences lire une conséquence. | Une signature atteste son signataire, pas l'honnêteté d'une simulation ou la vérité indépendante de ses résultats. |
| [Sentri](https://github.com/RedGnad/Sentri) | `AgentINFTSignerVerifier.sol` vérifie une liaison entre une identité et un signataire actif via une interface dédiée. | Des droits temporaires et précisément délimités peuvent permettre la contribution d'invités au personnage. | Ce fichier n'est ni un système de délégation Genesis existant ni une implémentation directement réutilisable sans adaptation. |

Les README de Neutrino, Mordant et Pulse montrent également une pratique de séparation entre proposition, règle, décision et preuve. L'idée utile pour Relay serait une narration interprétative qui reste subordonnée à l'état de jeu. Les usages financiers ne sont pas recommandés à partir de cette seule parenté technique. [Neutrino](https://github.com/RedGnad/Neutrino), [Mordant](https://github.com/RedGnad/Mordant), [Pulse](https://github.com/RedGnad/Pulse).

Starclub documente des missions dans un environnement 3D et la vérification d'interactions externes. Cela inspire une seconde question : la prochaine aventure d'un Chog pourrait-elle utiliser une action vécue dans un autre produit ? Il faudrait que cette action serve le plaisir ou la compréhension du joueur ; une série de transactions imposées ne prouve pas l'intérêt de la proposition. [Starclub](https://github.com/RedGnad/Starclub-gang).

La recommandation issue de cette lecture est de rechercher un **nouveau lien causal entre les expériences**, puis de réutiliser des briques seulement si elles servent ce lien. Les fonctions anciennes devront être déclarées comme préexistantes dans la soumission. Le brief demande cette transparence ; il reste à clarifier avec les organisateurs les conditions exactes applicables à une réutilisation importante.

Liens permanents des fichiers examinés :

- [Rob-a-Base : règles de vol](https://github.com/RedGnad/Rob-a-Base/blob/f57488b142fe4515df8a37efd137f45caabe6d5a/src/server/theft.ts).
- [CHOGTANKS : gestion du NFT](https://github.com/RedGnad/chogtanks-dev/blob/d22b7007d9e7406961202d46e0f1de2a243cc10a/Assets/Scripts/ChogTanksNFTManager.cs).
- [Viky-index : événements](https://github.com/RedGnad/Viky-index/blob/17515df656eec80576f3c1cf8b1a285de0bee3d5/src/EventHandlers.ts).
- [Axion : scorecards](https://github.com/RedGnad/Axion/blob/63e6305d9da6406e90717651816f7feffcf57811/src/arena/scorecard.ts).
- [Sentri : liaison identité-signataire](https://github.com/RedGnad/Sentri/blob/7523dded1e5b2f7e31e898c0527ef0a5edb6dc30/contracts/src/AgentINFTSignerVerifier.sol).
