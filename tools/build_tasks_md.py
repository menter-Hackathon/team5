"""tools/tasks.json から docs/tasks.md を作る。

    python3 tools/build_tasks_md.py

tasks.json を編集したらこれを実行して docs/tasks.md を更新する。
前提タスクの抜け・循環・日付の矛盾があれば警告を出す。
"""
import json
import sys
from datetime import date
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DATA = json.loads((ROOT / "tools" / "tasks.json").read_text(encoding="utf-8"))
OUT = ROOT / "docs" / "tasks.md"

WEEK = "月火水木金土日"
tasks = DATA["tasks"]
by_id = {t["id"]: t for t in tasks}
ms = {m["id"]: m for m in DATA["milestones"]}


def d(s):
    return date.fromisoformat(s)


def md(s):
    x = d(s)
    return f"{x.month}/{x.day}({WEEK[x.weekday()]})"


def link(t):
    n = t.get("issue")
    label = f"{t['id']} {t['title']}"
    return f"[{label}](https://github.com/menter-Hackathon/team5/issues/{n})" if n else label


def period(t):
    return md(t["start"]) if t["start"] == t["due"] else f"{md(t['start'])}〜{md(t['due'])}"


# ---- 検証 ----
errors, warnings = [], []
if len(by_id) != len(tasks):
    errors.append("IDが重複しています")
for t in tasks:
    for dep in t["deps"]:
        if dep not in by_id:
            errors.append(f"{t['id']}: 前提 {dep} が存在しません")
    if d(t["start"]) > d(t["due"]):
        errors.append(f"{t['id']}: 開始日が締切より後です")
if errors:
    sys.exit("\n".join(errors))

succ = {t["id"]: [] for t in tasks}
for t in tasks:
    for dep in t["deps"]:
        succ[dep].append(t["id"])

state = {}


def visit(n, stack):
    if state.get(n) == 1:
        sys.exit("循環しています: " + " → ".join(stack + [n]))
    if state.get(n) == 2:
        return
    state[n] = 1
    for dep in by_id[n]["deps"]:
        visit(dep, stack + [n])
    state[n] = 2


for t in tasks:
    visit(t["id"], [])

prio_rank = {"P0": 0, "P1": 1, "P2": 2}
for t in tasks:
    for dep in t["deps"]:
        p = by_id[dep]
        if d(p["due"]) > d(t["due"]):
            warnings.append(f"{t['id']}（締切 {md(t['due'])}）の前提 {dep} の締切が {md(p['due'])} で後になっています")
        if prio_rank[p["priority"]] > prio_rank[t["priority"]]:
            warnings.append(f"{t['id']}（{t['priority']}）が優先度の低い {dep}（{p['priority']}）に依存しています。仮のもので進める想定")

# ---- クリティカルパス（P0 の中で一番長い前提の連鎖） ----
memo = {}


def longest(n):
    if n in memo:
        return memo[n]
    best = []
    for dep in by_id[n]["deps"]:
        if by_id[dep]["priority"] == "P0":
            c = longest(dep)
            if len(c) > len(best):
                best = c
    memo[n] = best + [n]
    return memo[n]


critical = max((longest(t["id"]) for t in tasks if t["priority"] == "P0"), key=len)

# ---- 出力 ----
L = []
w = L.append
w("# タスク一覧")
w("")
w("> このファイルは `tools/tasks.json` と `tools/specs/` から `python3 tools/build_tasks_md.py` で作っています。直接編集せず tasks.json を直してください。")
w("> GitHub に登録した後は、Issue と Project ボードの方が最新です。")
w("")
w(f"全 {len(tasks)} タスク（P0: {sum(t['priority']=='P0' for t in tasks)} / P1: {sum(t['priority']=='P1' for t in tasks)} / P2: {sum(t['priority']=='P2' for t in tasks)}）")
w("")
w("## 見方")
w("")
w("- **前提**：このタスクを始める前に終わっているとよいタスク。終わっていなければ仮のもので進めてよい。")
w("- **後続**：このタスクが終わるのを待っているタスク。後続が多いものほど早めに。")
w("- **優先度**：P0 = MVPに必須 / P1 = 入れたい / P2 = 余裕があれば")
w("- ID の頭文字：PL = 企画、BS = 基盤、CD = コード、DS = デザイン、AN = アニメ、SD = サウンド、PR = 発表")
w("")
streams = {s["id"]: s for s in DATA["streams"]}
w("## 担当領域")
w("")
w("タスクを1つずつ割り振るのではなく、ひとまとまりの「担当領域」ごとに担当を決めます。領域の中のどのタスクから手をつけるかは担当者に任せます。")
w("")
for s in DATA["streams"]:
    mine = [by_id[x] for x in s["tasks"]]
    waits = sorted({dep for t in mine for dep in t["deps"] if by_id[dep]["stream"] != s["id"]})
    gives = sorted({x for t in mine for x in succ[t["id"]] if by_id[x]["stream"] != s["id"]})
    w(f"### {s['emoji']} {s['name']}（{len(mine)}タスク）")
    w("")
    w(s["description"])
    w("")
    w("| ID | タスク | 優先度 | 期間 |")
    w("|---|---|---|---|")
    for t in mine:
        w(f"| [{t['id']}](#{t['id'].lower()}) | {('#'+str(t['issue'])+' ') if t.get('issue') else ''}{t['title']} | {t['priority']} | {period(t)} |")
    w("")
    w(f"- 他の領域から受け取るもの：{', '.join(waits) or 'なし'}")
    w(f"- 他の領域に渡すもの：{', '.join(gives) or 'なし'}")
    w("")
