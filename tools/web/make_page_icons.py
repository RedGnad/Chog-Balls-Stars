#!/usr/bin/env python3
"""Make the icons of the game page from one Genesis portrait.

Usage: make_page_icons.py [metadata id, default 1287]

Writes icon-192.png, icon-512.png and apple-touch-icon.png into the page template of the game
(game/Assets/WebGLTemplates/Chog/TemplateData). The portrait is drawn as a disc on the page's
dark background, small enough to stay whole when a phone cuts the icon to a circle or a rounded
square. A working icon: the default portrait is the Genesis the user holds, CHOG #1287. Needs
Pillow, and the portraits fetched by tools/genesis-art/fetch_portraits.py.
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
BACKGROUND = (11, 16, 38)
# Share of the icon the portrait takes: inside the area every icon shape keeps.
PORTRAIT_SHARE = 0.72

metadata_id = sys.argv[1] if len(sys.argv) > 1 else "1287"
source = ROOT / "site" / "genesis-art" / "256" / f"{metadata_id}.jpg"
target = ROOT / "game" / "Assets" / "WebGLTemplates" / "Chog" / "TemplateData"
target.mkdir(parents=True, exist_ok=True)
portrait = Image.open(source).convert("RGB")


def icon(size):
    canvas = Image.new("RGB", (size, size), BACKGROUND)
    diameter = int(size * PORTRAIT_SHARE)
    face = portrait.resize((diameter, diameter), Image.LANCZOS)
    # Drawn four times larger then reduced, so the edge of the disc is smooth.
    mask = Image.new("L", (diameter * 4, diameter * 4), 0)
    ImageDraw.Draw(mask).ellipse((0, 0, diameter * 4 - 1, diameter * 4 - 1), fill=255)
    mask = mask.resize((diameter, diameter), Image.LANCZOS)
    offset = (size - diameter) // 2
    canvas.paste(face, (offset, offset), mask)
    return canvas


for name, size in (("icon-192.png", 192), ("icon-512.png", 512), ("apple-touch-icon.png", 180)):
    icon(size).save(target / name)
    print(target / name)
