#!/usr/bin/env bash
set -Eeuo pipefail

if [[ ${EUID} -ne 0 ]]; then
    echo "sudo ./deploy/scripts/install-backup-cron.sh"
    exit 1
fi

SCRIPT_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
install -o root -g root -m 0750 "${SCRIPT_DIR}/backup.sh" /usr/local/sbin/error-dungeon-backup
install -o root -g root -m 0644 "${SCRIPT_DIR}/../cron/error-dungeon-backup" /etc/cron.d/error-dungeon-backup
systemctl restart cron
date
cat /etc/cron.d/error-dungeon-backup

