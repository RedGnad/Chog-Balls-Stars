#!/usr/bin/env python3
"""Lays the layers of tools/blender/make_key_art.py into the submission banner: the night of the arena with a burst of light in the
middle, the floor, the towers firing their eyes, the heroes from the farthest to the nearest with a glow in their team's colour, the
attacks in flight with their trails, then the title in the game's font. Everything drawn comes from the game's own models and fonts;
the effects are the ones the genre's key art uses (radial burst, speed trails, rim glow, a thick outlined title).

    python3 tools/release/compose_banner.py [--layers .artifacts/key-art/banner] [--out research/evidence/banner-key-art.jpg]

The page of the hackathon shows the picture at 16:9 and at 16:7 (object-fit: cover): the title and the heroes stay in the middle
band of 1920 by 840.
"""

import argparse
import json
import math
import os

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
FONTS = os.path.join(ROOT, "game", "Assets", "Chog", "Resources", "Fonts")
TEAM = {"blue": (64, 150, 255), "red": (255, 70, 110)}
NIGHT_TOP, NIGHT_BOTTOM = (28, 14, 58), (62, 22, 78)
GOLD_TOP, GOLD_BOTTOM = (255, 236, 120), (255, 160, 30)
INK = (24, 10, 40)


def font(name, size):
    return ImageFont.truetype(os.path.join(FONTS, name), size)


def vertical_gradient(size, top, bottom):
    width, height = size
    column = Image.new("RGB", (1, height))
    for y in range(height):
        t = y / max(1, height - 1)
        column.putpixel((0, y), tuple(round(a + (b - a) * t) for a, b in zip(top, bottom)))
    return column.resize(size)


