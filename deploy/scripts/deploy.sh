#!/usr/bin/env bash
set -Eeuo pipefail

if [[ ${EUID} -ne 0 ]]; then
    echo "sudo ./deploy/scripts/deploy.sh"
    exit 1
fi

SCRIPT_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
PROJECT_ROOT=$(cd -- "${SCRIPT_DIR}/../.." && pwd)
APP_ROOT=/opt/error-dungeon-api
DATA_ROOT=/var/lib/error-dungeon
SERVICE_USER=errordungeon

apt-get update
DEBIAN_FRONTEND=noninteractive apt-get install -y python3 python3-venv nginx ufw sqlite3 rsync curl

if ! id "${SERVICE_USER}" >/dev/null 2>&1; then
    useradd --system --home-dir "${APP_ROOT}" --shell /usr/sbin/nologin --user-group "${SERVICE_USER}"
fi

install -d -o root -g "${SERVICE_USER}" -m 0750 "${APP_ROOT}"
install -d -o "${SERVICE_USER}" -g "${SERVICE_USER}" -m 0750 "${DATA_ROOT}"
rsync -a --delete --exclude '__pycache__' "${PROJECT_ROOT}/server/app/" "${APP_ROOT}/app/"
install -o root -g "${SERVICE_USER}" -m 0640 "${PROJECT_ROOT}/server/requirements.txt" "${APP_ROOT}/requirements.txt"

python3 -m venv "${APP_ROOT}/.venv"
"${APP_ROOT}/.venv/bin/pip" install --upgrade pip
"${APP_ROOT}/.venv/bin/pip" install --requirement "${APP_ROOT}/requirements.txt"
chown -R root:"${SERVICE_USER}" "${APP_ROOT}"
chmod -R o-rwx "${APP_ROOT}"

install -o root -g root -m 0644 "${PROJECT_ROOT}/deploy/systemd/error-dungeon-api.service" /etc/systemd/system/error-dungeon-api.service
install -o root -g root -m 0644 "${PROJECT_ROOT}/deploy/systemd/error-dungeon-game.service" /etc/systemd/system/error-dungeon-game.service
install -o root -g root -m 0644 "${PROJECT_ROOT}/deploy/nginx/error-dungeon.conf" /etc/nginx/sites-available/error-dungeon
ln -sfn /etc/nginx/sites-available/error-dungeon /etc/nginx/sites-enabled/error-dungeon
if [[ -L /etc/nginx/sites-enabled/default || -f /etc/nginx/sites-enabled/default ]]; then
    unlink /etc/nginx/sites-enabled/default
fi

if ! grep -Eq '(^|[[:space:]])error-dungeon\.local([[:space:]]|$)' /etc/hosts; then
    echo '127.0.0.1 error-dungeon.local' >> /etc/hosts
fi

ufw default deny incoming
ufw default allow outgoing
ufw allow OpenSSH
ufw allow 'Nginx HTTP'
ufw --force enable

nginx -t
systemctl daemon-reload
systemctl enable --now error-dungeon-api.service
systemctl restart nginx

date
systemctl --no-pager --full status error-dungeon-api.service
curl --fail --silent --show-error http://error-dungeon.local/api/health
echo

