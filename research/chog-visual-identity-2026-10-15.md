# Chog official visual identity (read 2026-10-15, curl only)

## Sources and routes tried
- `https://www.chog.xyz/` 200 (Next.js + Tailwind). `https://chog.xyz/` redirects there. `/home` and `/chogify` are 404 (they are asset folders, not pages).
- Pages linked from the HTML, all 200: `/tools`, `/maker`, `/messenger`, `/snapshoter`, `/swap`, `/team`. They share ONE stylesheet:
  `https://www.chog.xyz/_next/static/chunks/06l49n4dfv3q..css?dpl=dpl_Gh5Fbyc83UuGiwjaoz14KLwiTfk3` (below: "chog CSS"). 52 JS chunks from those pages were also read (below: "chog JS").
- `https://chogathon.chog.xyz/` 200: one HTML file with all of its CSS inline (`<style>`, lines 39 to 1297; below: "chogathon CSS").
- Extra source: `https://portal.chog.xyz/` (the "Portal" link in the chog.xyz nav), CSS `https://portal.chog.xyz/_next/static/chunks/bf2cf49be5fcdfa1.css?dpl=dpl_8PVb76Gqz7XPFHh9KZBKSbD9Xz18`.

## 1. Colour palette

### chog.xyz (mascot site)
- Theme: chog JS `ThemeProvider{attribute:"class",defaultTheme:"light",enableSystem:!0}`. Light is the default. Dark applies when the system is dark or the user switches.
- CSS custom properties (chog CSS): `:root{--background:#d8d6c7;--foreground:#000}`. The other colours are compiled into Tailwind classes. Names below are the class names.

| Token (class name) | Hex | Where it is used (chog.xyz HTML / chog JS) |
|---|---|---|
| light-primary | #D8D6C7 | page background (`<body class="dark:bg-dark-primary bg-light-primary">`), tool card fill |
| light-secondary | #EEECDF | popovers, active tab, gradient start |
| light-tertiary | #D0CEBC | select menus, secondary round buttons, borders |
| light-quaternary | #A6A49A | small pills (25 / 50 / 75 / Max), white/70 bold text |
| light-quinary | #B3B1A4 | colour of the hard offset shadow, social icon buttons, borders |
| light-disabled | #D0CEC0 | track of the PFP/Meme toggle (maker) |
| light-secondaryText | #706E61 | secondary text |
| **light-cheek** | **#D83E7E** | main accent: "About Chog" card fill, 4px border + text of the tool cards, outlined button border/text, filled small buttons, tooltips in the maker |
| dark-cheek | #CE437C | "Connect" button fill (both themes), hover fills |
| dark-primary | #0F1A26 | dark-mode page background, text on light buttons |
| dark-secondary / dark-tertiary | #1C3248 / #18293B | dark panels / hover |
| dark-quaternary = dark-quinary | #121B32 | dark pills, dark hard shadow |
| dark-secondaryText | #B3B1A3 | secondary text (dark) |
| arbitrary | #836EF9 | `<meta name="theme-color">` and TileColor, `text-[#836EF9]` (rare accent text) |
| arbitrary | #252525 | team card tooltip (`bg-[#252525]`) |
| SVG fill | #FDF7D3 | cream fill of `/icons/settings.svg` and `/icons/twitter.svg` (#383838 is the second colour in twitter.svg) |

Body text is black or white with opacity steps (/30 to /80). Pixel sampling (Pillow, my measurement, not CSS): `/brand/logo.png` background #FDF7D3 (72% of pixels), cheeks #D53F7E; `/chogify/face.webp` face #FDF6CD (90%); `/chogify/body.webp` body #331C71 (61%), shade #26165B.

### chogathon.chog.xyz (event site, dark only, `color-scheme: dark`)
`:root` verbatim (chogathon CSS): `--p:#836ef9` (buttons, stats band, quote rule) · `--ps:#a99bfc` (eyebrow, labels, focus outline) · `--ph:#9d8dfc` · `--p-glow:rgba(131,110,249,.45)` · `--p-subtle:rgba(131,110,249,.12)` · `--bg:#0e0b1a` (page) · `--bg2:#141026` (ticker band) · `--bg3:#181330` · `--tx:#f4f1fe` (text) · `--mu:rgba(244,241,254,.65)` (muted text) · `--ln:rgba(244,241,254,.12)` (hairlines) · `--ln-bright:rgba(169,155,252,.28)` · `--ok:#5ce68a` · `--card:#161228` · `--gold:#f59e0b` · `--radius-sm:8px` `--radius-md:12px` `--radius-lg:18px` `--radius-xl:24px` · `--shadow-card:0 16px 40px -10px rgba(0,0,0,.6), 0 0 0 1px var(--ln)`.
Other values: CTA gradient `135deg, #a99bfc, #836ef9 55%, #6f52e8`; h1 `<em>` gradient to #c084fc; glowing chip `#836ef9 to #a855f7`; blurred background orbs #836ef9, #ec4899, #22d3ee at opacity .22; #080612 and #161222 on two small panels.

