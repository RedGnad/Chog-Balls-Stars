# Résolution Genesis → profil de héros

Bibliothèque C# indépendante de Unity et Photon, conforme au module `HeroResolver` du plan.
Le catalogue est chargé une fois, validé et conservé privé. Chaque résultat comprend la clé
canonique du NFT, le hash des metadata, la version et le hash du catalogue, les IDs de combat
et les règles qui les ont produits. Aucun état de combat mutable n'est stocké ici.

Depuis la racine du dépôt :

```sh
python3 tools/hero-catalog/run.py test
python3 tools/hero-catalog/run.py inspect
```

La commande compile la bibliothèque et l'outil en C# 7.2 avec Mono puis les exécute réellement.
Les artefacts locaux vont dans `.artifacts/hero-catalog/`, ignoré par Git. Aucun téléchargement
de dépendance n'est nécessaire avec `mcs` et `mono` déjà installés.

Le projet déclare `netstandard2.1` pour son intégration ultérieure ; **la compilation par le
SDK .NET et le build Unity/IL2CPP/WebGL restent à vérifier**. Les tests actuels utilisent Mono
6.12 sur cette machine. Ils ne prouvent ni le fonctionnement du multijoueur ni les performances.

`GenesisSnapshot` attend un binding fourni par notre lecteur RPC/indexeur de confiance.
La comparaison de hash détecte une incohérence avec cette référence ; elle ne prouve pas
seule la validité du contenu IPFS. Cette bibliothèque n'authentifie pas un wallet, ne vérifie
pas `ownerOf` et ne doit pas recevoir un profil client comme source de vérité.

La résolution distingue une metadata absente, un trait absent, une règle non couverte et une
fiche 1:1 inconnue. Les erreurs portent un code stable pour le futur lobby/API. Le catalogue
de prototype couvre trois fixtures réelles ; les autres combinaisons seront ajoutées après
examen et les capacités liées aux assets du sample lors de la qualification Unity.
