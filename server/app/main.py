from __future__ import annotations

import html
import json
import sqlite3
from contextlib import asynccontextmanager
from datetime import datetime
from zoneinfo import ZoneInfo

from fastapi import FastAPI, HTTPException, Query, Request
from fastapi.middleware.gzip import GZipMiddleware
from fastapi.responses import HTMLResponse

from .config import settings
from .database import connect, initialize_database, transaction
from .models import LeaderboardSubmission, SaveUpload, ServerHeartbeat


KST = ZoneInfo("Asia/Seoul")


@asynccontextmanager
async def lifespan(_: FastAPI):
    initialize_database()
    yield


app = FastAPI(
    title="Error Dungeon Linux Server",
    version="1.0.0",
    docs_url="/api/docs",
    openapi_url="/api/openapi.json",
    lifespan=lifespan,
)
app.add_middleware(GZipMiddleware, minimum_size=800)


def now_kst() -> str:
    return datetime.now(KST).strftime("%Y-%m-%d %H:%M:%S KST")


def increment_visits() -> int:
    with transaction() as database:
        database.execute(
            "INSERT INTO counters(key, value) VALUES('visits', 1) "
            "ON CONFLICT(key) DO UPDATE SET value = value + 1"
        )
        row = database.execute(
            "SELECT value FROM counters WHERE key = 'visits'"
        ).fetchone()
        return int(row["value"])


def top_entries(limit: int) -> list[dict[str, object]]:
    database = connect()
    try:
        rows = database.execute(
            "SELECT player_name, max_region, max_boss, level, clear_time_sec, submitted_at "
            "FROM leaderboard ORDER BY max_region DESC, level DESC, clear_time_sec ASC, submitted_at ASC "
            "LIMIT ?",
            (limit,),
        ).fetchall()
        return [dict(row) for row in rows]
    finally:
        database.close()


def render_home(server_time: str, visits: int, leaders: list[dict[str, object]]) -> str:
    if leaders:
        leaderboard_rows = "".join(
            f"<tr><td>{rank}</td><td>{html.escape(str(entry['player_name']))}</td>"
            f"<td>{entry['level']}</td><td>{entry['max_region']}</td>"
            f"<td>{int(entry['clear_time_sec']) // 60:02d}:{int(entry['clear_time_sec']) % 60:02d}</td></tr>"
            for rank, entry in enumerate(leaders, start=1)
        )
    else:
        leaderboard_rows = '<tr><td colspan="5" class="empty">아직 등록된 원정 기록이 없습니다.</td></tr>'

    return f"""<!doctype html>
<html lang="ko">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Error Dungeon — Linux Server</title>
  <style>
    :root {{ color-scheme: dark; --ink:#e8e4dc; --muted:#96938d; --ember:#cc5c28; --blue:#4978c7; }}
    * {{ box-sizing:border-box; }}
    body {{ margin:0; min-height:100vh; color:var(--ink); background:radial-gradient(circle at 70% 15%,#17243a 0,#090b10 42%,#050608 100%); font-family:system-ui,sans-serif; }}
    main {{ width:min(1080px,92vw); margin:auto; padding:8vh 0; }}
    .eyebrow {{ color:var(--ember); letter-spacing:.24em; text-transform:uppercase; font-weight:700; }}
    h1 {{ margin:.3rem 0 0; font-family:serif; font-size:clamp(3rem,8vw,7rem); font-weight:500; letter-spacing:-.04em; }}
    .subtitle {{ color:var(--muted); font-size:1.1rem; max-width:48rem; }}
    .grid {{ display:grid; grid-template-columns:repeat(3,1fr); gap:1px; margin:3rem 0; background:#ffffff18; border:1px solid #ffffff18; }}
    .metric {{ padding:1.4rem; background:#0a0d13dd; }}
    .metric b {{ display:block; margin-top:.4rem; font-family:monospace; font-size:1.2rem; }}
    .metric span {{ color:var(--muted); font-size:.8rem; letter-spacing:.12em; }}
    section {{ background:#0a0d13bb; border:1px solid #ffffff18; padding:1.5rem; }}
    h2 {{ margin-top:0; font-weight:500; }}
    table {{ width:100%; border-collapse:collapse; }}
    th,td {{ text-align:left; padding:.8rem; border-bottom:1px solid #ffffff12; }}
    th {{ color:var(--muted); font-size:.75rem; letter-spacing:.12em; }}
    .empty {{ color:var(--muted); text-align:center; padding:2rem; }}
    footer {{ margin-top:2rem; display:flex; justify-content:space-between; color:var(--muted); font-size:.85rem; }}
    .live {{ color:#76c986; }}
    @media(max-width:680px) {{ .grid {{ grid-template-columns:1fr; }} footer {{ display:block; }} }}
  </style>
</head>
<body>
<main>
  <div class="eyebrow">Unity × Ubuntu 24.04</div>
  <h1>Error Dungeon</h1>
  <p class="subtitle">오류에 잠식된 폐성을 탐험하는 오픈월드 소울라이크. Unity의 전투 기록을 Linux API가 저장하고 이 화면을 요청마다 동적으로 생성합니다.</p>
  <div class="grid">
    <div class="metric"><span>SERVER STATUS</span><b class="live">● ONLINE</b></div>
    <div class="metric"><span>SERVER TIME</span><b>{html.escape(server_time)}</b></div>
    <div class="metric"><span>DYNAMIC VISITS</span><b>{visits:,}</b></div>
  </div>
  <section>
    <h2>원정 기록</h2>
    <table>
      <thead><tr><th>#</th><th>플레이어</th><th>레벨</th><th>지역</th><th>클리어</th></tr></thead>
      <tbody>{leaderboard_rows}</tbody>
    </table>
  </section>
  <footer><span>{html.escape(settings.student_identity)}</span><span>Nginx → FastAPI → SQLite</span></footer>
</main>
</body>
</html>"""


