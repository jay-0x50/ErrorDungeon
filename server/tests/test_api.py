from __future__ import annotations

from datetime import datetime, timezone


def submission(name: str = "박재영") -> dict[str, object]:
    return {
        "playerName": name,
        "maxRegion": 1,
        "maxBoss": "boss_doyun",
        "level": 12,
        "clearTimeSec": 945,
        "timestamp": datetime.now(timezone.utc).isoformat(),
    }


def test_home_is_dynamic_and_identifies_student(client):
    first = client.get("/")
    second = client.get("/")
    assert first.status_code == 200
    assert "3학년 4반 4번 박재영" in first.text
    assert "DYNAMIC VISITS" in first.text
    assert first.text != second.text


def test_webgl_page_session_is_dynamic(client):
    first = client.get("/api/page-session").json()
    second = client.get("/api/page-session").json()
    assert first["studentIdentity"] == "3학년 4반 4번 박재영"
    assert second["visits"] == first["visits"] + 1
    assert second["status"] == "online"


def test_leaderboard_submission_and_order(client):
    slower = submission("느린 원정자")
    slower["clearTimeSec"] = 1200
    faster = submission("빠른 원정자")
    faster["clearTimeSec"] = 800
    assert client.post("/api/leaderboard/submit", json=slower).status_code == 201
    assert client.post("/api/leaderboard/submit", json=faster).status_code == 201
    entries = client.get("/api/leaderboard/top?limit=2").json()["entries"]
    assert [entry["player_name"] for entry in entries] == ["빠른 원정자", "느린 원정자"]


def test_rejects_impossible_leaderboard_value(client):
    invalid = submission()
    invalid["level"] = 10000
    assert client.post("/api/leaderboard/submit", json=invalid).status_code == 422


def test_cloud_save_round_trip(client):
    payload = {"playerKey": "player-004", "payload": {"level": 7, "souls": 1340}}
    upload = client.post("/api/save/upload", json=payload)
    assert upload.status_code == 200
    download = client.get("/api/save/download/player-004")
    assert download.status_code == 200
    assert download.json()["payload"] == payload["payload"]


def test_unity_server_heartbeat(client):
    response = client.post(
        "/api/events/heartbeat",
        json={
            "instanceId": "unity-linux-01",
            "buildVersion": "0.1.0",
            "timestamp": datetime.now(timezone.utc).isoformat(),
        },
    )
    assert response.status_code == 202
    assert client.get("/api/server-status").json()["unityServers"] == 1