### portal.chog.xyz (extra)
Button gradients `#7139dd to #e02c98` (btn-primary) and `#53ffe9 to #e02c98` (btn-secondary), glow #8a38f5; `--background:#fff` (dark: #0a0a0a).

## 2. Fonts
- **chog.xyz**: first line of chog CSS `@import "https://fonts.googleapis.com/css2?family=Convergence&family=Gaegu&family=Galindo&family=Koulen&display=swap"`. Google serves only weight 400 for all four (that Google CSS was read). Fallback for the whole page: `Helvetica Neue, Helvetica, Arial`.
  - **Koulen** (titles, nav, buttons): nav `ul` uppercase text-xl; h1 "About Chog" 36 to 60px; h3 "$CHOG Contract Address"; buttons "Connect", "Watch video", "Copy", "Tools"; footer name "Chog" uppercase 40px; footer small print.
  - **Convergence** (body text): the About paragraph, the snapshoter stats labels and values (15px).
  - **Galindo** (maker tool UI): "Add Background", "Share on X" button (uppercase), the mobile menu (uppercase 15px), team names (bold xl).
  - **Gaegu** (handwritten): team member descriptions, 20 to 30px, cheek colour.
- **portal.chog.xyz** gives the roles as tokens: `--font-primary: Koulen`, `--font-body: Convergence`, `--font-number: Lateef` (self-hosted woff2 under `/_next/static/media/`, weight 400).
- **chogathon**: `https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&family=JetBrains+Mono:wght@400;500;600;700&family=Koulen&family=Fraunces:opsz,wght@9..144,600;9..144,700&display=swap`.
  - Body: Inter.
  - Titles: Koulen, class `.k` (uppercase, letter-spacing .04em, line-height .95). h1 `clamp(56px,15vw,200px)`, h2 `clamp(46px,7vw,92px)`, h3 30px.
  - Numbers: Koulen. Countdown `.n` up to 118px with `tabular-nums`, stats `.v` up to 70px.
  - Buttons, labels and eyebrows: JetBrains Mono 500, 11 to 13px, uppercase, letter-spacing .08 to .16em.
  - Fraunces 700: only the dashboard greeting "Hello".
- Licences: OFL.txt exists in google/fonts for koulen, galindo, gaegu, convergence, inter, jetbrainsmono and fraunces (`https://raw.githubusercontent.com/google/fonts/main/ofl/<name>/OFL.txt`, all 200). The TTF files `ofl/koulen/Koulen-Regular.ttf` and `ofl/galindo/Galindo-Regular.ttf` are listed by the GitHub API (I did not list the other folders).

## 3. Logo, mascot and icon assets (sizes read with `file` / WebP header after download)
**No wordmark image exists on either site.** The name is live text set in Koulen: the chog.xyz footer has `<p class="... uppercase lg:text-[40px] text-3xl font-bold ... font-koulen">Chog`, and chogathon has `.brand .k` at 22px reading "Chogathon".

| URL | Pixels | What / where used |
|---|---|---|
| https://www.chog.xyz/brand/logo.png | 96x96 RGBA | THE logo: Chog face (eyes, pink cheeks, open mouth) on a cream square. Header 48px `rounded-lg shadow-md`, theme-toggle knob, footer 40px. Also `/_next/image?url=%2Fbrand%2Flogo.png&w=96` and `&w=128` |
| https://www.chog.xyz/favicon.ico | 256x256 ICO | favicon (no `<link rel=icon>` in the HTML, default route) |
| https://www.chog.xyz/meta/image.png | 3600x3000 | og:image (the meta tag says 2600x2000, the file is 3600x3000): Chog drawing on a wall |
| https://www.chog.xyz/home/chog.png | 663x1080 | Chog, back view (hero) |
| https://www.chog.xyz/home/main.png, sky.png, grass.png, cot.png, cot-front.png | 4320x2799, 4320x3072, 4320x927, 4221x1359, 417x2469 | layers of the home hero illustration |
| https://www.chog.xyz/tools/jump.png | 1221x1135 | Chog front, jumping with a giant pen |
| https://www.chog.xyz/tools/write.png, coin.png, flat-coin.png, fire.gif | 147x195, 1267x1280, 1158x871, 780x768 | tool card art |
| https://www.chog.xyz/chog/curious.png, read.png | 1239x2753, 2048x2048 | full-figure poses |
| https://www.chog.xyz/swap/chog.png | 428x570 | swap mascot |
| https://www.chog.xyz/team/bigchog.png | 1311x1935 | team portrait |
| https://www.chog.xyz/messenger/messenger.png | 350x350 RGB | messenger tool |
| https://www.chog.xyz/chogify/body.webp, face.webp | 1000x1000 each | PFP maker base layers. Other layers follow `/chogify/<cat>/<cat>-<n>.webp` (chog JS): headwears 36, clothes 33, accessories 17, costume 11. `headwears-1.webp` answers 200 |
| https://www.chog.xyz/icons/settings.svg, twitter.svg | 20x22 viewBox (settings) | cream #FDF7D3 icons |
| https://www.chog.xyz/backgrounds/{maker,message,snapshot,swap,team}/{light,dark}.png | 1920x1080 RGBA (3 checked) | doodle pattern backgrounds |
| https://chogathon.chog.xyz/chogbadge.png | 640x640 RGB | favicon (`<link rel="icon" href="chogbadge.png">`) |
| https://chogathon.chog.xyz/chogbadge.jpg | 640x640 | header brand mark (20 references): Chog holding a "CHOG" ID card |
| https://chogathon.chog.xyz/chog1.jpg, chog5.jpg, chog6.jpg, chognice.jpg, chogwow.jpg | 772x900, 751x900, 900x716, 900x729, 750x900 | section art |
| https://chogathon.chog.xyz/banner.jpg, og.jpg | 1600x1280, 1000x1200 | banner / og:image |

