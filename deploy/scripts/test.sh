#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
PROJECT_ROOT=$(cd -- "${SCRIPT_DIR}/../.." && pwd)
VENV_ROOT="${PROJECT_ROOT}/.venv"

cd "${PROJECT_ROOT}/server"
if [[ -x "${VENV_ROOT}/bin/pip" ]]; then
    "${VENV_ROOT}/bin/pip" install --requirement requirements-dev.txt
    exec "${VENV_ROOT}/bin/python" -m pytest -q
fi
if python3 -m ensurepip --version >/dev/null 2>&1 && python3 -m venv "${VENV_ROOT}" && [[ -x "${VENV_ROOT}/bin/pip" ]]; then
    "${VENV_ROOT}/bin/pip" install --requirement requirements-dev.txt
    exec "${VENV_ROOT}/bin/python" -m pytest -q
fi
if ! python3 -c 'import fastapi, httpx, pytest, uvicorn' 2>/dev/null; then
    python3 -m pip install --user --break-system-packages --requirement requirements-dev.txt
fi
exec python3 -m pytest -q
