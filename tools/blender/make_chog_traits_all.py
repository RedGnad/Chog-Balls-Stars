"""Rebuilds every picture of the dressed Chogs in one go: the polished base, each Genesis, the checks,
then the sheets. It only runs the other scripts, in order, and stops at the first that fails.

Plain Python, no Blender needed to start it. Run it with the private environment of the tools:

    .artifacts/tools-venv/bin/python tools/blender/make_chog_traits_all.py [--only 1287,80] [--sheets-only]

Blender (5.2) is started without its interface for each model and each set of pictures: about one
minute per Genesis on this Mac. Everything goes under .artifacts/chog-body/traits/. The sheets read
the 1000-point pictures of the Genesis (.artifacts/genesis-art/1000/, fetched by
tools/genesis-art/fetch_portraits.py --size 1000 --quality 95 --ids ...) and the red pictures of
head H3 (.artifacts/chog-body/full/H3/, made by make_chog_full.py and render_chog_full.py).
"""

import argparse
import os
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
REPOSITORY = os.path.normpath(os.path.join(HERE, "..", ".."))
TRAITS = os.path.join(REPOSITORY, ".artifacts", "chog-body", "traits")
BLENDER = "/Applications/Blender.app/Contents/MacOS/Blender"

# The Genesis of the overview, in its order, then those shown under them as extra.
GENESIS = ("1287", "1241", "80", "1408", "1078", "83")
EXTRA = ("1948",)
# The Genesis whose sheet also sets the polished base beside head H3.
WITH_BASE = "1287"
# The brim of the cap before it was shortened and raised, kept for the before and after strip.
BRIM_BEFORE = {"BRIM_LENGTH": "0.130", "BRIM_RISE_DEG": "-9.0"}
BRIM_WORDS = (
    "Before: 0.130 long, dips by 9 degrees. From the game camera the brim hides the whole face",
    "After (kept): 0.060 long, rises by 35 degrees. The lenses show under it; the grin stays out of sight",
)
KEPT = ("CHOG_TRAITS", "CHOG_RENDER", "CHOG_SHEET", "CHOG_OVERVIEW", "CHOG_STRIP", "CHOG_FULL")


def run(words):
    """Runs one command and shows the lines of our scripts that tell of a count or of a failure."""
    done = subprocess.run(words, capture_output=True, text=True)
    for line in (done.stdout + done.stderr).splitlines():
        told = line.startswith(KEPT) and any(word in line for word in ("dressed", "NOT MODELLED", "FAILED", "wrote"))
        if told and "_phone" not in line or "Traceback" in line or "Error" in line:
            print(line[:220], flush=True)
    if done.returncode != 0:
        print((done.stdout + done.stderr)[-3000:])
        raise SystemExit(f"CHOG_ALL stopped: {' '.join(words)}")


def blender(script, *arguments):
    run([BLENDER, "--background", "--factory-startup", "--python", os.path.join(HERE, script), "--", *arguments])


def main():
    parser = argparse.ArgumentParser(description="Rebuilds every picture of the dressed Chogs.")
    parser.add_argument("--only", default=None, help="only these Genesis, separated by commas")
    parser.add_argument("--sheets-only", action="store_true", help="build and render nothing: only lay the sheets")
    arguments = parser.parse_args()
    wanted = arguments.only.split(",") if arguments.only else list(GENESIS + EXTRA)
    everything = arguments.only is None
    before = os.path.join(TRAITS, "experiments", "brim_before")

    if not arguments.sheets_only:
        if everything:
            blender("make_chog_traits.py", "--base")
            blender("render_chog_traits.py", "--name", "base", "--only", "studio,game")
        for number in wanted:
            blender("make_chog_traits.py", "--genesis", number)
            blender("render_chog_traits.py", "--name", number)
        if everything:
            # The same cap under another colour set and word, and the cap with its first brim.
            blender("make_chog_traits.py", "--genesis", WITH_BASE, "--hat", "MMGA Cap")
            blender("render_chog_traits.py", "--name", f"{WITH_BASE}_MMGACap", "--only", "bust", "--bust-size", "756")
            sets = [word for name, value in BRIM_BEFORE.items() for word in ("--set", f"{name}={value}")]
            blender("make_chog_traits.py", "--genesis", WITH_BASE, *sets, "--out", before)
            blender("render_chog_traits.py", "--name", WITH_BASE, "--only", "bust,towards",
                    "--blend", os.path.join(before, "ChogBody.blend"), "--out", before)

    python = sys.executable
    for number in wanted:
        words = [python, os.path.join(HERE, "make_chog_traits_sheet.py"), "--genesis", number]
        run(words + (["--with-base"] if number == WITH_BASE else []))
    if everything:
        run([python, os.path.join(HERE, "make_chog_brim_strip.py"), "--genesis", WITH_BASE,
             "--title", f"CHOG #{WITH_BASE}: the brim of the cap, before and after",
             "--row", f"{BRIM_WORDS[0]}={before}", "--row", f"{BRIM_WORDS[1]}={os.path.join(TRAITS, WITH_BASE)}",
             "--out", os.path.join(TRAITS, WITH_BASE, "brim_before_after.png")])
        run([python, os.path.join(HERE, "make_chog_traits_overview.py"), "--genesis", ",".join(GENESIS),
             "--extra", ",".join(EXTRA)])


if __name__ == "__main__":
    main()
