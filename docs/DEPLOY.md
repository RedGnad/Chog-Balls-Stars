# Putting the game online

**Nothing is deployed on a host.** Since 12 October 2026 the game has a public https address
through a tunnel from the development Mac (section "A public address from this Mac"): it
lives only while that Mac and the tunnel run. This page describes the path that was prepared
and tried on the development Mac, and what is still to decide. The judges need an address
that stays up without this Mac: this is the largest open gap.

## What gets deployed

One Node process, `server/src/host.js`, serves everything on one address:

| Address | What | Where it comes from |
| --- | --- | --- |
| `/` | the game (Unity Web build) | `site/game/` |
| `/hero/` | the "find your hero" page | `web/hero/` |
| `/genesis-art/256/` | the portraits | `site/genesis-art/256/` |
| `/api/` | the game services | `server/src/` |

One address matters: the game and the page call the services next to themselves, and the
message a wallet signs names that address.

## Steps

1. **Assemble the files** from a build that was checked:

   ```sh
   sh tools/release/check_all.sh                  # every check that runs without the Editor
   python3 tools/web/build_hero_data.py --check
   python3 tools/release/assemble_site.py --build <folder under .artifacts/unity>
   ```

   This fills `site/` (82 MB: the game, the portraits and their cuts, and since 12 October
   the judge: `site/judge/` with the tables it reads, `site/quantum-db.json` with the
   assets of that build) and writes `site/RELEASE.json`. The judge is built at that moment
   from the simulation sources: assemble right after the build. `site/` has to be in the
   repository the host builds from. `sh tools/release/check_judge.sh <dotnet>` replays the
   recorded matches with it.

2. **Try it here**, as the host will start it:

   ```sh
   npm ci --prefix server
   PUBLIC_ORIGIN=http://<this machine>:8087 PORT=8087 node server/src/host.js
   ```

3. **Create the service.** Since 12 October the service is a container (`Dockerfile` at the
   root): Node 22 for the services and the .NET 8 runtime for the judge, with `site/`,
   `server/src` and `web/hero` copied in. Building the image replays the recorded matches of
   `tests/fixtures/replays/` with the image's own .NET (`tools/release/check_judge.sh`) and
   fails if a winner differs, so a host never starts with a judge that does not work.
   `render.yaml` describes the service for Render, the host of the earlier game's server:
   the free plan for the first tries (the user's choice, kept awake by a ping from outside;
   no disk, so the careers and the records are lost each time the container is replaced),
   the settings, and in a comment the disk to add with the plan "starter". `PUBLIC_ORIGIN` is
   asked at creation: give the final public address, without a trailing slash. Render builds
   from a Git repository. **The image has never been built**: this Mac has no container
   engine running (8 GB of memory, the Editor open); `.github/workflows/release-image.yml`
   builds and starts it on GitHub's machines at each push, which will be its first test.
   Measured on this Mac: the judge takes 226 MB and about 5 seconds for a match of three
   minutes; the free and starter instances have 512 MB.
   Any other host that runs this container with a folder that survives restarts works the
   same way; the settings are listed at the top of `server/src/host.js`.

4. **Check the deployed address** before giving it to anyone: the game loads, a portrait
   shows, `/hero/?n=1287` shows a card, `/api/health` answers, a wallet connects and signs in
   from the browser of a real wallet. The message the wallet shows must name the public
   address.

## Photon: who a player is, and the record of a match

The secured match (D33) needs Photon's cloud to call the game services. That takes a public
https address for the services and about ten minutes in Photon's dashboard, on the Quantum
application the game uses. **Done on 12 October 2026 with the address of the tunnel, and
Photon calls these services** (what was seen is at the end of this section). The values
below come from Photon's manual as read on 11 October 2026
(`doc.photonengine.com/quantum/current/manual/webhooks` and
`.../realtime/current/reference/custom-authentication`).

The secret: the services make one at their first start and keep it in their data folder
(`photon-secret`), unless `PHOTON_SECRET` is set. It goes in two places of the dashboard and
nowhere else; never in the game or the repository.

