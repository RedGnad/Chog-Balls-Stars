#!/bin/sh
# Screenshot of a web page at a phone size: see screenshot.mjs, which this only calls.
# Usage: screenshot.sh <url> <output.png> [width] [height] [full page 1|0]
exec node "$(dirname "$0")/screenshot.mjs" "$@"
