"""Shows a change of the hero scripts against what the game holds now, before anything is exported (22 October 2026: a day
of 3D work was judged on close-ups against the art only, and the owner found many heroes worse than the live ones).

For each Genesis named, one row: its art, then the LIVE hero (the library committed at HEAD, what the deployed game draws)
and the TRY (the library the scripts of the working tree make for these Genesis only), side by side, in the three views of the
hero sheet and the home stage: close, three quarters, front. A try is kept only if it is clearly better than live.

Run it under the machine's lock (it starts Blender once per picture, one at a time):

    sh tools/dev/heavy_lock.sh <who> python3 tools/blender/try_against_live.py --name smirk --genesis 2,17,525

The sheet is written to research/evidence/try-<name>.jpg; the pictures stay under .artifacts/chog-body/try/<name>/.
"""

import argparse
import os
import subprocess
import sys

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
REPOSITORY = os.path.normpath(os.path.join(HERE, "..", ".."))
BLENDER = "/Applications/Blender.app/Contents/MacOS/Blender"
LIBRARY = "game/Assets/Chog/Resources/ChogParts.bytes"
ART = os.path.join(REPOSITORY, ".artifacts", "genesis-art", "1000")
ART_SMALL = os.path.join(REPOSITORY, ".artifacts", "genesis-art", "256")
VIEWS = ("stage_close", "stage", "stage_front")


def blender(*arguments):
    command = ["nice", "-n", "10", BLENDER, "--background", "--factory-startup", "-t", "4", "--python", *arguments]
    done = subprocess.run(command, cwd=REPOSITORY, capture_output=True, text=True)
    if done.returncode != 0:
        sys.exit("Blender failed: " + " ".join(arguments) + "\n" + done.stdout[-2000:] + done.stderr[-2000:])


def font(size):
    for path in ("/System/Library/Fonts/Helvetica.ttc", "/System/Library/Fonts/Supplemental/Arial.ttf"):
        if os.path.isfile(path):
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def art_of(number):
    for folder in (ART, ART_SMALL):
        path = os.path.join(folder, f"{number}.jpg")
        if os.path.isfile(path):
            return Image.open(path).convert("RGB")
    return None


def main():
    parser = argparse.ArgumentParser(description="The heroes of a try of the scripts beside the live ones.")
    parser.add_argument("--name", required=True, help="the name of the try (folder and sheet)")
    parser.add_argument("--genesis", required=True, help="numbers shown in the names, comma separated")
    parser.add_argument("--size", type=int, default=260)
    arguments = parser.parse_args()
    numbers = [int(word) for word in arguments.genesis.split(",") if word.strip()]
    out = os.path.join(REPOSITORY, ".artifacts", "chog-body", "try", arguments.name)
    os.makedirs(out, exist_ok=True)

    live = os.path.join(out, "live.bytes")
    with open(live, "wb") as file:
        file.write(subprocess.run(["git", "show", "HEAD:" + LIBRARY], cwd=REPOSITORY, capture_output=True, check=True).stdout)
    trial = os.path.join(out, "try.bytes")
    if os.path.isfile(trial):
        os.remove(trial)
    # The export builds the recruit from parts of others: a bare head (#76) and the Side Eye of Side=Right (#112).
    blender("tools/blender/export_chog_parts.py", "--", "--out", trial, "--check", "0",
            "--genesis", ",".join(map(str, sorted(set(numbers) | {76, 112}))))
    # Blender ends with 0 when the script raised: the library is the proof that the export ran.
    if not os.path.isfile(trial):
        sys.exit("the export wrote no library: run it by hand to read its error")

    for number in numbers:
        for which, library in (("live", live), ("try", trial)):
            blender("tools/blender/render_recruit.py", "--", "--hero", str(number), "--library", library,
                    "--name", f"{which}{number}", "--out", out, "--only", "stage")

    size = arguments.size
    columns = 1 + 2 * len(VIEWS)
    label = 34
    sheet = Image.new("RGB", (columns * size, label + len(numbers) * (size + label)), (236, 233, 226))
    draw = ImageDraw.Draw(sheet)
    small, big = font(18), font(22)
    titles = ["art"] + [f"{which} {view.replace('stage_', '').replace('stage', 'stage')}" for view in VIEWS for which in ("LIVE", "TRY")]
    for column, title in enumerate(titles):
        draw.text((column * size + 8, 8), title, fill=(30, 30, 30), font=big)
    for row, number in enumerate(numbers):
        top = label + row * (size + label)
        draw.text((8, top + 6), f"CHOG #{number}", fill=(30, 30, 30), font=small)
        art = art_of(number)
        if art is not None:
            sheet.paste(art.resize((size, size), Image.LANCZOS), (0, top + label))
        for index, view in enumerate(VIEWS):
            for offset, which in enumerate(("live", "try")):
                path = os.path.join(out, f"{which}{number}", f"chog_{which}{number}_{view}.png")
                if not os.path.isfile(path):
                    continue
                picture = Image.open(path).convert("RGB").resize((size, size), Image.LANCZOS)
                sheet.paste(picture, ((1 + index * 2 + offset) * size, top + label))
    target = os.path.join(REPOSITORY, "research", "evidence", f"try-{arguments.name}.jpg")
    sheet.save(target, quality=88)
    print("sheet", target)


if __name__ == "__main__":
    main()