def burst(size, centre, colours, rays=22, inner=0.0):
    """Rays of light from "centre", alternating colours, softened: the genre's versus burst."""
    width, height = size
    layer = Image.new("RGBA", size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    reach = math.hypot(width, height)
    for index in range(rays):
        a0 = (index / rays) * 2 * math.pi
        a1 = a0 + (2 * math.pi / rays) * 0.5
        points = [centre,
                  (centre[0] + math.cos(a0) * reach, centre[1] + math.sin(a0) * reach),
                  (centre[0] + math.cos(a1) * reach, centre[1] + math.sin(a1) * reach)]
        draw.polygon(points, fill=colours[index % len(colours)])
    return layer.filter(ImageFilter.GaussianBlur(3))


def radial_glow(size, centre, radius, colour, strength=1.0):
    width, height = size
    mask = Image.new("L", size, 0)
    ImageDraw.Draw(mask).ellipse([centre[0] - radius, centre[1] - radius, centre[0] + radius, centre[1] + radius],
                                 fill=round(255 * strength))
    mask = mask.filter(ImageFilter.GaussianBlur(radius * 0.45))
    layer = Image.new("RGBA", size, colour + (0,))
    layer.putalpha(mask)
    return layer


def alpha_of(image):
    return image.getchannel("A")


def rim_glow(layer, colour, width=10, strength=1.0):
    """A glow in "colour" round the shape of a layer: the team's light behind each hero."""
    alpha = alpha_of(layer).filter(ImageFilter.MaxFilter(width * 2 + 1)).filter(ImageFilter.GaussianBlur(width))
    alpha = alpha.point(lambda value: round(value * strength))
    glow = Image.new("RGBA", layer.size, colour + (0,))
    glow.putalpha(alpha)
    return glow


def add(base, layer):
    """Light added on top (screen), keeping the base's alpha."""
    rgb = ImageChops.screen(base.convert("RGB"), Image.composite(layer.convert("RGB"), Image.new("RGB", base.size), alpha_of(layer)))
    out = rgb.convert("RGBA")
    out.putalpha(alpha_of(base))
    return out


def trail(layer, start, end, colour, steps=14):
    """The object smeared back towards where it was thrown from, fading, with a streak of its team's colour under it."""
    size = layer.size
    out = Image.new("RGBA", size, (0, 0, 0, 0))
    dx, dy = start[0] - end[0], start[1] - end[1]
    streak = Image.new("RGBA", size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(streak)
    box = alpha_of(layer).getbbox()
    thick = max(6, ((box[3] - box[1]) if box else 40) * 0.55)
    for index in range(steps, 0, -1):
        t = index / steps
        fade = (1.0 - t) ** 1.4
        x, y = end[0] + dx * t, end[1] + dy * t
        radius = thick * (0.35 + 0.65 * (1.0 - t))
        draw.ellipse([x - radius, y - radius, x + radius, y + radius], fill=colour + (round(150 * fade),))
    out = Image.alpha_composite(out, streak.filter(ImageFilter.GaussianBlur(thick * 0.35)))
    for index in range(6, 0, -1):
        t = index / 6 * 0.45
        ghost = ImageChops.offset(layer, round(dx * t), round(dy * t))
        faded = ghost.copy()
        faded.putalpha(alpha_of(ghost).point(lambda value, f=(1.0 - t / 0.45) * 0.35: round(value * f)))
        out = Image.alpha_composite(out, faded)
    return out


def distance_look(image, depth, scale, near=8.0):
    """Far layers a little soft and veiled in the night's colour, so the near heroes read first (the depth of the genre's key art)."""
    if depth <= near:
        return image
    soft = image.filter(ImageFilter.GaussianBlur(min(3.0, (depth - near) * 0.22) * scale * 2))
    veil = Image.new("RGBA", image.size, (70, 40, 110, 255))
    veiled = Image.blend(soft, veil, min(0.38, (depth - near) * 0.045))
    veiled.putalpha(alpha_of(soft))
    return veiled


def shadow(size, feet, depth):
    """A soft dark ellipse under a hero's feet, smaller the farther it stands."""
    layer = Image.new("RGBA", size, (0, 0, 0, 0))
    radius = 560.0 / max(depth, 1.0)
    ImageDraw.Draw(layer).ellipse([feet[0] - radius, feet[1] - radius * 0.28, feet[0] + radius, feet[1] + radius * 0.28],
                                  fill=(10, 0, 20, 150))
    return layer.filter(ImageFilter.GaussianBlur(radius * 0.18))


def beam(size, start, end, colour, width):
    layer = Image.new("RGBA", size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    draw.line([tuple(start), tuple(end)], fill=colour + (200,), width=round(width * 2.4))
    glow = layer.filter(ImageFilter.GaussianBlur(width * 1.4))
    core = Image.new("RGBA", size, (0, 0, 0, 0))
    ImageDraw.Draw(core).line([tuple(start), tuple(end)], fill=(255, 250, 230, 255), width=max(2, round(width * 0.7)))
    return Image.alpha_composite(glow, core.filter(ImageFilter.GaussianBlur(1)))


def impact_star(size, centre, radius, colour):
    """A sharp star of light where two attacks meet."""
    layer = Image.new("RGBA", size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    tips = 10
    points = []
    for index in range(tips * 2):
        r = radius if index % 2 == 0 else radius * 0.32
        angle = index * math.pi / tips - math.pi / 2
        points.append((centre[0] + math.cos(angle) * r, centre[1] + math.sin(angle) * r))
    draw.polygon(points, fill=colour + (255,))
    soft = layer.filter(ImageFilter.GaussianBlur(radius * 0.12))
    core = radial_glow(size, centre, radius * 0.55, (255, 255, 240), 1.0)
    return Image.alpha_composite(Image.alpha_composite(soft, layer), core)


def outlined_text(size, text, font_face, centre, fill_top, fill_bottom, outline, stroke, shadow_offset, tilt=0.0):
    """A title as the genre writes it: a fill that runs from light to warm, a thick dark outline, a hard drop shadow, a slight tilt."""
    canvas = Image.new("RGBA", size, (0, 0, 0, 0))
    probe = ImageDraw.Draw(canvas)
    box = probe.textbbox((0, 0), text, font=font_face, stroke_width=stroke)
    width, height = box[2] - box[0], box[3] - box[1]
    pad = stroke * 3 + abs(shadow_offset[1]) + 10
    word = Image.new("RGBA", (width + pad * 2, height + pad * 2), (0, 0, 0, 0))
    draw = ImageDraw.Draw(word)
    origin = (pad - box[0], pad - box[1])
    # Shadow, then the outline, then the gradient fill cut to the letters.
    draw.text((origin[0] + shadow_offset[0], origin[1] + shadow_offset[1]), text, font=font_face, fill=outline,
              stroke_width=stroke, stroke_fill=outline)
    draw.text(origin, text, font=font_face, fill=outline, stroke_width=stroke, stroke_fill=outline)
    letters = Image.new("L", word.size, 0)
    ImageDraw.Draw(letters).text(origin, text, font=font_face, fill=255)
    fill = vertical_gradient(word.size, fill_top, fill_bottom).convert("RGBA")
    # A light line across the top half of the letters, as a gloss.
    gloss = Image.new("L", word.size, 0)
    ImageDraw.Draw(gloss).rectangle([0, 0, word.size[0], origin[1] + (box[3] - box[1]) * 0.42], fill=70)
    fill = Image.composite(Image.new("RGBA", word.size, (255, 255, 255, 255)), fill, gloss)
    word = Image.composite(fill, word, letters)
    if tilt:
        word = word.rotate(tilt, resample=Image.BICUBIC, expand=True)
    canvas.alpha_composite(word, (round(centre[0] - word.size[0] / 2), round(centre[1] - word.size[1] / 2)))
    return canvas


def main():
    parser = argparse.ArgumentParser(description="Lays the banner's layers.")
    parser.add_argument("--layers", default=os.path.join(ROOT, ".artifacts", "key-art", "banner"))
    parser.add_argument("--out", default=os.path.join(ROOT, "research", "evidence", "banner-key-art.jpg"))
    parser.add_argument("--title", default="CHOG BALLS STARS")
    parser.add_argument("--tagline", default="BET $CHOG. WIN THE BRAWL.")
    parser.add_argument("--footer", default="3v3 MOBA  •  1,969 CHOGS COME ALIVE  •  ON MONAD")
    arguments = parser.parse_args()
    with open(os.path.join(arguments.layers, "layers.json"), encoding="utf-8") as file:
        layers = json.load(file)
    size = tuple(layers["size"])
    width, height = size
    scale = width / 1920.0

    def load(name):
        return Image.open(os.path.join(arguments.layers, name)).convert("RGBA")

    # The night, a burst of the two teams' colours behind the clash, the floor fading into it.
    picture = vertical_gradient(size, NIGHT_TOP, NIGHT_BOTTOM).convert("RGBA")
    clash = (width * 0.5, height * 0.56)
    picture.alpha_composite(burst(size, clash, [(90, 60, 170, 70), (0, 0, 0, 0)], rays=26))
    picture.alpha_composite(radial_glow(size, (width * 0.22, height * 0.55), 520 * scale, TEAM["blue"], 0.45))
    picture.alpha_composite(radial_glow(size, (width * 0.78, height * 0.55), 520 * scale, TEAM["red"], 0.45))
    if "floor" in layers:
        floor = load(layers["floor"]["file"])
        # Darker and fading towards the far end, as if lit from the clash.
        fade = vertical_gradient(size, (0, 0, 0), (255, 255, 255)).convert("L").point(lambda value: min(255, round(value * 1.6)))
        floor.putalpha(ImageChops.multiply(alpha_of(floor), fade))
        floor = Image.blend(floor, Image.new("RGBA", size, (40, 14, 60, 0)), 0.18)
        picture.alpha_composite(floor)
    picture.alpha_composite(radial_glow(size, clash, 300 * scale, (255, 220, 170), 0.55))

    # The towers, far behind their teams, their eyes firing towards the clash.
    for tower in sorted(layers.get("towers", []), key=lambda entry: -entry["depth"]):
        # The towers stay sharp: they are the goal of the match, the owner wants them seen.
        image = load(tower["file"])
        colour = TEAM["blue"] if tower["side"] == "Ours" else TEAM["red"]
        picture.alpha_composite(rim_glow(image, colour, round(8 * scale), 0.8))
        picture.alpha_composite(image)
        eyes = tower["eyes"]
        target = (clash[0] + (-1 if tower["side"] == "Ours" else 1) * 60 * scale, clash[1] - 40 * scale)
        picture.alpha_composite(beam(size, eyes[:2], target, (255, 120, 60), 16 * scale))

    # The heroes from the farthest, each with its shadow and the light of its team; each attack is laid just after the hero
    # that threw it, so a nearer hero still passes in front of a farther attack.
    heroes = sorted(layers["heroes"], key=lambda entry: -entry["depth"])
    objects = list(layers.get("objects", []))
    for hero in heroes:
        image = distance_look(load(hero["file"]), hero["depth"], scale)
        picture.alpha_composite(shadow(size, hero["feet"][:2], hero["depth"]))
        picture.alpha_composite(rim_glow(image, TEAM[hero["team"]], round(9 * scale), 0.9))
        picture.alpha_composite(image)
        thrown = [entry for entry in objects if entry.get("team") == hero["team"] and "from" in entry
                  and abs(entry["from"][2] - hero["depth"]) < 1.2]
        for entry in thrown:
            objects.remove(entry)
            layer = load(entry["file"])
            colour = TEAM[entry["team"]]
            picture.alpha_composite(trail(layer, entry["from"][:2], entry["at"][:2], colour))
            picture.alpha_composite(rim_glow(layer, (255, 240, 200), round(6 * scale), 0.7))
            picture.alpha_composite(layer)
    for entry in objects:
        picture.alpha_composite(load(entry["file"]))

    # Where the nearest attacks meet, a star of light.
    near = [entry for entry in layers.get("objects", []) if "from" in entry]
    if len(near) >= 2:
        pair = sorted(near, key=lambda entry: entry["at"][2])[:2]
        meet = ((pair[0]["at"][0] + pair[1]["at"][0]) / 2, (pair[0]["at"][1] + pair[1]["at"][1]) / 2)
        picture.alpha_composite(impact_star(size, meet, 70 * scale, (255, 236, 150)))

    # A vignette, then the title, its tagline and the line under it.
    vignette = Image.new("L", size, 0)
    ImageDraw.Draw(vignette).ellipse([-width * 0.15, -height * 0.25, width * 1.15, height * 1.25], fill=255)
    vignette = vignette.filter(ImageFilter.GaussianBlur(160 * scale))
    dark = Image.new("RGBA", size, (10, 0, 22, 255))
    dark.putalpha(ImageChops.invert(vignette).point(lambda value: round(value * 0.75)))
    picture.alpha_composite(dark)
    title = outlined_text(size, arguments.title, font("LilitaOne-Regular.ttf", round(150 * scale)), (width * 0.5, height * 0.215),
                          GOLD_TOP, GOLD_BOTTOM, INK, round(11 * scale), (0, round(12 * scale)), tilt=2.0)
    picture.alpha_composite(title)
    tagline = outlined_text(size, arguments.tagline, font("LilitaOne-Regular.ttf", round(54 * scale)), (width * 0.5, height * 0.315),
                            (255, 255, 255), (220, 210, 255), INK, round(6 * scale), (0, round(6 * scale)), tilt=2.0)
    picture.alpha_composite(tagline)
    footer = outlined_text(size, arguments.footer, font("Nunito-Black.ttf", round(34 * scale)), (width * 0.5, height * 0.85),
                           (255, 255, 255), (235, 230, 255), INK, round(5 * scale), (0, round(4 * scale)))
    picture.alpha_composite(footer)

    os.makedirs(os.path.dirname(arguments.out), exist_ok=True)
    picture.convert("RGB").save(arguments.out, quality=92)
    print("banner", arguments.out)


if __name__ == "__main__":
    main()
