"""Builds and renders a list of dressed Genesis into one folder, one Blender at a time. Pictures only:
nothing here goes into the game.

Plain Python, no Blender needed to start it. Run it with the private environment of the tools:

    .artifacts/tools-venv/bin/python tools/blender/make_chog_traits_batch.py \
        --out .artifacts/chog-body/traits/batch-2 --genesis 39,34,1287 [--only bust,towards] [--skip-done]
    # one script of this folder, once nothing heavy runs, its output printed (coverage, budget):
    .artifacts/tools-venv/bin/python tools/blender/make_chog_traits_batch.py \
        --run make_chog_traits.py -- --budget

It starts ONE Blender for many Genesis (make_chog_traits_many.py, up to --per-run of them, 40 by
default; until batch 4 it started Blender twice per Genesis, and the owner saw it open and close in a
loop): for each Genesis the model (make_chog_traits.py, into <out>/<number>/) then its pictures
(render_chog_traits.py: by default the bust and the game angle coming towards the camera, with its
phone-size copy). A run never starts beside another heavy job: before it, the script waits until no
Blender process is left on the machine and until the lock file .artifacts/heavy-job.lock no longer
says "busy" (another heavy job, a Unity build for instance, writes it). Blender is started lowered
("nice -n 10") and with 4 threads ("-t 4"): this Mac has 8 GB of memory and Unity stays open beside it.

It prints one line per Genesis (triangles, parts, values left out), and every Genesis that failed.
"""

import argparse
import json
import os
import subprocess
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
REPOSITORY = os.path.normpath(os.path.join(HERE, "..", ".."))
BLENDER = "/Applications/Blender.app/Contents/MacOS/Blender"
THREADS = "4"
NICENESS = "10"
WAIT_LIMIT_S = 900
# Another heavy job (a Unity build) writes "busy" into this file while it runs, "free" after.
HEAVY_LOCK = os.path.join(REPOSITORY, ".artifacts", "heavy-job.lock")
LOCK_WAIT_LIMIT_S = 3600


def blender_running():
    """True when a Blender process runs anywhere on the machine."""
    found = subprocess.run(["pgrep", "-f", "Blender.app/Contents/MacOS/Blender"], capture_output=True, text=True)
    return bool(found.stdout.strip())


def heavy_job_running():
    """True while the lock file of the heavy jobs exists and says "busy"."""
    if not os.path.isfile(HEAVY_LOCK):
        return False
    with open(HEAVY_LOCK, encoding="utf-8", errors="replace") as source:
        return "busy" in source.read()


def wait_until_free():
    """Waits until no other Blender runs and no heavy job holds the lock."""
    waited = 0.0
    while True:
        if heavy_job_running():
            if waited > LOCK_WAIT_LIMIT_S:
                raise SystemExit(f"CHOG_BATCH {HEAVY_LOCK} has said busy for an hour: stopped")
            if waited % 60.0 == 0.0:
                print(f"CHOG_BATCH waiting: {HEAVY_LOCK} says busy", flush=True)
        elif blender_running():
            if waited > WAIT_LIMIT_S:
                raise SystemExit("CHOG_BATCH another Blender has been running for 15 minutes: stopped")
        else:
            return
        time.sleep(5.0)
        waited += 5.0


def blender(script, *arguments, stop_on_failure=True):
    """Runs one Blender script without its window, once nothing heavy runs. Returns its output and
    whether it went through."""
    wait_until_free()
    words = ["nice", "-n", NICENESS, BLENDER, "--background", "--factory-startup", "-t", THREADS,
             "--python", os.path.join(HERE, script), "--", *arguments]
    done = subprocess.run(words, capture_output=True, text=True)
    output = done.stdout + done.stderr
    through = not (done.returncode != 0 or "Traceback" in output or "FAILED" in output)
    if not through:
        for line in output.splitlines():
            if "FAILED" in line or "Traceback" in line or "Error" in line:
                print(line[:2000])
        if stop_on_failure:
            print(output[-4000:])
            raise SystemExit(f"CHOG_BATCH stopped: {' '.join(words)}")
    return output, through


def main():
    parser = argparse.ArgumentParser(description="Builds and renders several dressed Genesis, one Blender at a time.")
    parser.add_argument("--out", help="the folder that receives one folder per Genesis")
    parser.add_argument("--genesis", help="numbers shown in the names (CHOG #n), separated by commas")
    parser.add_argument("--only", default="bust,towards", help="the families of pictures of render_chog_traits.py")
    parser.add_argument("--skip-done", action="store_true", help="leave out a Genesis whose pictures are all there")
    parser.add_argument("--per-run", type=int, default=40, help="how many Genesis one Blender builds and renders")
    parser.add_argument("--no-render", action="store_true", help="build only (to check the leaves and triangles)")
    parser.add_argument("--run", default=None, help="in place of a batch: one script of this folder, the words "
                        "after -- given to it, its output printed")
    words = sys.argv[1:]
    passed = words[words.index("--") + 1:] if "--" in words else []
    arguments = parser.parse_args(words[:words.index("--")] if "--" in words else words)
    if arguments.run:
        print(blender(arguments.run, *passed)[0])
        return
    if not arguments.out or not arguments.genesis:
        raise SystemExit("CHOG_BATCH give --out and --genesis, or --run SCRIPT -- WORDS")
    out = os.path.abspath(arguments.out)
    numbers = [number.strip() for number in arguments.genesis.split(",") if number.strip()]
    if arguments.skip_done:
        def done(number):
            folder = os.path.join(out, number)
            return all(os.path.isfile(os.path.join(folder, f"chog_{number}_{name}.png"))
                       for name in ("bust", "game_front_phone"))
        print(f"CHOG_BATCH already done: {', '.join(number for number in numbers if done(number)) or 'none'}")
        numbers = [number for number in numbers if not done(number)]
    started = time.monotonic()
    failed = False
    for start in range(0, len(numbers), max(1, arguments.per_run)):
        chunk = numbers[start:start + max(1, arguments.per_run)]
        print(f"CHOG_BATCH one Blender for {len(chunk)} Genesis: {', '.join(chunk)}", flush=True)
        extra = ["--no-render"] if arguments.no_render else []
        output, through = blender("make_chog_traits_many.py", "--genesis", ",".join(chunk), "--into", out,
                                  "--only", arguments.only, *extra, *passed, stop_on_failure=False)
        failed = failed or not through
        for number in chunk:
            path = os.path.join(out, number, "parts.json")
            if f"#{number} FAILED" in output or not os.path.isfile(path):
                print(f"CHOG_BATCH #{number}: FAILED (see above)", flush=True)
                continue
            with open(path, encoding="utf-8") as source:
                built = json.load(source)
            parts = ", ".join(name for name, part in built["parts"].items() if not part["on"].startswith("arm"))
            left_out = f"  NOT MODELLED: {', '.join(built['left_out'])}" if built["left_out"] else ""
            print(f"CHOG_BATCH #{number}: {built['triangles_total']} triangles, {parts}{left_out}", flush=True)
    print(f"CHOG_BATCH done: {len(numbers)} Genesis in {time.monotonic() - started:.0f} s, into {out}")
    if failed:
        raise SystemExit("CHOG_BATCH some Genesis failed: see the FAILED lines")


if __name__ == "__main__":
    sys.exit(main())
