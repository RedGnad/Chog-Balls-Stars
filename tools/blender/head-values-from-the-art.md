# Head values: what the art shows, what the builder makes (batch 4, 15 October 2026)

Every value of the trait `Head`, Side by Side, with the Genesis whose pictures were looked at (CHOG numbers,
the number shown in the name, not the canonical tokenId), what those pictures show, and what
`make_chog_traits.py` builds now. The decisions are in code: `HEAD_VALUES` in `chog_trait_hats.py`, one line per
value and Side, which gives the leaves of the crown (and other numbers) in place of those of the hat shape.

How it was looked at: contact sheets of five Genesis per value and Side, spread over those that wear it (all of
them when five or fewer), kept in `.artifacts/chog-body/traits/batch-4/art-looked-at/look_01.png` to
`look_11.png`; then the 1000-point pictures (`.artifacts/genesis-art/1000/`) of most of those, side by side with
the batch 3 renders; all 45 Bob Hair and all 40 Spartan Helmet Genesis on one sheet each. "Far side": on
Side=Right the head is seen three quarters from its left, so its right side is the left of the picture.

## What holds for whole groups

- **Side=Right** draws the head in the skin colour behind a cream face. A bare head (#75, #112, #76) shows a row
  of cream points along the top of the face and, behind it, short skin-coloured spikes on top and three or four
  down the far side. They are much shorter than the leaves of head H3: about a tenth of the width of the head,
  where H3 had about four tenths.
- **Under a cap or a bucket hat of Side=Right**: two to four short points stacked down the far side, from just
  under the brim to the cheek, pointing out sideways, about as wide as long. The uppermost often shows beside
  the crown of the hat. Never through the hat, never turned down like an ear.
- **Side=Left** is framed closer on the face and draws the whole head in cream: a bare head (#15) has a row of
  cream points on top and no skin-coloured spike at all. **Under every cap and beanie of Side=Left no spike shows,
  beside or behind the hat.** (The builder keeps the skin colour for the leaves of a bare Side=Left head: not
  changed in this batch.)
- **Skull and Radioactive bases** draw no spike (skull heads, horned heads). These bases are not built: the
  builder makes the Origin head only.

## The table

"Leaves" is what the builder keeps of the seven lines of `SPIKES` (0 top, 1 upper pair, 2 middle pair, 3 lowest
pair, 4 top of the back row, 5 back pair, 6 straight back); all are made `LEAF_LENGTH` 0.70 times as long as on H3.

| Value | Side | Genesis looked at | What the art shows | What we build now |
| --- | --- | --- | --- | --- |
| (no Head) | Right | 16, 75, 414, 947, 1529, 37, 40, 42, 76, 82, 83, 88, 112 | Bare heads (#75, #112, #76, #82, #83): cream points on top, short skin spikes on top and 3 or 4 down the far side. The others wear what their Body gives: a dragon hood (#16, #42), a dark hood and red bandana (#40, #88, #414), a yellow hood (#37), an astronaut suit (#947; on #1529 the spikes show inside the helmet ring) | The whole crown, shorter (0.70); a hood or costume replaces it (Body entries) |
| (no Head) | Left | 15, 655, 985, 1509, 1923 | #15: the whole head in cream, a row of cream points on top, no skin spike. The others wear a hood from their Body (a yellow hoodie, frog suits) | The whole crown in the skin colour (Side=Left cream spikes not modelled) |
| (no Head) | none | 53, 434, 973 | One-of-a-kind pictures (1:1 base: a fire creature, a painted face, a frog) | Not built |
| TISM Cap | Right | 3, 412, 991, 1467, 1946 | Red crown, dark brim, white TISM edged dark; 2 to 4 spikes on the far side, the first beside the crown, down to the cheek | Cap, word; leaves 2, 3, 5, 6 short and narrow under the rim (`SIDE_POINTS`) |
| MMGA Cap | Right | 5, 211, 678, 1576, 1936 | Light grey crown, light blue brim, blue MMGA; 2 or 3 spikes on the far side | Cap; `SIDE_POINTS` |
| Horny Cap | Right | 97, 644, 1033, 1438, 1947 | Blue crown, lilac brim, yellow HORNY edged dark; 2 or 3 spikes stacked on the far side from just under the brim | Cap; `SIDE_POINTS` |
| Choggie Cap | Right | 123, 295, 667, 1329, 1922 | White crown, pink brim, purple CHOG; 2 or 3 spikes on the far side | Cap; `SIDE_POINTS` |
| Boob Cap | Right | 114, 633, 1021, 1569, 1939 | Orange crown and brim, white BOOB; 2 or 3 spikes on the far side | Cap; `SIDE_POINTS` |
| Blue Bucket Cap | Right | 80, 574, 966, 1347, 1961 | Plain blue bucket hat; 2 or 3 spikes on the far side | Bucket hat; `SIDE_POINTS` |
| Orange Bucket Cap | Right | 107, 666, 1178, 1711, 1956 | Plain orange bucket hat; 2 or 3 spikes on the far side | Bucket hat; `SIDE_POINTS` |
| Gray Bucket Cap | Right | 62, 620, 1003, 1511, 1958 | Plain light grey bucket hat; 2 or 3 spikes on the far side | Bucket hat; `SIDE_POINTS` |
| Balls Cap | Right | 19, 676, 1195, 1557, 1942 | Purple bucket hat, a white stitched drawing on the top front; 3 spikes stacked from under the brim to the cheek | Bucket hat; `SIDE_POINTS` |
| Cloud Cap | Right | 35, 564, 896, 1321, 1872 | Blue bucket hat, a white cloud of swirls on top; 1 or 2 spikes on the far side (dark skins: hard to see) | Bucket hat; `SIDE_POINTS` |
| Dollar Cap | Right | 6, 329, 943, 1404, 1888 | Cyan bucket hat, a label JUST A KID WITH $1 AND A DREAM; 2 or 3 spikes on the far side | Bucket hat; `SIDE_POINTS` |
| Thunder Cap | Right | 56, 525, 962, 1454, 1964 | Blue bucket hat, a yellow bolt on the top front; 2 or 3 spikes on the far side | Bucket hat; `SIDE_POINTS` |
| Star Cap | Right | 24, 520, 1140, 1542, 1965 | Grey bucket hat, a white star on top; 2 spikes on the far side | Bucket hat; `SIDE_POINTS` |
| Peace hat | Right | 1, 550, 793, 1546, 1940 | Grey-green bucket hat, a white peace sign; 2 or 3 spikes on the far side | Bucket hat; `SIDE_POINTS` |
| Red Peace Cap | Right | 60, 489, 843, 1478, 1762 | Red bucket hat, a white peace sign; 2 or 3 spikes on the far side | Bucket hat; `SIDE_POINTS` |
| Mon Cap | Right | 8, 461, 792, 1309, 1934 | Purple bucket hat, white Monad mark; 1 or 2 spikes on the far side under the brim | Bucket hat; `SIDE_POINTS` |
| Mon Cap | Left | 361, 864, 1065, 1423, 1761 | White cap, purple brim, purple Monad mark; no spike | Cap; no leaf |
| Crown | Right | 44, 858, 1127, 1358, 1899 | Gold crown, red and blue gems; 3 or 4 spikes on the far side from beside the band to the cheek, none inside the crown | Crown; leaves 2, 3, 5, 6 under the band (`CROWN_POINTS`); a gold floor inside, above the head |
| Wizard Hat | Right | 10, 472, 872, 1495, 1809 | Dark blue with yellow stars (its top out of the frame); 2 or 3 larger spikes on the far side from under the hat | Wizard hat; larger points (`BIG_SIDE_POINTS`) |
| Durag | Right | 51, 570, 1001, 1563, 1884 | Dark grey cloth with folds over the brow; 3 large spikes on the far side from the height of the top of the durag to the cheek; no point of hair on the brow | Durag; leaves 2, 3, 5 (`DURAG_POINTS`); the seven points of hair on the brow kept, as the owner asked in batch 3 |
| Spartan Helmet | Right | all 40, closely 21, 331, 1081, 1536, 1903 | A full helmet down to the jaw: cheek guards both sides, a nose guard down between the eyes, eye openings under its brow, mouth and chin open between the guards, a light yellow trim along every edge, a tall red plume from the brow over the top; 3 or 4 spikes behind it on the far side. No glasses or visor on any of the 40 | Rebuilt helmet (edge per angle: brow just above the eyes, nose guard, cheek guards to the jaw, open back); leaves 3, 5, 6 longer, out of the open back |
| Russian Hat | Right | 39, 322, 830, 1177, 1785 | Tan fur crown and roll, fur flaps hanging at the far side; 1 or 2 small spikes on the far side behind the flap, near the top | Ushanka; leaves 5 and 6, small, under the fur at the back (`USHANKA_POINTS`) |
| Russian Hat | Left | 34, 504, 982, 1518, 1876 | Cream fur, a dark leather flap with white snaps at the side of the face; no spike | Ushanka; no leaf |
| Red Headband | Right | 12, 686, 1014, 1790, 1957 | Red band across the brow, a darker line along its lower edge; above it the cream points and the whole crown, and spikes down the far side | Headband; leaves 0 and 1 above the band, 3, 5, 6 under it (`BAND_POINTS`) |
| Japan Headband | Right | 65, 523, 891, 1435, 1962 | White band, a red sun on its front, a grey fold line; the whole crown above it | Headband; `BAND_POINTS` |
| Bob Hair | Right | all 45, closely 43, 48, 159, 470, 482, 1137, 1380 | Yellow curly hair: a big mass on top, puffs beside the face; a red band tied over the brow, both its edges waving, a darker lower edge. The same yellow and red on all 45, whatever the skin. On #43, #470, #1137 the tip of one skin spike shows above the band on the far side | Curls in one yellow, a red band with waving edges around the hair over the brow, curls kept off the band; no leaf (the spike tip is not built: it would have to come through the hair) |
| Rainbow Wig | Left | 29, 346, 1276, 1469, 1967 | An afro in blocks of colour around a face with a curly edge; no spike | Curly wig in six colours; no leaf (unchanged) |
| Clown Nose | Right | 18, 406, 920, 1553, 1926 | A red ball on the nose; the whole crown as on a bare head | Nose ball; the whole crown |
| Plumber | Left | 17, 531, 1325, 1601, 1885 | A red plunger cup at the front of the top, its stick out to the upper right of the picture; around it the points of the head, in cream as Side=Left draws a head | Plunger; every leaf (16 October 2026: the cup on the crown and leaves 0 and 1 taken out until then) |
| Robber | Right | 14, 652, 927, 1692, 1960 | Grey knitted balaclava with ribs, eye holes with a rolled edge; 2 or 3 spikes come out of the knit on the far side, near the top | Balaclava; leaves 2, 3, 5, 6 through the knit, as drawn ("THROUGH", recorded in parts.json) |
| BTC Cap | Left | 162, 492, 702, 1579, 1954 | Orange crown, white Bitcoin B, dark brown brim; no spike | Cap, logo; no leaf |
| MCGA | Left | 103, 568, 1028, 1472, 1951 | Purple cap, white MAKE CHOG GREAT AGAIN in two lines; no spike | Cap; no leaf |
| Chog Cap | Left | 142, 578, 1129, 1362, 1882 | The same drawing as MCGA; no spike | Cap; no leaf |
| Buttcoin Cap | Left | 203, 516, 970, 1378, 1930 | Orange crown, white Buttcoin, dark brown brim; no spike | Cap; no leaf |
| MEGA Chog | Left | 45, 426, 714, 1406, 1891 | Cream crown, dark MEGA CHOG in two lines, orange brim; no spike | Cap; no leaf |
| 10k TPS Cap | Left | 26, 575, 980, 1538, 1823 | White crown, dark grey brim, a purple box: 10K in white (its 0 with a short upright bar inside) over TPS in purple; no spike | Cap, box; a bar inside the 0; no leaf |
| JEET Cap | Left | 63, 563, 1029, 1555, 1963 | Green crown, light green brim, white JEET; no spike | Cap; no leaf |
| RETARD Cap | Left | 46, 379, 929, 1505, 1871 | Purple crown, lilac brim, cream RETARD edged dark; no spike | Cap; no leaf |
| WAGMI Cap | Left | 59, 485, 1025, 1528, 1929 | Magenta crown, pink brim, white WAGMI; no spike | Cap; no leaf |
| NEET | Left | 7, 396, 562, 1022, 1949 | White crown, blue outlined NEET, blue brim; no spike | Cap; no leaf |
| MCGA Cap | Left | 36, 528, 677, 1149, 1900 | Red crown and brim, orange MCGA; no spike | Cap; no leaf |
| Wizard Beanie | Left | 70, 565, 969, 1314, 1948 | Blue beanie, yellow rings on the dome, a tall ribbed cuff; no spike | Beanie; no leaf |
| Choggie Beanie | Left | 4, 271, 841, 1422, 1968 | Dark grey beanie, ribbed cuff, a small lavender spiky Chog patch; no spike | Beanie, patch; no leaf |
| BTC Beanie | Left | 2, 517, 1182, 1392, 1604 | Orange ribbed beanie with a small pizza slice patch (no Bitcoin mark); no spike | Beanie, pizza patch; no leaf |
| RH Beanie | Left | 191, 510, 1017, 1602, 1919 | Lime ribbed beanie, a black feather on the dome; no spike | Beanie, feather; no leaf |
| Mon Beanie | Left | 71, 765, 1002, 1351, 1801 | Light blue beanie, its cuff striped blue and purple; no spike | Beanie; no leaf |
| BOOB Cap | Right (Skull) | 846, 1079, 1522 | Orange cap, white BOOB, on skull heads; no spike | Cap; no leaf (base not built) |
| ETH Cap | Right (Skull) | 1313, 1889 | Pale grey cap with a dark Ether mark, on skull heads; no spike | Cap, logo; no leaf (base not built) |
| Peace Cap | Right (Skull) | 1642 | Red bucket hat with a peace sign, on a skull head; no spike | Bucket hat; no leaf (base not built) |
| 8 Ball Beanie | Right (Skull) | 388 | Dark beanie with an 8 ball on its side, on a skull head; no spike | Beanie, the 8 ball not drawn; no leaf (base not built) |
| Jeet Cap | Left (Skull) | 1221 | Green JEET cap on a skull head; no spike | Cap; no leaf (base not built) |
| BTC Cap | Right (Skull) | 729, 972, 1372 | Orange cap and brim, white Bitcoin B, on skull heads; no spike | Cap, logo; no leaf (base not built) |
| Chog Cap | Right (Skull) | 1324 | The white CHOG cap with a pink brim of Choggie Cap, on a skull head; no spike | Choggie Cap drawing (new entry of `HATS_OF_SIDE`); no leaf (base not built) |

## Not modelled (the later "special Chogs" stage)

17 values, 37 Genesis, all on the Skull or Radioactive base, which the builder does not make. Every value was
looked at on `look_08.png` to `look_11.png`, and the notes below checked again on the 1000-point pictures of the
first two Genesis of each line (the one Genesis when there is one); the others wear the same value.

| Value | Side, base, Genesis | What the art shows |
| --- | --- | --- |
| Red Horn | Left, Radioactive: 275, 473, 672, 1067, 1200 | Two brown ram horns rising from the upper corners of the brow and curling out above the head; the spikes all around |
| Silver wreath | Left, Skull: 207, 418, 939, 1331 | A laurel wreath of pale leaves across the skull |
| 3rd Eye | Left, Radioactive: 598, 703, 1657, 1849 | A third eye on the forehead |
| Demon Horn | Left, Radioactive: 343, 625, 1460 | Five orange ridged horns among the spikes: three on the top of the brow, one low on each side curving up |
| Holy Seal | Left, Radioactive: 415, 743, 1813 | A large gold wheel standing upright above the brow, a grid inside it and short rays around it, each crossed near its tip; the spikes behind |
| Gold Thorn | Left, Skull: 1723, 1819, 1838 | A crown of gold thorns around the skull |
| Purple Horn | Left, Radioactive: 31, 1745 | Two purple ram horns, as Red Horn |
| Void Horn | Left, Radioactive: 140, 1222 | Five lilac ridged horns among the spikes, as Demon Horn |
| Taped Banana | Right, Skull: 373, 978 | A banana taped to the forehead with grey tape |
| Diamond | Left, Radioactive: 834, 1009 | A blue gem on the forehead |
| Red Bandana | Left, Radioactive: 874, 1192 | An orange-red bandana over the head, knotted on top |
| Silver Thorn | Left, Skull: 1107, 1647 | A crown of grey thorns around the skull |
| Blue Laser | Left, Radioactive: 772 | A wide light blue beam leaving the middle of the brow from a white glow, out to the upper right of the picture |
| Poison Shroom | Left, Radioactive: 1100 | Mushrooms growing out of the head among the spikes: pale green caps spotted orange on pale stems, a tall one on top, one leaning out, a small orange one spotted green low on the side |
| devil horn | Left, Radioactive: 1225 | Five red ridged horns among the spikes, as Demon Horn |
| Mafia Hat | Right, Skull: 1311 | A pale fedora with a dark band |
| Head Wrap | Right, Skull: 1861 | A white wrapped cloth (turban) over the skull |
