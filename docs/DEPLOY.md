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

### What Photon hands back of the cookie (seen 17 October 2026)

The `AuthCookie` our custom authentication answers comes back in the webhooks, but an object nested in it does not
come back as an object: `{"pass": {"tokenId": 1795, "lender": "0x80fb...", "issuedAt": 1792227857183}}` was handed back
as `"pass": [[1795], ["0x80fb..."], [1792227857183]]`, its keys lost (seen at `/api/admissions/recent` on Render, a guest's
admission). Flat fields came back as sent (`passTokenId`, `passLender`, `passIssuedAt`, numbers as numbers). So the cookie
carries only flat fields that matter (`server/src/photon.js`, `passOf`); the guests' path had only ever been tested with
our own requests before, never through Photon's cloud, and every guest played a recruit until then.

### Photon's limit on application versions (found 17 October 2026)

At 16:41 UTC Photon began refusing every client that gave it a version it had not seen:
`OperationResponse 230: ReturnCode: 32767 (Too many application versions are in use)` on the master
server, then `DisconnectException: InvalidAuthentication` in the game. The version already in use in
production kept working. Each build had named itself to Photon by its own stamp, once per mode
(`-siege`, `-coins`): about forty versions in a day of builds. Photon's glossary only says "There is a
limit of how many an AppId can have in parallel"
(https://doc.photonengine.com/realtime/v5/reference/glossary); no number and no expiry are published.

Since then the game gives Photon `sim-<16 hex digits>`, a SHA-256 of what decides how a match plays:
the simulation's code and schema (`Assets/QuantumUser/Simulation`, `.cs` and `.qtn`), Quantum's
assemblies and the exported simulation assets (`QualificationBuild.SimulationStamp`, stable across
two builds of the same code). The build writes it into `Resources/ChogSimulationStamp` and into
`simulation.txt` beside the build; `assemble_site.py` records it as `simulation` in `RELEASE.json`;
the services compare rooms and judge matches with it (`servedPhotonVersion`, `node.js`), or with the
build as before when a release has none. A build that changes only screens or looks keeps the name.
**Before deploying a build whose stamp is new, check that a local build with that stamp can connect**:
the console of a capture shows the line above when Photon refuses.

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
restarting `serve_public.sh` with the new address and correcting Photon's dashboard. Tried
on 14 October 2026: `ngrok http 8087 --url 'https://'`, which ngrok's manual
(`https://ngrok.com/docs/universal-gateway/domains/`) gives for the account's own dev domain,
also drew a new address on this account (ngrok 3.25.1). A file
asked without "Accept-Encoding: gzip" comes back unpacked (46 MB for the program alone):
`check_public.sh` asks for headers only. Everything served goes up the Mac's own connection: on
16 October 2026, with the Mac on a phone's shared connection, the tunnel gave 0.46 MB/s (about a
minute for a first visit of the 24 MB game); the header `ngrok-agent-ips` of any answer through the
tunnel names the address the Mac goes out from.

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

## Render, step by step (free plan, written 16 October 2026)

What Render's pages said that day (`https://render.com/docs/free.md`, `https://render.com/pricing.md`,
`https://render.com/docs/infrastructure-as-code.md`): a free web service has 0.1 CPU and 512 MB, sleeps
after 15 minutes without a request and takes about a minute to wake, loses its files when it sleeps or
restarts, and cannot have a disk; a workspace gets 750 free instance hours a month (one service awake all
month is 744), 500 build minutes and 5 GB of outbound traffic, beyond which a workspace without a card is
suspended until the next month. A Docker service may be free (the `docker` runtime and the `free` plan of
the Blueprint reference). The build of the game is 24 MB, so 5 GB is about 200 first visits.

