#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
PROJECT_ROOT=$(cd -- "${SCRIPT_DIR}/../.." && pwd)
VENV_ROOT="${PROJECT_ROOT}/.venv"

export ERROR_DUNGEON_DB="${PROJECT_ROOT}/server/error-dungeon.sqlite3"
cd "${PROJECT_ROOT}/server"
if [[ -x "${VENV_ROOT}/bin/pip" ]]; then
    "${VENV_ROOT}/bin/pip" install --requirement requirements.txt
    exec "${VENV_ROOT}/bin/uvicorn" app.main:app --host 127.0.0.1 --port 8080 --reload
fi
if python3 -m ensurepip --version >/dev/null 2>&1 && python3 -m venv "${VENV_ROOT}" && [[ -x "${VENV_ROOT}/bin/pip" ]]; then
    "${VENV_ROOT}/bin/pip" install --requirement requirements.txt
    exec "${VENV_ROOT}/bin/uvicorn" app.main:app --host 127.0.0.1 --port 8080 --reload
fi
if ! python3 -c 'import fastapi, uvicorn' 2>/dev/null; then
    python3 -m pip install --user --break-system-packages --requirement requirements.txt
fi
exec python3 -m uvicorn app.main:app --host 127.0.0.1 --port 8080 --reload
