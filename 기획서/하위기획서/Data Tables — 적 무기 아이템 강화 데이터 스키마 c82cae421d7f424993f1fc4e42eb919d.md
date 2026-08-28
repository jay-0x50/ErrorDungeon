# Data Tables — 적/무기/아이템/강화 데이터 스키마

## 0. 목표

- 게임 데이터를 코드에서 분리 (JSON/CSV/데이터에셋)
- 코덱스가 테이블 구조 그대로 파일/에셋으로 만들 수 있게 스키마 정의

---

## 1. Enemy 스키마(예: enemy_types.json)

```json
{
  "id": "soldier_spear_01",
  "name": "폐성 창병",
  "tier": "normal",
  "base": {
    "hp": 420,
    "attack": 38,
    "defense": 12,
    "poise": 35,
    "stamina": 60
  },
  "scaling": {
    "regionLevel": 1,
    "hpMulPerLevel": 0.18,
    "atkMulPerLevel": 0.14
  },
  "resist": {
    "bleed": -0.1,
    "poison": 0.0,
    "fire": 0.1,
    "lightning": 0.0
  },
  "drops": [
    {"itemId": "mat_iron_shard", "chance": 0.25, "min": 1, "max": 2},
    {"itemId": "currency_souls", "chance": 1.0, "min": 40, "max": 65}
  ],
  "aiProfile": "melee_basic"
}
```

---

## 2. Weapon 스키마(예: weapons.json)

```json
{
  "id": "wp_spear_doyun",
  "name": "도윤의 장창",
  "type": "spear",
  "rarity": "boss",
  "weight": 7.5,
  "baseAtk": 155,
  "guard": {"damageCut": 0.62, "staminaMul": 1.0},
  "scaling": {"str": 0.55, "dex": 0.75},
  "moveset": "spear_longreach",
  "skill": {"id": "sk_lightning_thrust", "cost": 18}
}
```

---

## 3. Item 스키마(예: items.json)

```json
{
  "id": "it_heal_flask",
  "name": "회복 물약",
  "type": "consumable",
  "stackMax": 10,
  "use": {"healHpFlat": 220, "healHpPct": 0.0, "castTimeSec": 1.1},
  "refill": {"atCheckpoint": true, "refillTo": 3}
}
```

---

## 4. Upgrade 스키마(예: upgrades.json)

```json
{
  "weaponId": "wp_spear_doyun",
  "maxLevel": 10,
  "levels": [
    {"level": 1, "costSouls": 300, "materials": [{"itemId": "mat_iron_shard", "count": 2], "atkMul": 1.03},
    {"level": 2, "costSouls": 520, "materials": [{"itemId": "mat_iron_shard", "count": 4], "atkMul": 1.06}
  ]
}
```

---

## 5. 체크리스트(코덱스용)

- 데이터 로더(핫리로드는 선택)
- 밸런스 값은 데이터에서만 수정 가능하게 설계
- 세이브 데이터에는 ‘id + 레벨/수량’만 저장(정적 스펙은 테이블에서 조회)