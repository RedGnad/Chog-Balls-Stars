# Web pages next to the game

## `hero/`: find your Chog Genesis hero

One static page, no build step and no library. A holder types the number in the name of a
Genesis (CHOG #1287 is `1287`) or a wallet address, and gets the hero card the game shows:
portrait, what the outfit, the eyes, the aura and the rarity give, and the career of that
Genesis. A wallet lists every Genesis it holds. "Share" gives a link that opens on that hero
(`?n=1287`). Under it, the ten Genesis with the most wins.

It loads in a second on a phone, where the game itself is a 22 MB download: it is meant as the
first thing a holder opens, and as the thing they pass on. The game's menu links to it (SEE
EVERY HERO) for a visitor whose browser has no wallet or whose wallet holds no Genesis.

| File | What it is |
| --- | --- |
| `hero/index.html` | the page: markup, style and script in one file |
| `hero/heroes.json` | its data, **generated**: do not edit |

```sh
python3 tools/web/build_hero_data.py           # after a roster import in Unity
python3 tools/web/build_hero_data.py --check   # fails if heroes.json is stale
```

`heroes.json` repacks three content files and decides nothing: the roster
(`content/genesis-roster.json`), the sentences of the hero card
(`content/genesis-card-text.json`, written by the Unity roster import so the page and the game
always say the same thing) and the skin colours.

The page expects, next to itself: the portraits at `../genesis-art/256/{number}.jpg`, the game
services at `../api/` (`genesis/owned`, `career`, `career/top`; see `server/README.md`) and the
game at `../`. The local test server provides all three:

```sh
node server/src/local.js
python3 tools/unity/serve_webgl.py --latest --host 0.0.0.0 --port 8083
# http://<this machine>:8083/hero/
```

**Limits.** Nothing is deployed. A shared link previews with the same title and sentence for
every hero and no image: a preview per hero needs a server that renders one. On a phone
browser without a wallet, over HTTPS, the page offers MetaMask's documented link to reopen it
in the browser of the MetaMask app; that button was not exercised. A number lookup needs no
services; a wallet lookup and the careers do.
