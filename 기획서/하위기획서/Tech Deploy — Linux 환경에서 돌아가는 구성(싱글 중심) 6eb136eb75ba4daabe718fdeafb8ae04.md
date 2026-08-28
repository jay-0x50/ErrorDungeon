# Tech/Deploy — Linux 환경에서 돌아가는 구성(싱글 중심)

## 0. 목표

- 게임은 ‘싱글 플레이 클라이언트’가 본체
- 리눅스 배포 환경은 ‘선택 서버(랭킹/세이브)’를 의미

---

## 1. 아키텍처(권장)

### 1.1 싱글 오프라인(기본)

- 클라이언트
    - 전투/AI/월드 상태: 로컬
    - 세이브: 로컬 파일

### 1.2 온라인 기능(선택)

- Linux 서버(API)
    - 랭킹 등록/조회
    - (선택) 세이브 동기화
    - (선택) 버전 체크/패치 노트

---

## 2. Linux 서버 스택(예시)

- OS: Ubuntu 24.04 LTS
- Reverse Proxy: Nginx
- App: FastAPI 또는 Go HTTP 또는 Node(Express/Fastify)
- DB: PostgreSQL(권장) / SQLite(단일 서버)
- Process: systemd
- Firewall: ufw

---

## 3. API(초안)

### 3.1 Leaderboard

- POST /api/leaderboard/submit
    - body: { playerName, maxRegion, maxBoss, level, clearTimeSec, timestamp }
- GET /api/leaderboard/top?limit=50

### 3.2 Save Sync(선택)

- POST /api/save/upload
- GET /api/save/download

---

## 4. systemd 유닛(예시)

```
[Unit]
Description=error-dungeon api
After=network.target

[Service]
User=gamed
WorkingDirectory=/opt/error-dungeon-api
ExecStart=/opt/error-dungeon-api/run.sh
Restart=always
RestartSec=2

[Install]
WantedBy=multi-user.target
```

---

## 5. Nginx(예시)

```
server {
  listen 80;
  server_name error-dungeon.local;

  location /api/ {
    proxy_pass http://127.0.0.1:8080/;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
  }
}
```

---

## 6. UFW(예시)

- allow 22
- allow 80 (또는 443)
- deny 기타

---

## 7. 저장/보안

- 최소: HTTPS는 선택(가능하면)
- 입력 검증/레이트리밋(선택)
- 랭킹은 서버에서 값 검증(상식적인 범위 체크)