1. Sign in on render.com with GitHub and let Render read `RedGnad/chogathon` (private).
2. New, Blueprint, the repository, branch `main`: Render reads `render.yaml` and asks the values marked
   `sync: false`. `PUBLIC_ORIGIN`: `https://chog-genesis-game.onrender.com` (the address Render gives the
   service; if the name is taken it adds a suffix, then correct the value once the address shows).
   `PHOTON_AUTH`: `1`. `PHOTON_SECRET`, `REOWN_PROJECT_ID`, `REFEREE_KEY`: the contents of `photon-secret`,
   `reown-project-id` and `mainnet-referee-key` in `.artifacts/services-public/` on the development Mac
   (copied with `pbcopy < <file>`, never printed). The secret is the one Photon's dashboard already holds.
3. Deploy. The first build of the container ever made happens here; it replays the recorded matches and
   fails if a winner differs. Its log is what to look at if it fails.
4. `sh tools/release/check_public.sh https://<address>` from the Mac.
5. Photon's dashboard: only the two addresses change (`/api/photon/auth`, `/api/photon/hook` on the new
   address). From then on the instance on the Mac no longer hears from Photon.
6. Reown's dashboard: add the new domain to the project's allowed domains.
7. cron-job.org: a request to `https://<address>/api/health` every 10 minutes keeps it awake.

Each push to `main` that changes what the image is made of (the `buildFilter` of `render.yaml`: the files
the Dockerfile copies, and `render.yaml`) deploys again and so empties the careers and the records of the
matches; a push of docs, research or the 3D tools alone does not (`https://render.com/docs/monorepo-support.md`,
"Setting build filters"). A commit can also skip its deploy with `[skip render]` in its message
(`https://render.com/docs/deploys.md`).

Done on 16 October 2026: the service is `https://chog-genesis-game.onrender.com` (no suffix was needed) and
step 4 passed from the Mac (page, build files in gzip, hero page, the three contracts on mainnet, the owner's
Genesis read, Photon's entries refused without the secret). Photon's dashboard and the cron were still to do.

## The public address: Vercel in front of Render (16 October 2026)

The game is **Chog Balls Stars** (named Chog Arena until 22 October 2026; the address keeps that name) and its address is `https://chogarena.vercel.app` (project `chogarena` of the owner's
Vercel account; `chog-arena.vercel.app` belonged to someone else). Vercel serves the pages: the game, the hero
page and the portraits, laid out by `sh tools/release/vercel_site.sh` from `site/` and `web/hero`, and published
with `sh tools/release/vercel_site.sh deploy` (the vercel CLI, logged in as the owner). Everything under `/api/`
is passed on to Render by a rewrite of `vercel.json`, so a page and its services keep one address. Photon still
calls Render directly, and cron-job.org keeps calling `https://chog-genesis-game.onrender.com/api/health`.

- `PUBLIC_ORIGIN` is set by `render.yaml` (a value, no longer `sync: false`, which Render ignores after the first
  creation); the Blueprint's automatic sync applied it about two minutes after the push. A page asked on Render
  is sent to the public address (308, `movedToPublic` in `server/src/static.js`); `/api/` is not.
- **After each build: publish both.** `game/create` refuses a page whose build differs from the one Render
  serves, so a push that changes `site/` is followed by `vercel_site.sh deploy` as soon as Render shows the new
  build (`/api/photon`).
- Reown: the address must be in the project's allowed domains for a phone's wallet app to connect (the owner's
  step). A wallet of the browser (MetaMask, Rabby) does not need it.
- Vercel's Hobby limits (`https://vercel.com/docs/limits`): 100 MB per CLI upload (the layout is 70 MB), 15,000
  files (3,261), 5,000 file uploads a day (only changed files are uploaded again).
- Checked on 16 October, 21:38: `check_public.sh` on the address (page, the three `.gz` files in gzip, portrait,
  hero page, services), the sign-in message naming `chogarena.vercel.app`, and a scripted match from it with
  Metal drawing: same build as Render, a click attacked while W was held.

## MetaMask's site scan (found 16 October 2026)

MetaMask blocks `chog-genesis-game.onrender.com` with a warning page before anything is connected. The
extension asks `https://dapp-scanning.api.cx.metamask.io/v2/scan?url=<hostname>` (constants
`PHISHING_DETECTION_BASE_URL` and `PHISHING_DETECTION_SCAN_ENDPOINT` in
`https://github.com/MetaMask/core/blob/main/packages/phishing-controller/src/PhishingController.ts`); the
answer's `recommendedAction` is `BLOCK` or `NONE`. Asked that day with `curl` (header
`x-request-source: extension`):