Where things are in the dashboard, as found on 12 October 2026 in the user's account: the
game's application is "Chog Arena", of type Quantum, under APPS, "Development"
(`https://dashboard.photonengine.com/publiccloud/development`); its page is
`https://dashboard.photonengine.com/app/manage/<AppId>`, with "Add a new authentication
provider for: CUSTOM SERVER" and, under "Plugins", "Quantum 3" with EDIT, which opens a list of
key and value pairs. That night the dev agent filled both forms in the user's browser with
the public address of the tunnel and every value below except the secret, which it is not
allowed to type into a page; the secret was put in the clipboard for the user to paste twice
and save. The user saved the authentication provider from the prepared form, and the plugin
from another form that held the secret alone; the dev agent then added the fifteen other
pairs to that saved configuration and saved it (16 pairs in all). A secret is copied without
being shown with `tr -d '\n' < <data folder>/photon-secret | pbcopy`. The page of the
application also has a box "Allow anonymous clients to connect, independently of configured
providers", which is not ticked: since the provider exists, a game that does not identify
its player cannot connect at all.

1. **Authentication** (dashboard, the application, "Authentication", custom server):

   | Field | Value |
   | --- | --- |
   | Authentication URL | `https://<public address>/api/photon/auth` |
   | Key/value pair | `key` = the secret |
   | Reject all clients if not available | yes |
   | Allow anonymous clients | no, once the game is told to authenticate (step 3) |

2. **Webhooks** (dashboard, the application, "Plugins", edit, one pair per line):

   | Key | Value |
   | --- | --- |
   | `WebHookBaseUrl` | `https://<public address>/api/photon/hook` |
   | `WebHookSecret` | the secret |
   | `WebHookEnableOnCreate`, `WebHookEnableOnJoin`, `WebHookEnableOnLeave`, `WebHookEnableOnClose` | `true` |
   | `WebHookEnableGameConfigs`, `WebHookEnableAddPlayer`, `WebHookEnablePlayerAdded`, `WebHookEnablePlayerRemoved`, `WebHookEnableGameResult`, `WebHookEnableReplay` | `true` |
   | `WebHookEnableCreateGame`, `WebHookEnableRuntimePlayer`, `WebHookEnableRuntimeConfig` | `true` (other spellings of the same switches found in Photon's own pages; harmless) |
   | `WebHookReplaySendIntervalInSec` | `10` |

   Photon says a change takes up to a minute.

3. **Tell the game**: start the services with `PHOTON_AUTH=1` (`--photon-auth` locally). The
   game then connects to Photon with the wallet's session or the guest's pass. Do this after
   step 1: with the switch on and the dashboard not set up, nobody can connect.

4. **The judge**: the services need the program that plays a match again and the simulation
   assets of the build that is online (`VERIFIER_DLL`, `QUANTUM_DB`, see the top of
   `server/src/host.js`), and .NET 8 on the same machine. `render.yaml` describes a Node
   service without .NET: a match gets no verdict there until that is written. On this Mac it
   works today.

5. **Check with one real match**: open the game, sign in, play a match of two players to
   its end, then look in the data folder: `matches/<name of the match>/` must hold
   `match.json` (who entered, with which Genesis), `replay-start.json`, the
   `replay-chunk-*.json` and, a few seconds after the room closes, `verdict.json`. If the
   shape of what Photon sends differs from the manual, it will show here first.

**What was seen on 12 October 2026, 03:06 to 03:17, with Photon's real cloud** (one match
played in the headless browser from the public instance, as a visitor who asked for Genesis
1795 by the test entry `?token=`):

| Step | What Photon sent | What came of it |
| --- | --- | --- |
| Connecting | `GET /api/photon/auth` with the key | 200: a visitor, with a name of its own |
| Creating the match | `game/create`, `game/configs` | 200; the match is named `Quantum:RU:<room>~<our suffix>` |
| Entering | `player/add`, `player/added` | the admission records `askedToken 1795`, `tokenId 0`: no right to it, a recruit, and the game does play a recruit |
| Playing | `replay/start`, then a `replay/chunk` every 10 seconds (36 in all) | kept as sent |
| The end | `game/result`, `player/removed`, `game/leave`, `game/close` | the judge replays Photon's record a few seconds later: team 1 won, 6 to 10 by coins, as the game announced ("Defeat" for team 0) |

The journal and the verdict of that match are kept as
`research/evidence/photon-first-real-match-journal-2026-10-12.json` and
`photon-first-real-match-verdict-2026-10-12.json`. **Not seen:** a signed-in wallet or a
guest with a pass going through (only a visitor did), two players in one match, a match with
a stake, what Photon does when the address does not answer.

Since the webhooks are set, every match created with this Photon application calls the
public address, from whichever page the players opened (the local test addresses too): a
test entry `?token=` no longer gives its Genesis, since the admission knows no right to it.
The manual says a webhook that fails is tried three more times, then "the request fails";
that this refuses the creation of the match is expected and **not yet seen**. To go back:
clear `WebHookBaseUrl` in the dashboard, and tick "Allow anonymous clients" or keep
`--photon-auth`.

