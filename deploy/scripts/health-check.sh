#!/usr/bin/env bash
set -u

date
whoami
lsb_release -ds 2>/dev/null || true
uptime
echo '[disk]'
df -h / /var/lib/error-dungeon 2>/dev/null || df -h /
echo '[memory]'
free -h
echo '[services]'
systemctl is-enabled error-dungeon-api.service 2>/dev/null || true
systemctl is-active error-dungeon-api.service 2>/dev/null || true
systemctl is-enabled nginx 2>/dev/null || true
systemctl is-active nginx 2>/dev/null || true
if systemctl list-unit-files error-dungeon-game.service >/dev/null 2>&1; then
    systemctl is-active error-dungeon-game.service 2>/dev/null || true
fi
echo '[ports]'
ss -lntp | awk 'NR == 1 || /:80 |:443 |127\.0\.0\.1:8080/'
echo '[firewall]'
ufw status 2>/dev/null || true
echo '[http]'
curl --fail --silent --show-error http://127.0.0.1:8080/api/health
echo
curl --fail --silent --show-error --resolve error-dungeon.local:80:127.0.0.1 http://error-dungeon.local/api/server-status
echo

