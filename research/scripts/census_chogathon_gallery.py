#!/usr/bin/env python3
"""Read the project gallery of the Chogathon site, the way the site's own page does.

The home page (https://chogathon.chog.xyz/) is one HTML file. At load it reads the table
"projects" of its Supabase project with the public client key written in the page, newest
first, and merges the rows with five example projects written in the page itself. This script
makes the same read and keeps the result as evidence. The client key is taken from the page at
run time; it is never printed and never written to the output.

  python3 -I research/scripts/census_chogathon_gallery.py --output research/evidence/chogathon-gallery-<date>.json

Read-only. Images are left out: a banner stored as inline data is replaced by its size.
"""
import argparse
import hashlib
import json
import re
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

SITE = "https://chogathon.chog.xyz/"
BROWSER = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36"


def fetch(url, headers):
    request = urllib.request.Request(url, headers={"User-Agent": BROWSER, **headers})
    with urllib.request.urlopen(request, timeout=60) as response:
        return response.status, response.read()


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--output", type=Path, help="write the census as JSON")
    args = parser.parse_args()

    status, page = fetch(SITE, {"Accept": "text/html"})
    html = page.decode("utf-8", errors="replace")
    base = re.search(r'const SUPABASE_URL = "([^"]+)"', html)
    key = re.search(r'const SUPABASE_ANON = "([^"]+)"', html)
    signup_end = re.search(r'const CHOG_SIGNUP_END = ([^;]+);', html)
    if not base or not key:
        raise SystemExit("The page no longer names its data source the same way: read it again by hand.")

    # The example projects written in the page, to tell them from real ones.
    seeds = re.findall(r'\{ t: "([^"]+)", d: "', html[html.find("const _projects = ["):html.find("const _starred")])

    # The columns the page itself selects (23 October 2026): "select=*" includes banner_url, up to 6 MB of base64 a row, which
    # passes Postgres' statement timeout (HTTP 500), as the page's own comment says.
    columns = "id,user_id,title,description,app_link,author,tokens,status,created_at,video_url,tech_stack,gtm_plan"
    query = base.group(1) + "/rest/v1/projects?select=" + columns + "&order=created_at.desc"
    read_status, body = fetch(query, {"apikey": key.group(1), "Authorization": "Bearer " + key.group(1), "Accept": "application/json"})
    rows = json.loads(body)

    # The page shows a live number of applications in its stats strip: the same count, and
    # nothing else of that table, is read here.
    applications = None
    count_request = urllib.request.Request(
        base.group(1) + "/rest/v1/registrations?select=id", method="HEAD",
        headers={"User-Agent": BROWSER, "apikey": key.group(1), "Authorization": "Bearer " + key.group(1), "Prefer": "count=exact"})
    try:
        with urllib.request.urlopen(count_request, timeout=60) as response:
            total = (response.headers.get("Content-Range") or "").rpartition("/")[2]
            applications = int(total) if total.isdigit() else None
    except OSError:
        applications = None

    projects = []
    for row in rows:
        banner = row.get("banner_url") or ""
        kept = {name: value for name, value in row.items() if name != "banner_url"}
        kept["banner"] = f"inline image, {len(banner)} characters" if banner.startswith("data:") else banner
        projects.append(kept)

    census = {
        "recorded_at_utc": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "site": SITE,
        "page_http_status": status,
        "page_bytes": len(page),
        "page_sha256": hashlib.sha256(page).hexdigest(),
        "signup_end_expression": signup_end.group(1) if signup_end else None,
        "source": "table 'projects' of the site's Supabase project, read with the public client key of the page (not recorded here)",
        "read_http_status": read_status,
        "applications_shown_by_the_page": applications,
        "example_projects_written_in_the_page": seeds,
        "project_count": len(projects),
        "columns": sorted({name for project in projects for name in project}),
        "projects": projects,
    }

    print(f"{census['recorded_at_utc']}  page {status}, {len(page)} bytes; projects table {read_status}: {len(projects)} rows")
    print("applications shown by the page:", applications if applications is not None else "not readable")
    print("example projects in the page:", ", ".join(seeds) or "none")
    for project in projects:
        title = str(project.get("title", ""))[:40]
        text = " ".join(str(project.get("description", "")).split())[:110]
        print(f"- {title:40s} | {str(project.get('author', ''))[:22]:22s} | {str(project.get('app_link', ''))[:50]:50s} | {text}")

    if args.output:
        args.output.write_text(json.dumps(census, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")
        print("written:", args.output)


if __name__ == "__main__":
    main()