## A public address from this Mac (a tunnel)

Allowed by the user on 12 October 2026 ("oui je valide"). It makes the game on this Mac
reachable from the internet while it runs. The tunnel points at the same program a host would
start (`server/src/host.js` on the assembled `site/`), not at the test server: the test
server forwards neither the secret header of Photon's requests nor a body over 100 KB.

```sh
python3 tools/release/assemble_site.py --latest        # site/, with the build's simulation assets
ngrok http 8087                                        # prints the public address
sh tools/release/serve_public.sh https://<public address> --port 8087   # add --photon-auth later
sh tools/release/check_public.sh https://<public address>               # what a browser and Photon would ask
sh tools/release/browse_public.sh https://<public address> <label>      # menu to match in the headless browser
```

| What | Value |
| --- | --- |
| Data of the public instance | `.artifacts/services-public/` (its own careers, session secret and `photon-secret`), apart from the local test services |
| The judge | on: `serve_public.sh` builds the replay program and gives it the assets of the assembled build (`site/quantum-db.json`, never served) |
| Stakes | off: no wager contract is deployed |
| Test entries | `?token=`, `?charged=` and `?bots=` are ignored on a public address; `?perf=1` still shows the measuring overlay |

Seen through the public address on 12 October, 01:36: the page, the three compressed build
files with `Content-Encoding: gzip` (23 MB together), a portrait, the hero page, the services
(`/api/health`), a refusal (403) of Photon's two entries without their secret, and one run
from the menu into a match in the headless browser, as a recruit. **Not seen:** a phone, a
real wallet, Photon calling in.

Limits of the free ngrok plan (its documentation, read 12 October 2026,
`https://ngrok.com/docs/pricing-limits/free-plan-limits/`): 1 GB of data out and 20,000
requests a month, so about 40 whole loads of the game; a warning page in front of a
browser's first visit ("Visit Site", then quiet for 7 days; requests that are not from a
browser, such as Photon's, never see it); and with the account as it is set up on this Mac
the address is drawn at random at each start of the tunnel, so restarting ngrok means
restarting `serve_public.sh` with the new address and correcting Photon's dashboard. A file
asked without "Accept-Encoding: gzip" comes back unpacked (46 MB for the program alone):
`check_public.sh` asks for headers only.

## The wallet connector: a Reown project id

A player on a phone's normal browser connects a wallet app through Reown AppKit
(`game/README.md`, "A wallet app connected from the normal browser"). It needs a project id
from `https://dashboard.reown.com` (create a project, copy "Project ID"; if the project
lists allowed domains, the public address of the game must be among them). In that
dashboard, switch off the sign-in by e-mail and by social accounts for the project: the page
asks for neither, but what the dashboard has on wins over the page (read in the code of
AppKit 1.8.24: "remoteFeatures.email ?? features.email ?? true"), and an account made that
way holds no Genesis and counts as a monthly user. The id is a setting of the deployment,
never a line of the repository:

| Where | How |
| --- | --- |
| A host | the setting `REOWN_PROJECT_ID` |
| This Mac, public instance | a file: `pbpaste > .artifacts/services-public/reown-project-id`, then start `serve_public.sh` again |
| This Mac, local test services | the same file in `.artifacts/services/`, or `--reown-project-id` |

Without it `GET /api/wallet` answers an empty id and the page offers only the wallet of the
browser. Reown's terms as read on 12 October 2026 (its licence of 25 August 2025,
`https://raw.githubusercontent.com/reown-com/appkit/main/LICENSE.md`, and
`https://reown.com/pricing`): free up to 2,500,000 RPC calls and 500 users a month, a user
being an account "that authenticates and connects"; its FAQ says "only authenticated
users count toward this, not self-custodial connections", and the game signs players in
itself, without Reown's sign-in. Above that, a paid plan (Pro shown at 890 USD a year).
Reown may change these thresholds. **Never run with a real project id or a real wallet.**

## Why not GitHub Pages

Asked by the user on 12 October. GitHub Pages "is a static site hosting service": it sends
files and runs nothing, so the game would open there without its services, which means a
recruit only (no sign-in, no Genesis, no career, no loan, no stake, no record of a match).
It also cannot be told to send `Content-Encoding`, so the build would have to be made with
Unity's "Decompression Fallback" (files named `.unityweb`, "a large loader and a less
efficient loading process", Unity manual, "Deploy a Web application"). Limits read on
12 October: a site of 1 GB at most, 100 GB of traffic a month, and "not intended for or
allowed to be used as a free web-hosting service to run your online business". A host that
runs the Node process is needed in any case; it then serves the game as well.

