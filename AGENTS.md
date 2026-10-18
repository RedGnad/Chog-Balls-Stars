# Continuité de travail

Le périmètre actuel est un MOBA Genesis multijoueur en vue haute 3D/isométrique, de préférence
PWA mobile. L'utilisateur demande des recherches fondées, un prototype jouable rapide et
une architecture permettant ensuite d'enrichir le jeu.

Avant toute tâche de développement : lire `docs/STATUS.md`, `docs/PLAN.md` et les sections
pertinentes de `docs/ARCHITECTURE-QUANTUM.md`. L'ancien `docs/ARCHITECTURE.md` documente
le candidat Fusion et n'est plus l'architecture active. Les anciennes explorations dans `research/` ne sont
pas des décisions de produit actuelles. Les dernières instructions utilisateur prévalent.

- Reprendre le jalon et la prochaine action de STATUS ; distinguer confirmé, recommandé,
  implémenté et vérifié. Ne pas inventer de validation ou de mesure.
- Les visuels commencent par des placeholders. Priorité à une partie complète et aux
  comportements NFT observables, puis au contenu et au polish.
- Préserver les frontières catalogue, simulation, présentation et services. Inspecter le
  sample acquis avant de créer des abstractions ou de supposer des API.
- Traits/raretés : utiliser les données enregistrées et leur couverture exacte ; le tokenId
  canonique diffère du numéro CHOG affiché. Une attribution de capacité est un choix de design.
- Tester les invariants touchés, particulièrement propriété, simulation, achats et attribution
  des résultats. Une lecture statique ne prouve pas l'exécution d'un parcours.
- Mettre à jour les décisions actives si elles changent et STATUS après le travail. Conserver
  une prochaine action concrète et les limites encore ouvertes.
- Garder les preuves à leur emplacement ; éviter de recopier des règles de gameplay dans
  plusieurs documents. Les sources et la recherche ne constituent pas du code livré.

Ces règles de continuité n'ajoutent pas de demande de permission pour les travaux déjà
autorisés par l'utilisateur. Elles ne demandent pas de délégation à des sous-agents.

Agents (any assistant) : lire aussi `docs/HANDOFF.md`, qui donne le contexte, le travail ouvert et les règles de travail sur cette machine.