| Answer | Hostnames |
| --- | --- |
| `BLOCK` | ours on `onrender.com`, and any made-up name under `onrender.com`, `vercel.app`, `netlify.app`, `pages.dev`, `github.io`, `trycloudflare.com` (names that do not exist are blocked too) |
| `NONE` | the ngrok tunnel, made-up names under `ngrok-free.app`, `ngrok.app`, `fly.dev`, `up.railway.app`, `duckdns.org`, made-up `.xyz` and `.com` domains, `chogathon.chog.xyz` |

Made-up names prove little: see the correction below, measured on real sites.
The fix is a domain of our own pointed at the Render service (Render's Hobby plan includes two custom
domains, `https://render.com/docs/custom-domains.md`), with `PUBLIC_ORIGIN` set to it (the sign-in message
names it) and the domain added at Reown; Photon's two addresses can stay on `onrender.com`, since a browser
never opens them. MetaMask's static list (`https://github.com/MetaMask/eth-phishing-detect`,
`src/config.json`) does not list our address, but it lists 37 `onrender.com` names and two Chog look-alikes
(`party-chog.icu`, `chog.digital`): a new domain with "chog" in it may later be taken for one. Porkbun's
public price list (`POST https://api.porkbun.com/api/json/v3/pricing/get`) gave that day: `.xyz` 2.04 USD the
first year, 14.21 USD a renewal; `.com` 11.08 USD.

Asked again later that day: `chogtanks.vercel.app` (the owner's earlier game, an old site) answers `NONE`,
while a new name under `vercel.app` answers `BLOCK`: an address earns its way out with time, so moving to
Vercel would not help. Made-up names that answered `NONE`: `deno.dev`, `web.app` (Firebase; `firebaseapp.com`
is `BLOCK`), `hf.space`, `koyeb.app`, `surge.sh`, `zeabur.app`, `northflank.app`, `is-a.dev`, `js.org`, `eu.org`,
`mooo.com`; `workers.dev` and `azurestaticapps.net` answer `BLOCK`. Only `ipfs.io`, `dweb.link`,
`cf-ipfs.com`, `cloudflare-ipfs.com`, `irys.xyz` and `sites.google.com` are judged by path
(`PHISHING_DETECTION_PATH_BASED_ROOT_DOMAINS` in `packages/phishing-controller/src/utils.ts`).

The way out of a dApp-scan block: Blockaid's form at `https://report.blockaid.io` (it posts to
`/api/submitDappForm`; it asks the domain, the wallet, an email and a captcha, so the owner sends it). In the
`MetaMask/eth-phishing-detect` issues of September and October 2026, removal requests for a dApp-scan block
were closed in 1 to 123 hours; one for a `github.io` page ended unblocked, one for a `vercel.app` page was sent
to Blockaid's form and was still `BLOCK` on 16 October.

**Correction, measured the same evening on real sites.** The rule above ("an unknown subdomain of those hosts is
blocked by default") was drawn from made-up names, and it is wrong as a rule: of 33 live `vercel.app` sites named
in the owner's projects (some are other teams' entries noted during research), 10 answer `NONE` and 23 `BLOCK`.
The `github.io` evidence was a name that serves nothing (404). What a `BLOCK` does is in the extension's
`ui/hooks/useOriginTrustSignals.ts`: `BLOCK` shows the site as "Malicious", `WARN` as a warning, `VERIFIED` as
verified, `NONE` shows nothing. The game's pages on Vercel (`tools/release/vercel_site.sh deploy`, production
address `https://chog-genesis.vercel.app`, project `chog-genesis` of the owner's account) answered `BLOCK` too
right after their first publication.