w("## 一番遅れてはいけない流れ（クリティカルパス）")
w("")
w("ここが1日遅れると、全体が1日遅れます。")
w("")
w(" → ".join(f"**{i}** {by_id[i]['title']}" for i in critical))
w("")
w("## マイルストーンごとの一覧")
for m in DATA["milestones"]:
    w("")
    w(f"### {m['title']}（〜{md(m['due'])}）")
    w("")
    w(m["description"])
    w("")
    w("| ID | タスク | 担当領域 | 優先度 | 期間 | 前提 | 後続 |")
    w("|---|---|---|---|---|---|---|")
    for t in tasks:
        if t["milestone"] != m["id"]:
            continue
        title = (f"#{t['issue']} " if t.get("issue") else "") + t["title"] + (" 🗣" if t.get("needs_discussion") else "")
        w(f"| [{t['id']}](#{t['id'].lower()}) | {title} | {streams[t['stream']]['emoji']} {streams[t['stream']]['name']} | {t['priority']} | {period(t)} | "
          f"{', '.join(t['deps']) or '－'} | {', '.join(succ[t['id']]) or '－'} |")
w("")
w("🗣 = 要審議")
w("")
w("## 前後関係の図")
w("")
w("矢印は「前提 → 後続」。太枠が P0。")
w("")
w("```mermaid")
w("flowchart LR")
for m in DATA["milestones"]:
    w(f'  subgraph {m["id"]}["{m["title"]}"]')
    for t in tasks:
        if t["milestone"] == m["id"]:
            nid = t["id"].replace("-", "_")
            w(f'    {nid}["{t["id"]}<br/>{t["title"]}"]')
    w("  end")
for t in tasks:
    for dep in t["deps"]:
        w(f'  {dep.replace("-", "_")} --> {t["id"].replace("-", "_")}')
cat_class = {"企画": "pl", "基盤": "bs", "コード": "cd", "デザイン": "ds", "アニメ": "an", "サウンド": "sd", "発表": "pr"}
for cat, cls in cat_class.items():
    color = DATA["categories"][cat]["color"]
    w(f"  classDef {cls} fill:#{color}33,stroke:#{color}")
for t in tasks:
    nid = t["id"].replace("-", "_")
    w(f"  class {nid} {cat_class[t['category']]}")
p0 = [t["id"].replace("-", "_") for t in tasks if t["priority"] == "P0"]
w("  classDef p0 stroke-width:3px")
w(f"  class {','.join(p0)} p0")
w("```")
w("")
w("## 各タスクの概要")
w("")
w("作るもの・仕様・完了条件などの詳細は各 Issue に書いてあります（元の文章は `tools/specs/`）。")
for s_ in DATA["streams"]:
    w("")
    w(f"### {s_['emoji']} {s_['name']}")
    for x in s_["tasks"]:
        t = by_id[x]
        w("")
        w(f"#### {t['id']}")
        w("")
        head = f"**{link(t)}**　{t['priority']} / {ms[t['milestone']]['title']} / {period(t)}"
        if t.get("parent"):
            head += f" / 親：{link(by_id[t['parent']])}"
        if t.get("needs_discussion"):
            head += "　`要審議`"
        w(head)
        w("")
        w(t["summary"])

OUT.write_text("\n".join(L) + "\n", encoding="utf-8")
print(f"{OUT.relative_to(ROOT)} を更新しました（{len(tasks)} タスク）")
for x in warnings:
    print("注意:", x)
