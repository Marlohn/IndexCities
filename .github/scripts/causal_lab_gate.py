#!/usr/bin/env python3
"""Trusted read-only gate for event-driven CausalLab."""
import datetime as dt
import json
import os
import subprocess
import sys
from pathlib import Path

REPO = "Marlohn/IndexCities"
CI = "causal-lab-ci.yml"
ACTIVE_LAB_BRANCH = "experimento/motor-causal-10k-80y-autonomo"
DAILY_LIMIT = 8

def api(path):
    p = subprocess.run(["gh", "api", "--method", "GET", f"repos/{REPO}/{path}"],
                       check=True, capture_output=True, text=True)
    return json.loads(p.stdout)

def output(allowed, reason, previous=""):
    print(f"CausalLab gate: {reason}; proceed={allowed}")
    if os.getenv("GITHUB_OUTPUT"):
        with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf8") as fd:
            fd.write(f"proceed={str(allowed).lower()}\n")
            fd.write("previous=" + str(previous) + "\n")
    if not allowed and reason == "OPENAI_API_KEY missing":
        print("::warning::Set OPENAI_API_KEY in repository Actions secrets and run the workflow manually.")

def main():
    if os.getenv("GITHUB_REPOSITORY") != REPO:
        output(False, "different repository"); return
    if os.getenv("HAS_API_KEY") != "true":
        output(False, "OPENAI_API_KEY missing"); return
    event_type = os.getenv("GITHUB_EVENT_NAME", "")
    payload = json.loads(Path(os.environ["GITHUB_EVENT_PATH"]).read_text())
    previous = ""
    if event_type == "workflow_run":
        run = payload.get("workflow_run", {})
        previous = run.get("id", "")
        if (run.get("name") not in ("IndexCities Causal Lab CI", "Causal Engine - integrated reality check")
                or run.get("head_repository", {}).get("full_name") != REPO
                or not (
                    run.get("head_branch") == "main" and run.get("event") == "workflow_dispatch"
                    or run.get("head_branch") == ACTIVE_LAB_BRANCH and run.get("event") == "push"
                )):
            output(False, "irrelevant or untrusted completion"); return
        if run.get("conclusion") not in ("success", "failure", "cancelled", "timed_out"):
            output(False, "CI unfinished"); return
    elif event_type not in ("workflow_dispatch", "push", "schedule"):
        output(False, "untrusted event"); return
    if api("pulls/52").get("state") != "open":
        output(False, "PR #52 not open"); return
    active = api("actions/runs?per_page=100").get("workflow_runs", [])
    if any(r.get("head_branch") == ACTIVE_LAB_BRANCH
           and r.get("status") in ("queued", "in_progress", "waiting", "requested", "pending")
           for r in active):
        output(False, "existing CausalEngine branch has an active CI"); return
    runs = api(f"actions/workflows/{CI}/runs?per_page=100").get("workflow_runs", [])
    if any(r.get("status") in ("queued", "in_progress", "waiting", "requested", "pending") for r in runs):
        output(False, "another experiment CI is active"); return
    today = dt.datetime.now(dt.timezone.utc).date()
    count = sum(dt.date.fromisoformat(r["created_at"][:10]) == today for r in runs)
    if count >= DAILY_LIMIT:
        output(False, f"daily cap reached: {count}/{DAILY_LIMIT}"); return
    output(True, f"PR #52 open; {count}/{DAILY_LIMIT} CI runs used today", previous)

if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        print(f"::error::Failed closed: {type(e).__name__}: {e}")
        sys.exit(1)
