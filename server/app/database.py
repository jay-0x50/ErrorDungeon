from __future__ import annotations

import sqlite3
from collections.abc import Iterator
from contextlib import contextmanager

from .config import settings


SCHEMA = """
PRAGMA journal_mode = WAL;
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS counters (
    key TEXT PRIMARY KEY,
    value INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS leaderboard (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    player_name TEXT NOT NULL CHECK(length(player_name) BETWEEN 1 AND 24),
    max_region INTEGER NOT NULL CHECK(max_region BETWEEN 0 AND 99),
    max_boss TEXT NOT NULL CHECK(length(max_boss) BETWEEN 1 AND 64),
    level INTEGER NOT NULL CHECK(level BETWEEN 1 AND 999),
    clear_time_sec INTEGER NOT NULL CHECK(clear_time_sec BETWEEN 1 AND 31536000),
    client_timestamp TEXT NOT NULL,
    submitted_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now'))
);

CREATE INDEX IF NOT EXISTS idx_leaderboard_rank
ON leaderboard(max_region DESC, level DESC, clear_time_sec ASC, submitted_at ASC);

CREATE TABLE IF NOT EXISTS world_events (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    event_type TEXT NOT NULL,
    actor TEXT NOT NULL,
    detail TEXT NOT NULL,
    created_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now'))
);

CREATE TABLE IF NOT EXISTS server_instances (
    instance_id TEXT PRIMARY KEY,
    build_version TEXT NOT NULL,
    last_seen_at TEXT NOT NULL,
    received_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now'))
);

CREATE TABLE IF NOT EXISTS cloud_saves (
    player_key TEXT PRIMARY KEY CHECK(length(player_key) BETWEEN 3 AND 64),
    payload TEXT NOT NULL,
    checksum TEXT NOT NULL,
    updated_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now'))
);
"""


def connect() -> sqlite3.Connection:
    connection = sqlite3.connect(settings.database_path, timeout=10)
    connection.row_factory = sqlite3.Row
    connection.execute("PRAGMA foreign_keys = ON")
    connection.execute("PRAGMA busy_timeout = 5000")
    return connection


@contextmanager
def transaction() -> Iterator[sqlite3.Connection]:
    connection = connect()
    try:
        connection.execute("BEGIN IMMEDIATE")
        yield connection
        connection.commit()
    except Exception:
        connection.rollback()
        raise
    finally:
        connection.close()


def initialize_database() -> None:
    settings.database_path.parent.mkdir(parents=True, exist_ok=True)
    connection = connect()
    try:
        connection.executescript(SCHEMA)
        connection.commit()
    finally:
        connection.close()

