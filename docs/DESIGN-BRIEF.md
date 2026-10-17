# Design brief: a Supercell-inspired Chog Arena (17 October 2026)

The owner asked on 17 October 2026 for the whole experience to be redone from scratch, inspired by Clash of Clans
"in a professional context": menus and match, colours and lights, polished step by step. The current screens are
placeholders, not a base. The brief to discuss with the owner (in French) is the doc "Chog Arena, cadrage design":
https://claude.ai/code/artifact/1f411f44-0a2c-4a4f-9473-e1990425bafd. This file keeps what the research found, in
short, with its sources, so it is not looked up twice.

## Guardrail

Supercell's Fan Content Policy forbids games built on Supercell assets and any fan content involving NFTs
(https://supercell.com/en/fan-content-policy/). Principles only: no Supercell font, icon, sound, character or
screenshot ships. Reference screenshots stay in `.artifacts/design-refs/` (git-ignored; `supercell/SOURCES.txt` lists
their URLs). The Photon sample's own art is licensed for internal development only (`game/License.txt:4`): replacing
the UI and HUD removes it from the public build; the arena set and the Cartoon FX pack are still to be checked.

## What the research settled

| Topic | Finding | Source |
| --- | --- | --- |
| Rewards | Right after the match, one progression track per hero (Clash Royale removed chests and timers on 31 March 2025; Brawl Stars merged its tracks on 3 June 2025): our on-chain mastery is that track | supercell.com release notes (in the brief's Sources) |
| Layout | Landscape, Brawl Stars hub (hero centre, controls on the edges, PLAY bottom right by the mode), Clash of Clans corners (profile top left, currencies top right) | https://supercell.com/en/games/clashofclans/blog/news/weve-revamped-the-clash-of-clans-user-interface/ |
| Buttons | Rounded rectangle, radius about 10% of height, 2-3 px near-black outline, light top band about 40%, darker lip about 10%; PLAY about 0.21 W x 0.15 H; never under 44 pt | Measured on Game UI Database screenshots; Apple HIG |
| Fonts | Lilita One (OFL) for titles and buttons; Nunito 800-900 (OFL, fixed-width digits) for numbers and text; Luckiest Guy (Apache 2.0) for banners | https://github.com/google/fonts |
| Colour roles | Main action yellow, confirm green, secondary blue, close red, self/allies/enemies green/blue/red; ours: Chog gold #F59E0B, green #5CE68A, purple #836EF9, pink #D83E7E reserved for $CHOG | Measured; chog.xyz and chogathon.chog.xyz CSS |
| Match look | Brawl Stars lights with matcaps, shadows at 25% darkening, a coloured ring under every character; our night palette already matches Brawl Stars' night map | https://github.com/tailsjs/brawl-stars-assets (unofficial dump) |
| Lighting plan | Matcap shading for heroes and arena, warm key #FFE8C2, blob shadow and team ring, baked AO, desktop-only post-processing, camera pitch test at 60-65 degrees; phone budget +0.5 ms per step over a 31 ms median | Unity docs (webgl-graphics, mobile optimization tips) |
| Juice | 15 items in order: pooled effects, hit flash (the `_Flash` property of `ChogHero.shader` is unused), hit sounds with pitch variation, flinch, view-only hit stop, damage numbers, death and respawn, camera shake with a switch, directional impacts, attacker kick, trailing health bars, reacting structures, special-ready pop, Android haptics, animated end screen; view only, never `Time.timeScale` | Juice it or lose it (https://github.com/grapefrukt/juicy-breakout), The art of screenshake, Lin et al. 2022 |
| UI technology | UGUI with a component kit (9-slice sprites, TMP outline and underlay presets, PrimeTween or DOTween, UIEffect MIT); not UI Toolkit (no keyframe animation, 2026 web crashes), not an HTML layer before the deadline (rewrite of 3,457 lines of money paths) | https://docs.unity.com/en-us/engine/6000.6/manual/uitoolkits/ui-system-compare.md |
| Own assets | Rob-a-Base (MIT, ours) has a Clash-style panel and button generator (`tools/ui/build-ui-textures.js`) and Baloo 2 | Local copy in the session scratchpad |

## Decisions asked of the owner (the brief's table)

1. Remove the recruit's 15% health penalty. 2. A TRY mode: any Genesis against bots, no career, no stake. 3. A light
camera shake with a switch. 4. Night or day palette for the match, compared live. 5. UGUI with a component kit.
6. Contracts v3 with EIP-2612 permits ($CHOG supports it, checked on chain). 7. A first-match coach. 8. Lilita One and
Nunito instead of Koulen and Convergence (Koulen kept for the logo).

## Access notes

- The fandom wikis answer through their MediaWiki API: `https://<wiki>.fandom.com/api.php?action=parse&page=<Page>&prop=wikitext&format=json`.
- RoyaleAPI, the Epic store FAQs and schedule.gdconf.com return Cloudflare 403 even with browser headers.
- Blockworks answers to curl with a browser user agent.
- Game UI Database pages: Clash of Clans id 1298, Clash Royale id 1299, Brawl Stars id 465.
- App Store lookup ids: 529479190 (Clash of Clans), 1053012308 (Clash Royale), 1229016807 (Brawl Stars).
