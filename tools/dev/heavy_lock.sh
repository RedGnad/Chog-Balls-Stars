#!/bin/sh
# Runs one heavy job (Blender, a Unity build request, a headless browser) under the machine's lock.
#   sh tools/dev/heavy_lock.sh <who> <command> [arguments...]
# The lock is the directory .artifacts/heavy-job.d: mkdir either creates it or fails, so two waiters
# can never both take it (reading "free" in .artifacts/heavy-job.lock and then writing "busy" could:
# two Blender processes ran together on 22 October 2026). The text file still says who holds it.
# Put a time limit on the job itself, never around this script: "timeout 400 sh heavy_lock.sh ..."
# also counts the wait for the lock and kills the job before it ends.
# A restart of the Mac leaves the lock taken (seen 22 October 2026): when nothing heavy runs, free it
# with "rmdir .artifacts/heavy-job.d" and write "free" in .artifacts/heavy-job.lock.
ROOT=$(cd "$(dirname "$0")/../.." && pwd)
LOCK="$ROOT/.artifacts/heavy-job.d"
NOTE="$ROOT/.artifacts/heavy-job.lock"
WHO="$1"; shift
until mkdir "$LOCK" 2>/dev/null; do sleep 3; done
echo "busy $WHO" > "$NOTE"
# Released once: a TERM used to run the release twice (for TERM, then for EXIT), and the second time
# could free a lock another job had taken in between.
release() {
  trap - EXIT INT TERM
  echo free > "$NOTE"
  rmdir "$LOCK"
}
trap release EXIT
# Stopped, it stops its job too before freeing the lock: a Blender left running without the lock would
# let a second one start beside it.
"$@" &
JOB=$!
trap 'kill "$JOB" 2>/dev/null; wait "$JOB" 2>/dev/null; release; exit 143' INT TERM
wait "$JOB"