## What was tried, and what was not

Tried on the Mac, through the LAN address: the host started from the assembled `site/`; the
build files are sent with their `gzip` header; the hero page, a portrait and the ownership read
answer; a whole menu-to-match run in headless Chrome with a stand-in wallet; a sign-in message
that names `PUBLIC_ORIGIN` even when the request claims another address. 27 service tests.

Tried on 11 October, on the Mac: a whole staked match with Photon's part played by a script
(`sh tools/contracts/rehearse_secure_match.sh`, see `docs/STATUS.md`). 67 service tests.

Tried on 12 October: the same host program behind an https tunnel (section above).

Tried on 12 October, later: Photon's real cloud calling these services through the tunnel,
for a whole match that the judge then replayed ("Photon" section).

Not tried: Render itself, a container, a domain name, a real wallet, a phone on the public
address, more than one player at a time on the services.

## To decide (the user)

| Question | Why it is open |
| --- | --- |
| Which host, and a paid instance or not | On Render a disk needs a paid instance. A free one loses every career at each restart, sleeps after 15 minutes without traffic and takes about a minute to wake (Render documentation, read 9 October 2026). Prices as a search of 12 October gave them from Render's page (its Markdown copy is `https://render.com/pricing.md`; not opened directly): Starter 7 USD a month with 512 MB and half a CPU, custom Docker containers allowed, a disk 0.25 USD per GB a month. The judge needs .NET 8 beside Node, so the service has to be a container (a Dockerfile, not written yet); whether 512 MB is enough for a replay was not measured. |
| Which address | `PUBLIC_ORIGIN` must be the address players open: it is the one written in the sign-in message, and a wallet may warn when it differs from the page. |
| The test entry `?token=` | Settled in code on 9 October, to be confirmed (D26): on a public address `?token=`, `?charged=` and `?bots=` are ignored; they keep working on this computer and the local network. A judge or a friend without a Genesis plays one through a link its holder lends (LEND IT on the menu, `?pass=`), valid thirty days at most. To give the judges access: sign in with the wallet that holds a Genesis on the public address, lend it, and put the link in the testing instructions. |
| The rental in $CHOG | Optional. A holder offers a Genesis at a price a day and any wallet rents one once a rental contract is deployed and its address set as `RENTAL_ADDRESS`; without it neither button shows. To decide: the share of each payment that is burned (fixed at deployment), whether several players may rent one Genesis at once (built that way), whether the free link loan stays beside it, and who deploys ([contract notes](../contracts/README.md)). Never run on the real chain. |
| The $CHOG shop | Optional. The game sells the second form of an attack once a shop contract is deployed and its address set as `SHOP_ADDRESS`; without it nothing is offered. To decide: the price, where the $CHOG goes (treasury, burn, or a split fixed at deployment), and who deploys and owns the contract ([contract notes](../contracts/README.md)). Never run on the real chain. |
| A private repository first | Render builds from a Git repository, and a private one raises none of the questions of the next row. Asked of the user on 12 October, not created. Before the first commit: `python3 tools/release/scan_secrets.py` names the files that may carry a secret without printing any value. Its run of 12 October named 51 files, by their look all hashes, transaction ids and public addresses; the four that looked like more were read: an Alchemy address with its public "demo" key, the $CHOG token address (twice), and the census script, which reads the hackathon site's key from the page at run time and stores none. The whole list has not been read file by file. The tree is about 500 MB without what `.gitignore` leaves out; no file is over 100 MB. |
| What goes in a public repository | `site/` carries the collection's art (the user states the project holds the rights, D19) and a build that contains the sample's third-party assets. The source tree carries the Photon sample: its licence terms (7.3) allow the sample code "in the Application" and nothing grants publishing its source, and its animations, UI images and effect prefabs count as assets that may not be redistributed (`docs/PHOTON-ARCHITECTURE-AUDIT.md`, rows 19 and 34, and its list of what must be replaced). The licence of the Cartoon FX pack was not read. The hackathon asks to share the repository: which repository, and with what in it, has to be settled before the first commit. |

## Limits of what would be online

The limits of the services apply unchanged (`server/README.md`): the careers are still
credited from what the players' pages report (only the stakes are paid on the replayed
match), the careers are one JSON file on one disk with no backup, one
instance only, no rate limit beyond one credited match a minute per Genesis. Photon's own
limits (concurrent players of the AppId's plan) were not looked at.
