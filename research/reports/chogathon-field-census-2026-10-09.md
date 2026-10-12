# Chogathon field census, 9 October 2026

First look at who else is building, made on 9 October 2026 between 17:01 and 17:15 Paris,
right after sign-up closed. **It is a sample, not the field:** the site counts 311
applications and this census found eight public repositories: six entries with code, one
empty placeholder and one probable cross-submission from another hackathon.
Private repositories, entries not named after Chog and everything started later are invisible
here. The census must be run again before the deadline.

Facts below come from the hackathon page, from GitHub and from reading the code of each
repository (no code was run). Each section says which. What is inference is labelled.

## 1. How the site works once sign-up is closed

Read from the page itself (`https://chogathon.chog.xyz/`, one HTML file, 164,211 bytes at
16:55 and at 17:01 Paris). The page can change: these are facts about that version.

| Point | What the page does |
| --- | --- |
| Dates | sign-up ended 9 Oct 15:00 UTC; the hackathon ends 14 days later, 23 Oct 15:00 UTC (17:00 Paris) |
| Applications | 311 shown publicly. One row per person: project name, team name, role, members, X and Discord handles, pitch. Editing is closed |
| Public gallery | reads the table `projects`: **0 rows** at 17:01, 17:03 and 17:12 Paris |
| Adding a project | refused once sign-up is closed ("Submissions are closed, projects can no longer be added"); before that the dashboard was locked. Nobody could add one through the page, so the empty gallery says nothing about how many teams build |
| Judging view | for the organisers, behind an X login. It lists the **applications** as entries and scores each on Novelty 30, Fun 20, Retention 20, Clarity 15, Execution 15, with an "NFT gate" switch (off counts as disqualified) and a status New / Reviewed / Qualified / Disqualified. Scores stay in the judge's browser and are exported as CSV |
| Final deliverables | **no form** for the demo, the video or the repository in this version. Where and when they are handed in is not known from the page |

To do: ask the organisers (Discord) how the final deliverables are submitted, or watch the page.
The gallery read is scripted: `python3 -I research/scripts/census_chogathon_gallery.py`
([result](../evidence/chogathon-gallery-2026-10-09-after-opening.json)).

## 2. Routes tried

| Route | Result |
| --- | --- |
| Gallery table, the way the page reads it | 0 rows |
| Page code | section 1 |
| Web search, three plain queries on "Chogathon" | nothing indexed |
| Web search, thorough mode, "Chogathon" | three GitHub repositories |
| GitHub repository search: `chogathon` in name, description or README; `chog` created since 25 Sep; `chog` updated since 6 Oct | 35 results, 7 relevant |
| GitHub code search: `chogathon`, the Genesis contract address, the $CHOG address | token lists, one repository already found and one more (declub); fresh repositories are not indexed |

Not tried: X (needs a login) and the Chog Discord, where teams are most likely to show work.

## 3. What was found

All commits of every repository predate the building window (9 Oct 15:00 UTC), which is
expected a few minutes after it opened. "Real chain read" means the code calls the Genesis
contract `0xc96d31f8626c6d03fae5dcd3d61e3fb9f4a73763` on Monad mainnet (chain 143).

| Repository | What it is | Chog NFT | $CHOG | Public demo | Size |
| --- | --- | --- | --- | --- | --- |
| [cipoklean/chog-mischief](https://github.com/cipoklean/chog-mischief) | asynchronous daily "prank war": each Genesis pranks one other per day, the target may dodge | real chain read at sign-in and at each action; **traits decide powers**; **state kept per token id**, not per wallet | none, by choice | `chog-mischief.vercel.app` answers, with a guest path that needs no NFT | 42 commits, 8 and 9 Oct, tests, written fairness model |
| [Afghanistan8/ryoko-chog](https://github.com/Afghanistan8/ryoko-chog) | each Chog gets its own wallet (ERC-6551) and an agent that walks it through nine timed stages; idle progression with a leaderboard | real chain read; progress is void when the holder changes; traits only flavour text | **real**: each stage burns 1,000 $CHOG; contracts deployed on mainnet | `ryoko-chog.vercel.app` answers | 14 commits, 9 Oct, 58 contract tests; a stage lasts at least 2 days |
| [PamanSam01/Chogram](https://github.com/PamanSam01/Chogram) | studio that renders a Genesis into short videos (intro, price alert, meme) | real read exists but nothing enforces ownership: samples work without a wallet | real burn of 500 $CHOG for a "premium" that only the browser checks | `chogram.vercel.app` answers; video export is off there | 1 commit, 8 Oct |
| [OluwapelumiAdebisi/ChogHub](https://github.com/OluwapelumiAdebisi/ChogHub) | the Chog becomes a chat agent that lists events and quests; history attested per token | a mock collection on testnet; the Genesis address is absent | a mock token on testnet | `chog-hub.vercel.app` answers | 2 commits, 9 Oct |
| [catalystberry842-alt/chog-world-order](https://github.com/catalystberry842-alt/chog-world-order) | wallet "passport" with reputation stamps and a board of paid collaborations | **none**: "NFT ownership is optional" | none | not stated | 4 commits, 9 Oct |
| [NathanOyewole/possess](https://github.com/NathanOyewole/possess) | one page: "possess" a token id for a 24 h run, click to collect scars. Pitch: "the NFT is the save file" | real `ownerOf` gate; traits not read; the run lives in one browser only, so nothing passes to a buyer | read of the balance only | none found | 4 commits, 434 lines, 7 Oct |
| [Leno3z4/Chog-Fight-club](https://github.com/Leno3z4/Chog-Fight-club) | a README of one line: "a battle royal of the sorts" | nothing yet | nothing yet | the stated address answers 404 | placeholder, 6 Oct |
| [nhatquang261002/declub](https://github.com/nhatquang261002/declub) | clubs and an NFT swap market for any Monad collection; probably built for another hackathon track, Chog Genesis is its demo collection | a testnet copy of the collection; Chog is not required | none | `declub.fun` answers | 145 commits since 28 Sep, 455 contract tests |

Not entries: `PettyMiggzy/chogi` (a separate token project, no commit since July),
`0xDeRauX/chog_dash` (a dashboard rebuilt daily), `comfreec/chogpan` (unrelated).

## 4. What it means for us (inference)

Read with the limits above: eight repositories out of 311 applications, read on day 0.

- **Nobody in the sample builds a real-time or multiplayer game.** The one name in our
  category, Chog Fight Club, is an empty repository. To watch.
- **Our two pillars are not unique.** chog-mischief already ships "traits decide what the
  hero can do" and "history follows the token, not the wallet", with a live address. What
  nobody else in the sample has is a match played in real time between holders, and attacks
  drawn from the art of each Genesis.
- **Four entries already answer at a public address a judge can open,** one with a path that
  needs no NFT. We have none. This is our largest gap and it is not a development problem: it waits
  for a hosting decision ([DEPLOY](../../docs/DEPLOY.md)).
- **The $CHOG bonus is already claimed for real** by ryoko-chog (burn on mainnet, contracts
  deployed). Our shop contract is written and tested but neither deployed nor used by the game.
- **A judge without a Genesis must still see the product.** chog-mischief gives guests bots to
  play against. With us a visitor without a Genesis plays a recruit, which shows none of what
  is new. A "try a Genesis" entry for judges is a decision to take (today the test option
  `?token=` does it, and it is also a hole).
- **Pre-existing work has to be declared.** Every repository in the sample was written before
  the window opened; so was ours. The deliverables ask for it.

No ranking is attempted: the sample is too small and the judges' real weights are not known.
