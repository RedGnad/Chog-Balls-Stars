# Configuration Fusion

[`photon-fusion.json`](photon-fusion.json) conserve l'AppID Fusion fourni par l'utilisateur
le 8 octobre 2026 et la topologie décidée. C'est la source de notre outil Editor,
**pas un format de configuration natif Photon ni un fichier relu pendant une partie**.
L'AppID est un identifiant client, aucun secret de compte ou serveur n'est stocké ici.

`FusionProjectSetup` l'a appliqué au véritable
`game-fusion/Assets/Photon/Fusion/Resources/PhotonAppSettings.asset`, champ `AppIdFusion`.
Version de qualification : `chog-p0-fusion-1`, région `eu`. La connexion au cloud a réussi
avec le serveur et deux clients. [Preuve](../research/evidence/fusion-network-qualification.json).
Après modification du JSON, utiliser **Chog → Qualification → Préparer la scène Fusion**
pour réappliquer ces réglages. Le sample Quantum `game/` et l'application PUN2 chogtanks
restent séparés ; aucun forfait n'a été modifié et l'activation de 100 CCU n'est pas vérifiée.

SDK fourni par l'utilisateur et importé dans `game-fusion/` :
[Fusion SDK 2.1.3 stable build 2390](https://downloads.photonengine.com/download/fusion/photon-fusion-2.1.3-stable-2390.unitypackage?pre=sp).
La [page officielle des versions](https://doc.photonengine.com/fusion/v2/getting-started/sdk-download)
donne aussi ce téléchargement. Le package local est ignoré par Git ; son empreinte et
les fichiers importés sont dans le [relevé SDK](../research/evidence/fusion-sdk-qualification.json).
Le refus du téléchargement web automatisé reste un constat historique, résolu par la
fourniture de l'archive locale. Aucun compte, forfait ou ancien projet n'a été modifié.
