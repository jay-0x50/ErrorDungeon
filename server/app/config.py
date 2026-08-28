from __future__ import annotations

import os
from dataclasses import dataclass
from pathlib import Path


@dataclass(frozen=True, slots=True)
class Settings:
    app_name: str
    student_identity: str
    database_path: Path
    max_leaderboard_limit: int
    trusted_proxy: str

    @classmethod
    def from_environment(cls) -> "Settings":
        database_path = Path(
            os.environ.get("ERROR_DUNGEON_DB", "/var/lib/error-dungeon/error-dungeon.sqlite3")
        ).expanduser()
        return cls(
            app_name="Error Dungeon",
            student_identity=os.environ.get(
                "ERROR_DUNGEON_STUDENT", "3학년 4반 4번 박재영"
            ),
            database_path=database_path,
            max_leaderboard_limit=50,
            trusted_proxy=os.environ.get("ERROR_DUNGEON_TRUSTED_PROXY", "127.0.0.1"),
        )


settings = Settings.from_environment()

