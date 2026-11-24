"""Run bounded, recorded tests against freshly compiled code in the live editor."""
import argparse
import json
from pathlib import Path
import sys
import time
from unity_bridge import call

p = argparse.ArgumentParser()
p.add_argument("--filter", nargs="+")
p.add_argument("--label", required=True)
p.add_argument("--compile", action="store_true")
args = p.parse_args()


def state():
    response = call("get_editor_state")
    return response.get("result", response).get("data") or {}


def wait_playing(value):
    for attempt in range(60):
        s = state()
        mode = s.get("editor", {}).get("play_mode", {})
        # MCP 10.1.2 uses isPlayingOrWillChangePlaymode for is_changing;
        # that is also true for the entire ordinary Play session.
        if mode.get("is_playing") == value and (value or not mode.get("is_changing")) and not s.get("compilation", {}).get("is_compiling"):
            return
        time.sleep(.5)
    raise TimeoutError("Editor did not finish its play-mode transition")


if args.compile:
    call("manage_editor", {"action": "stop"})
    wait_playing(False)
    call("refresh_unity", {"mode": "force", "scope": "all", "compile": "request"})
    # Unity 6 defers a pending compile in the background. Entering Play flushes it.
    call("manage_editor", {"action": "play"})
    wait_playing(True)
    call("manage_editor", {"action": "stop"})
    wait_playing(False)

parameters = {"mode": "EditMode"}
parameters["groupNames" if args.filter else "assemblyNames"] = args.filter or ["HanaMart.Tests.EditMode"]
started = call("run_tests", parameters)
data = started.get("result", started).get("data", {})
job = data.get("job_id")
if not job:
    raise RuntimeError(started)
print("Test job " + job, flush=True)
for attempt in range(150):
    response = call("get_test_job", {"job_id": job, "includeFailedTests": True, "includeDetails": True})
    data = response.get("result", response).get("data", {})
    if data.get("status") in ("succeeded", "failed"):
        destination = Path("Logs/HanaMart/tests") / (args.label + ".json")
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")
        summary = (data.get("result") or {}).get("summary") or data.get("progress")
        print(json.dumps({"status": data.get("status"), "summary": summary, "evidence": str(destination)}, ensure_ascii=True))
        total = (data.get("result") or {}).get("summary", {}).get("total", data.get("progress", {}).get("total", 0))
        sys.exit(0 if data.get("status") == "succeeded" and total > 0 else 1)
    time.sleep(1)
raise TimeoutError("Test job did not complete within 150 polls")
