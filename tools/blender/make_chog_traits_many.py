"""Builds and renders many dressed Genesis in ONE run of Blender (batch 4, 15 October 2026). Pictures
only: nothing here goes into the game.

The owner saw Blender open and close in a loop while make_chog_traits_batch.py started it twice per
Genesis. This script does the same work for a whole list in one process: for each Genesis,
make_chog_traits.make_one() (the model, into <into>/<number>/) then render_chog_traits.render_one()
(the pictures, into the same folder). Start it through make_chog_traits_batch.py, which waits for no
other Blender and for the heavy-job lock, and lowers it:

    .artifacts/tools-venv/bin/python tools/blender/make_chog_traits_batch.py \
        --out .artifacts/chog-body/traits/batch-4 --genesis 97,19,4 [--only bust,towards]

Started by hand (one Blender, without its window):

    /Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup -t 4 \
        --python tools/blender/make_chog_traits_many.py -- --genesis 97,19,4 \
        --into .artifacts/chog-body/traits/batch-4 [--only bust,towards] [--no-render] \
        [--set NAME=VALUE] [--head-set NAME=VALUE]

A Genesis whose build fails (a leaf through its hat, over 8,000 triangles) is said with FAILED and
the others go on; the run then ends with an error. One line per Genesis starts with CHOG_MANY.
"""

import argparse
import os
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
sys.dont_write_bytecode = True   # no __pycache__ folder beside the scripts

import make_chog_traits as traits  # noqa: E402  (the folder of this file must be on the path first)
import render_chog_traits as pictures  # noqa: E402


def main():
    parser = argparse.ArgumentParser(description="Builds and renders many dressed Genesis in one Blender.")
    parser.add_argument("--genesis", required=True, help="numbers shown in the names (CHOG #n), separated by commas")
    parser.add_argument("--into", required=True, help="the folder that receives one folder per Genesis")
    parser.add_argument("--only", default="bust,towards", help="the families of pictures of render_chog_traits.py")
    parser.add_argument("--no-render", action="store_true", help="build only: the models and parts.json")
    parser.add_argument("--hat", default=None, help="wear this entry of HATS in place of the hat of each Genesis "
                        "(a try: give another --into than the batch)")
    parser.add_argument("--set", action="append", default=[], metavar="NAME=VALUE", help="a number of the hat put "
                        "in place of its own (may be given several times)")
    parser.add_argument("--head-set", action="append", default=[], metavar="NAME=VALUE", help="a number of the head "
                        "put in place of its own, for instance LEAF_LENGTH=0.85 (may be given several times)")
    arguments = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    numbers = [number.strip() for number in arguments.genesis.split(",") if number.strip()]
    into = os.path.abspath(arguments.into)
    replaced, head_replaced = traits.numbers_given(arguments.set), traits.numbers_given(arguments.head_set)
    failed = []
    started = time.monotonic()
    for index, number in enumerate(numbers):
        folder = os.path.join(into, number)
        try:
            traits.make_one(number, folder, arguments.hat, replaced, head_replaced)
            if not arguments.no_render:
                pictures.render_one(number, os.path.join(folder, "ChogBody.blend"), folder, arguments.only)
        except SystemExit as stop:
            failed.append(number)
            print(f"CHOG_MANY {index + 1}/{len(numbers)} #{number} FAILED: {stop}", flush=True)
            continue
        print(f"CHOG_MANY {index + 1}/{len(numbers)} #{number} done ({time.monotonic() - started:.0f} s)", flush=True)
    print(f"CHOG_MANY {len(numbers) - len(failed)} of {len(numbers)} built into {into} in "
          f"{time.monotonic() - started:.0f} s" + (f"; failed: {', '.join(failed)}" if failed else ""), flush=True)
    if failed:
        sys.exit(1)


if __name__ == "__main__":
    main()
