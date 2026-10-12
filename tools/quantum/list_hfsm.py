#!/usr/bin/env python3
"""Prints a state machine of the exported simulation assets (Bot SDK HFSM) as text: each state
with what it does on entering, on each tick and on leaving, and each way out with the
decision that opens it and where it leads.

Usage: list_hfsm.py <quantum-db.json> [text the asset paths must contain, default "Coin Grab Manager"]

The machine that runs a match (selection, countdown, play, end) is "Coin Grab Manager": this
is where the rules of winning are read, since nothing in the C# sources states them.
"""
import json
import sys
from pathlib import Path

ONE = 65536.0


def short(type_name):
    return type_name.split(",")[0].replace("Quantum.", "")


def number(value):
    if isinstance(value, dict) and "RawValue" in value:
        return f"{value['RawValue'] / ONE:g}"
    return value


def brief(asset):
    """The fields of an action, a decision or a function that say what it does."""
    skip = {"$type", "Identifier", "Label"}
    parts = []
    for key, value in asset.items():
        if key in skip:
            continue
        if isinstance(value, dict) and "Source" in value:
            # An AIParam: a constant (DefaultValue) or the result of a function.
            function = value.get("FunctionRef", {}).get("Id", {}).get("Value", 0)
            parts.append(f"{key}={'fn:' + str(function) if function else number(value.get('DefaultValue'))}")
        elif isinstance(value, dict) and "RawValue" in value:
            parts.append(f"{key}={number(value)}")
        elif isinstance(value, dict) and set(value) == {"Id"}:
            parts.append(f"{key}->{value['Id'].get('Value')}")
        elif not isinstance(value, (dict, list)):
            parts.append(f"{key}={value}")
    return ", ".join(parts)


def main():
    assets = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8-sig"))
    wanted = sys.argv[2] if len(sys.argv) > 2 else "Coin Grab Manager"
    by_guid = {a.get("Identifier", {}).get("Guid", {}).get("Value"): a for a in assets}

    def name(guid):
        asset = by_guid.get(guid)
        if not asset:
            return f"<{guid}>"
        return asset.get("Identifier", {}).get("Path", "").split("|")[-1]

    def describe(ref):
        guid = ref.get("Id", {}).get("Value") if isinstance(ref, dict) else ref
        asset = by_guid.get(guid)
        if not asset:
            return f"<missing {guid}>"
        return f"{short(asset.get('$type', '?'))}({brief(asset)})"

    for asset in assets:
        path = asset.get("Identifier", {}).get("Path", "")
        kind = short(asset.get("$type", ""))
        if wanted not in path:
            continue
        if kind == "HFSMState":
            print(f"STATE {name(asset['Identifier']['Guid']['Value'])}  (children: "
                  f"{[name(c.get('Id', {}).get('Value')) for c in asset.get('ChildrenLinks', [])]})")
            for moment in ("OnEnterLinks", "OnUpdateLinks", "OnExitLinks"):
                for link in asset.get(moment, []):
                    print(f"   {moment[:-5]:9} {describe(link)}")
            for transition in asset.get("Transitions", []):
                target = name(transition.get("StateLink", {}).get("Id", {}).get("Value"))
                decision = transition.get("DecisionLink", {})
                print(f"   -> {target or '(set)'}  when {describe(decision) if decision.get('Id', {}).get('Value') else 'always'}"
                      f"  priority {transition.get('Priority')}  event {transition.get('EventKey')}")
        elif kind in ("HFSMRoot", "HFSMTransitionSet"):
            print(f"{kind} {path.split('|')[-1]}: {brief(asset)}")
    print()
    print("Functions and decisions named above:")
    for asset in assets:
        path = asset.get("Identifier", {}).get("Path", "")
        kind = short(asset.get("$type", ""))
        if wanted in path and kind not in ("HFSMState", "HFSMRoot", "HFSMTransitionSet"):
            print(f"   {asset['Identifier']['Guid']['Value']}  {path.split('|')[-1]}: {kind}({brief(asset)})")


if __name__ == "__main__":
    main()
