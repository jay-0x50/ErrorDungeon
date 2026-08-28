#!/usr/bin/env bash
set -Eeuo pipefail

if [[ ${EUID} -ne 0 ]]; then
    echo "sudo ./deploy/scripts/enable-https.sh"
    exit 1
fi

SCRIPT_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
CERT_DIR=/etc/ssl/error-dungeon

apt-get update
DEBIAN_FRONTEND=noninteractive apt-get install -y openssl
install -d -o root -g root -m 0700 "${CERT_DIR}"
openssl req -x509 -newkey rsa:3072 -sha256 -days 365 -nodes \
    -keyout "${CERT_DIR}/error-dungeon.key" \
    -out "${CERT_DIR}/error-dungeon.crt" \
    -subj '/C=KR/ST=Seoul/O=Error Dungeon/CN=error-dungeon.local' \
    -addext 'subjectAltName=DNS:error-dungeon.local'
chmod 0600 "${CERT_DIR}/error-dungeon.key"
install -o root -g root -m 0644 "${SCRIPT_DIR}/../nginx/error-dungeon-https.conf" /etc/nginx/sites-available/error-dungeon
ufw delete allow 'Nginx HTTP' || true
ufw allow 'Nginx Full'
nginx -t
systemctl reload nginx
curl --insecure --fail --silent --show-error https://error-dungeon.local/api/health
echo

