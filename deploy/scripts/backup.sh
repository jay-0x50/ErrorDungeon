#!/usr/bin/env bash
set -Eeuo pipefail

DATABASE=/var/lib/error-dungeon/error-dungeon.sqlite3
BACKUP_ROOT=/var/backups/error-dungeon
STAMP=$(date +%Y%m%d-%H%M%S)
SNAPSHOT="${BACKUP_ROOT}/error-dungeon-${STAMP}.sqlite3"

install -d -m 0750 "${BACKUP_ROOT}"
if [[ ! -f "${DATABASE}" ]]; then
    echo "database not found: ${DATABASE}"
    exit 1
fi
sqlite3 "${DATABASE}" ".backup '${SNAPSHOT}'"
gzip --best "${SNAPSHOT}"
find "${BACKUP_ROOT}" -maxdepth 1 -type f -name 'error-dungeon-*.sqlite3.gz' -mtime +14 -delete
echo "${SNAPSHOT}.gz"

