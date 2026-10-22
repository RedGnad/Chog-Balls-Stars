#!/bin/sh
# Runs one heavy job (Blender, a Unity build request, a headless browser) under the machine's lock.
#   sh tools/dev/heavy_lock.sh <who> <command> [arguments...]
# The lock is the directory .artifacts/heavy-job.d: mkdir either creates it or fails, so two waiters
# can never both take it (reading "free" in .artifacts/heavy-job.lock and then writing "busy" could:
# two Blender processes ran together on 22 October 2026). The text file still says who holds it.
ROOT=$(cd "$(dirname "$0")/../.." && pwd)
LOCK="$ROOT/.artifacts/heavy-job.d"
NOTE="$ROOT/.artifacts/heavy-job.lock"
WHO="$1"; shift
until mkdir "$LOCK" 2>/dev/null; do sleep 3; done
echo "busy $WHO" > "$NOTE"
trap 'echo free > "$NOTE"; rmdir "$LOCK"' EXIT INT TERM
"$@"
