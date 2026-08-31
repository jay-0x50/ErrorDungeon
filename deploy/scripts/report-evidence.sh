#!/usr/bin/env bash
set -Eeuo pipefail

PROJECT_ROOT=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)
STAGE=${1:-all}

heading() {
    echo
    echo "========== $1 =========="
    date
    whoami
}

stage_1() {
    heading '캡처 1-1 · Ubuntu 환경'
    lsb_release -a
    hostname -I
    heading '캡처 1-2 · 설치 버전'
    nginx -v
    python3 --version
}

stage_2() {
    heading '캡처 2-1 · 앱 전용 계정'
    id errordungeon
    getent passwd errordungeon
    heading '캡처 2-2 · 관리자 권한 없음'
    groups errordungeon
    if getent group sudo | grep -qw errordungeon; then
        echo '주의: errordungeon 계정이 sudo 그룹에 포함되어 있습니다.'
    else
        echo '확인: errordungeon 계정은 sudo 그룹에 포함되어 있지 않습니다.'
    fi
    heading '캡처 2-3 · 폴더 권한과 소유자'
    namei -l /opt/error-dungeon-api
    ls -ld /opt/error-dungeon-api /var/lib/error-dungeon /var/www/error-dungeon-webgl
}

stage_3() {
    heading '캡처 3-1 · UFW 방화벽'
    sudo ufw status verbose
    heading '캡처 3-2 · 공개 포트와 내부 앱 포트'
    sudo ss -lntp | awk 'NR == 1 || /:22 |:80 |:443 |127\.0\.0\.1:8080/'
}

stage_5_code() {
    heading '캡처 5-1(a) · 동적 페이지 코드'
    sed -n '1,175p' "${PROJECT_ROOT}/server/app/main.py"
    heading '캡처 5-1(b) · 저장·입력 처리 코드'
    sed -n '176,280p' "${PROJECT_ROOT}/server/app/main.py"
}

stage_5_curl() {
    heading '캡처 5-2 · 앱이 직접 응답'
    curl --fail --silent --show-error http://127.0.0.1:8080/api/page-session
    echo
    heading '캡처 5-3 · 시간과 방문 수가 달라지는 동적 증명'
    curl --fail --silent --show-error http://127.0.0.1:8080/api/page-session
    echo
    sleep 3
    curl --fail --silent --show-error http://127.0.0.1:8080/api/page-session
    echo
}

stage_5_tree() {
    heading '캡처 S-3 · 프로젝트 핵심 폴더 구조'
    find "${PROJECT_ROOT}/server" "${PROJECT_ROOT}/deploy" -maxdepth 3 \
        -not -path '*/__pycache__*' -not -path '*/.pytest_cache*' -printf '%y %p\n' | sort
}

stage_6() {
    heading '캡처 6-1 · systemd 유닛 파일'
    cat /etc/systemd/system/error-dungeon-api.service
    heading '캡처 6-2 · active와 enabled 상태'
    systemctl --no-pager --full status error-dungeon-api.service
    systemctl is-active error-dungeon-api.service
    systemctl is-enabled error-dungeon-api.service
}

stage_6_reboot() {
    heading '캡처 6-3 · 재부팅 후 자동 실행 증명'
    uptime
    systemctl is-active error-dungeon-api.service
    systemctl is-enabled error-dungeon-api.service
    systemctl is-active nginx.service
    systemctl is-enabled nginx.service
}

stage_7() {
    heading '캡처 7-1 · Nginx 서버 블록'
    cat /etc/nginx/sites-available/error-dungeon
    heading '캡처 7-2 · 심볼릭 링크와 설정 검사'
    ls -l /etc/nginx/sites-enabled/error-dungeon
    sudo nginx -t
    heading '캡처 7-4 · 도메인 200 응답'
    curl -I http://error-dungeon.local
}

stage_9() {
    heading '캡처 9-1 · 최근 API 재시작 로그'
    journalctl -u error-dungeon-api.service --since 'today' --no-pager -n 35
    heading '캡처 9-2 · 수정 후 전체 상태'
    "${PROJECT_ROOT}/deploy/scripts/health-check.sh"
}

stage_bonus() {
    heading '가산점 · 자동 배포와 상태 점검 스크립트'
    ls -l "${PROJECT_ROOT}/deploy/scripts/deploy.sh" \
        "${PROJECT_ROOT}/deploy/scripts/install-webgl.sh" \
        "${PROJECT_ROOT}/deploy/scripts/health-check.sh"
    sed -n '1,120p' "${PROJECT_ROOT}/deploy/scripts/health-check.sh"
    heading '가산점 · 입력 저장 API와 SQLite'
    curl --fail --silent --show-error -X POST http://error-dungeon.local/api/save/upload \
        -H 'Content-Type: application/json' \
        -d '{"playerKey":"report-demo","payload":{"student":"박재영","stage":"final-report"}}'
    echo
    curl --fail --silent --show-error http://error-dungeon.local/api/save/download/report-demo
    echo
    curl --fail --silent --show-error http://error-dungeon.local/api/server-status
    echo
    sudo sqlite3 /var/lib/error-dungeon/error-dungeon.sqlite3 '.tables'
    heading '가산점 · cron 자동 백업'
    if [[ -f /etc/cron.d/error-dungeon-backup ]]; then
        cat /etc/cron.d/error-dungeon-backup
        sudo ls -lh /var/backups/error-dungeon
    else
        echo '아직 미설치: sudo ./deploy/scripts/install-backup-cron.sh'
    fi
}

case "${STAGE}" in
    1) stage_1 ;;
    2) stage_2 ;;
    3) stage_3 ;;
    5-code) stage_5_code ;;
    5-curl) stage_5_curl ;;
    5-tree) stage_5_tree ;;
    6) stage_6 ;;
    6-reboot) stage_6_reboot ;;
    7) stage_7 ;;
    9) stage_9 ;;
    bonus) stage_bonus ;;
    all)
        stage_1
        stage_2
        stage_3
        stage_5_curl
        stage_5_tree
        stage_6
        stage_7
        stage_9
        stage_bonus
        ;;
    *)
        echo '사용법: ./deploy/scripts/report-evidence.sh {1|2|3|5-code|5-curl|5-tree|6|6-reboot|7|9|bonus|all}'
        exit 2
        ;;
esac
