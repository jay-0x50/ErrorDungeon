#!/usr/bin/env bash
set -Eeuo pipefail

DATABASE=/var/lib/error-dungeon/error-dungeon.sqlite3
BACKUP_ROOT=/var/backups/error-dungeon
PROJECT_ROOT=${ERROR_DUNGEON_PROJECT_ROOT:-/home/jay-0x50/ErrorDungeon}
STAMP=$(date +%Y%m%d-%H%M%S)
SNAPSHOT="${BACKUP_ROOT}/error-dungeon-${STAMP}.sqlite3"
PROJECT_ARCHIVE="${BACKUP_ROOT}/error-dungeon-project-${STAMP}.tar.gz"

install -d -m 0750 "${BACKUP_ROOT}"
if [[ ! -f "${DATABASE}" ]]; then
    echo "database not found: ${DATABASE}"
    exit 1
fi
sqlite3 "${DATABASE}" ".backup '${SNAPSHOT}'"
gzip --best "${SNAPSHOT}"
if [[ -d "${PROJECT_ROOT}/server" && -d "${PROJECT_ROOT}/deploy" ]]; then
    tar --create --gzip --file "${PROJECT_ARCHIVE}" --directory "${PROJECT_ROOT}" \
        server deploy Assets/ErrorDungeon Assets/WebGLTemplates ProjectSettings Packages 기획서
else
    echo "project not found: ${PROJECT_ROOT}" >&2
    exit 1
fi
find "${BACKUP_ROOT}" -maxdepth 1 -type f -name 'error-dungeon-*.sqlite3.gz' -mtime +14 -delete
find "${BACKUP_ROOT}" -maxdepth 1 -type f -name 'error-dungeon-project-*.tar.gz' -mtime +14 -delete
echo "${SNAPSHOT}.gz"
echo "${PROJECT_ARCHIVE}"
