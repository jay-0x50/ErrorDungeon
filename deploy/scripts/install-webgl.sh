#!/usr/bin/env bash
set -Eeuo pipefail

if [[ ${EUID} -ne 0 ]]; then
    echo "sudo ./deploy/scripts/install-webgl.sh [Unity WebGL build directory]"
    exit 1
fi

SCRIPT_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
PROJECT_ROOT=$(cd -- "${SCRIPT_DIR}/../.." && pwd)
SOURCE_DIR=${1:-"${PROJECT_ROOT}/Builds/WebGL"}
TARGET_DIR=/var/www/error-dungeon-webgl
APP_ROOT=/opt/error-dungeon-api
SERVICE_USER=errordungeon

if [[ ! -f "${SOURCE_DIR}/index.html" || ! -d "${SOURCE_DIR}/Build" ]]; then
    echo "missing WebGL build: ${SOURCE_DIR}"
    exit 1
fi

install -d -o root -g www-data -m 0755 "${TARGET_DIR}"
rsync -a --delete "${SOURCE_DIR}/" "${TARGET_DIR}/"
chown -R root:www-data "${TARGET_DIR}"
find "${TARGET_DIR}" -type d -exec chmod 0755 {} +
find "${TARGET_DIR}" -type f -exec chmod 0644 {} +

if ! id "${SERVICE_USER}" >/dev/null 2>&1; then
    echo "missing service account: run sudo ./deploy/scripts/deploy.sh first"
    exit 1
fi
rsync -a --delete --exclude '__pycache__' "${PROJECT_ROOT}/server/app/" "${APP_ROOT}/app/"
chown -R root:"${SERVICE_USER}" "${APP_ROOT}"
chmod -R o-rwx "${APP_ROOT}"

install -o root -g root -m 0644 "${PROJECT_ROOT}/deploy/nginx/error-dungeon.conf" /etc/nginx/sites-available/error-dungeon
ln -sfn /etc/nginx/sites-available/error-dungeon /etc/nginx/sites-enabled/error-dungeon
nginx -t
systemctl restart error-dungeon-api.service
systemctl reload nginx

curl --fail --silent --show-error http://127.0.0.1/ | grep -q "unity-canvas"
curl --fail --silent --show-error --retry 20 --retry-delay 1 --retry-connrefused --retry-max-time 30 http://127.0.0.1/api/page-session
echo
systemctl --no-pager --full status nginx.service
