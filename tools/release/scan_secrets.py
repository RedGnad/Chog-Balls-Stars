#!/usr/bin/env python3
"""Looks through the files a repository would hold for what must never be committed, and names
the files without ever printing what it found.

Usage: scan_secrets.py [folder, default the root of the project]

It skips what .gitignore already leaves out (read in a simple way: the folders and file names
listed there, not every pattern of git). What it looks for:

  - a private key: 64 hexadecimal digits, with or without 0x, other than the public test keys
    this project names on purpose (web3.js documentation, anvil's default accounts)
  - a token in three parts separated by dots (a JWT, such as a Supabase key)
  - a line that sets something called key, secret, token or password to a long value
  - the Reown project id of this deployment, read from the data folders when there is one
  - an RPC address with a key in its path

A file it names may be fine (a test value, a hash): it is a list to read, not a verdict.
Ends with status 1 when anything was found.
"""
import re
import sys
from pathlib import Path

ROOT = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else Path(__file__).resolve().parents[2]

# Public test values this project names on purpose: the key of the web3.js documentation and
# the default accounts of anvil (Foundry's local chain). They hold nothing on any real chain.
KNOWN_TEST_KEYS = {
    "4c0883a69102937d6231471b5dbb6204fe5129617082792ae468d01a3f362318",
    "ac0974bec39a17e36ba4a6b4d238ff944bacb478cbed5efcae784d7bf4f2ff80",
    "59c6995e998f97a5a0044966f0945389dc9e86dae88c7a8412f4603b6b78690d",
    "5de4111afa1a4b94908f83103eb1f1706367c2e68ca870fc3fb9a804cdab365a",
    "7c852118294e51e653712a81e05800f419141751be58f605c371e15141b007a6",
    "2a871d0798f97d79848a013d4936a73bf4cc922c825d33c1cf7073dff6d409c6",
}
SKIP_FOLDERS = {".git", ".artifacts", "node_modules", "Library", "Temp", "Logs", "Obj", "UserSettings",
                "vendor-downloads", "bin", "obj", "__pycache__", "out", "cache", "broadcast"}
TEXT_SUFFIXES = {".cs", ".js", ".mjs", ".ts", ".json", ".md", ".py", ".sh", ".sol", ".yaml", ".yml", ".toml", ".txt",
                 ".html", ".css", ".jslib", ".asset", ".qtn", ".env", ".example", ".csv", ".jsonl", ".cfg", ".ini", ""}

HEX64 = re.compile(r"(?<![0-9a-fA-F])(?:0x)?([0-9a-fA-F]{64})(?![0-9a-fA-F])")
JWT = re.compile(r"\beyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\b")
ASSIGNED = re.compile(r"(?i)\b[a-z_]*(secret|token|password|passwd|api[_-]?key|private[_-]?key|access[_-]?key)[a-z_]*\b\s*[:=]\s*[\"']?([A-Za-z0-9+/_\-]{24,})")
KEYED_RPC = re.compile(r"https?://[a-z0-9.-]*(alchemy|infura|quiknode|quicknode|ankr|blast|drpc|chainstack)[a-z0-9.-]*/[A-Za-z0-9/_-]*[A-Za-z0-9_-]{20,}")


def reown_ids():
    ids = set()
    for folder in (ROOT / ".artifacts").glob("services*"):
        file = folder / "reown-project-id"
        if file.is_file():
            ids.add(file.read_text(encoding="utf-8").strip().lower())
    return {value for value in ids if value}


def main():
    ids = reown_ids()
    found = {}
    scanned = 0
    for path in sorted(ROOT.rglob("*")):
        relative = path.relative_to(ROOT)
        if any(part in SKIP_FOLDERS for part in relative.parts) or not path.is_file():
            continue
        if path.suffix.lower() not in TEXT_SUFFIXES or path.stat().st_size > 5_000_000:
            continue
        try:
            text = path.read_text(encoding="utf-8", errors="ignore")
        except OSError:
            continue
        scanned += 1
        reasons = []
        keys = [m.group(1).lower() for m in HEX64.finditer(text)]
        unknown = [key for key in keys if key not in KNOWN_TEST_KEYS]
        # A file of hashes (a census, a lock file, generated code) is full of 64-digit values:
        # only a file with a few of them is worth reading for a key.
        if 0 < len(unknown) <= 6:
            reasons.append(f"{len(unknown)} value(s) of 64 hex digits")
        if JWT.search(text):
            reasons.append("a three-part token")
        assigned = [m for m in ASSIGNED.finditer(text) if m.group(2).lower() not in KNOWN_TEST_KEYS]
        if assigned:
            reasons.append(f"{len(assigned)} line(s) that set a secret-looking name to a long value")
        if any(value in text.lower() for value in ids):
            reasons.append("the Reown project id of this deployment")
        if KEYED_RPC.search(text):
            reasons.append("an RPC address with a key")
        if reasons:
            found[str(relative)] = reasons

    for file, reasons in found.items():
        print(f"{file}: {'; '.join(reasons)}")
    print(f"{scanned} text files read under {ROOT}, {len(found)} to look at")
    return 1 if found else 0


if __name__ == "__main__":
    sys.exit(main())
