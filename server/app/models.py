from __future__ import annotations

import re
from datetime import datetime, timezone
from hashlib import sha256
from typing import Any

from pydantic import BaseModel, Field, field_validator


SAFE_ID = re.compile(r"^[A-Za-z0-9_.:-]+$")


class LeaderboardSubmission(BaseModel):
    playerName: str = Field(min_length=1, max_length=24)
    maxRegion: int = Field(ge=0, le=99)
    maxBoss: str = Field(min_length=1, max_length=64)
    level: int = Field(ge=1, le=999)
    clearTimeSec: int = Field(ge=1, le=31_536_000)
    timestamp: datetime

    @field_validator("playerName")
    @classmethod
    def normalize_player_name(cls, value: str) -> str:
        value = " ".join(value.strip().split())
        if not value or any(ord(character) < 32 for character in value):
            raise ValueError("invalid player name")
        return value

    @field_validator("maxBoss")
    @classmethod
    def validate_boss_id(cls, value: str) -> str:
        if not SAFE_ID.fullmatch(value):
            raise ValueError("invalid boss id")
        return value

    @field_validator("timestamp")
    @classmethod
    def validate_timestamp(cls, value: datetime) -> datetime:
        if value.tzinfo is None:
            value = value.replace(tzinfo=timezone.utc)
        now = datetime.now(timezone.utc)
        if abs((value.astimezone(timezone.utc) - now).total_seconds()) > 31_536_000:
            raise ValueError("timestamp is outside the accepted range")
        return value.astimezone(timezone.utc)


class ServerHeartbeat(BaseModel):
    instanceId: str = Field(min_length=1, max_length=128)
    buildVersion: str = Field(min_length=1, max_length=32)
    timestamp: datetime

    @field_validator("instanceId")
    @classmethod
    def validate_instance_id(cls, value: str) -> str:
        if not SAFE_ID.fullmatch(value):
            raise ValueError("invalid instance id")
        return value


class SaveUpload(BaseModel):
    playerKey: str = Field(min_length=3, max_length=64)
    payload: dict[str, Any]

    @field_validator("playerKey")
    @classmethod
    def validate_player_key(cls, value: str) -> str:
        if not SAFE_ID.fullmatch(value):
            raise ValueError("invalid player key")
        return value

    def checksum(self) -> str:
        import json

        encoded = json.dumps(
            self.payload, ensure_ascii=False, sort_keys=True, separators=(",", ":")
        ).encode("utf-8")
        if len(encoded) > 100_000:
            raise ValueError("save payload is too large")
        return sha256(encoded).hexdigest()

