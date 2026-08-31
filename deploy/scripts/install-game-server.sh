#!/usr/bin/env bash
set -Eeuo pipefail

if [[ ${EUID} -ne 0 ]]; then
    echo "sudo ./deploy/scripts/install-game-server.sh [Unity LinuxServer build directory]"
    exit 1
fi

SCRIPT_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
PROJECT_ROOT=$(cd -- "${SCRIPT_DIR}/../.." && pwd)
SOURCE_DIR=${1:-"${PROJECT_ROOT}/Builds/LinuxServer"}
TARGET_DIR=/opt/error-dungeon-game

if [[ ! -x "${SOURCE_DIR}/ErrorDungeonServer.x86_64" ]]; then
    echo "missing executable: ${SOURCE_DIR}/ErrorDungeonServer.x86_64"
    exit 1
fi

install -d -o errordungeon -g errordungeon -m 0750 "${TARGET_DIR}"
rsync -a --delete "${SOURCE_DIR}/" "${TARGET_DIR}/"
chown -R errordungeon:errordungeon "${TARGET_DIR}"
chmod 0750 "${TARGET_DIR}/ErrorDungeonServer.x86_64"
install -o root -g root -m 0644 "${PROJECT_ROOT}/deploy/systemd/error-dungeon-game.service" /etc/systemd/system/error-dungeon-game.service
systemctl daemon-reload
systemctl enable error-dungeon-game.service
systemctl restart error-dungeon-game.service
systemctl --no-pager --full status error-dungeon-game.service
