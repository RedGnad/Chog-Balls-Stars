#!/usr/bin/env python3
"""Serve the local Unity WebGL build with its Brotli/Gzip response headers."""
import argparse
import json
import urllib.error
import urllib.request
from datetime import datetime
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import unquote, urlsplit


# Genesis portraits are served next to the game but live outside the build folder
# (written by tools/genesis-art/fetch_portraits.py, not under version control).
ART_PREFIX = "/genesis-art/"
ART_ROOT = (Path(__file__).resolve().parents[2] / ".artifacts/genesis-art").resolve()
# The "find your hero" page is plain files of the repository, served next to the game.
HERO_PREFIX = "/hero/"
HERO_ROOT = (Path(__file__).resolve().parents[2] / "web/hero").resolve()
PERF_REPORTS = Path(__file__).resolve().parents[2] / ".artifacts/perf-reports.jsonl"
REPLAYS = Path(__file__).resolve().parents[2] / ".artifacts/replays"
# The game calls its services on its own origin; locally they run in another process
# (node server/src/local.js) and every /api/ request is forwarded to them.
API_PREFIX = "/api/"


class WebGLHandler(SimpleHTTPRequestHandler):
    follow_latest = False
    api_base = "http://127.0.0.1:8090"
    drop_first = ""
    dropped = False

    def translate_path(self, path):
        url_path = unquote(urlsplit(path).path)
        if url_path.startswith(ART_PREFIX):
            target = (ART_ROOT / url_path[len(ART_PREFIX):]).resolve()
            # Never leave the portrait folder, whatever the request path contains.
            return str(target) if ART_ROOT in target.parents else str(ART_ROOT / "not-found")
        if url_path == HERO_PREFIX.rstrip("/") or url_path.startswith(HERO_PREFIX):
            target = (HERO_ROOT / url_path[len(HERO_PREFIX):]).resolve()
            inside = target == HERO_ROOT or HERO_ROOT in target.parents
            return str(target) if inside else str(HERO_ROOT / "not-found")
        # With --latest every request is resolved against the newest finished build,
        # so a new build is served without restarting this server.
        if self.follow_latest:
            newest = latest_build()
            if newest is not None and str(newest) != getattr(WebGLHandler, "current", None):
                print(f"WebGL: now serving {newest}", flush=True)
                WebGLHandler.current = str(newest)
            if getattr(WebGLHandler, "current", None):
                self.directory = WebGLHandler.current
        return super().translate_path(path)

    def log_request(self, code="-", size="-"):
        # The page itself is logged with the browser that asked for it: when a phone fails to
        # load the game, this says which browser it was (a link often opens inside another app).
        if urlsplit(self.path).path == "/":
            self.log_message('"%s" %s browser "%s"', self.requestline, str(code), self.headers.get("User-Agent", "?"))
            return
        super().log_request(code, size)

    def forward_to_services(self):
        """Relay a /api/ request to the local game services and send their answer back."""
        length = min(int(self.headers.get("Content-Length") or 0), 100000)
        body = self.rfile.read(length) if length else None
        headers = {name: self.headers[name] for name in ("Content-Type", "Authorization") if self.headers.get(name)}
        # The services write the address of the game page in the message a wallet signs.
        headers["X-Forwarded-Host"] = self.headers.get("Host") or ""
        headers["X-Forwarded-Proto"] = "http"
        request = urllib.request.Request(self.api_base + self.path, data=body, headers=headers, method=self.command)
        try:
            with urllib.request.urlopen(request, timeout=30) as response:
                status, answer, kind = response.status, response.read(), response.headers.get("Content-Type")
        except urllib.error.HTTPError as error:
            status, answer, kind = error.code, error.read(), error.headers.get("Content-Type")
        except OSError:
            status, kind = 502, "application/json"
            answer = b'{"error":"services_down","message":"the local game services are not running (node server/src/local.js)"}'
        self.send_response(status)
        self.send_header("Content-Type", kind or "application/json")
        self.send_header("Content-Length", str(len(answer)))
        self.end_headers()
        self.wfile.write(answer)

    def do_GET(self):
        path = urlsplit(self.path).path
        if path.startswith(API_PREFIX):
            self.forward_to_services()
            return
        if self.drop_first and self.drop_first in path and not WebGLHandler.dropped:
            # Test option: once, behave like a connection that broke before any answer, to see
            # the game page load the file again by itself.
            WebGLHandler.dropped = True
            print(f"WebGL: dropped the first request for {path} (test option)", flush=True)
            self.close_connection = True
            return
        super().do_GET()

    def do_POST(self):
        if urlsplit(self.path).path.startswith(API_PREFIX):
            self.forward_to_services()
            return
        # With the page option record=1 the game posts the replay file of a finished match: it
        # is kept whole, named after what the game said of it, to be played again outside the
        # game (tools/quantum/headless.py verify). A test tool: nothing here is trusted.
        if urlsplit(self.path).path == "/replay-upload":
            length = int(self.headers.get("Content-Length") or 0)
            if not 0 < length <= 30_000_000:
                self.send_error(413)
                return
            claimed = {key: "".join(ch for ch in value if ch.isdigit() or ch == "-")[:12]
                       for key, value in (part.split("=", 1) for part in urlsplit(self.path).query.split("&") if "=" in part)}
            REPLAYS.mkdir(parents=True, exist_ok=True)
            name = f"{datetime.now().strftime('%Y%m%dT%H%M%S')}-winner{claimed.get('winner', 'x')}-tick{claimed.get('tick', 'x')}.json"
            (REPLAYS / name).write_bytes(self.rfile.read(length))
            print(f"WebGL: replay kept as {REPLAYS / name} ({length} bytes)", flush=True)
            self.send_response(204)
            self.end_headers()
            return
        # The game's performance overlay posts one JSON report a minute (page option perf=1).
        # Each is kept as a line with the client address, so a phone run can be read here.
        if urlsplit(self.path).path != "/perf-report":
            self.send_error(404)
            return
        length = min(int(self.headers.get("Content-Length") or 0), 20000)
        body = self.rfile.read(length).decode("utf-8", errors="replace").replace("\n", " ")
        PERF_REPORTS.parent.mkdir(parents=True, exist_ok=True)
        with PERF_REPORTS.open("a", encoding="utf-8") as reports:
            reports.write(json.dumps({"received": datetime.now().isoformat(timespec="seconds"),
                                      "client": self.client_address[0], "report": body}) + "\n")
        self.send_response(204)
        self.end_headers()

    def guess_type(self, path):
        if path.endswith((".br", ".gz")):
            path = path.rsplit(".", 1)[0]
        if path.endswith(".wasm"):
            return "application/wasm"
        if path.endswith(".data"):
            return "application/octet-stream"
        return super().guess_type(path)

    def end_headers(self):
        path = urlsplit(self.path).path
        if path.endswith(".br"):
            self.send_header("Content-Encoding", "br")
        elif path.endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")
        # A portrait never changes; the build must always be read fresh.
        self.send_header("Cache-Control", "public, max-age=3600" if path.startswith(ART_PREFIX) else "no-store")
        super().end_headers()


