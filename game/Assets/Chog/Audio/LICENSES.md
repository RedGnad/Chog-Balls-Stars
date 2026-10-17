# Audio of the game: sources and licences

Every file under `Resources/ChogAudio/` is CC0 1.0 (public domain dedication,
https://creativecommons.org/publicdomain/zero/1.0/): no attribution is required and commercial use is allowed.
Credit is given here anyway. Nothing here is CC-BY or non-commercial. Collected on 17 October 2026; each licence
was read on the source page and, where the pack has one, in the licence file inside the download.

Treatment of every file: decoded, converted to Ogg Vorbis 44.1 kHz (sounds mono at quality 4, music stereo at
quality 3), the gain lowered so the true peak stays near -1 dBTP, short tails trimmed where noted. Unity's WebGL
build re-encodes every clip as AAC (https://docs.unity3d.com/6000.2/Documentation/Manual/webgl-audio.html).

## Files

| File (under `Resources/ChogAudio/`) | Source file | Pack, author | Changes |
| --- | --- | --- | --- |
| `Hit/hit_punch_1.ogg` | `impactPunch_medium_000.ogg` | Impact Sounds, Kenney | cut at 0.30 s, fade from 0.18 s |
| `Hit/hit_punch_2.ogg` | `impactPunch_medium_001.ogg` | Impact Sounds, Kenney | same |
| `Hit/hit_punch_3.ogg` | `impactPunch_medium_003.ogg` | Impact Sounds, Kenney | same |
| `Hit/hit_punch_4.ogg` | `impactPunch_medium_004.ogg` | Impact Sounds, Kenney | same |
| `StructureHit/structure_knock_1.ogg` | `impactPlank_medium_001.ogg` | Impact Sounds, Kenney | cut at 0.45 s, fade from 0.30 s |
| `StructureHit/structure_knock_2.ogg` | `impactPlank_medium_002.ogg` | Impact Sounds, Kenney | same |
| `StructureHit/structure_knock_3.ogg` | `impactPlank_medium_003.ogg` | Impact Sounds, Kenney | same |
| `StructureFall/structure_crash_1.ogg` | `explosionCrunch_000.ogg` | Sci-Fi Sounds, Kenney | none |
| `StructureFall/structure_crash_2.ogg` | `explosionCrunch_002.ogg` | Sci-Fi Sounds, Kenney | none |
| `Death/death_slam_pop.ogg` | `impactPunch_heavy_002.ogg` mixed with `balloon_pop.flac` | Impact Sounds, Kenney; Balloon Sounds, AntumDeluge | the two summed |
| `Death/death_pop.ogg` | `balloon_pop.flac` | Balloon Sounds, AntumDeluge | none |
| `Respawn/respawn_chime.ogg` | `confirmation_002.ogg` | Interface Sounds, Kenney | 6 dB lower |
| `Respawn/respawn_whoosh.ogg` | `maximize_002.ogg` | Interface Sounds, Kenney | 8 dB lower |
| `Click/click_1.ogg` | `click2.ogg` | UI Audio, Kenney | none |
| `Click/click_2.ogg` | `select_002.ogg` | Interface Sounds, Kenney | none |
| `Reward/reward_glass.ogg` | `glass_001.ogg` | Interface Sounds, Kenney | an octave lower (resampled); the game raises it one note a step |
| `MusicMenu/menu_loop.ogg` | `379 [Misc Extras] bgm_menu_title_1_relaxed.ogg` | 12 Music Loops, SubspaceAudio | none; 54.857 s, 32 bars at about 140 BPM, loops on a bar |
| `MusicMatch/match_loop.ogg` | `129 [Retro Sports] bgm_stage_3.ogg` | 12 Music Loops, SubspaceAudio | none; 60.000 s, 40 bars at about 160 BPM, loops on a bar |

## Where each pack comes from

- **Kenney (Kenney Vleugels, https://kenney.nl).** Each page's table reads "License: Creative Commons CC0"; the
  License.txt in each zip reads "(Creative Commons Zero, CC0) http://creativecommons.org/publicdomain/zero/1.0/".
  - Impact Sounds: https://kenney.nl/assets/impact-sounds,
    https://kenney.nl/media/pages/assets/impact-sounds/87b4ddecda-1677589768/kenney_impact-sounds.zip
  - Interface Sounds: https://kenney.nl/assets/interface-sounds,
    https://kenney.nl/media/pages/assets/interface-sounds/fa43c1dd4d-1677589452/kenney_interface-sounds.zip
  - UI Audio: https://kenney.nl/assets/ui-audio,
    https://kenney.nl/media/pages/assets/ui-audio/490d233f68-1677590494/kenney_ui-audio.zip
  - Sci-Fi Sounds: https://kenney.nl/assets/sci-fi-sounds,
    https://kenney.nl/media/pages/assets/sci-fi-sounds/6b296f9ecf-1677589334/kenney_sci-fi-sounds.zip
- **Balloon Sounds (AntumDeluge, OpenGameArt).** https://opengameart.org/content/balloon-sounds, file
  https://opengameart.org/sites/default/files/balloon_pop.flac; licence box "License(s): CC0". Its pop comes from
  "Balloon-Burst-05.wav" by Gniffelbaf, https://freesound.org/people/Gniffelbaf/sounds/82119/, licensed CC0 there.
- **12 Music Loops (SubspaceAudio, OpenGameArt).** https://opengameart.org/node/170803, file
  https://opengameart.org/sites/default/files/12_indie_game_music_loops_0.zip; licence box "License(s): CC0".

The music and the sounds were chosen from their measurements (length, loudness, attack, spectrum) by a session
that could not listen to them: they are to be heard and kept or replaced by a person.
