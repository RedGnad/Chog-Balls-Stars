#!/usr/bin/env python3
"""Summarise the console of a headless browser run (tools/unity/qualify_webgl.mjs).

  summarise_log.py <browser-smoke-events.json>

Prints the wallet and career lines, the skills fired and the damage dealt (page option perf=1),
when each Genesis signature fired with its simulation tick and whether the local player
started an attack at that tick, then the warnings, errors and failed requests. Skill and damage lines are written when a frame is predicted: a corrected
prediction can write a use again with another tick, so a use written twice with the same tick
is one use.
"""
import json
import sys
from collections import Counter

events = json.load(open(sys.argv[1]))
skills, damages, problems = Counter(), Counter(), Counter()
story, signatures = [], []
previous, seen, start = {}, set(), None
# Ticks at which the local player's own character started an attack, basic or special.
local_casts = set()
# Ticks of attack starts (anyone's) that stood once verified, and of those a correction undid.
confirmed, canceled = set(), set()

for event in events:
    method = event.get("method")
    params = event.get("params", {})
    if method == "Runtime.exceptionThrown":
        problems["exception: " + json.dumps(params.get("exceptionDetails", {}))[:200]] += 1
        continue
    if method == "Network.loadingFailed":
        problems["request failed: " + str(params.get("errorText")) + " " + str(params.get("url"))[:110]] += 1
        continue
    if method != "Runtime.consoleAPICalled":
        continue

    text = " ".join(str(argument.get("value", "")) for argument in params.get("args", []))
    first = text.split("\n")[0]
    if "CHOG_WALLET" in text or "CHOG_CAREER" in text:
        story.append(first[:220])
    elif "CHOG_COMBAT skill " in text:
        words = text.split("CHOG_COMBAT skill ")[1].split()
        name = words[0]
        tick = int(words[2]) if len(words) > 2 and words[1] == "tick" else None
        skills[name] += 1
        if "Genesis" in name:
            at = params["timestamp"] / 1000.0
            start = at if start is None else start
            again = " (same tick again)" if tick is not None and (name, tick) in seen else ""
            seen.add((name, tick))
            gap = f"+{tick - previous[name]} ticks" if tick is not None and previous.get(name) is not None else ""
            previous[name] = tick
            signatures.append((tick, f"{at - start:8.2f}s  tick {tick}  {name:44s} {gap}{again}"))
    elif "CHOG_COMBAT cast confirmed " in text or "CHOG_COMBAT cast canceled " in text:
        words = text.split("CHOG_COMBAT cast ")[1].split()
        if len(words) > 2 and words[1] == "tick":
            (confirmed if words[0] == "confirmed" else canceled).add(int(words[2]))
    elif "CHOG_COMBAT cast by local " in text:
        words = text.split("CHOG_COMBAT cast by local ")[1].split()
        if len(words) > 2 and words[1] == "tick":
            local_casts.add(int(words[2]))
    elif "CHOG_COMBAT damage " in text:
        damages[text.split("CHOG_COMBAT damage ")[1].split()[0]] += 1
    elif params.get("type") in ("warning", "error") or "Exception" in text:
        problems[first[:160]] += 1

if story:
    print("wallet and career:")
    for line in story:
        print("  " + line)
if skills:
    print("skills fired (lines):")
    for name, count in skills.most_common():
        print(f"  {count:4d}  {name}")
    print("damage events by amount:")
    for amount, count in sorted(damages.items(), key=lambda item: -item[1]):
        print(f"  {count:4d}  {amount}")
if signatures:
    # "local" marks a signature fired at a tick where the local player's character started an attack.
    print("Genesis signatures in time:")
    for tick, line in signatures[:60]:
        print(line + ("  local" if tick in local_casts else ""))
if local_casts:
    # A start that was only predicted is undone when the verified frame disagrees: "canceled".
    def outcome(tick):
        return "confirmed" if tick in confirmed else "canceled" if tick in canceled else "not settled"
    print("attacks started by the local player, by tick: "
          + ", ".join(f"{tick} {outcome(tick)}" for tick in sorted(local_casts)[:40]))
print("warnings, errors and failed requests:")
for text, count in problems.most_common(15):
    print(f"  {count:4d}  {text}")