**What the verdict is, found later that evening.** The same service answers with its reasons at
`https://dapp-scanning.api.cx.metamask.io/scan?url=https://<hostname>`: for the Render address, the Vercel
address and a one-line text page with no script published the same evening on the owner's Vercel account
(`plain-page-probe.vercel.app`), it answers `recommendedAction: BLOCK`, `riskFactors: [{type: "DRAINER",
severity: "CRITICAL", message: "Domain identified as a wallet drainer."}]`, `status: COMPLETE`; `chogtanks.vercel.app`
answers `NONE`; `app.uniswap.org`, `opensea.io` and `magiceden.io` answer `VERIFIED`; `example.com`, `wikipedia.org`,
`chog.xyz`, `monad.xyz` and `chogathon.chog.xyz` answer `NONE`. The warning the owner saw ("Continuez à vos
risques et périls" / `trustSignalPhishingWarning`) is the extension's connection modal, shown whenever the verdict is
`BLOCK` (`ui/pages/permissions-connect/connection-trust-signal-gate/connection-trust-signal-gate.tsx`, added on 13
August 2026, commit `f9c78f66`); before it, the verdict was only a note in confirmation screens. In the
`MetaMask/eth-phishing-detect` tracker, reports of this `DRAINER` verdict on honest sites went from 5 a month in July
and August to 18 in September and 23 in the first half of October 2026 (search "dApp scan"); several concern
sites years old (xdao.app) or with no transaction at all (equiramarkets.com); MetaMask's AlexHerman1 answers "this is
already unflagged" or "no longer blocked" between 1 and 123 hours later, after the owner's report at
`https://report.blockaid.io`. So the block is Blockaid's current scanner, not this code: it is cleared on request.

**Why the owner's other projects never showed it (measured 16 October, 22:00).** Their projects with a domain of
their own, all bought and served through Vercel (`vercel domains ls`: `viky.cash` 35 days old, `lock-in.quest`,
`sentri-fi.xyz`), answer `NONE`; 14 of their 17 `*.vercel.app` addresses answer `BLOCK`, among them projects from
February and April 2025 (`nadomons0`, `monstack`), so age is not what clears it. Made-up subdomains of their
domains (`chogarena.lock-in.quest`) and unregistered names (`chogarena.xyz`, `.com`, `.fun`, `.app`) answer `NONE`.
Vercel's price quote (`vercel domains price <name>`, no purchase): `chogarena.xyz` 1.99 USD the first year then
13 USD, `.fun` and `.lol` 1.99 then 27, `.com` 11.25, `.app` 9.99 then 15. RDAP (`https://rdap.org/domain/<name>`)
found none of the `chogarena` names registered.

**What the documentation and a wider survey add (research of 16 October 2026, late evening).**
- No self-serve way exists to clear or verify a hostname: Blockaid's docs are behind a login (`docs.blockaid.io`
  redirects to `docs-login.blockaid.io`); its public OpenAPI spec says positive labels such as `verified_domain` are
  "curated and organization assertions only, never engine heuristics"; the portal `https://report.blockaid.io` offers
  `/mistake` (false positive) and `/verifiedProject` (developer verification), both a form posting to
  `/api/submitDappForm`. MetaMask staff send owners there ("you can relay with blockaid directly, we cannot action
  thus here", eth-phishing-detect #298864). An allowlist entry in eth-phishing-detect is not read by the scan.
- The extension only scans while "Security alerts" is on (`trust-signals-middleware.ts`); dismissing the modal is
  not remembered; MetaMask Mobile uses the same verdict. Reown's domain verification does not reach MetaMask's
  extension.
- Survey of 456 homepages of web3 repositories: `vercel.app` 120 of 160 `BLOCK`, `github.io` 108 of 116,
  `netlify.app` 20 of 20, `onrender.com` 4 of 4; custom domains 2 of 120 (8 of 253 for repositories created in 2026).
  `viem.sh` and `uniswap.github.io` are `BLOCK` too. One operator moved two Render services to its own domain and the
  warning went (#297670). In the 46 issues since August, every clearance found followed a report.
- Rabby does not use Blockaid: its engine (`RabbyHub/rabby-security-engine`, `src/rules/connect.ts`) asks
  `https://api.rabby.io/v1/engine/origin/{is_scam?source=rabby|metamask|scamsniffer, is_verified, popularity_level,
  third_party_collect_list}?origin=https://<host>`; for `chogarena.vercel.app` no scam flag, popularity `very_low`, so
  two yellow warnings to dismiss before connecting. Rabby supports Ledger (USB/WebHID) and has Monad built in
  (`https://api.rabby.io/v1/chain/list`: `"id":"monad","community_id":143`).
- So the way out without sending anything is a domain of our own on the Vercel project, with `PUBLIC_ORIGIN` and
  Reown following it; then check the verdict once it is live.

## Keeping careers and match records on the free plan (researched 17 October 2026)

What must outlive a restart of the free service (it has no disk): `careers.json` (careers, TOP GENESIS, and
the times a holder took a lent Genesis back; about 250 bytes per credited report, rewritten whole), and for
a staked match `match.json`, `verdict.json` and `wager.json` (about 2 KB a match together) until `settle` is
called; after one hour the contract lets anyone refund instead. The replay chunks (about 7.8 KB every 10 s,
up to about 540 KB a match) are needed only until the judge has run, from local files
(`tools/quantum/Verifier.cs`). Sign-in needs nothing stored: challenges, sessions and passes are tokens signed
with `SESSION_SECRET`, which Render keeps across deploys (`generateValue: true`).

Pages read on 17 October 2026, the providers' own:

| Option | Free limits | Lasts? | Card | From Node |
| --- | --- | --- | --- | --- |
| Upstash Redis Free (https://upstash.com/pricing/redis, https://upstash.com/docs/redis/features/restapi) | 256 MB, 500K commands a month | Persistent; an instant database made without an account is deleted after 3 days unless claimed | None | Plain `fetch` to its REST API |
| Turso Free (https://turso.tech/pricing) | 5 GB | Archived after 10 days idle, unarchived by hand | None | `fetch` (`/v2/pipeline`) |
| Neon Free (https://neon.com/docs/introduction/plans) | 1 GB | Kept; compute suspended over a limit | None | Needs its driver |
| Supabase Free (https://supabase.com/docs/guides/platform/free-project-pausing) | 500 MB | Paused after a week without activity | Not stated | `fetch` (PostgREST) |
| Cloudflare KV (https://developers.cloudflare.com/kv/platform/limits/) | 1,000 writes a day | Durable; one write per second per key | Not checked | REST |
| Firestore Spark (https://firebase.google.com/pricing) | 1 GiB, 20K writes a day | Kept | None | Needs a signed Google token or `firebase-admin` |
| MongoDB Atlas M0 (https://www.mongodb.com/docs/atlas/reference/free-shared-limitations/) | 512 MB | Paused after 30 days without a connection | Not stated | Driver only (its Data API ended 30 September 2025) |
| Vercel Blob Hobby (https://vercel.com/docs/vercel-blob/usage-and-pricing) | 2,000 uploads a month | Blocked 30 days when over | | Its package |
| Render Key Value Free (https://render.com/docs/key-value.md) | 25 MB | Lost at every restart | | |
| Render Postgres Free (https://render.com/docs/free.md) | 1 GB | Expires 30 days after creation | | `pg` driver |
| Render Starter + disk (https://render.com/pricing.md, https://render.com/docs/disks.md) | 7 USD a month (512 MB, half a CPU) + 0.25 USD per GB a month | Durable; one instance, no zero-downtime deploy | Yes | No change |

The player ladder now has an optional Upstash REST store in `server/src/rank-store.js`.
It saves only immutable rank events, not replay chunks, and uses `SET NX` so two judge
callbacks cannot overwrite one match's award. `host.js` uses it only when
`UPSTASH_REDIS_REST_URL`, `UPSTASH_REDIS_REST_TOKEN` and `RANK_ENABLED=1` are present.
At startup, the host checks both a Redis ping and a short-lived write. `GET /api/health`
reports `rankStorage` as `writable`, `unavailable` or `not_configured`, and `rankEnabled`
separately. A failed storage probe keeps rank off without taking the game server down.
Neither field reveals the URL or token.
The URL and token belong only in Render's environment, never in Git or browser code.
The existing Render service remains the game host; the cron may keep it awake, but cannot
protect local files from a restart or deploy. A restart before a match is judged can
still lose that incomplete replay; no rank is credited without its verdict. Genesis
careers remain on Monad. On 21 October, the owner added the two Render settings and the
public health endpoint confirmed writable storage. `RANK_ENABLED=1` was then deployed;
the public endpoint reports `rankStorage: writable` and `rankEnabled: true`. A signed
Recruit wallet completed a public Siege on 21 October. Its non-void defeat produced one
immutable rank event and one match in `/api/rank/me`; the score stayed at zero because
of the Bronze floor. A positive award, restart persistence and six-human play are not
yet live-verified. The result UI stayed on `VERIFYING THE MATCH` until the player left
the Photon room; do not present its delayed reveal as verified.

## To decide (the user)

| Question | Why it is open |
| --- | --- |
| Which host, and a paid instance or not | On Render a disk needs a paid instance. A free one loses every career at each restart, sleeps after 15 minutes without traffic and takes about a minute to wake (Render documentation, read 9 October 2026). Prices as a search of 12 October gave them from Render's page (its Markdown copy is `https://render.com/pricing.md`; not opened directly): Starter 7 USD a month with 512 MB and half a CPU, custom Docker containers allowed, a disk 0.25 USD per GB a month. The judge needs .NET 8 beside Node, so the service has to be a container (a Dockerfile, not written yet); whether 512 MB is enough for a replay was not measured. |
| Which address | `PUBLIC_ORIGIN` must be the address players open: it is the one written in the sign-in message, and a wallet may warn when it differs from the page. |
| The test entry `?token=` | Settled in code on 9 October, to be confirmed (D26): on a public address `?token=`, `?charged=` and `?bots=` are ignored; they keep working on this computer and the local network. A judge or a friend without a Genesis plays one through a link its holder lends (LEND IT on the menu, `?pass=`), valid thirty days at most. To give the judges access: sign in with the wallet that holds a Genesis on the public address, lend it, and put the link in the testing instructions. |
| The rental in $CHOG | Deployed on Monad mainnet on 16 October 2026 (`contracts/README.md`). Open on 17 October: whether a paid rental may outlive a sale of the Genesis (today it does), see `docs/PRODUCT-REVIEW.md`. |
| The $CHOG shop | Deployed on Monad mainnet on 16 October 2026 (`contracts/README.md`); the owner bought the second form of #1287 with it that day. |
| A private repository first | Render builds from a Git repository, and a private one raises none of the questions of the next row. Asked of the user on 12 October, not created. Before the first commit: `python3 tools/release/scan_secrets.py` names the files that may carry a secret without printing any value. Its run of 12 October named 51 files, by their look all hashes, transaction ids and public addresses; the four that looked like more were read: an Alchemy address with its public "demo" key, the $CHOG token address (twice), and the census script, which reads the hackathon site's key from the page at run time and stores none. The whole list has not been read file by file. The tree is about 500 MB without what `.gitignore` leaves out; no file is over 100 MB. |
| What goes in a public repository | `site/` carries the collection's art (the user states the project holds the rights, D19) and a build that contains the sample's third-party assets. The source tree carries the Photon sample: its licence terms (7.3) allow the sample code "in the Application" and nothing grants publishing its source, and its animations, UI images and effect prefabs count as assets that may not be redistributed (`docs/PHOTON-ARCHITECTURE-AUDIT.md`, rows 19 and 34, and its list of what must be replaced). The licence of the Cartoon FX pack was not read. The hackathon asks to share the repository: which repository, and with what in it, has to be settled before the first commit. |

## Limits of what would be online

The limits of the services apply unchanged (`server/README.md`): the careers are still
credited from what the players' pages report (only the stakes are paid on the replayed
match), the careers are one JSON file on one disk with no backup, one
instance only, no rate limit beyond one credited match a minute per Genesis. Photon's own
limits (concurrent players of the AppId's plan) were not looked at.
