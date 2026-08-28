from __future__ import annotations

import importlib
import os

import pytest
from fastapi.testclient import TestClient


@pytest.fixture()
def client(tmp_path, monkeypatch):
    database_path = tmp_path / "test.sqlite3"
    monkeypatch.setenv("ERROR_DUNGEON_DB", str(database_path))

    import app.config
    import app.database
    import app.main

    importlib.reload(app.config)
    importlib.reload(app.database)
    importlib.reload(app.main)
    with TestClient(app.main.app) as test_client:
        yield test_client

