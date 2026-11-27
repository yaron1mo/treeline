#!/usr/bin/env python3
"""Office digest for Telegram (2026-11-27): what runs now, what waits, what needs Yaroslav, what finished.

Reads GitHub with gh (no AI model, no tokens). Prints the message in Ukrainian (it is for Yaroslav).
Usage: digest.py <owner/repo> [hours back for "done", default 3]
"""
import datetime as dt
import json
import subprocess
import sys

repo = sys.argv[1]
hours = float(sys.argv[2]) if len(sys.argv) > 2 else 3
since = dt.datetime.now(dt.timezone.utc) - dt.timedelta(hours=hours)


def gh(path):
    out = subprocess.run(["gh", "api", "--paginate", path], capture_output=True, text=True, check=True).stdout
    return json.loads(out) if out.strip().startswith("[") or out.strip().startswith("{") else []


def labels(i):
    return {l["name"] for l in i.get("labels", [])}


def when(s):
    return dt.datetime.fromisoformat(s.replace("Z", "+00:00"))


issues = [i for i in gh(f"repos/{repo}/issues?state=open&per_page=100") if "pull_request" not in i]
prs = [i for i in gh(f"repos/{repo}/issues?state=open&per_page=100") if "pull_request" in i]
closed = [i for i in gh(f"repos/{repo}/issues?state=closed&per_page=50&sort=updated&direction=desc")
          if i.get("closed_at") and when(i["closed_at"]) >= since]

running, queued, needs_me, needs_spec = [], [], [], []
for i in issues:
    ls = labels(i)
    t = f"#{i['number']} {i['title']}"
    if "needs:yaroslav" in ls:
        needs_me.append(t)
    if "in-progress" in ls or "agent:mistral" in ls or "agent:codex" in ls:
        who = "Mistral" if "agent:mistral" in ls or "mistral" in i["title"].lower() else "Codex"
        running.append(f"{who}: {t}")
    elif "queue:mistral" in ls or "queue:codex" in ls:
        queued.append(("Mistral" if "queue:mistral" in ls else "Codex") + f": {t}")
    elif "needs-spec" in ls:
        needs_spec.append(t)
for p in prs:
    if "needs:yaroslav" in labels(p):
        needs_me.append(f"PR #{p['number']} {p['title']}")

review = [f"PR #{p['number']} {p['title']}" for p in prs if "needs:yaroslav" not in labels(p)
          and "ready-to-merge" not in labels(p) and p["user"]["login"] != "claude[bot]"
          and any(x in p["title"].lower() for x in ("mistral", "codex"))]
done = [f"{'PR' if 'pull_request' in i else ''} #{i['number']} {i['title']}".strip() for i in closed
        if i["title"] not in ("Dispatcher log",)]


def block(title, items, limit=8):
    if not items:
        return []
    out = [title] + [f"• {x}" for x in items[:limit]]
    if len(items) > limit:
        out.append(f"• … ще {len(items) - limit}")
    return out + [""]


lines = ["📋 Treeline: стан офісу", ""]
lines += block("▶️ Працюють зараз:", running)
lines += block("🟠 Чекають на тебе:", needs_me)
lines += block("⏳ У черзі:", queued)
lines += block("✍️ Чекають ТЗ від Claude:", needs_spec)
lines += block("🔎 PR агентів відкриті:", review)
lines += block(f"✅ Закрито за {hours:g} год:", done)
if len(lines) == 2:
    lines.append("Нічого не відбувається. Черга порожня.")
print("\n".join(lines).strip())
