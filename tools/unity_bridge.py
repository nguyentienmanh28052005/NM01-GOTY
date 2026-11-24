"""Small CLI for the already installed MCPForUnity loopback bridge.

Usage: python tools/unity_bridge.py command [JSON parameters]
Only connects to the status file belonging to this workspace.
"""
import json
from pathlib import Path
import socket
import struct
import sys
import time
import urllib.error
import urllib.request


def call(command, params=None, timeout=45):
    # The editor can switch between stdio and its installed HTTP MCP server.
    # Select the actual instance by the full project hash (not the current/default session).
    http = "http://127.0.0.1:8090"
    instances = []
    for attempt in range(25):
        try:
            with urllib.request.urlopen(http + "/api/instances", timeout=2) as response:
                instances = json.load(response).get("instances", [])
        except (OSError, ValueError):
            break  # No HTTP server: discover its stdio bridge below.
        target = [i for i in instances if i.get("project") == "NM01-GOTY" and i.get("hash") == "fccbaeec7da3b4aa"]
        if target:
            break
        time.sleep(1)  # Server remains alive while its Unity domain reconnects.
    target = [i for i in instances if i.get("project") == "NM01-GOTY" and i.get("hash") == "fccbaeec7da3b4aa"]
    if len(target) == 1:
        payload = json.dumps({"type": command, "params": params or {}, "unity_instance": "NM01-GOTY@" + target[0]["hash"]}).encode()
        request = urllib.request.Request(http + "/api/command", data=payload, headers={"Content-Type": "application/json"})
        with urllib.request.urlopen(request, timeout=timeout) as response:
            return json.load(response)
    project = (Path(__file__).resolve().parents[1] / "Assets").as_posix().casefold()
    deadline = time.monotonic() + timeout
    # Domain reload briefly removes the status file/listener. Retry connection only;
    # never replay a command after sending it, since it may mutate the editor.
    sock = None
    for attempt in range(20):
        statuses = []
        for p in (Path.home() / ".unity-mcp").glob("unity-mcp-status-*.json"):
            try:
                statuses.append(json.loads(p.read_text(encoding="utf-8-sig")))
            except (OSError, ValueError):
                continue
        matches = [s for s in statuses if s.get("project_path", "").replace("\\", "/").casefold() == project]
        if len(matches) > 1:
            raise RuntimeError(f"More than one bridge for {project}")
        try:
            if matches:
                sock = socket.create_connection(("127.0.0.1", matches[0]["unity_port"]), 1)
                break
        except OSError:
            pass
        time.sleep(1)
    if sock is None:
        raise TimeoutError(f"No ready bridge for {project} after 20 connection attempts")
    with sock:
        sock.settimeout(timeout)
        welcome = bytearray()
        for _ in range(512):
            c = sock.recv(1)
            if not c:
                raise ConnectionError("Bridge closed during handshake")
            welcome.extend(c)
            if c == b"\n":
                break
        if b"FRAMING=1" not in welcome:
            raise RuntimeError(f"Unexpected handshake: {welcome!r}")
        data = json.dumps({"type": command, "params": params or {}}).encode("utf-8")
        sock.sendall(struct.pack(">Q", len(data)) + data)

        def read_exact(count):
            result = bytearray()
            while len(result) < count and time.monotonic() < deadline:
                sock.settimeout(max(.1, deadline - time.monotonic()))
                chunk = sock.recv(count - len(result))
                if not chunk:
                    raise ConnectionError("Bridge closed before completing response")
                result.extend(chunk)
            if len(result) != count:
                raise TimeoutError("Bridge response deadline exceeded")
            return bytes(result)

        for _ in range(100):
            length = struct.unpack(">Q", read_exact(8))[0]
            if length > 64 * 1024 * 1024:
                raise ValueError("Bridge response too large")
            if length:
                return json.loads(read_exact(length))
        raise TimeoutError("Too many heartbeat frames")


if __name__ == "__main__":
    def compact(value):
        if isinstance(value, dict):
            return {k: compact(v) for k, v in value.items() if k not in ("imageBase64", "previewBase64")}
        if isinstance(value, list):
            return [compact(v) for v in value]
        return value
    print(json.dumps(compact(call(sys.argv[1], json.loads(sys.argv[2]) if len(sys.argv) > 2 else {})), ensure_ascii=True))
