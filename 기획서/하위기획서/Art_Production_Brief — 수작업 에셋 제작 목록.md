# Art Production Brief — 수작업 에셋 제작 목록

## 현재 구현의 역할

현재 게임에는 아래 항목이 코드 기반 3D 아트로 구현되어 있다.

- 320×320 규모 탐험 월드와 5개 지역 실루엣
- 절차형 지형/암석/고목/기둥/폐가/성벽/보스룸
- 플레이어, 일반병, 정예병, 도윤의 다중 파츠 실루엣
- 오류 침식 셰이더, 절차형 재질, 안개/재 파티클, 조명
- 세 기억 수집 → 봉인문 개방 → 도윤 전투의 스토리 동선

이 상태는 **플레이 가능한 아트 디렉션 버티컬 슬라이스**다. 출시 수준의 캐릭터 표면 디테일과 애니메이션은 아래 원본 에셋으로 교체해야 한다. 게임플레이 루트와 콜라이더는 이미 분리되어 있으므로, 새 모델은 자식 Visual만 교체하면 된다.

---

## P0 — 반드시 사람이 제작하거나 구매해야 하는 에셋

### 1. 무너진 장군, 도윤

- 파일: `Assets/ErrorDungeon/Art/Characters/Doyun/CHR_Doyun.fbx`
- 권장 예산: 35k~60k triangles, LOD1 20k, LOD2 8k
- 텍스처: 2K 또는 4K, BaseColor / Normal / Mask(Metallic·AO·Smoothness) / Emission
- 머티리얼 슬롯: `Armor`, `Cloth`, `ErrorCracks` 최대 3개
- 필수 실루엣: 중갑, 비대칭 견갑, 부러진 지휘관 장식, 1.3배 체고의 장창
- 오류 균열: 별도 메시 또는 마스크로 제작. `_ErrorIntensity` 하나로 P1→P2 전환 가능해야 함
- 리그: Unity Humanoid 호환. 창은 오른손 본 또는 별도 weapon socket에 부착
- 필수 애니메이션:
  - Idle, Walk, Run, TurnL/R
  - Combo_01(2타), Combo_02(5타)
  - Sweep, JumpSlam, LightningCharge
  - Hit, Knockdown, Phase2, Death
- 텔 규칙: 공격 판정 전 자세가 서로 확실히 달라야 하며, 창끝 Emission이 카메라 거리 15m에서도 보여야 함

### 2. 무명자 플레이어

- 파일: `Assets/ErrorDungeon/Art/Characters/Player/CHR_Nameless.fbx`
- 권장 예산: 25k~45k triangles, LOD1 15k, LOD2 6k
- 텍스처: 2K 1세트, 갑옷/천 마스크 분리
- 리그: Unity Humanoid, Root Motion은 끔
- 필수 애니메이션:
  - Idle, Walk/Run 8방향, Dodge 4방향
  - LightAttack, HeavyAttack, Guard, Parry, Drink, Hit, Death
- 디자인 불변 조건: 얼굴보다 후드/망토 실루엣이 먼저 읽히고, 호박색 허리띠만 안전 색으로 사용

### 3. 모듈러 폐성 키트

- 폴더: `Assets/ErrorDungeon/Art/Environment/RuinKit/`
- 단위: 1 Unity unit = 1m, 피벗은 바닥 중앙 또는 스냅 모서리
- 필수 모듈:
  - 벽 2m / 4m / 8m
  - 안쪽/바깥쪽 모서리
  - 성문 아치, 문짝, 계단, 기둥, 난간
  - 온전/파손 변형 각 2종
  - 잔해 더미 5종, 개별 돌 6종
  - 고목 4종, 부러진 표지석 4종
- 텍스처: 2K trim sheet 1장 + tiling stone 1장 + vertex color moss/rust mask
- 성능: 모듈 하나 500~5k triangles, 콜라이더는 Box 조합 또는 별도 `_COL` 저해상도 메시

### 4. 체크포인트 히어로 에셋

- 파일: `Assets/ErrorDungeon/Art/Props/Checkpoint/PRP_EmberSword.fbx`
- 권장 예산: 8k~15k triangles, 2K 텍스처
- 구성: 재 속에 꽂힌 검, 돌 제단, 불씨 메시를 분리
- 불씨는 Emission 마스크와 별도 VFX 부착점을 제공
- 게임에서 가장 자주 보는 오브젝트이므로 보스 다음 우선순위

---

## P1 — 품질을 크게 올리는 후속 에셋

- 일반병 2종과 정예병 1종: 공통 Humanoid 리그, 무기만 교체 가능하게 제작
- 지면 재질 3종: 젖은 흙, 죽은 이끼, 부서진 순례길. 2K tileable PBR
- 지역별 히어로 에셋:
  - 회백숲: 맹세의 제단
  - 침몰기록소: 기울어진 기록탑
  - 부서진 마을: 울리지 못한 종루
  - 폐성: 세 기억의 봉인문
- 오디오: 바람/재, 오류 전기음, 체크포인트 불씨, 종루 저주파 공명

---

## Unity 전달 규칙

1. FBX 축은 Y-Up, Z-Forward, scale 1.0으로 내보낸다.
2. 모델 파일 안에 머티리얼을 내장하지 말고 `.mat`을 Unity에서 별도로 만든다.
3. 텍스처 이름은 `T_<Asset>_BaseColor`, `_Normal`, `_Mask`, `_Emission`으로 통일한다.
4. 시각 메시에는 공격/이동 콜라이더를 넣지 않는다. 기존 캐릭터 루트의 `CharacterController`와 전투 스크립트를 유지한다.
5. 교체 대상은 런타임 계층의 다음 자식이다.
   - 플레이어: `The nameless wanderer - layered visual`
   - 일반 적: `Hollow soldier visual` / `Castle executioner visual`
   - 보스: `Doyun - fractured general armor`
6. 애니메이션 이벤트는 피해를 직접 주지 않는다. 실제 판정은 기존 AI/전투 코루틴이 담당하고, 이벤트는 VFX·사운드에만 사용한다.

---

## 완료 판정

- 1080p에서 플레이어/잡몹/보스가 배경과 즉시 구분된다.
- 도윤의 4개 주요 패턴을 애니메이션 첫 0.3초 안에 구별할 수 있다.
- 오류 색은 감염·위험·서사 오브젝트에만 사용되고 일반 장식에는 쓰지 않는다.
- 한 화면에 서로 다른 에셋 출처가 보여도 돌·금속·천의 명도와 거칠기가 같은 세계로 읽힌다.
- Linux Client와 WebGL 양쪽에서 핑크 머티리얼과 누락 애니메이션이 없다.