@app.get("/", response_class=HTMLResponse)
def home() -> HTMLResponse:
    server_time = now_kst()
    visits = increment_visits()
    return HTMLResponse(render_home(server_time, visits, top_entries(5)))


@app.get("/api/health")
def health() -> dict[str, object]:
    database = connect()
    try:
        database.execute("SELECT 1").fetchone()
    finally:
        database.close()
    return {"status": "ok", "service": settings.app_name, "serverTime": now_kst()}


@app.get("/api/page-session")
def page_session() -> dict[str, object]:
    return {
        "status": "online",
        "serverTime": now_kst(),
        "visits": increment_visits(),
        "studentIdentity": settings.student_identity,
    }


@app.get("/api/server-status")
def server_status() -> dict[str, object]:
    database = connect()
    try:
        visits = database.execute(
            "SELECT value FROM counters WHERE key = 'visits'"
        ).fetchone()
        rankings = database.execute("SELECT COUNT(*) AS count FROM leaderboard").fetchone()
        active_instances = database.execute(
            "SELECT COUNT(*) AS count FROM server_instances "
            "WHERE julianday(received_at) >= julianday('now', '-2 minutes')"
        ).fetchone()
    finally:
        database.close()
    return {
        "status": "online",
        "serverTime": now_kst(),
        "visits": int(visits["value"]) if visits else 0,
        "rankings": int(rankings["count"]),
        "unityServers": int(active_instances["count"]),
    }


@app.get("/api/leaderboard/top")
def leaderboard_top(
    limit: int = Query(default=10, ge=1, le=settings.max_leaderboard_limit),
) -> dict[str, object]:
    return {"entries": top_entries(limit), "generatedAt": now_kst()}


@app.post("/api/leaderboard/submit", status_code=201)
def leaderboard_submit(payload: LeaderboardSubmission, request: Request) -> dict[str, object]:
    client_ip = request.client.host if request.client else "unknown"
    try:
        with transaction() as database:
            cursor = database.execute(
                "INSERT INTO leaderboard(player_name, max_region, max_boss, level, clear_time_sec, client_timestamp) "
                "VALUES(?, ?, ?, ?, ?, ?)",
                (
                    payload.playerName,
                    payload.maxRegion,
                    payload.maxBoss,
                    payload.level,
                    payload.clearTimeSec,
                    payload.timestamp.isoformat(),
                ),
            )
            database.execute(
                "INSERT INTO world_events(event_type, actor, detail) VALUES(?, ?, ?)",
                ("boss_clear", payload.playerName, f"{payload.maxBoss} via {client_ip}"),
            )
            entry_id = int(cursor.lastrowid)
    except sqlite3.IntegrityError as exception:
        raise HTTPException(status_code=422, detail="invalid leaderboard entry") from exception
    return {"accepted": True, "entryId": entry_id}


@app.post("/api/events/heartbeat", status_code=202)
def server_heartbeat(payload: ServerHeartbeat) -> dict[str, bool]:
    with transaction() as database:
        database.execute(
            "INSERT INTO server_instances(instance_id, build_version, last_seen_at) VALUES(?, ?, ?) "
            "ON CONFLICT(instance_id) DO UPDATE SET build_version=excluded.build_version, "
            "last_seen_at=excluded.last_seen_at, received_at=strftime('%Y-%m-%dT%H:%M:%fZ', 'now')",
            (payload.instanceId, payload.buildVersion, payload.timestamp.isoformat()),
        )
    return {"accepted": True}


@app.get("/api/events/recent")
def recent_events(limit: int = Query(default=10, ge=1, le=50)) -> dict[str, object]:
    database = connect()
    try:
        rows = database.execute(
            "SELECT event_type, actor, detail, created_at FROM world_events "
            "ORDER BY id DESC LIMIT ?",
            (limit,),
        ).fetchall()
    finally:
        database.close()
    return {"events": [dict(row) for row in rows]}


@app.post("/api/save/upload")
def save_upload(payload: SaveUpload) -> dict[str, object]:
    try:
        checksum = payload.checksum()
    except ValueError as exception:
        raise HTTPException(status_code=413, detail=str(exception)) from exception
    serialized = json.dumps(payload.payload, ensure_ascii=False, sort_keys=True, separators=(",", ":"))
    with transaction() as database:
        database.execute(
            "INSERT INTO cloud_saves(player_key, payload, checksum) VALUES(?, ?, ?) "
            "ON CONFLICT(player_key) DO UPDATE SET payload=excluded.payload, checksum=excluded.checksum, "
            "updated_at=strftime('%Y-%m-%dT%H:%M:%fZ', 'now')",
            (payload.playerKey, serialized, checksum),
        )
    return {"saved": True, "checksum": checksum}


@app.get("/api/save/download/{player_key}")
def save_download(player_key: str) -> dict[str, object]:
    if not 3 <= len(player_key) <= 64 or not all(
        character.isalnum() or character in "_.:-" for character in player_key
    ):
        raise HTTPException(status_code=422, detail="invalid player key")
    database = connect()
    try:
        row = database.execute(
            "SELECT payload, checksum, updated_at FROM cloud_saves WHERE player_key = ?",
            (player_key,),
        ).fetchone()
    finally:
        database.close()
    if row is None:
        raise HTTPException(status_code=404, detail="save not found")
    return {
        "playerKey": player_key,
        "payload": json.loads(row["payload"]),
        "checksum": row["checksum"],
        "updatedAt": row["updated_at"],
    }