Local copies (never executed): `/private/tmp/claude-501/-Users-red-g-CascadeProjects-chogathon/899443d7-e622-496c-bd95-9d79f7445321/scratchpad/chog-assets/`.

## 4. UI style traits
**chog.xyz** (chog CSS + class lists in the HTML and JS)
- Radius: pills everywhere (`rounded-full`) for buttons and nav. Tool cards `rounded-[20px]`, About card `rounded-3xl` (24px), popovers and menus `rounded-xl` (12px).
- Borders: thick and coloured, never black. Tool cards `border-4 border-light-cheek`. Outlined buttons `border-2` in cheek pink or white. Popovers `border-2 border-white`. No thick dark UI outlines exist in the CSS. The thick dark ink outline belongs to the illustrations, not to the UI.
- Shadows: hard, almost unblurred offset ("sticker" depth): `shadow-[0px_4px_1px_#B3B1A4]` and `shadow-[4px_4px_1px_#B3B1A4]`. In dark mode these are #121B32 or #1D334A. On group hover the shadow shrinks to `0px 2px 1px` (pressed look). The Connect button uses plain Tailwind `shadow-md`.
- Buttons:
  - Primary: cheek pink fill, white Koulen uppercase text, pill shape, 1px white border on mobile ("Connect": h-10, min-w 129px, text-lg).
  - Outline: white fill, 2px #D83E7E border, pink Koulen text at 20 to 24px. On hover: pink fill, white border, white text, 300ms ("Watch video", "Copy").
  - Tool card: beige fill, 4px pink border, pink uppercase text at 50 to 60px. On hover it fills pink with white text and the label slides.
  - Small pill: #A6A49A fill, white/70 bold xs text.
- Motion: `hover:rotate-180` on a small button, `hover:scale-150` on partner logos, a hover drop-shadow on images.
- Case: Koulen is set uppercase in nav, buttons and titles.
- Texture: hand-drawn grey doodles (coins, stars, arrows, cups) on beige, in `/backgrounds/*/light.png`. They are layered at `before:opacity-20` in light mode and 5% in dark mode. The home hero is a painted, layered scene.
- Tone: playful and warm. Paper beige with bubblegum pink, a handwritten font (Gaegu), copy such as "gchog!" and "Made with ❤️ by Chog".

**chogathon.chog.xyz** (chogathon CSS)
- Look: dark purple night with glass panels (`backdrop-filter: blur`), blurred colour orbs and an SVG film grain at opacity .04.
- `.btn`: #836ef9 fill with a 1px border of the same colour, radius 10px (CTA 13px), JetBrains Mono 500 12.5px uppercase with letter-spacing .1em, padding 15px 26px. It has a hard black offset shadow `4px 4px 0 rgba(0,0,0,.55)`. On hover it moves `translate(-2px,-2px)` and the shadow grows to 6px with a purple glow. On press the shadow drops to `1px 1px 0`. A white shine sweeps across on hover. The ghost variant `.btn.ln` is transparent with a hairline border.
- Pills (eyebrow, badges, chips, tabs): radius 999px with a 1px lavender border. Cards: radius 18px (countdown), 26px (tilting hero card), 16px (images). Lines are 1px hairlines with no thick outlines.
- Tone: an event or tech site, dark and fairly sober with neon purple. It does not use the mascot site's beige and pink.
- Both sites share: Koulen uppercase titles, pill buttons, hard offset shadows that compress when pressed, and the purple #836EF9 (the main accent on chogathon, only the theme-color on chog.xyz).

## 5. Brand usage rules
None published that I could find.
- Routes tried, all 404 on both hosts: `/terms /terms-of-service /tos /legal /privacy /brand /brand-kit /brandkit /press /press-kit /media-kit /assets /rules /faq /robots.txt /sitemap.xml /manifest.json /site.webmanifest`.
- The only related text: the chog.xyz footer says "© 2026 Chog. All rights reserved.", and chogathon line 1518 says "...growing Chog into an ever-growing IP."
- A web search for Chog brand guidelines, logo use or an IP licence returned only Monad's own kit (https://monad.xyz/brand-and-media-kit, which covers Monad's marks, not Chog's).
- Not checked: Chog's X and Discord posts, and the contractURI of the Genesis contract.