def latest_build():
    """Newest finished qualification build folder, or None.

    A build counts as finished once its build-report.json exists next to the webgl folder.
    """
    artifacts = Path(__file__).resolve().parents[2] / ".artifacts/unity"
    builds = [p for p in artifacts.glob("*/webgl")
              if (p / "index.html").is_file() and (p.parent / "build-report.json").is_file()]
    return max(builds, key=lambda p: (p / "index.html").stat().st_mtime).resolve() if builds else None


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=8080)
    parser.add_argument("--directory", type=Path,
                        default=Path(__file__).resolve().parents[2] / "game-fusion/Builds/WebGL")
    parser.add_argument("--latest", action="store_true",
                        help="Always serve the newest finished build made by Chog > Qualification "
                             "(.artifacts/unity/*/webgl); follows new builds without a restart.")
    parser.add_argument("--services", default=WebGLHandler.api_base,
                        help="Where /api/ requests are forwarded (the local game services).")
    parser.add_argument("--drop-first", default="", metavar="TEXT",
                        help="Test option: the first request whose path contains TEXT gets no answer, "
                             "as when a connection breaks (for instance framework.js).")
    args = parser.parse_args()
    WebGLHandler.api_base = args.services.rstrip("/")
    WebGLHandler.drop_first = args.drop_first
    directory = latest_build() if args.latest else args.directory.resolve()
    if directory is None:
        parser.error("No build under .artifacts/unity/*/webgl; run Chog > Qualification > Build WebGL first.")
    if not (directory / "index.html").is_file():
        parser.error(f"No WebGL build at {directory}; run Chog > Qualification > Build WebGL first.")
    WebGLHandler.follow_latest = args.latest
    WebGLHandler.current = str(directory)
    handler = partial(WebGLHandler, directory=str(directory))
    # A page opens several connections at once and this server closes each after one answer:
    # the default queue of 5 waiting connections is too short for a phone on Wi-Fi.
    ThreadingHTTPServer.request_queue_size = 64
    ThreadingHTTPServer.daemon_threads = True
    with ThreadingHTTPServer((args.host, args.port), handler) as server:
        print(f"WebGL: http://{args.host}:{args.port} serving {directory}", flush=True)
        try:
            server.serve_forever()
        except KeyboardInterrupt:
            pass


if __name__ == "__main__":
    main()
