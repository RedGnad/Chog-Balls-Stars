# The basic attack drawn from the hat (design, 15 October 2026)

The owner said yes on 15 October to the basic attack showing the Genesis, and asked that it be done
"bien beau, accurate, et optimisé", as one of the hero moments of the game. This page is the design;
what is built and seen is said at the end.

## The rule

- **The `Head` trait gives the object of the basic attack.** It is the trait the game camera sees
  first (the hat, seen from above), so the attack reads as coming from what the hero wears.
- **The class (from `Body`) gives the gesture**, as it does today: the brute strikes in front of
  it, the ranger throws, the controller casts a slower bolt. Damage, range and timing do not
  change: only what is drawn.
- **The object comes from the picture of the Genesis, not from its name**: what is printed or
  stitched on the hat when there is a drawing (the bolt of Thunder Cap, the coin of Dollar Cap),
  the hat itself when the print is a word (TISM, WAGMI: the cap is thrown and spins like a
  frisbee, its word on it), the thing itself when the Head value is a thing (the plunger, the
  clown nose). What each picture shows is in `tools/blender/head-values-from-the-art.md`.
- **A hero without a hat throws a leaf of its own crown**, in its skin colour.

## The objects, value by value (base Origin, 45 values and the bare head)

| Head value (Genesis) | Object | What hits |
| --- | --- | --- |
| no hat (229) | a leaf of the crown, skin colour | a splash of small leaves |
| Mon Cap (70) | the Monad mark (purple rounded diamond) | a purple ring |
| Russian Hat (66) | a snowball | a puff of snow |
| Balls Cap (60) | the bucket hat, its stitched drawing on it | a puff |
| Horny Cap (58), TISM (49), Choggie Cap (51), Boob Cap (46), MMGA (34), MCGA (34), Chog Cap (32), Buttcoin (31), 10k TPS (30), JEET (30), MEGA Chog (30), RETARD (27), WAGMI (25), NEET (24), MCGA Cap (21) | the cap itself, spinning flat, its word on it | a ring in the colour of the cap |
| Crown (52) | the crown, spinning | gold sparks |
| Wizard Hat (47) | a yellow star (the stars of the hat) | a burst of stars |
| Cloud Cap (47) | a white cloud | a puff and drops |
| Durag (47) | the tail of the durag, a dark ribbon | a dark ring |
| Blue, Orange, Gray Bucket Cap (45, 45, 38) | the bucket hat, spinning | a ring in its colour |
| Bob Hair (45) | a yellow curl | yellow puffs |
| Dollar Cap (44) | a gold coin with a $ | coins |
| Spartan Helmet (40) | a spear | sparks |
| Star Cap (40) | a white star | a burst of stars |
| Thunder Cap (40) | a yellow bolt | a crackle |
| Peace hat (38), Red Peace Cap (38) | a peace sign, white | a white ring |
| Red Headband (34) | the red band, as a ring | a red ring |
| Japan Headband (31) | a white disc with the red sun | a white and red ring |
| BTC Cap (32) | a bitcoin coin | coins |
| Clown Nose (31) | the red nose, bouncing | a squash |
| Choggie Beanie (27) | the small lavender Chog of the patch | a puff |
| Wizard Beanie (26) | a yellow ring (the rings of the beanie) | a ring |
| BTC Beanie (25) | the slice of pizza of the patch | crumbs |
| RH Beanie (25) | an arrow with the black feather | a feather |
| Robber (24) | a money bag | coins |
| Plumber (23) | the plunger, flying cup first | a pop |
| Mon Beanie (20) | a ball of yarn in its two blues | a puff |
| Rainbow Wig (18) | a ball of rainbow | colours |

The 100 Genesis on another base (Skull, Radioactive, 1:1) get theirs with their heads.

## How it is drawn (performance)

- Each object is a small 3D model made in Blender with the heroes (a few hundred triangles at most),
  coloured in its vertices and drawn with the one material of the heroes (`ChogHero.shader`): no
  new texture, no new material, one draw call an object. The caps and hats reuse the very parts the
  heroes wear.
- Objects are kept and reused rather than made at each attack; the impact is a few small pieces
  thrown out and faded, drawn the same way.
- Measured on the owner's phone with `?perf=1`, six heroes attacking, before and after.

## Built and seen (15 October 2026, night)

- **Built:** the 22 objects of the table in `tools/blender/chog_attack_objects.py` (40 triangles for the
  bolt, 1,680 for the cloud), exported with the heroes into the library of parts
  (`tools/blender/export_chog_parts.py`, format 3: each Genesis records the piece its basic attack
  throws and how it moves). The hats thrown are the very parts the heroes wear. In the game,
  `ChogAttackLook` replaces what the sample drew for the three basic attacks (the archer's arrow,
  the knight's slash, the spellcaster's first bolt) with that object, grown in a twelfth of a second,
  turning as its style says; the strike of the brute pops out in front and fades in its quarter of a
  second; where an attack ends, seven small pieces in its colour fly out (`ChogImpact`). The
  simulation is not touched: same reach, same timing, same damage.
- **Seen:** every object rendered from the side in Blender
  (`tools/blender/render_chog_attack_objects.py`); nine heroes beside their object under the game
  camera in the game (`?heroes=1287,39,56,75,10,6,21,17,0`); snowballs thrown by a Russian Hat in a
  headless match.
- **Not seen yet:** a strike of the brute and a burst filmed frame by frame; the cost on a phone
  (`?perf=1`, six heroes attacking); the special attack (from the Eyes) is still drawn flat.

## The special: the eyes it comes from (15 October 2026)

The special attack comes from the Eyes trait and was already drawn from it (the beams of laser eyes, the
fan of a visor, the stare of drawn eyes: `GenesisAttackView`). When it fires, the eyes the hero wears now
also pop up in 3D above it for under a second, turned to the camera, with a burst in their colour
(`ChogSignatureFlash`, the very part of the library the hero wears). Seen on the viewer page
(`?heroes=1287,1241,491,46,162&flash=1`); not yet caught in a match picture.
